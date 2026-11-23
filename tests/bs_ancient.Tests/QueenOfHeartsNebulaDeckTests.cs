using System.Reflection;
using System.Reflection.Emit;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BsAncient.Tests;

public sealed class QueenOfHeartsNebulaDeckTests
{
    [Fact]
    public void EachGeneratedRewardReceivesTwoExtraChoices()
    {
        FieldInfo bonus = typeof(QueenOfHeartsNebulaDeckCardRewardCountPatch)
            .GetField("ExtraRewardOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(2, bonus.GetRawConstantValue());
        var instructions = PatchProcessor.GetOriginalInstructions(
            typeof(QueenOfHeartsNebulaDeckCardRewardCountPatch).GetMethod("Prefix")!);
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == nameof(QueenOfHeartsNebulaDeckRelic.TryGetSafeRewardOptionCount));
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Ldc_I4_2);
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Add);
    }

    [NativeRewardFact]
    public void BonusUsesOneNativeRewardInsteadOfDuplicatingAlreadyModifiedCards()
    {
        var instructions = RewardHookInstructions();
        Assert.Single(instructions, instruction => instruction.opcode == OpCodes.Newobj
            && instruction.operand is ConstructorInfo constructor
            && constructor.DeclaringType == typeof(CardReward));
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Ldc_I4_3);
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == "ForRoom");
        Assert.DoesNotContain(instructions, instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(CardFactory));
    }

    [NativeRewardFact]
    public void BonusRequiresTheOwnerCombatAndAnExistingNonemptyCardReward()
    {
        var instructions = RewardHookInstructions();
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == "get_Owner");
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Isinst
            && Equals(instruction.operand, typeof(CombatRoom)));
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == nameof(Enumerable.OfType)
            && method.GetGenericArguments().SequenceEqual([typeof(CardReward)]));
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == nameof(Enumerable.Any));
    }

    [NativeRewardFact]
    public void ExhaustedPoolIsCheckedBeforeAddingTheBonusReward()
    {
        var instructions = RewardHookInstructions();
        int safeCount = instructions.FindIndex(instruction => instruction.operand is MethodInfo method
            && method.Name == nameof(QueenOfHeartsNebulaDeckRelic.TryGetSafeRewardOptionCount));
        int createReward = instructions.FindIndex(instruction => instruction.opcode == OpCodes.Newobj
            && instruction.operand is ConstructorInfo constructor
            && constructor.DeclaringType == typeof(CardReward));
        Assert.True(safeCount >= 0 && createReward > safeCount);
        Assert.Contains(instructions.Take(safeCount), instruction => instruction.operand is MethodInfo method
            && method.Name == nameof(List<Reward>.RemoveAll));
    }

    [NativeRewardFact]
    public void BonusStaysInTheSynchronizedRewardSetWithoutOpeningANestedScreen()
    {
        MethodInfo hook = RewardHook();
        Assert.Null(hook.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>());
        Assert.DoesNotContain(RewardHookInstructions(), instruction => instruction.operand is MethodInfo method
            && (method.Name is "Offer" or "ShowScreen" or "Run" or "SelectUnsynchronized"));
    }

    [Fact]
    public void ForgottenCardsRemainSavedAcrossReloads()
    {
        var property = typeof(QueenOfHeartsNebulaDeckRelic)
            .GetProperty(nameof(QueenOfHeartsNebulaDeckRelic.BlackSouls_ForgottenCardIds))!;
        Assert.NotNull(property.GetCustomAttribute<SavedPropertyAttribute>());
        Assert.True(property.CanRead && property.CanWrite);
    }

    private static MethodInfo RewardHook() => typeof(QueenOfHeartsNebulaDeckRelic)
        .GetMethod(nameof(QueenOfHeartsNebulaDeckRelic.TryModifyRewards))!;

    private static List<CodeInstruction> RewardHookInstructions() =>
        PatchProcessor.GetOriginalInstructions(RewardHook());

    private sealed class NativeRewardFactAttribute : FactAttribute
    {
        public NativeRewardFactAttribute()
        {
#if !BS_BETA_1130
            // The stable test project copies the installed runtime, which may be a different game branch.
            if (!typeof(CardReward).GetConstructors().Any(constructor =>
                constructor.GetParameters() is { Length: 4 } parameters
                && parameters[0].ParameterType == typeof(MegaCrit.Sts2.Core.Runs.CardCreationOptions)))
            {
                Skip = "Requires a matching 0.111 game runtime; run the Beta113 suite on game 0.113.";
            }
#endif
        }
    }
}

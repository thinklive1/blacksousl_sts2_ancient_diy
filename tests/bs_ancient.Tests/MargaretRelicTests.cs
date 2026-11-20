using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BlackSouls.Scripts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rewards;

namespace BsAncient.Tests;

public sealed class MargaretRelicTests
{
    [Theory]
    [InlineData(0, 200, CardRarity.Uncommon, 1)]
    [InlineData(12, 200, CardRarity.Uncommon, 1)]
    [InlineData(13, 300, CardRarity.Rare, 1)]
    [InlineData(18, 300, CardRarity.Rare, 1)]
    [InlineData(19, 500, CardRarity.Rare, 2)]
    [InlineData(27, 500, CardRarity.Rare, 2)]
    public void SupplyScoreKeepsTheExistingRewardTiers(int score, int gold, CardRarity rarity, int cardRewards)
    {
        Assert.Equal((gold, rarity, cardRewards), MargaretRelic.GetSupplyRewardPlan(score));
    }

    [Fact]
    public void SupplyCallbackCannotOpenOrWaitForANestedRewardsScreen()
    {
        MethodInfo supply = GetMethod("Supply");
        Assert.Null(supply.GetCustomAttribute<AsyncStateMachineAttribute>());
        var instructions = PatchProcessor.GetOriginalInstructions(supply);
        Assert.DoesNotContain(instructions, instruction => instruction.operand is MethodInfo method
            && (method.Name == "GrantSupplyRewards" || method.DeclaringType == typeof(RewardsCmd)
                || method.DeclaringType == typeof(PlayerCmd)));
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Stfld
            && instruction.operand is FieldInfo field && field.Name == "_pendingSupplyReward");
    }

    [Fact]
    public void CompletedSourceRewardConsumesTheTriggerBeforeGrantingBonusRewards()
    {
        MethodInfo hook = typeof(MargaretRelic).GetMethod(nameof(MargaretRelic.AfterRewardTaken))!;
        Type stateMachine = hook.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        MethodInfo body = stateMachine.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var instructions = PatchProcessor.GetOriginalInstructions(body).ToList();
        int clear = instructions.FindIndex(instruction => instruction.opcode == OpCodes.Stfld
            && instruction.operand is FieldInfo field && field.Name == "_pendingSupplyReward");
        int grant = instructions.FindIndex(instruction => instruction.operand is MethodInfo method
            && method.Name == "GrantSupplyRewards");
        Assert.True(clear >= 0 && grant > clear);
        Assert.Equal(OpCodes.Ldnull, instructions[clear - 1].opcode);
        Assert.Contains(instructions.Take(clear), instruction => instruction.opcode == OpCodes.Ceq
            || instruction.operand is MethodInfo method && method.Name == nameof(ReferenceEquals));
        Assert.Contains(instructions.Take(clear), instruction => instruction.opcode == OpCodes.Ldfld
            && instruction.operand is FieldInfo field && field.Name == "reward");
        Assert.Contains(instructions.Take(clear), instruction => instruction.operand is MethodInfo method
            && method.Name == "get_Owner");
    }

    [Fact]
    public void ClonedRelicsCannotShareAnInFlightRewardTrigger()
    {
        var instructions = PatchProcessor.GetOriginalInstructions(GetMethod("DeepCloneFields")).ToList();
        int clear = instructions.FindIndex(instruction => instruction.opcode == OpCodes.Stfld
            && instruction.operand is FieldInfo field && field.Name == "_pendingSupplyReward");
        Assert.True(clear > 0);
        Assert.Equal(OpCodes.Ldnull, instructions[clear - 1].opcode);
        Assert.Null(typeof(MargaretRelic).GetField("_pendingSupplyReward", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetCustomAttribute<MegaCrit.Sts2.Core.Saves.Runs.SavedPropertyAttribute>());
    }

    private static MethodInfo GetMethod(string name) => typeof(MargaretRelic)
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
}

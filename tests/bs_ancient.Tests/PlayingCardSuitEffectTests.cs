using System.Reflection;
using System.Runtime.CompilerServices;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BsAncient.Tests;

public sealed class PlayingCardSuitEffectTests
{
    [Theory]
    [InlineData(typeof(HeartSuitEnchantment), 2)]
    [InlineData(typeof(DiamondSuitEnchantment), 10)]
    [InlineData(typeof(ClubSuitEnchantment), 4)]
    [InlineData(typeof(SpadeSuitEnchantment), 6)]
    public void OrdinaryAndStandaloneSuitPathsUseTheSameDoubledAmount(Type suitType, int amount)
    {
        MethodInfo ordinary = suitType.GetMethod("TriggerSuitEffect",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo standalone = typeof(PlayingCardSuitEnchantment).GetMethod("TriggerStandaloneSuitEffect",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Contains(PatchProcessor.GetOriginalInstructions(ordinary), instruction => instruction.LoadsConstant(amount));
        Assert.Contains(PatchProcessor.GetOriginalInstructions(standalone), instruction => instruction.LoadsConstant(amount));
    }

    [Fact]
    public void LastAceStillHasRetainAndExhaustAndCannotBeUpgraded()
    {
        var card = (LastAceCard)RuntimeHelpers.GetUninitializedObject(typeof(LastAceCard));
        Assert.Contains(CardKeyword.Retain, card.CanonicalKeywords);
        Assert.Contains(CardKeyword.Exhaust, card.CanonicalKeywords);
        Assert.Equal(0, card.MaxUpgradeLevel);
    }

    [Fact]
    public void LastAceStillTriggersAllSuitsTwiceWhenPlayedAlone()
    {
        MethodInfo onPlay = typeof(LastAceCard).GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Type stateMachine = onPlay.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        MethodInfo body = stateMachine.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var instructions = PatchProcessor.GetOriginalInstructions(body);
        int trigger = instructions.FindIndex(instruction => instruction.operand is MethodInfo method
            && method.Name == "TriggerAllSuitEffects");
        Assert.True(trigger > 0);
        Assert.True(instructions[trigger - 1].LoadsConstant(2));
    }
}

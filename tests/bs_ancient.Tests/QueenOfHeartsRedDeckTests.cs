using System.Reflection;
using System.Runtime.CompilerServices;
using BlackSouls.Scripts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;

namespace BsAncient.Tests;

public sealed class QueenOfHeartsRedDeckTests
{
    [Fact]
    public void DiscardRemainsRegisteredAsASynchronizedRitsuAction()
    {
        MethodInfo registration = typeof(Entry).GetMethod("RegisterQueenOfHeartsRedDeckRightClick",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.Contains(PatchProcessor.GetOriginalInstructions(registration), instruction =>
            instruction.operand is MethodInfo method && method.DeclaringType == typeof(ModRightClickRegistry)
            && method.Name == nameof(ModRightClickRegistry.Register));
    }

    [Fact]
    public void ExecutionRequiresTheOwnersQueueContextWithoutConstructingAFallback()
    {
        MethodInfo execute = GetExecutionBody();
        var instructions = PatchProcessor.GetOriginalInstructions(execute);
        Assert.Contains(instructions, instruction => instruction.operand is Type type
            && type == typeof(GameActionPlayerChoiceContext));
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == "get_OwnerId");
        Assert.DoesNotContain(instructions, instruction => instruction.operand is ConstructorInfo constructor
            && typeof(PlayerChoiceContext).IsAssignableFrom(constructor.DeclaringType));
    }

    [Fact]
    public void DiscardUsesTheNativeAwaitableDiscardAndDrawCommand()
    {
        var instructions = PatchProcessor.GetOriginalInstructions(GetExecutionBody());
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(CardCmd) && method.Name == nameof(CardCmd.DiscardAndDraw));
        Assert.DoesNotContain(instructions, instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(Task) && method.Name == nameof(Task.Run));
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.Name == "CanDiscardAndDraw");
    }

    [Fact]
    public void ExecutionGuardChecksThePlayersPhaseAndTheCardsCombatState()
    {
        MethodInfo guard = typeof(QueenOfHeartsRedDeckRelic).GetMethod("CanDiscardAndDraw",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var instructions = PatchProcessor.GetOriginalInstructions(guard);
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(PlayerCombatState) && method.Name == "get_Phase");
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(CardModel) && method.Name == "get_CombatState");
        Assert.Contains(instructions, instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(CardModel) && method.Name == "get_IsInCombat");
        Assert.Equal(3, QueenOfHeartsRedDeckRelic.DiscardsPerCombat);
    }

    private static MethodInfo GetExecutionBody()
    {
        // Inspect integration without constructing Godot-native models; this is not a network simulation.
        MethodInfo method = typeof(QueenOfHeartsRedDeckRelic).GetMethod("TryDiscardAndDraw",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Type stateMachine = method.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        return stateMachine.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
}

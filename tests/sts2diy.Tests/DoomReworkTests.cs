using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class DoomReworkTests
{
    private static readonly Type Rules = typeof(Entry).Assembly.GetType("Sts2Diy.Scripts.DoomRework", true)!;
    private static readonly Type Patches = typeof(Entry).Assembly.GetType("Sts2Diy.Scripts.Patches.VanillaDoomPatches", true)!;

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(2, 3, 0)]
    [InlineData(3, 4, 1)]
    [InlineData(11, 16, 3)]
    [InlineData(13, 19, 4)]
    [InlineData(15, 22, 5)]
    [InlineData(16, 24, 5)]
    [InlineData(21, 31, 7)]
    [InlineData(29, 43, 9)]
    public void AmountsRoundDownAndBacklashUsesTheOriginal(int amount, int scaled, int backlash)
    {
        Assert.Equal((decimal)scaled, InvokeRule("Scale", amount));
        Assert.Equal((decimal)backlash, InvokeRule("Backlash", amount));
    }

    [Fact]
    public void EveryTargetExistsAndEveryReplacementHasTheOriginalCallSignature()
    {
        Type patch = Patches.GetNestedType("DoomProducerPatch", BindingFlags.NonPublic)!;
        MethodInfo targetsMethod = patch.GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo rewrite = patch.GetMethod("Transpiler", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodBase[] targets = ((IEnumerable<MethodBase>)targetsMethod.Invoke(null, null)!).ToArray();
        Assert.True(targets.Length >= 9);
        foreach (MethodBase target in targets)
        {
            var original = PatchProcessor.GetOriginalInstructions(target).ToArray();
            var expectedCalls = original.Where(IsDoomCall).Select(i => (MethodInfo)i.operand).ToArray();
            var result = ((IEnumerable<CodeInstruction>)rewrite.Invoke(null, [original])!).ToArray();
            Assert.DoesNotContain(result, IsDoomCall);
            var replacements = result.Where(i => i.operand is MethodInfo m && m.DeclaringType == Rules)
                .Select(i => (MethodInfo)i.operand).ToArray();
            Assert.Equal(expectedCalls.Length, replacements.Length);
            for (int i = 0; i < replacements.Length; i++)
            {
                Assert.Equal(expectedCalls[i].ReturnType, replacements[i].ReturnType);
                Assert.Equal(expectedCalls[i].GetParameters().Select(p => p.ParameterType),
                    replacements[i].GetParameters().Select(p => p.ParameterType));
            }
        }
    }

    [Fact]
    public void AllVanillaCardsWithDirectDoomCallsAreCovered()
    {
        Type[] covered = (Type[])Patches.GetField("DirectCards", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        List<string> uncovered = [];
        foreach (Type card in typeof(CardModel).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(CardModel))))
        {
            MethodInfo? play = card.GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Type? machine = play?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
            MethodInfo? body = machine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
            if (body == null || !PatchProcessor.GetOriginalInstructions(body).Any(IsDoomCall)) continue;
            if (card != typeof(NegativePulse) && !covered.Contains(card)) uncovered.Add(card.FullName!);
        }
        Assert.True(uncovered.Count == 0, $"Uncovered vanilla Doom cards: {string.Join(", ", uncovered)}");
    }

    [Fact]
    public void AllVanillaPowersWithDoomCallsAreAccountedFor()
    {
        Type[] covered = (Type[])Patches.GetField("DelayedPowers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        List<string> uncovered = [];
        foreach (Type power in typeof(PowerModel).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(PowerModel))))
        {
            foreach (MethodInfo method in power.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                Type? machine = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
                MethodInfo? body = machine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
                if (body == null || !PatchProcessor.GetOriginalInstructions(body).Any(IsDoomCall)) continue;
                if (!covered.Contains(power)) uncovered.Add(power.FullName!);
            }
        }
        Assert.True(uncovered.Count == 0, $"Uncovered vanilla Doom powers: {string.Join(", ", uncovered.Distinct())}");
    }

    [Fact]
    public void HarmonyCanInstallAndRemoveAllPatchesWithoutStartingTheGame()
    {
        Harmony harmony = new("sts2diy.tests.doom");
        try
        {
            harmony.PatchAll(typeof(Entry).Assembly);
            Assert.NotEmpty(harmony.GetPatchedMethods());
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
    }

    [Fact]
    public void BacklashUsesUnpatchedNativePowerCommand()
    {
        MethodInfo apply = Rules.GetMethod("ApplyBacklash", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Contains(PatchProcessor.GetOriginalInstructions(apply), IsDoomCall);
        Assert.DoesNotContain(PatchProcessor.GetOriginalInstructions(apply), i =>
            i.operand is MethodInfo m && m.DeclaringType == Rules && m.Name.StartsWith("Apply"));
    }

    private static decimal InvokeRule(string name, int amount) =>
        (decimal)Rules.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [(decimal)amount])!;

    private static bool IsDoomCall(CodeInstruction instruction) =>
        instruction.operand is MethodInfo m && m.DeclaringType == typeof(PowerCmd) && m.Name == "Apply"
        && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual([typeof(DoomPower)]);
}

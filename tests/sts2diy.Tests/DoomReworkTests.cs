using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

    [Theory]
    [InlineData(10, 10, 3)]
    [InlineData(10, 60, 3)]
    [InlineData(10, 110, 3)]
    [InlineData(15, 15, 5)]
    [InlineData(15, 65, 5)]
    [InlineData(15, 115, 5)]
    public void NoEscapeBacklashAndPreviewUseOnlyTheBaseDoom(int baseDoom, int calculatedDoom, int expected)
    {
        var card = (NoEscape)RuntimeHelpers.GetUninitializedObject(typeof(NoEscape));
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card,
            new DynamicVarSet([new CalculationBaseVar(baseDoom)]));
        MethodInfo rule = Rules.GetMethod("BacklashForCard", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal((decimal)expected, rule.Invoke(null, [(decimal)calculatedDoom, card]));
        Assert.Equal(Math.Floor(calculatedDoom * 1.5m), InvokeRule("Scale", calculatedDoom));

        var description = new LocString("cards", "NO_ESCAPE.description");
        Type patch = Patches.GetNestedType("DoomDescriptionPatch", BindingFlags.NonPublic)!;
        patch.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [card, description]);
        Assert.Equal((decimal)expected, description.Variables["DiySelfDoom"]);
        Assert.Contains(PatchProcessor.GetOriginalInstructions(
            Rules.GetMethod("ApplyBacklash", BindingFlags.Static | BindingFlags.NonPublic)!),
            i => Equals(i.operand, rule));
    }

    [Fact]
    public void OtherCardsStillUseTheirOriginalAppliedAmountForBacklash()
    {
        MethodInfo rule = Rules.GetMethod("BacklashForCard", BindingFlags.Static | BindingFlags.NonPublic)!;
        var card = (Scourge)RuntimeHelpers.GetUninitializedObject(typeof(Scourge));
        Assert.Equal(20m, rule.Invoke(null, [60m, card]));
        Assert.Equal(20m, rule.Invoke(null, [60m, null]));
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void NoEscapeDescriptionShowsTheBaseBacklashInsteadOfAThirdOfTheResult(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        string text = json.RootElement.GetProperty("NO_ESCAPE.description").GetString()!;
        Assert.Contains("{DiySelfDoom}", text);
        Assert.Contains("{CalculationBase:diff()}", text);
        Assert.DoesNotContain("结果三分之一", text);
        Assert.DoesNotContain("one third", text);
    }

    [Fact]
    public void EveryTargetExistsAndEveryReplacementHasTheOriginalCallSignature()
    {
        Type patch = Patches.GetNestedType("DoomProducerPatch", BindingFlags.NonPublic)!;
        MethodInfo targetsMethod = patch.GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo rewrite = patch.GetMethod("Transpiler", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodBase[] targets = ((IEnumerable<MethodBase>)targetsMethod.Invoke(null, null)!).ToArray();
        Assert.True(targets.Length >= 7);
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
            // These two cards replace OnPlay entirely rather than retaining vanilla Doom application.
            if (card != typeof(NegativePulse) && card != typeof(BlightStrike) && card != typeof(Death)
                && !covered.Contains(card))
                uncovered.Add(card.FullName!);
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
                if (power != typeof(NeurosurgePower) && power != typeof(UnderworldPower)
                    && !covered.Contains(power)) uncovered.Add(power.FullName!);
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

    [Fact]
    public void ExemptCardsAndTheirOngoingPowersAreNotPatched()
    {
        Type[] direct = (Type[])Patches.GetField("DirectCards", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Type[] delayed = (Type[])Patches.GetField("DelayedPowers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.DoesNotContain(typeof(Death), direct);
        Assert.DoesNotContain(typeof(NeurosurgePower), delayed);
        Assert.DoesNotContain(typeof(UnderworldPower), delayed);
        Type patch = Patches.GetNestedType("DoomProducerPatch", BindingFlags.NonPublic)!;
        var targets = (IEnumerable<MethodBase>)AccessTools.Method(patch, "TargetMethods").Invoke(null, null)!;
        foreach (MethodBase target in targets)
        {
            Type? source = target.DeclaringType?.DeclaringType;
            Assert.DoesNotContain(source, new[] { typeof(Death), typeof(NeurosurgePower), typeof(UnderworldPower) });
        }
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void ExemptCardsAndPowersUseNativeDescriptions(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string folder = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "cards.json")));
        using var powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "powers.json")));
        foreach (string id in new[] { "DEATH", "NEUROSURGE", "UNDERWORLD" })
            Assert.False(cards.RootElement.TryGetProperty(id + ".description", out _));
        foreach (string id in new[] { "NEUROSURGE_POWER", "UNDERWORLD_POWER" })
            Assert.DoesNotContain(powers.RootElement.EnumerateObject(), property => property.Name.StartsWith(id + "."));
        string reaper = cards.RootElement.GetProperty("REAPER_FORM.description").GetString()!;
        Assert.Contains(language == "zhs" ? "不会因" : "cannot die", reaper);
    }

    private static decimal InvokeRule(string name, int amount) =>
        (decimal)Rules.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [(decimal)amount])!;

    private static bool IsDoomCall(CodeInstruction instruction) =>
        instruction.operand is MethodInfo m && m.DeclaringType == typeof(PowerCmd) && m.Name == "Apply"
        && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual([typeof(DoomPower)]);
}

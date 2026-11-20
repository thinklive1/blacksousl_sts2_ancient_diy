using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class PowerRegressionTests
{
    [Theory]
    [InlineData(0, 1, 1, 0)]
    [InlineData(1, 1, 2, 0)]
    [InlineData(2, 1, 3, 0)]
    [InlineData(3, 1, 0, 1)]
    [InlineData(3, 2, 0, 2)]
    [InlineData(3, 3, 0, 3)]
    public void RaimentTracksProgressAndAwardsEnergyForEveryStack(int progress, int stacks, int next, int energy)
    {
        var result = ((int Progress, int Energy))Method(typeof(CalamityRaimentPower), "AdvanceProgress")
            .Invoke(null, [progress, stacks])!;
        Assert.Equal((next, energy), result);
    }

    [Fact]
    public void AddingAnotherRaimentDoesNotDiscardExistingProgress()
    {
        int progress = 0;
        int energy = 0;
        int[] stackCounts = [1, 1, 2, 2, 2, 2, 2, 2];
        foreach (int stacks in stackCounts)
        {
            var result = ((int Progress, int Energy))Method(typeof(CalamityRaimentPower), "AdvanceProgress")
                .Invoke(null, [progress, stacks])!;
            progress = result.Progress;
            energy += result.Energy;
        }
        Assert.Equal(0, progress);
        Assert.Equal(4, energy);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(4, 8)]
    [InlineData(6, 12)]
    public void PactBlockScalesWithSummonAmount(int summon, int block)
    {
        Assert.Equal((decimal)block, Method(typeof(DeathLifePactPower), "GetOstyBlock").Invoke(null, [summon]));
    }

    [Theory]
    [InlineData(typeof(DeathLifePactPower), "Remove")]
    [InlineData(typeof(BitterHatredPower), "set_Sts2Diy_Active")]
    public void EndOfTurnEffectsRequireTheOwnerToBeAParticipant(Type power, string effect)
    {
        MethodInfo hook = power.GetMethod("AfterSideTurnEnd")!;
        MethodInfo body = AsyncBody(hook);
        var instructions = PatchProcessor.GetOriginalInstructions(body).ToList();
        int contains = instructions.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(Enumerable)
            && m.Name == nameof(Enumerable.Contains) && m.IsGenericMethod
            && m.GetGenericArguments().SequenceEqual([typeof(Creature)]));
        int action = instructions.FindIndex(i => i.operand is MethodInfo m && m.Name == effect);
        Assert.True(contains >= 0 && action > contains);
        Assert.Contains(instructions.Take(contains), i =>
            i.operand is FieldInfo f && f.Name == "participants" || i.opcode == OpCodes.Ldarg_3);
        Assert.Contains(instructions.Skip(contains + 1).Take(action - contains - 1), i =>
            i.opcode.FlowControl == FlowControl.Cond_Branch);
    }

    [Fact]
    public void CallbacksUseTheStackCalculationsAndCommitProgressBeforeGrantingEnergy()
    {
        var raiment = PatchProcessor.GetOriginalInstructions(AsyncBody(typeof(CalamityRaimentPower)
            .GetMethod(nameof(CalamityRaimentPower.AfterPowerAmountChanged))!)).ToList();
        int advance = raiment.FindIndex(i => i.operand is MethodInfo m && m.Name == "AdvanceProgress");
        int commit = raiment.FindIndex(i => i.operand is MethodInfo m && m.Name == "set_Sts2Diy_DoomApplications");
        int grant = raiment.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(PlayerCmd)
            && m.Name == nameof(PlayerCmd.GainEnergy));
        Assert.True(advance >= 0 && commit > advance && grant > commit);
        var pact = PatchProcessor.GetOriginalInstructions(AsyncBody(typeof(DeathLifePactPower)
            .GetMethod(nameof(DeathLifePactPower.AfterDamageGiven))!));
        Assert.Contains(pact, i => i.operand is MethodInfo m && m.Name == "GetOstyBlock");
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void BothNormalAndCanonicalTooltipsHaveAllTheirVariables(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "powers.json")));
        foreach (var entry in new[]
        {
            (typeof(CalamityRaimentPower), "STS2DIY_POWER_CALAMITY_RAIMENT_POWER", new object?[] { 3, 2 }),
            (typeof(DeathLifePactPower), "STS2DIY_POWER_DEATH_LIFE_PACT_POWER", new object?[] { 4 })
        })
        {
            LocString description = new("powers", entry.Item2 + ".description");
            Method(entry.Item1, "AddDescriptionVariables").Invoke(null, new object?[] { description }.Concat(entry.Item3).ToArray());
            string text = json.RootElement.GetProperty(entry.Item2 + ".description").GetString()!;
            foreach (Match selector in Regex.Matches(text, @"\{(\w+)"))
                Assert.True(description.Variables.ContainsKey(selector.Groups[1].Value), selector.Value);
            Assert.False(json.RootElement.TryGetProperty(entry.Item2 + ".smartDescription", out _));
            var getter = PatchProcessor.GetOriginalInstructions(entry.Item1.GetProperty("Description")!.GetMethod!);
            Assert.Contains(getter, i => i.operand is MethodInfo m && m.Name == "AddDescriptionVariables");
        }
        LocString progress = new("powers", "test");
        Method(typeof(CalamityRaimentPower), "AddDescriptionVariables").Invoke(null, [progress, 3, 2]);
        Assert.Equal(3m, Convert.ToDecimal(progress.Variables["DoomApplications"]));
        Assert.Equal(2m, Convert.ToDecimal(progress.Variables["EnergyPerTrigger"]));
        LocString pact = new("powers", "test");
        Method(typeof(DeathLifePactPower), "AddDescriptionVariables").Invoke(null, [pact, 4]);
        Assert.Equal(4m, Convert.ToDecimal(pact.Variables["SummonAmount"]));
        Assert.Equal(8m, Convert.ToDecimal(pact.Variables["BlockAmount"]));
    }

    [Fact]
    public void RaimentReusesBothNativeShroudIcons()
    {
        MethodInfo getter = typeof(CalamityRaimentPower).GetProperty("AssetProfile")!.GetMethod!;
        var instructions = PatchProcessor.GetOriginalInstructions(getter);
        Assert.Contains(instructions, i => i.operand is MethodInfo m && m.DeclaringType == typeof(ModelDb)
            && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual([typeof(ShroudPower)]));
        Assert.Contains(instructions, i => i.operand is MethodInfo m && m.Name == "get_IconPath");
        Assert.Contains(instructions, i => i.operand is MethodInfo m && m.Name == "get_ResolvedBigIconPath");
        Assert.DoesNotContain(instructions, i => i.operand is string path && path.StartsWith("res://sts2diy/"));
    }

    [Fact]
    public void ExistingSaveFieldNamesRemainAvailable()
    {
        Assert.NotNull(typeof(CalamityRaimentPower).GetProperty("Sts2Diy_DoomApplications")!.GetCustomAttribute<SavedPropertyAttribute>());
        Assert.NotNull(typeof(BitterHatredPower).GetProperty("Sts2Diy_Pending")!.GetCustomAttribute<SavedPropertyAttribute>());
        Assert.NotNull(typeof(BitterHatredPower).GetProperty("Sts2Diy_Active")!.GetCustomAttribute<SavedPropertyAttribute>());
    }

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static MethodInfo AsyncBody(MethodInfo method)
    {
        Type? stateMachine = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        return stateMachine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)! ?? method;
    }
}

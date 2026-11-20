using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Patches;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class CardAdjustmentTests
{
    [Fact]
    public void ShroudIsUncommonWithoutChangingOtherCardsRarity()
    {
        MethodInfo prefix = PatchMethod("ShroudRarityPatch", "Prefix");
        object?[] shroud = [RuntimeHelpers.GetUninitializedObject(typeof(Shroud)), CardRarity.Rare];
        Assert.Equal(false, prefix.Invoke(null, shroud));
        Assert.Equal(CardRarity.Uncommon, shroud[1]);

        object?[] other = [RuntimeHelpers.GetUninitializedObject(typeof(NegativePulse)), CardRarity.Common];
        Assert.Equal(true, prefix.Invoke(null, other));
        Assert.Equal(CardRarity.Common, other[1]);
    }

    [Fact]
    public void ShroudUpgradeIncreasesTheEnergyRewardWithoutChangingCostOrProgress()
    {
        DynamicVar original = new("Unrelated", 123m);
        object?[] arguments = [new DynamicVar[] { original }];
        PatchMethod("ShroudBaseValuesPatch", "Postfix").Invoke(null, arguments);
        DynamicVar[] vars = ((IEnumerable<DynamicVar>)arguments[0]!).ToArray();
        Assert.Same(original, vars[0]);
        DynamicVar energy = Assert.Single(vars, v => v.Name == "RaimentEnergy");
        Assert.Equal(1m, energy.BaseValue);
        energy.UpgradeValueBy(1m);
        Assert.Equal(2m, energy.BaseValue);

        var upgrade = PatchProcessor.GetOriginalInstructions(PatchMethod("ShroudUpgradePatch", "Prefix"));
        Assert.Contains(upgrade, i => Equals(i.operand, "RaimentEnergy"));
        Assert.Single(upgrade, i => i.operand is MethodInfo m && m.Name == nameof(DynamicVar.UpgradeValueBy));
        Assert.DoesNotContain(upgrade, i => i.operand is MethodInfo m && m.Name == "get_EnergyCost");
        var play = PatchProcessor.GetOriginalInstructions(PatchMethod("ShroudPlayPatch", "Prefix"));
        Assert.Contains(play, i => Equals(i.operand, "RaimentEnergy"));
        Assert.Contains(play, i => i.operand is MethodInfo m && m.Name == "get_BaseValue");
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void ShroudDescriptionPreviewsTheUpgradeableEnergyReward(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        Assert.Contains("{RaimentEnergy:diff()}", json.RootElement.GetProperty("SHROUD.description").GetString());
    }

    [Fact]
    public void NegativePulseUsesTheSameBaseValuesForPreviewAndPlay()
    {
        DynamicVar extra = new("Unrelated", 123m);
        object?[] arguments = [new DynamicVar[]
        {
            new BlockVar(5m, ValueProp.Move), new PowerVar<DoomPower>(7m), extra
        }];
        PatchMethod("NegativePulseBaseValuesPatch", "Postfix").Invoke(null, arguments);
        DynamicVar[] vars = ((IEnumerable<DynamicVar>)arguments[0]!).ToArray();
        Assert.Equal(7m, Assert.Single(vars.OfType<BlockVar>()).BaseValue);
        Assert.Equal(11m, Assert.Single(vars.OfType<PowerVar<DoomPower>>()).BaseValue);
        Assert.Same(extra, vars[2]);
        Assert.Equal(123m, extra.BaseValue);

        // Vanilla's upgrade and the patch together produce 9 Block and 15 original Doom.
        vars[0].UpgradeValueBy(1m);
        vars[1].UpgradeValueBy(4m);
        vars[0].UpgradeValueBy(1m);
        Assert.Equal(9m, vars[0].BaseValue);
        Assert.Equal(15m, vars[1].BaseValue);
        var patch = PatchProcessor.GetOriginalInstructions(PatchMethod("NegativePulseUpgradePatch", "Postfix"));
        Assert.Single(patch, i => i.operand is MethodInfo m && m.Name == nameof(DynamicVar.UpgradeValueBy));
        Assert.DoesNotContain(patch, i => i.operand is MethodInfo m && m.Name == "get_Doom");
    }

    [Fact]
    public void NegativePulseTargetsOnlyEnemiesAndAppliesBacklashOnceThroughTheAreaHelper()
    {
        Type patch = typeof(VanillaCardPatches).GetNestedType("NegativePulsePlayPatch", BindingFlags.NonPublic)!;
        MethodInfo play = patch.GetMethod("ApplyToAllEnemies", BindingFlags.Static | BindingFlags.NonPublic)!;
        Type machine = play.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        var body = PatchProcessor.GetOriginalInstructions(machine.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance)!);
        Assert.Contains(body, i => i.operand is MethodInfo m && m.Name == "get_HittableEnemies");
        Assert.DoesNotContain(body, i => i.operand is MethodInfo m && m.Name is "get_Players" or "get_Allies" or "get_Osty");
        Assert.Single(body, i => i.operand is MethodInfo m && m.DeclaringType?.Name == "DoomRework" && m.Name == "ApplyMultiple");
        Assert.DoesNotContain(body, i => i.operand is MethodInfo m && m.DeclaringType?.Name == "DoomRework" && m.Name == "ApplyBacklash");
    }

    [Theory]
    [InlineData("zhs", "所有敌人")]
    [InlineData("eng", "ALL enemies")]
    public void NegativePulseDescriptionTargetsEnemiesOnly(string language, string target)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        string text = json.RootElement.GetProperty("NEGATIVE_PULSE.description").GetString()!;
        Assert.Contains(target, text);
        Assert.DoesNotContain("所有生物", text);
        Assert.DoesNotContain("ALL creatures", text);
    }

    [Fact]
    public void SharedDoomRetainsWithoutExhaustAndOnlyUpgradedPlaysEnableProtection()
    {
        MethodInfo keywords = typeof(SharedDoomCard).GetProperty(nameof(SharedDoomCard.CanonicalKeywords))!.GetMethod!;
        var initial = PatchProcessor.GetOriginalInstructions(keywords);
        Assert.Contains(initial, i => LoadsInteger(i, (int)CardKeyword.Retain));
        Assert.DoesNotContain(initial, i => LoadsInteger(i, (int)CardKeyword.Exhaust));
        Assert.Null(typeof(SharedDoomCard).GetMethod("OnUpgrade",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        MethodInfo play = typeof(SharedDoomCard).GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Type machine = play.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        var body = PatchProcessor.GetOriginalInstructions(machine.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!).ToList();
        int upgraded = body.FindIndex(i => i.operand is MethodInfo m && m.Name == "get_IsUpgraded");
        int protect = body.FindIndex(i => i.operand is MethodInfo m && m.Name == "set_Sts2Diy_DoomProtection");
        int doubleDoom = body.FindIndex(i => i.operand is MethodInfo m && m.Name == "Apply" && m.IsGenericMethod
            && m.GetGenericArguments().SequenceEqual([typeof(DoomPower)]));
        Assert.True(upgraded >= 0 && protect > upgraded && doubleDoom > protect);
        Assert.Contains(body.Skip(upgraded + 1).Take(protect - upgraded - 1), i => i.opcode.FlowControl == FlowControl.Cond_Branch);
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void BitterHatredUsesTheUpgradeableVariableInBothLanguages(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        string text = json.RootElement.GetProperty("STS2DIY_CARD_BITTER_HATRED_CARD.description").GetString()!;
        Assert.Contains("{BitterHatredPower:diff()}", text);
        Assert.DoesNotContain("50%", text);
    }

    private static MethodInfo PatchMethod(string type, string method) => typeof(VanillaCardPatches)
        .GetNestedType(type, BindingFlags.NonPublic)!.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static bool LoadsInteger(CodeInstruction instruction, int value)
    {
        if (instruction.opcode == OpCodes.Ldc_I4) return (int)instruction.operand == value;
        if (instruction.opcode == OpCodes.Ldc_I4_S) return Convert.ToInt32(instruction.operand) == value;
        return value switch
        {
            0 => instruction.opcode == OpCodes.Ldc_I4_0,
            1 => instruction.opcode == OpCodes.Ldc_I4_1,
            2 => instruction.opcode == OpCodes.Ldc_I4_2,
            3 => instruction.opcode == OpCodes.Ldc_I4_3,
            4 => instruction.opcode == OpCodes.Ldc_I4_4,
            5 => instruction.opcode == OpCodes.Ldc_I4_5,
            6 => instruction.opcode == OpCodes.Ldc_I4_6,
            7 => instruction.opcode == OpCodes.Ldc_I4_7,
            8 => instruction.opcode == OpCodes.Ldc_I4_8,
            _ => false
        };
    }
}

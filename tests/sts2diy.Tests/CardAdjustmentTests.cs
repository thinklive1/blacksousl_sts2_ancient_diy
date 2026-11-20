using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Patches;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class CardAdjustmentTests
{
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
    public void SharedDoomStartsWithRetainAndExhaustAndOnlyRemovesExhaustOnUpgrade()
    {
        MethodInfo keywords = typeof(SharedDoomCard).GetProperty(nameof(SharedDoomCard.CanonicalKeywords))!.GetMethod!;
        var initial = PatchProcessor.GetOriginalInstructions(keywords);
        Assert.Contains(initial, i => LoadsInteger(i, (int)CardKeyword.Retain));
        Assert.Contains(initial, i => LoadsInteger(i, (int)CardKeyword.Exhaust));
        MethodInfo upgrade = typeof(SharedDoomCard).GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var upgraded = PatchProcessor.GetOriginalInstructions(upgrade);
        Assert.Contains(upgraded, i => LoadsInteger(i, (int)CardKeyword.Exhaust));
        Assert.Contains(upgraded, i => i.operand is MethodInfo m && m.Name == "RemoveKeyword");
        Assert.DoesNotContain(upgraded, i => i.operand is MethodInfo m && m.Name == "UpgradeBy");
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

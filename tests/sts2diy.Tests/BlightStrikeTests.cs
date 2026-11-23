using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class BlightStrikeTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.blight_strike");
    private static bool _inCombat;
    private static readonly Type Patches = typeof(Entry).Assembly.GetType("Sts2Diy.Scripts.Patches.BlightStrikePatches", true)!;

    public BlightStrikeTests()
    {
        _inCombat = true;
        _harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(BlightStrikeTests), nameof(InCombat)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(false, 0, 9)]
    [InlineData(true, 0, 12)]
    [InlineData(false, 1, 9)]
    [InlineData(true, 1, 12)]
    [InlineData(false, 2, 10)]
    [InlineData(false, 7, 12)]
    [InlineData(true, 7, 15)]
    [InlineData(false, 8, 13)]
    [InlineData(true, 8, 16)]
    [InlineData(false, 35, 26)]
    [InlineData(true, 35, 29)]
    public void NativePreviewAndDamageCalculationUseBasePlusHalfOwnersDoomRoundedDown(bool upgraded, int doom, int expected)
    {
        var owner = CreateCard(doom);
        var other = CreateCard(99);
        if (upgraded) Assert.Equal(false, Patch("UpgradePatch", "Prefix").Invoke(null, [owner.Card]));
        CalculatedDamageVar damage = owner.Card.DynamicVars.CalculatedDamage;
        Assert.Equal((decimal)expected, damage.Calculate(null));
        damage.UpdateCardPreview(owner.Card, CardPreviewMode.None, null, runGlobalHooks: false);
        Assert.Equal((decimal)expected, damage.PreviewValue);
        Assert.Equal(doom, owner.Doom.Amount);
        Assert.Equal(99, other.Doom.Amount);
        Assert.Equal(1m, owner.Card.DynamicVars.ExtraDamage.BaseValue);
    }

    [Fact]
    public void DoomChangesAreReadLiveAndNonCombatPreviewsOnlyShowBaseDamage()
    {
        var owner = CreateCard(5);
        Assert.Equal(11m, owner.Card.DynamicVars.CalculatedDamage.Calculate(null));
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(owner.Doom, 17);
        Assert.Equal(17m, owner.Card.DynamicVars.CalculatedDamage.Calculate(null));
        _inCombat = false;
        Assert.Equal(9m, owner.Card.DynamicVars.CalculatedDamage.Calculate(null));
    }

    [Fact]
    public void ReplacementUsesOneNativeCalculatedAttackAndHasNoDoomApplication()
    {
        MethodInfo play = Patch("PlayPatch", "Play");
        Type machine = play.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        var body = PatchProcessor.GetOriginalInstructions(AccessTools.Method(machine, "MoveNext")).ToArray();
        MethodInfo attack = (MethodInfo)Assert.Single(body, i => i.operand is MethodInfo m
            && m.DeclaringType == typeof(DamageCmd) && m.Name == "Attack").operand;
        Assert.Equal(typeof(CalculatedDamageVar), Assert.Single(attack.GetParameters()).ParameterType);
        Assert.DoesNotContain(body, i => i.operand is MethodInfo m && m.DeclaringType == typeof(PowerCmd));
        Assert.DoesNotContain(body, i => i.operand is MethodInfo m && m.DeclaringType?.Name == "DoomRework");
        var producer = typeof(Entry).Assembly.GetType("Sts2Diy.Scripts.Patches.VanillaDoomPatches", true)!;
        var covered = (Type[])producer.GetField("DirectCards", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.DoesNotContain(typeof(BlightStrike), covered);
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void LocalizationUsesCalculatedPreviewAndRemovesTheOldDoomApplication(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        string text = json.RootElement.GetProperty("BLIGHT_STRIKE.description").GetString()!;
        Assert.Contains("{CalculatedDamage:diff()}", text);
        Assert.Contains(language == "zhs" ? "一半" : "half", text);
        Assert.Contains(language == "zhs" ? "向下取整" : "rounded down", text);
        Assert.DoesNotContain("1.5", text);
        Assert.DoesNotContain("150%", text);
        Assert.DoesNotContain("三分之一", text);
        Assert.DoesNotContain("one third", text);
    }

    private static bool InCombat(ref bool __result)
    {
        __result = _inCombat;
        return false;
    }

    private static MethodInfo Patch(string name, string method) =>
        Patches.GetNestedType(name, BindingFlags.NonPublic)!.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static (BlightStrike Card, DoomPower Doom) CreateCard(int doomAmount)
    {
        var card = (BlightStrike)RuntimeHelpers.GetUninitializedObject(typeof(BlightStrike));
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        var doom = (DoomPower)RuntimeHelpers.GetUninitializedObject(typeof(DoomPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_upgradePreviewType").SetValue(card, CardUpgradePreviewType.Combat);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Player), "_runPiles").SetValue(player, Array.Empty<CardPile>());
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, doomAmount);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel> { doom });
        creature.CombatState = (CombatState)RuntimeHelpers.GetUninitializedObject(typeof(CombatState));
        object?[] arguments = [null];
        Assert.Equal(false, Patch("BaseValuesPatch", "Prefix").Invoke(null, arguments));
        var set = new DynamicVarSet((IEnumerable<DynamicVar>)arguments[0]!);
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card, set);
        set.InitializeWithOwner(card);
        return (card, doom);
    }
}

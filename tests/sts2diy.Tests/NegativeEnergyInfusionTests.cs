using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts.Cards;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class NegativeEnergyInfusionTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.negative_energy_infusion");
    private static readonly Dictionary<Player, Creature?> Osties = [];
    private static readonly List<(Creature Actor, Creature Target, decimal Damage, int Hits, CardPlay Play)> Attacks = [];
    private static CardPlay? _play;
    private static bool _inCombat;
    private static bool _ending;
    private static Action? _reaction;

    public NegativeEnergyInfusionTests()
    {
        Osties.Clear();
        Attacks.Clear();
        _play = null;
        _inCombat = true;
        _ending = false;
        _reaction = null;
        Patch(AccessTools.PropertyGetter(typeof(Player), nameof(Player.Osty)), nameof(GetOsty));
        Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)), nameof(InCombat));
        Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), nameof(Ending));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.FromOsty)), nameof(FromOsty));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)), nameof(Execute));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(4, 5)]
    public async Task NativeMultiHitCountIsOnePlusDoomedCreaturesNotDoomStacks(int count, int expected)
    {
        var f = Fixture();
        for (int i = 0; i < count; i++)
            AddCreature(f.Combat, CombatSide.Enemy, doom: 20 + i);
        await Play(f.Card, f.Enemy);
        var attack = Assert.Single(Attacks);
        Assert.Equal(expected, attack.Hits);
        Assert.Equal(6m, attack.Damage);
        Assert.Same(f.Osty, attack.Actor);
        Assert.Same(f.Enemy, attack.Target);
        Assert.Same(f.Card, attack.Play.Card);
        Assert.Equal((decimal)expected, Hits(f.Card).Calculate(f.Enemy));
    }

    [Fact]
    public async Task OwnerTeammatePetAndEnemyEachCountOnceButDeadAndZeroDoomDoNot()
    {
        var f = Fixture();
        Doom(f.Player.Creature, 4);
        Doom(f.Osty, 8);
        Doom(f.Enemy, 12);
        AddCreature(f.Combat, CombatSide.Player, doom: 30);
        AddCreature(f.Combat, CombatSide.Enemy, doom: 100, hp: 0);
        AddCreature(f.Combat, CombatSide.Enemy, doom: 0);
        var otherCombat = Fixture();
        Doom(otherCombat.Player.Creature, 50);
        await Play(f.Card, f.Enemy);
        Assert.Equal(5, Assert.Single(Attacks).Hits);
        Assert.Equal(4, f.Player.Creature.GetPowerAmount<DoomPower>());
        Assert.Equal(8, f.Osty.GetPowerAmount<DoomPower>());
        Assert.Equal(12, f.Enemy.GetPowerAmount<DoomPower>());
    }

    [Fact]
    public async Task CountIsFrozenBeforeAttackReactionsAndNextPlayReadsTheNewState()
    {
        var f = Fixture();
        Doom(f.Enemy, 1);
        _reaction = () => Doom(f.Player.Creature, 8);
        await Play(f.Card, f.Enemy);
        Assert.Equal(2, Assert.Single(Attacks).Hits);
        _reaction = null;
        await Play(f.Card, f.Enemy);
        Assert.Equal(3, Attacks[1].Hits);
    }

    [Fact]
    public void PreviewReadsDoomAndDeathsLiveButOutsideCombatShowsOneHit()
    {
        var f = Fixture();
        var hits = Hits(f.Card);
        Assert.Equal(1m, hits.Calculate(null));
        DoomPower doom = Doom(f.Enemy, 5);
        hits.UpdateCardPreview(f.Card, CardPreviewMode.None, null, false);
        Assert.Equal(2m, hits.PreviewValue);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, 0);
        Assert.Equal(1m, hits.Calculate(null));
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, 9);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(f.Enemy, 0);
        Assert.Equal(1m, hits.Calculate(null));
        Doom(f.Player.Creature, 10);
        _inCombat = false;
        Assert.Equal(1m, hits.Calculate(null));
    }

    [Fact]
    public async Task UpgradeOnlyIncreasesEachHitToEight()
    {
        var f = Fixture();
        Doom(f.Enemy, 3);
        AccessTools.Method(typeof(NegativeEnergyInfusionCard), "OnUpgrade").Invoke(f.Card, null);
        await Play(f.Card, f.Enemy);
        var attack = Assert.Single(Attacks);
        Assert.Equal(8m, attack.Damage);
        Assert.Equal(2, attack.Hits);
        Assert.Equal(1, f.Card.EnergyCost.Canonical);
        Assert.Empty(f.Card.CanonicalKeywords);
        var tags = (HashSet<CardTag>)AccessTools.Property(typeof(NegativeEnergyInfusionCard), "CanonicalTags")
            .GetValue(f.Card)!;
        Assert.Contains(CardTag.OstyAttack, tags);
        var constructor = PatchProcessor.GetOriginalInstructions(typeof(NegativeEnergyInfusionCard).GetConstructor(Type.EmptyTypes)!);
        Assert.Contains(constructor, i => i.LoadsConstant((int)CardRarity.Common));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task MissingOrDeadOstyDeadOwnerDeadTargetAndEndedCombatDoNotAttack(int scenario)
    {
        var f = Fixture();
        if (scenario == 0) Osties[f.Player] = null;
        if (scenario == 1) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(f.Osty, 0);
        if (scenario == 2) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(f.Player.Creature, 0);
        if (scenario == 3) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(f.Enemy, 0);
        if (scenario == 4) _ending = true;
        await Play(f.Card, f.Enemy);
        Assert.Empty(Attacks);
    }

    [Theory]
    [InlineData("zhs", "负能量灌注")]
    [InlineData("eng", "Negative Energy Infusion")]
    public void LocalizationIncludesOstyDamageAndCalculatedHitPreview(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        const string key = "STS2DIY_CARD_NEGATIVE_ENERGY_INFUSION_CARD";
        Assert.Equal(title, json.RootElement.GetProperty(key + ".title").GetString());
        string text = json.RootElement.GetProperty(key + ".description").GetString()!;
        Assert.Contains("{OstyDamage:diff()}", text);
        Assert.Contains("{CalculatedHits:diff()}", text);
    }

    private void Patch(MethodBase method, string prefix) => _harmony.Patch(method,
        prefix: new HarmonyMethod(typeof(NegativeEnergyInfusionTests), prefix));
    private static CalculatedVar Hits(NegativeEnergyInfusionCard card) => (CalculatedVar)card.DynamicVars["CalculatedHits"];
    private static bool GetOsty(Player __instance, ref Creature? __result)
    { __result = Osties.GetValueOrDefault(__instance); return false; }
    private static bool InCombat(ref bool __result) { __result = _inCombat; return false; }
    private static bool Ending(ref bool __result) { __result = _ending; return false; }

    private static bool FromOsty(AttackCommand __instance, Creature osty, CardModel card, CardPlay cardPlay,
        ref AttackCommand __result)
    {
        AccessTools.Property(typeof(AttackCommand), nameof(AttackCommand.Attacker)).SetValue(__instance, osty);
        _play = cardPlay;
        __result = __instance;
        return false;
    }

    private static bool Execute(AttackCommand __instance, PlayerChoiceContext choiceContext,
        ref Task<AttackCommand> __result)
    {
        Assert.IsType<ThrowingPlayerChoiceContext>(choiceContext);
        var target = (Creature)AccessTools.Field(typeof(AttackCommand), "_singleTarget").GetValue(__instance)!;
        decimal damage = (decimal)AccessTools.Field(typeof(AttackCommand), "_damagePerHit").GetValue(__instance)!;
        int hits = (int)AccessTools.Field(typeof(AttackCommand), "_hitCount").GetValue(__instance)!;
        Attacks.Add((__instance.Attacker!, target, damage, hits, _play!));
        _reaction?.Invoke();
        __result = Task.FromResult(__instance);
        return false;
    }

    private static Task Play(NegativeEnergyInfusionCard card, Creature enemy)
    {
        var play = (CardPlay)RuntimeHelpers.GetUninitializedObject(typeof(CardPlay));
        AccessTools.Field(typeof(CardPlay), "<Card>k__BackingField").SetValue(play, card);
        AccessTools.Field(typeof(CardPlay), "<Target>k__BackingField").SetValue(play, enemy);
        return (Task)AccessTools.Method(typeof(NegativeEnergyInfusionCard), "OnPlay")
            .Invoke(card, [new ThrowingPlayerChoiceContext(), play])!;
    }

    private static (Player Player, Creature Osty, Creature Enemy, CombatState Combat, NegativeEnergyInfusionCard Card) Fixture()
    {
        var combat = (CombatState)RuntimeHelpers.GetUninitializedObject(typeof(CombatState));
        AccessTools.Field(typeof(CombatState), "_allies").SetValue(combat, new List<Creature>());
        AccessTools.Field(typeof(CombatState), "_enemies").SetValue(combat, new List<Creature>());
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        Creature creature = AddCreature(combat, CombatSide.Player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Player), "_runPiles").SetValue(player, Array.Empty<CardPile>());
        Creature osty = AddCreature(combat, CombatSide.Player);
        Creature enemy = AddCreature(combat, CombatSide.Enemy);
        Osties[player] = osty;
        var card = (NegativeEnergyInfusionCard)RuntimeHelpers.GetUninitializedObject(typeof(NegativeEnergyInfusionCard));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_upgradePreviewType").SetValue(card, CardUpgradePreviewType.Combat);
        AccessTools.Field(typeof(CardModel), "_energyCost").SetValue(card, new CardEnergyCost(card, 1, false));
        var values = new DynamicVarSet((IEnumerable<DynamicVar>)AccessTools.Property(typeof(NegativeEnergyInfusionCard), "CanonicalVars").GetValue(card)!);
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card, values);
        values.InitializeWithOwner(card);
        return (player, osty, enemy, combat, card);
    }

    private static Creature AddCreature(CombatState combat, CombatSide side, int doom = 0, int hp = 100)
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        creature.CombatState = combat;
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, hp);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, side);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel>());
        ((List<Creature>)(side == CombatSide.Player ? combat.Allies : combat.Enemies)).Add(creature);
        if (doom != 0) Doom(creature, doom);
        return creature;
    }

    private static DoomPower Doom(Creature creature, int amount)
    {
        var doom = (DoomPower)RuntimeHelpers.GetUninitializedObject(typeof(DoomPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(doom, true);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, amount);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(doom, creature);
        ((List<PowerModel>)creature.Powers).Add(doom);
        return doom;
    }
}

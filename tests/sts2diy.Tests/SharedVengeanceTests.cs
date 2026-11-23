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
using Sts2Diy.Scripts.Cards;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class SharedVengeanceTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.shared_vengeance");
    private static readonly Dictionary<Player, Creature?> Osties = [];
    private static readonly List<(Creature Attacker, decimal Damage, Creature Target, CardPlay Play)> Attacks = [];
    private static readonly Dictionary<AttackCommand, CardPlay> Plays = [];
    private static bool _ending;
    private static int _reaction;

    public SharedVengeanceTests()
    {
        Osties.Clear();
        Attacks.Clear();
        Plays.Clear();
        _ending = false;
        _reaction = 0;
        Patch(AccessTools.PropertyGetter(typeof(Player), nameof(Player.Osty)), nameof(GetOsty));
        Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), nameof(Ending));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.FromCard)), nameof(FromCard));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.FromOsty)), nameof(FromOsty));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)), nameof(Execute));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(false, 6, 9)]
    [InlineData(true, 8, 12)]
    public async Task TwoAttacksUseSeparateAttackersAndTheSameCardPlay(bool upgraded, int playerDamage, int ostyDamage)
    {
        var owner = Fixture();
        if (upgraded) AccessTools.Method(typeof(SharedVengeanceCard), "OnUpgrade").Invoke(owner.Card, null);
        await Play(owner.Card, owner.Enemy);
        Assert.Equal(2, Attacks.Count);
        Assert.Equal((owner.Player.Creature, (decimal)playerDamage, owner.Enemy),
            (Attacks[0].Attacker, Attacks[0].Damage, Attacks[0].Target));
        Assert.Equal((owner.Osty, (decimal)ostyDamage, owner.Enemy),
            (Attacks[1].Attacker, Attacks[1].Damage, Attacks[1].Target));
        Assert.Same(Attacks[0].Play, Attacks[1].Play);
        Assert.Same(owner.Card, Attacks[0].Play.Card);
    }

    [Theory]
    [InlineData(1)] // First hit kills the enemy.
    [InlineData(2)] // A reaction ends the fight.
    [InlineData(3)] // A reaction kills Osty.
    [InlineData(4)] // A reaction removes Osty.
    [InlineData(5)] // A reaction kills the player.
    public async Task ReactionsAfterTheFirstAttackSafelySkipTheSecond(int reaction)
    {
        var owner = Fixture();
        _reaction = reaction;
        await Play(owner.Card, owner.Enemy);
        Assert.Single(Attacks);
    }

    [Fact]
    public async Task MissingOstyDoesNotPreventThePlayersAttack()
    {
        var owner = Fixture();
        Osties[owner.Player] = null;
        await Play(owner.Card, owner.Enemy);
        Assert.Single(Attacks);
        Assert.Same(owner.Player.Creature, Attacks[0].Attacker);
    }

    [Fact]
    public async Task OnlyOwnActualOstyDeathsLowerCombatCostAndTheCostNeverGoesBelowZero()
    {
        var owner = Fixture();
        var other = Fixture();
        var context = new ThrowingPlayerChoiceContext();
        await owner.Card.AfterDeath(context, other.Osty, null, false, 0);
        await owner.Card.AfterDeath(context, owner.Enemy, null, false, 0);
        await owner.Card.AfterDeath(context, owner.Osty, null, true, 0);
        Assert.Equal(2, owner.Card.EnergyCost.GetWithModifiers(CostModifiers.Local));
        await owner.Card.AfterDeath(context, owner.Osty, null, false, 0);
        Assert.Equal(1, owner.Card.EnergyCost.GetWithModifiers(CostModifiers.Local));
        await owner.Card.AfterDeath(context, owner.Osty, null, false, 0);
        Assert.Equal(0, owner.Card.EnergyCost.GetWithModifiers(CostModifiers.Local));
        await owner.Card.AfterDeath(context, owner.Osty, null, false, 0);
        Assert.Equal(0, owner.Card.EnergyCost.GetWithModifiers(CostModifiers.Local));
        Assert.Equal(2, owner.Card.EnergyCost.Canonical);
        Assert.Equal(2, other.Card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }

    [Theory]
    [InlineData(PileType.Hand, 1)]
    [InlineData(PileType.Draw, 1)]
    [InlineData(PileType.Discard, 1)]
    [InlineData(PileType.Exhaust, 1)]
    [InlineData(PileType.Deck, 2)]
    public async Task DiscountsApplyInCombatPilesButNeverToThePermanentDeck(PileType type, int expected)
    {
        var owner = Fixture(type);
        await owner.Card.AfterDeath(new ThrowingPlayerChoiceContext(), owner.Osty, null, false, 0);
        Assert.Equal(expected, owner.Card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }

    [Theory]
    [InlineData("zhs", "同仇敌忾")]
    [InlineData("eng", "Shared Vengeance")]
    public void LocalizedDescriptionPreviewsBothAttackers(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        Assert.Equal(title, json.RootElement.GetProperty("STS2DIY_CARD_SHARED_VENGEANCE_CARD.title").GetString());
        string description = json.RootElement.GetProperty("STS2DIY_CARD_SHARED_VENGEANCE_CARD.description").GetString()!;
        Assert.Contains("{Damage:diff()}", description);
        Assert.Contains("{OstyDamage:diff()}", description);
    }

    private void Patch(MethodBase method, string prefix) => _harmony.Patch(method,
        prefix: new HarmonyMethod(typeof(SharedVengeanceTests), prefix));

    private static bool GetOsty(Player __instance, ref Creature? __result)
    {
        __result = Osties.GetValueOrDefault(__instance);
        return false;
    }

    private static bool Ending(ref bool __result) { __result = _ending; return false; }

    private static bool FromCard(AttackCommand __instance, CardModel card, CardPlay cardPlay, ref AttackCommand __result)
        => SetAttacker(__instance, card.Owner.Creature, card, cardPlay, ref __result);

    private static bool FromOsty(AttackCommand __instance, Creature osty, CardModel card, CardPlay cardPlay,
        ref AttackCommand __result) => SetAttacker(__instance, osty, card, cardPlay, ref __result);

    private static bool SetAttacker(AttackCommand command, Creature attacker, CardModel card, CardPlay play,
        ref AttackCommand result)
    {
        AccessTools.Property(typeof(AttackCommand), nameof(AttackCommand.Attacker)).SetValue(command, attacker);
        AccessTools.Property(typeof(AttackCommand), nameof(AttackCommand.ModelSource)).SetValue(command, card);
        Plays.Add(command, play);
        result = command;
        return false;
    }

    private static bool Execute(AttackCommand __instance, PlayerChoiceContext choiceContext,
        ref Task<AttackCommand> __result)
    {
        Assert.IsType<ThrowingPlayerChoiceContext>(choiceContext);
        var target = (Creature)AccessTools.Field(typeof(AttackCommand), "_singleTarget").GetValue(__instance)!;
        decimal damage = (decimal)AccessTools.Field(typeof(AttackCommand), "_damagePerHit").GetValue(__instance)!;
        Attacks.Add((__instance.Attacker!, damage, target, Plays[__instance]));
        if (Attacks.Count == 1)
        {
            Player player = ((CardModel)__instance.ModelSource!).Owner;
            if (_reaction == 1) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(target, 0);
            if (_reaction == 2) _ending = true;
            if (_reaction == 3) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(Osties[player], 0);
            if (_reaction == 4) Osties[player] = null;
            if (_reaction == 5) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(player.Creature, 0);
        }
        __result = Task.FromResult(__instance);
        return false;
    }

    private static Task Play(SharedVengeanceCard card, Creature enemy)
    {
        var play = (CardPlay)RuntimeHelpers.GetUninitializedObject(typeof(CardPlay));
        AccessTools.Field(typeof(CardPlay), "<Card>k__BackingField").SetValue(play, card);
        AccessTools.Field(typeof(CardPlay), "<Target>k__BackingField").SetValue(play, enemy);
        return (Task)AccessTools.Method(typeof(SharedVengeanceCard), "OnPlay")
            .Invoke(card, [new ThrowingPlayerChoiceContext(), play])!;
    }

    private static (Player Player, Creature Osty, Creature Enemy, SharedVengeanceCard Card) Fixture(PileType pileType = PileType.Hand)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        Creature creature = NewCreature(CombatSide.Player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        var state = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        AccessTools.Field(typeof(Player), "<PlayerCombatState>k__BackingField").SetValue(player, state);
        var pile = new CardPile(pileType);
        AccessTools.Field(typeof(PlayerCombatState), "_piles").SetValue(state,
            pile.IsCombatPile ? new[] { pile } : Array.Empty<CardPile>());
        AccessTools.Field(typeof(Player), "_runPiles").SetValue(player,
            pile.IsCombatPile ? Array.Empty<CardPile>() : new[] { pile });
        Creature osty = NewCreature(CombatSide.Player);
        Osties[player] = osty;
        var card = (SharedVengeanceCard)RuntimeHelpers.GetUninitializedObject(typeof(SharedVengeanceCard));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_energyCost").SetValue(card, new CardEnergyCost(card, 2, false));
        var vars = (IEnumerable<DynamicVar>)typeof(SharedVengeanceCard)
            .GetProperty("CanonicalVars", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!;
        var values = new DynamicVarSet(vars);
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card, values);
        values.InitializeWithOwner(card);
        ((List<CardModel>)pile.Cards).Add(card);
        return (player, osty, NewCreature(CombatSide.Enemy), card);
    }

    private static Creature NewCreature(CombatSide side)
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 50);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, side);
        return creature;
    }
}

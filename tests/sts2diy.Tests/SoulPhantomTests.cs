using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using Sts2Diy.Scripts;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class SoulPhantomTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.soul_phantom");
    private static bool _ending;
    private static bool _endDuringSelection;
    private static int _selections;
    private static int _maxSelect;
    private static CardModel[] _chosen = [];
    private static readonly List<CardModel> Upgrades = [];
    private static SoulPhantomPower? _appliedPower;

    public SoulPhantomTests()
    {
        _ending = _endDuringSelection = false;
        _selections = _maxSelect = 0;
        _chosen = [];
        Upgrades.Clear();
        _appliedPower = null;
        Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), nameof(Ending));
        Patch(AccessTools.PropertyGetter(typeof(PowerModel), "SelectionScreenPrompt"), nameof(Prompt));
        Patch(AccessTools.Method(typeof(CardCmd), nameof(CardCmd.ApplyKeyword)), nameof(ApplyKeyword));
        Patch(AccessTools.Method(typeof(CardCmd), nameof(CardCmd.Upgrade), [typeof(CardModel), typeof(CardPreviewStyle)]), nameof(Upgrade));
        Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromCombatPile),
            [typeof(PlayerChoiceContext), typeof(CardPile), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>)]), nameof(Select));
        var machine = AccessTools.Method(typeof(SoulPhantomCard), "OnPlay")
            .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        _harmony.Patch(AccessTools.Method(machine, "MoveNext"),
            transpiler: new HarmonyMethod(typeof(SoulPhantomTests), nameof(ReplacePowerApply)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlayingTheCardInstallsTheCorrectVersion(bool upgraded)
    {
        var owner = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.Hand);
        var card = Card<SoulPhantomCard>(owner.Player, owner.State.PlayPile);
        AccessTools.Field(typeof(CardModel), "_currentUpgradeLevel").SetValue(card, upgraded ? 1 : 0);
        _appliedPower = owner.Power;
        await (Task)AccessTools.Method(typeof(SoulPhantomCard), "OnPlay")
            .Invoke(card, [new ThrowingPlayerChoiceContext(), Play(card)])!;
        Assert.DoesNotContain(CardKeyword.Ethereal, soul.Keywords);
        Assert.Equal(upgraded, soul.IsUpgraded);
        Assert.Equal(upgraded, owner.Power.Sts2Diy_UpgradeSouls);
        Assert.Equal(0, _selections);
    }

    [Fact]
    public async Task UnappliedPowerDoesNotModifySouls()
    {
        var owner = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.Hand);
        var card = Card<SoulPhantomCard>(owner.Player, owner.State.PlayPile);
        await (Task)AccessTools.Method(typeof(SoulPhantomCard), "OnPlay")
            .Invoke(card, [new ThrowingPlayerChoiceContext(), Play(card)])!;
        Assert.DoesNotContain(CardKeyword.Ethereal, soul.Keywords);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentAndFutureSoulsChangeOnlyForTheirOwner(bool upgraded)
    {
        var owner = Fixture();
        var other = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.Hand);
        var discarded = Card<Soul>(owner.Player, owner.State.DiscardPile);
        var attack = Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        var deckSoul = Card<Soul>(owner.Player, owner.Deck);
        var foreign = Card<Soul>(other.Player, other.State.Hand);

        owner.Power.Activate(upgraded);
        Assert.DoesNotContain(CardKeyword.Ethereal, soul.Keywords);
        Assert.Contains(CardKeyword.Exhaust, soul.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, discarded.Keywords);
        Assert.Equal(upgraded, soul.IsUpgraded);
        Assert.Equal(upgraded, discarded.IsUpgraded);
        Assert.DoesNotContain(CardKeyword.Ethereal, attack.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, deckSoul.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, foreign.Keywords);
        Assert.False(deckSoul.IsUpgraded);

        var future = Card<Soul>(owner.Player, owner.State.DrawPile);
        await owner.Power.AfterCardEnteredCombat(future);
        await owner.Power.AfterCardEnteredCombat(foreign);
        await owner.Power.AfterCardEnteredCombat(future);
        Assert.DoesNotContain(CardKeyword.Ethereal, future.Keywords);
        Assert.Equal(upgraded, future.IsUpgraded);
        Assert.DoesNotContain(CardKeyword.Ethereal, foreign.Keywords);
        Assert.Equal(upgraded ? 3 : 0, Upgrades.Count);
    }

    [Fact]
    public async Task SavedUpgradeFlagSurvivesRestorationAndNormalCopiesCannotDowngradeIt()
    {
        var first = Fixture();
        first.Power.Activate(true);
        var restored = Fixture();
        PropertyInfo saved = typeof(SoulPhantomPower).GetProperty(nameof(SoulPhantomPower.Sts2Diy_UpgradeSouls))!;
        Assert.NotNull(saved.GetCustomAttribute<SavedPropertyAttribute>());
        saved.SetValue(restored.Power, saved.GetValue(first.Power));
        restored.Power.Activate(false);
        var future = Card<Soul>(restored.Player, restored.State.Hand);
        await restored.Power.AfterCardEnteredCombat(future);
        Assert.True(restored.Power.Sts2Diy_UpgradeSouls);
        Assert.True(future.IsUpgraded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EtherealFromOtherSourcesIsNotRemoved(bool upgraded)
    {
        var owner = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.Hand);
        ApplyKeyword(soul, [CardKeyword.Ethereal]);
        owner.Power.Activate(upgraded);
        await owner.Power.AfterCardEnteredCombat(soul);
        Assert.Contains(CardKeyword.Ethereal, soul.Keywords);
        Assert.Equal(upgraded, soul.IsUpgraded);
    }

    [Fact]
    public async Task SoulsCanStillBeExplicitlyChosenToReceiveEthereal()
    {
        var owner = Fixture();
        var played = Card<Soul>(owner.Player, owner.State.PlayPile);
        var chosen = Card<Soul>(owner.Player, owner.State.DrawPile);
        var untouched = Card<Soul>(owner.Player, owner.State.Hand);
        owner.Power.Activate(false);
        Assert.DoesNotContain(CardKeyword.Ethereal, chosen.Keywords);
        _chosen = [chosen];
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(played));
        Assert.Contains(CardKeyword.Ethereal, chosen.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, untouched.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, played.Keywords);
    }

    [Fact]
    public async Task SoulPlayUsesTheOriginalContextAndOnlyChangesChosenDrawPileCards()
    {
        var owner = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.PlayPile);
        var target = Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        var notChosen = Card<DefendNecrobinder>(owner.Player, owner.State.DrawPile);
        _chosen = [target];
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(soul));
        Assert.Equal(1, _selections);
        Assert.Equal(1, _maxSelect);
        Assert.Contains(CardKeyword.Ethereal, target.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, notChosen.Keywords);
        Assert.Same(owner.State.DrawPile, target.Pile);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OptionalSelectionCanBeSkippedOrAbortedWhenCombatEnds(bool ending)
    {
        var owner = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.PlayPile);
        var target = Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        _chosen = ending ? [target] : [];
        _endDuringSelection = ending;
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(soul));
        Assert.Equal(1, _selections);
        Assert.DoesNotContain(CardKeyword.Ethereal, target.Keywords);
    }

    [Fact]
    public async Task EmptyOrAlreadyEtherealDrawPilesDoNotOpenASelection()
    {
        var owner = Fixture();
        var soul = Card<Soul>(owner.Player, owner.State.PlayPile);
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(soul));
        var target = Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        ApplyKeyword(target, [CardKeyword.Ethereal]);
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(soul));
        Assert.Equal(0, _selections);
    }

    [Fact]
    public async Task ForeignCardsOtherCardTypesAndEndedCombatsDoNotTrigger()
    {
        var owner = Fixture();
        var other = Fixture();
        Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        var foreign = Card<Soul>(other.Player, other.State.PlayPile);
        var ordinary = Card<StrikeNecrobinder>(owner.Player, owner.State.PlayPile);
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(foreign));
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(ordinary));
        var own = Card<Soul>(owner.Player, owner.State.PlayPile);
        _ending = true;
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(own));
        Assert.Equal(0, _selections);
    }

    [Fact]
    public async Task RepeatedSoulPlaysAndStackedCopiesCanChooseAgain()
    {
        var owner = Fixture();
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(owner.Power, 2);
        var soul = Card<Soul>(owner.Player, owner.State.PlayPile);
        Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        Card<DefendNecrobinder>(owner.Player, owner.State.DrawPile);
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(soul));
        await owner.Power.AfterCardPlayed(new ThrowingPlayerChoiceContext(), Play(soul));
        Assert.Equal(2, _selections);
        Assert.Equal(2, _maxSelect);
    }

    [Fact]
    public void RetiringHauntPreservesOtherPoolsAndExistingModels()
    {
        Type patch = typeof(Entry).Assembly.GetType("Sts2Diy.Scripts.Patches.SoulPhantomPatches", true)!;
        MethodInfo method = AccessTools.Method(patch, "Postfix");
        var haunt = (Haunt)RuntimeHelpers.GetUninitializedObject(typeof(Haunt));
        var replacement = (SoulPhantomCard)RuntimeHelpers.GetUninitializedObject(typeof(SoulPhantomCard));
        IEnumerable<CardModel> original = new CardModel[] { haunt, replacement };
        object[] args = [RuntimeHelpers.GetUninitializedObject(typeof(NecrobinderCardPool)), original];
        method.Invoke(null, args);
        Assert.Equal(new CardModel[] { replacement }, (IEnumerable<CardModel>)args[1]);
        args = [RuntimeHelpers.GetUninitializedObject(typeof(ColorlessCardPool)), original];
        method.Invoke(null, args);
        Assert.Same(original, args[1]);
        Assert.NotNull(AccessTools.Method(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards)));
    }

    [Theory]
    [InlineData("zhs", "灵魂虚影")]
    [InlineData("eng", "Soul Phantom")]
    public void BilingualLocalizationIncludesTheUpgradeAndSelectionPrompt(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string folder = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "cards.json")));
        using var powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "powers.json")));
        Assert.Equal(title, cards.RootElement.GetProperty("STS2DIY_CARD_SOUL_PHANTOM_CARD.title").GetString());
        Assert.Contains("{IfUpgraded:show:", cards.RootElement.GetProperty("STS2DIY_CARD_SOUL_PHANTOM_CARD.description").GetString());
        Assert.Contains("{UpgradeSouls:cond:", powers.RootElement.GetProperty("STS2DIY_POWER_SOUL_PHANTOM_POWER.description").GetString());
        Assert.Contains("{Amount}", powers.RootElement.GetProperty("STS2DIY_POWER_SOUL_PHANTOM_POWER.selectionScreenPrompt").GetString());
        string oldEffect = language == "zhs" ? "灵魂[/gold]获得[gold]虚无" : "Souls[/gold] gain [gold]Ethereal";
        Assert.DoesNotContain(oldEffect, cards.RootElement.GetProperty("STS2DIY_CARD_SOUL_PHANTOM_CARD.description").GetString()!);
        Assert.DoesNotContain(oldEffect, powers.RootElement.GetProperty("STS2DIY_POWER_SOUL_PHANTOM_POWER.description").GetString()!);
    }

    private void Patch(MethodBase method, string prefix) => _harmony.Patch(method,
        prefix: new HarmonyMethod(typeof(SoulPhantomTests), prefix));

    private static bool Ending(ref bool __result) { __result = _ending; return false; }

    private static IEnumerable<CodeInstruction> ReplacePowerApply(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(PowerCmd)
                && method.Name == nameof(PowerCmd.Apply))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(SoulPhantomTests), nameof(ApplyPower));
            }
            yield return instruction;
        }
    }

    private static Task<SoulPhantomPower?> ApplyPower(PlayerChoiceContext context, Creature target, decimal amount,
        Creature? applier, CardModel? source, bool silent)
    {
        Assert.IsType<SoulPhantomCard>(source);
        Assert.Same(source.Owner.Creature, target);
        Assert.Same(target, applier);
        Assert.Equal(1m, amount);
        return Task.FromResult(_appliedPower);
    }

    private static bool Prompt(ref LocString __result)
    {
        __result = new LocString("powers", "STS2DIY_POWER_SOUL_PHANTOM_POWER.selectionScreenPrompt");
        return false;
    }

    private static bool ApplyKeyword(CardModel card, CardKeyword[] keywords)
    {
        var local = (HashSet<CardKeyword>)AccessTools.Field(typeof(CardModel), "_keywords").GetValue(card)!;
        local.UnionWith(keywords);
        return false;
    }

    private static bool Upgrade(CardModel card, CardPreviewStyle style)
    {
        Assert.True(card.IsUpgradable);
        Upgrades.Add(card);
        AccessTools.Field(typeof(CardModel), "_currentUpgradeLevel").SetValue(card, 1);
        return false;
    }

    private static bool Select(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs,
        Func<CardModel, bool> filter, ref Task<IEnumerable<CardModel>> __result)
    {
        Assert.IsType<ThrowingPlayerChoiceContext>(context);
        Assert.Same(player.PlayerCombatState!.DrawPile, pile);
        Assert.Equal(0, prefs.MinSelect);
        Assert.True(prefs.RequireManualConfirmation);
        Assert.Contains(pile.Cards, card => filter(card));
        _selections++;
        _maxSelect = prefs.MaxSelect;
        if (_endDuringSelection) _ending = true;
        __result = Task.FromResult<IEnumerable<CardModel>>(_chosen);
        return false;
    }

    private static CardPlay Play(CardModel card)
    {
        var play = (CardPlay)RuntimeHelpers.GetUninitializedObject(typeof(CardPlay));
        AccessTools.Field(typeof(CardPlay), "<Card>k__BackingField").SetValue(play, card);
        return play;
    }

    private static T Card<T>(Player player, CardPile pile) where T : CardModel
    {
        var card = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_keywords").SetValue(card, new HashSet<CardKeyword>(card.CanonicalKeywords));
        ((List<CardModel>)pile.Cards).Add(card);
        return card;
    }

    private static (Player Player, PlayerCombatState State, CardPile Deck, SoulPhantomPower Power) Fixture()
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        var state = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 30);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Player), "<PlayerCombatState>k__BackingField").SetValue(player, state);
        var deck = new CardPile(PileType.Deck);
        AccessTools.Field(typeof(Player), "_runPiles").SetValue(player, new[] { deck });
        foreach (string name in new[] { "Hand", "DrawPile", "DiscardPile", "ExhaustPile", "PlayPile" })
        {
            PileType type = name switch { "Hand" => PileType.Hand, "DrawPile" => PileType.Draw,
                "DiscardPile" => PileType.Discard, "ExhaustPile" => PileType.Exhaust, _ => PileType.Play };
            AccessTools.Field(typeof(PlayerCombatState), $"<{name}>k__BackingField").SetValue(state, new CardPile(type));
        }
        var power = (SoulPhantomPower)RuntimeHelpers.GetUninitializedObject(typeof(SoulPhantomPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(power, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(power, creature);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, 1);
        return (player, state, deck, power);
    }
}

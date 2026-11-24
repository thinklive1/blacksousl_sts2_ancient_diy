using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class VoidPassageTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.void_passage");
    private static List<CardModel> _affected = [];
    private static List<(Player Player, decimal Amount, PlayerChoiceContext Context)> _summons = [];
    private static Action<Player>? _onSummon;
    private static bool _ending;
    private static NoSummonPower? _restriction;

    public VoidPassageTests()
    {
        _affected = [];
        _summons = [];
        _onSummon = null;
        _ending = false;
        _restriction = null;
        _harmony.Patch(AccessTools.Method(typeof(CardCmd), nameof(CardCmd.ApplyKeyword)),
            prefix: new HarmonyMethod(typeof(VoidPassageTests), nameof(ApplyKeyword)));
        _harmony.Patch(AccessTools.Method(typeof(OstyCmd), nameof(OstyCmd.Summon)),
            prefix: new HarmonyMethod(typeof(VoidPassageTests), nameof(Summon)));
        _harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)),
            prefix: new HarmonyMethod(typeof(VoidPassageTests), nameof(Ending)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Fact]
    public async Task OnlyNewlyEtherealOwnerHandCardsSummonTwoEachWithTheOriginalContext()
    {
        var owner = Fixture();
        var other = Fixture();
        CardModel first = Card<StrikeNecrobinder>(owner.Player, owner.State.Hand);
        CardModel second = Card<DefendNecrobinder>(owner.Player, owner.State.Hand);
        CardModel existing = Card<StrikeNecrobinder>(owner.Player, owner.State.Hand, true);
        CardModel draw = Card<StrikeNecrobinder>(owner.Player, owner.State.DrawPile);
        CardModel discard = Card<StrikeNecrobinder>(owner.Player, owner.State.DiscardPile);
        CardModel deck = Card<StrikeNecrobinder>(owner.Player, owner.Deck);
        CardModel foreign = Card<StrikeNecrobinder>(other.Player, other.State.Hand);
        var context = new ThrowingPlayerChoiceContext();
        await Play(owner.Card, context);
        Assert.Equal(new[] { first, second }, _affected);
        Assert.Equal(2, _summons.Count);
        Assert.All(_summons, summon =>
        {
            Assert.Same(owner.Player, summon.Player);
            Assert.Equal(2m, summon.Amount);
            Assert.Same(context, summon.Context);
        });
        Assert.Contains(CardKeyword.Ethereal, existing.Keywords);
        foreach (CardModel card in new[] { draw, discard, deck, foreign })
            Assert.DoesNotContain(CardKeyword.Ethereal, card.Keywords);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyOrAlreadyEtherealHandsDoNotSummon(bool existing)
    {
        var owner = Fixture();
        if (existing) Card<StrikeNecrobinder>(owner.Player, owner.State.Hand, true);
        await Play(owner.Card, new ThrowingPlayerChoiceContext());
        Assert.Empty(_affected);
        Assert.Empty(_summons);
    }

    [Fact]
    public async Task SummonReactionsCannotExpandTheOriginalHandSnapshot()
    {
        var owner = Fixture();
        CardModel first = Card<StrikeNecrobinder>(owner.Player, owner.State.Hand);
        CardModel second = Card<DefendNecrobinder>(owner.Player, owner.State.Hand);
        CardModel? drawn = null;
        _onSummon = _ =>
        {
            Assert.Contains(CardKeyword.Ethereal, first.Keywords);
            Assert.Contains(CardKeyword.Ethereal, second.Keywords);
            drawn ??= Card<StrikeNecrobinder>(owner.Player, owner.State.Hand);
        };
        await Play(owner.Card, new ThrowingPlayerChoiceContext());
        Assert.Equal(2, _summons.Count);
        Assert.NotNull(drawn);
        Assert.DoesNotContain(CardKeyword.Ethereal, drawn.Keywords);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SummonLoopStopsWhenAReactionEndsCombatOrKillsTheOwner(bool death)
    {
        var owner = Fixture();
        Card<StrikeNecrobinder>(owner.Player, owner.State.Hand);
        Card<DefendNecrobinder>(owner.Player, owner.State.Hand);
        _onSummon = _ =>
        {
            if (death) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(owner.Player.Creature, 0);
            else _ending = true;
        };
        await Play(owner.Card, new ThrowingPlayerChoiceContext());
        Assert.Equal(2, _affected.Count);
        Assert.Single(_summons);
    }

    [Fact]
    public async Task PartnersSummonRestrictionDoesNotPreventKeywordApplication()
    {
        var owner = Fixture();
        Card<StrikeNecrobinder>(owner.Player, owner.State.Hand);
        _restriction = (NoSummonPower)RuntimeHelpers.GetUninitializedObject(typeof(NoSummonPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(_restriction, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(_restriction, owner.Player.Creature);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(_restriction, 2);
        await Play(owner.Card, new ThrowingPlayerChoiceContext());
        Assert.Single(_affected);
        Assert.Equal(0m, Assert.Single(_summons).Amount);
    }

    [Fact]
    public void UpgradeRemovesExhaustButRetainsEtherealCostAndSummonAmount()
    {
        var owner = Fixture();
        Assert.Equal(new[] { CardKeyword.Ethereal, CardKeyword.Exhaust }, owner.Card.CanonicalKeywords);
        AccessTools.Field(typeof(CardModel), "_isUpgrading").SetValue(owner.Card, true);
        AccessTools.Method(typeof(VoidPassageCard), "OnUpgrade").Invoke(owner.Card, null);
        var keywords = owner.Card.GetKeywordsWithSources(KeywordSources.Local);
        Assert.Contains(CardKeyword.Ethereal, keywords);
        Assert.DoesNotContain(CardKeyword.Exhaust, keywords);
        Assert.Equal(1, owner.Card.EnergyCost.Canonical);
        Assert.Equal(2m, owner.Card.DynamicVars.Summon.BaseValue);
        var constructor = PatchProcessor.GetOriginalInstructions(typeof(VoidPassageCard).GetConstructor(Type.EmptyTypes)!);
        Assert.Contains(constructor, instruction => instruction.LoadsConstant((int)CardType.Skill));
        Assert.Contains(constructor, instruction => instruction.LoadsConstant((int)CardRarity.Rare));
    }

    [Theory]
    [InlineData("zhs", "虚空引渡")]
    [InlineData("eng", "Void Passage")]
    public void RenamedCardHasLocalizedTextWithoutOverridingCallOfTheVoid(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        Assert.Equal(title, json.RootElement.GetProperty("STS2DIY_CARD_VOID_PASSAGE_CARD.title").GetString());
        Assert.Contains("{Summon:diff()}", json.RootElement.GetProperty("STS2DIY_CARD_VOID_PASSAGE_CARD.description").GetString());
        Assert.False(json.RootElement.TryGetProperty("CALL_OF_THE_VOID.description", out _));
        Assert.False(json.RootElement.TryGetProperty("STS2DIY_CARD_VOID_CALLING_CARD.title", out _));
    }

    private static Task Play(VoidPassageCard card, PlayerChoiceContext context) =>
        (Task)AccessTools.Method(typeof(VoidPassageCard), "OnPlay").Invoke(card, [context, null])!;
    private static bool Ending(ref bool __result) { __result = _ending; return false; }

    private static bool ApplyKeyword(CardModel card, CardKeyword[] keywords)
    {
        Assert.Equal(CardKeyword.Ethereal, Assert.Single(keywords));
        ((HashSet<CardKeyword>)AccessTools.Field(typeof(CardModel), "_keywords").GetValue(card)!).UnionWith(keywords);
        _affected.Add(card);
        return false;
    }

    private static bool Summon(PlayerChoiceContext choiceContext, Player summoner, decimal amount,
        AbstractModel? source, ref Task<SummonResult> __result)
    {
        Assert.IsType<VoidPassageCard>(source);
        decimal actual = _restriction?.ModifySummonAmount(summoner, amount, source) ?? amount;
        _summons.Add((summoner, actual, choiceContext));
        _onSummon?.Invoke(summoner);
        __result = Task.FromResult(new SummonResult(null, actual));
        return false;
    }

    private static T Card<T>(Player player, CardPile pile, bool ethereal = false) where T : CardModel
    {
        var card = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        var keywords = new HashSet<CardKeyword>(card.CanonicalKeywords);
        if (ethereal) keywords.Add(CardKeyword.Ethereal);
        AccessTools.Field(typeof(CardModel), "_keywords").SetValue(card, keywords);
        ((List<CardModel>)pile.Cards).Add(card);
        return card;
    }

    private static (Player Player, PlayerCombatState State, CardPile Deck, VoidPassageCard Card) Fixture()
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
        foreach (var entry in new[] { ("Hand", PileType.Hand), ("DrawPile", PileType.Draw),
            ("DiscardPile", PileType.Discard), ("ExhaustPile", PileType.Exhaust), ("PlayPile", PileType.Play) })
            AccessTools.Field(typeof(PlayerCombatState), $"<{entry.Item1}>k__BackingField")
                .SetValue(state, new CardPile(entry.Item2));
        var card = Card<VoidPassageCard>(player, state.PlayPile);
        AccessTools.Field(typeof(CardModel), "_energyCost").SetValue(card, new CardEnergyCost(card, 1, false));
        var vars = new DynamicVarSet((IEnumerable<DynamicVar>)typeof(VoidPassageCard)
            .GetProperty("CanonicalVars", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!);
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card, vars);
        vars.InitializeWithOwner(card);
        return (player, state, deck, card);
    }
}

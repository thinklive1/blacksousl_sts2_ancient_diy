using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts;

/// <summary>Provides persistent and per-combat trigger limits for playing-card suit enchantments.</summary>
public abstract class PlayingCardSuitEnchantment : ModEnchantmentTemplate
{
    public const int MinTriggersPerCombat = 1;
    public const int MaxTriggersPerCombat = 13;

    private int _remainingTriggers;
    private bool _triggerBudgetInitialized;
    private bool _triggeredThisCombat;
    private PlayingCardPokerHand<CardModel>? _pokerHandForThisPlay;
    private CardPlay? _pokerCardPlay;
    private bool _resolvedPokerEffectForCardPlay;

    public override bool ShowAmount => true;

    public override int DisplayAmount => Amount;

    public override bool HasExtraCardText => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("RemainingTriggers", 0)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(MyKeywords.TexasHoldemRules)
    ];

    protected abstract string SuitIconPath { get; }

    internal abstract PlayingCardSuit PokerSuit { get; }

    /// <summary>Formats face-card ranks without changing their persisted numeric values.</summary>
    internal static string GetRankDisplayText(int rank)
    {
        return rank switch
        {
            1 => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => rank.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public override EnchantmentAssetProfile AssetProfile => new(IconPath: SuitIconPath);

    [SavedProperty]
    public int BlackSouls_RemainingTriggers
    {
        get => _remainingTriggers;
        set
        {
            AssertMutable();
            _remainingTriggers = Math.Clamp(value, 0, MaxTriggersPerCombat);
        }
    }

    [SavedProperty]
    public bool BlackSouls_TriggerBudgetInitialized
    {
        get => _triggerBudgetInitialized;
        set
        {
            AssertMutable();
            _triggerBudgetInitialized = value;
        }
    }

    protected override void OnEnchant()
    {
        Amount = Math.Clamp(Amount, MinTriggersPerCombat, MaxTriggersPerCombat);
        if (!BlackSouls_TriggerBudgetInitialized)
        {
            BlackSouls_RemainingTriggers = Amount;
            BlackSouls_TriggerBudgetInitialized = true;
        }

        SyncRemainingTriggersForDisplay();
    }

    public override void RecalculateValues()
    {
        base.RecalculateValues();
        SyncRemainingTriggersForDisplay();
    }

    public override Task BeforeCombatStart()
    {
        _triggeredThisCombat = false;
        EnsurePersistentTriggerBudgetInitialized();
        SyncRemainingTriggersForDisplay();
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card != Card)
        {
            return Task.CompletedTask;
        }

        _pokerCardPlay = cardPlay;
        _resolvedPokerEffectForCardPlay = false;
        _pokerHandForThisPlay = FindPokerHandInHand();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card || Card.Owner?.Creature is not { IsDead: false } owner)
        {
            return;
        }

        if (_pokerCardPlay == cardPlay
            && !_resolvedPokerEffectForCardPlay
            && _pokerHandForThisPlay?.Cards.Any(pokerCard => pokerCard.Value.Enchantment == this) == true)
        {
            _resolvedPokerEffectForCardPlay = true;
            await TriggerPokerEffects(choiceContext, owner, cardPlay);
            return;
        }

        await TryTriggerSuitEffect(choiceContext, owner, cardPlay, consumePersistentTrigger: true);
    }

    protected abstract Task TriggerSuitEffect(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay);

    /// <summary>Returns whether the card can receive a new suit or reroll an existing suit.</summary>
    internal static bool CanReceiveOrRerollSuit(CardModel card)
    {
        return card.Enchantment is PlayingCardSuitEnchantment
            || card.Enchantment == null && CanEnchantWithAnySuit(card);
    }

    /// <summary>Returns whether at least one suit enchantment can be placed on an unenchanted card.</summary>
    internal static bool CanEnchantWithAnySuit(CardModel card)
    {
        return ModelDb.Enchantment<HeartSuitEnchantment>().CanEnchant(card)
            || ModelDb.Enchantment<DiamondSuitEnchantment>().CanEnchant(card)
            || ModelDb.Enchantment<ClubSuitEnchantment>().CanEnchant(card)
            || ModelDb.Enchantment<SpadeSuitEnchantment>().CanEnchant(card);
    }

    /// <summary>Applies a random suit and rank, replacing an existing suit when present.</summary>
    internal static bool TryEnchantRandomSuit(CardModel card, int minRank, int maxRank)
    {
        if (card.Owner == null || !CanReceiveOrRerollSuit(card))
        {
            return false;
        }

        if (card.Enchantment is PlayingCardSuitEnchantment)
        {
            CardCmd.ClearEnchantment(card);
        }

        if (!CanEnchantWithAnySuit(card))
        {
            return false;
        }

        int rank = card.Owner.RunState.Rng.Niche.NextInt(minRank, maxRank + 1);
        PlayingCardSuit[] suits = Enum.GetValues<PlayingCardSuit>()
            .OrderBy(_ => card.Owner.RunState.Rng.Niche.NextInt(100000))
            .ToArray();

        foreach (PlayingCardSuit suit in suits)
        {
            if (TryEnchant(card, suit, rank))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Triggers every currently available suit effect the requested number of times.</summary>
    internal static async Task TriggerAllSuitEffects(
        PlayerChoiceContext choiceContext,
        Creature owner,
        CardPlay? cardPlay,
        int triggerCount = 1)
    {
        triggerCount = Math.Max(0, triggerCount);
        if (triggerCount == 0 || owner.Player == null)
        {
            return;
        }

        PlayingCardSuit[] suits = Enum.GetValues<PlayingCardSuit>();
        for (int triggerIndex = 0; triggerIndex < triggerCount; triggerIndex++)
        {
            foreach (PlayingCardSuit suit in suits)
            {
                await TriggerStandaloneSuitEffect(choiceContext, owner, cardPlay, suit);
            }
        }
    }

    /// <summary>Executes one standalone suit effect without relying on a card enchantment instance.</summary>
    private static Task TriggerStandaloneSuitEffect(
        PlayerChoiceContext choiceContext,
        Creature owner,
        CardPlay? cardPlay,
        PlayingCardSuit suit)
    {
        return suit switch
        {
            PlayingCardSuit.Heart => CreatureCmd.Heal(owner, 1),
            PlayingCardSuit.Diamond => PlayerCmd.GainGold(5, owner.Player!),
            PlayingCardSuit.Club => PowerCmd.Apply<FlexPotionPower>(
                choiceContext,
                owner,
                2,
                owner,
                cardPlay?.Card,
                false),
            PlayingCardSuit.Spade => CreatureCmd.GainBlock(
                owner,
                3,
                ValueProp.Move,
                cardPlay),
            _ => Task.CompletedTask
        };
    }

    /// <summary>Resolves the selected poker hand, including wildcard-assigned suit effects.</summary>
    internal static async Task TriggerPokerHand(
        PlayerChoiceContext choiceContext,
        Creature owner,
        CardPlay? cardPlay,
        PlayingCardPokerHand<CardModel> pokerHand)
    {
        await PowerCmd.Apply<PokerHandAnnouncementPower>(
            choiceContext,
            owner,
            (int)pokerHand.Rank,
            owner,
            cardPlay?.Card,
            false);
        PokerHandAnnouncementPower.ShowHand(pokerHand.Rank);

        foreach (PlayingCardPokerCard<CardModel> pokerCard in pokerHand.Cards)
        {
            PlayingCardSuitEnchantment? enchantment = pokerCard.Value.Enchantment as PlayingCardSuitEnchantment;
            if (enchantment == null && pokerCard.IsWildcard)
            {
                enchantment = GetSuitEnchantments(owner.Player!)
                    .FirstOrDefault(candidate => candidate.PokerSuit == pokerCard.Suit);
            }

            if (enchantment == null)
            {
                continue;
            }

            for (int triggerIndex = 0; triggerIndex < pokerHand.SuitEffectTriggerCount; triggerIndex++)
            {
                await enchantment.TriggerSuitEffect(choiceContext, owner, cardPlay);
            }
        }
    }

    /// <summary>Finds the strongest hand in the current hand, optionally forcing a card into it.</summary>
    internal static PlayingCardPokerHand<CardModel>? FindPokerHandIncludingWildcard(
        Player player,
        CardModel? forcedCard = null)
    {
        List<CardModel> cards = PileType.Hand.GetPile(player).Cards.ToList();
        if (forcedCard != null && !cards.Contains(forcedCard))
        {
            cards.Add(forcedCard);
        }

        List<PlayingCardPokerCard<CardModel>> pokerCards = cards
            .Select(card => card.Enchantment as PlayingCardSuitEnchantment != null
                ? new PlayingCardPokerCard<CardModel>(
                    card,
                    Math.Clamp(((PlayingCardSuitEnchantment)card.Enchantment!).Amount, MinTriggersPerCombat, MaxTriggersPerCombat),
                    ((PlayingCardSuitEnchantment)card.Enchantment!).PokerSuit)
                : card is LastAceCard
                    ? new PlayingCardPokerCard<CardModel>(card, 1, PlayingCardSuit.Heart, IsWildcard: true)
                    : (PlayingCardPokerCard<CardModel>?)null)
            .OfType<PlayingCardPokerCard<CardModel>>()
            .ToList();

        return PlayingCardPokerHandEvaluator.FindBestHand(pokerCards);
    }

    /// <summary>Returns whether a hand card belongs to the current strongest poker hand.</summary>
    internal static bool IsCurrentPokerHandMember(CardModel card)
    {
        if (card.Owner == null || card.Pile?.Type != PileType.Hand)
        {
            return false;
        }

        PlayingCardPokerHand<CardModel>? pokerHand = FindPokerHandIncludingWildcard(card.Owner);
        return pokerHand?.Cards.Any(pokerCard => ReferenceEquals(pokerCard.Value, card)) == true;
    }

    /// <summary>Triggers this suit through the poker-table action without playing the underlying card text.</summary>
    internal Task TriggerForPokerTraining(PlayerChoiceContext choiceContext, Creature owner) =>
        TryTriggerSuitEffect(choiceContext, owner, null, consumePersistentTrigger: true);

    private async Task TryTriggerSuitEffect(
        PlayerChoiceContext choiceContext,
        Creature owner,
        CardPlay? cardPlay,
        bool consumePersistentTrigger)
    {
        if (_triggeredThisCombat)
        {
            return;
        }

        if (consumePersistentTrigger && !TrySpendPersistentTrigger())
        {
            return;
        }

        _triggeredThisCombat = true;
        await TriggerSuitEffect(choiceContext, owner, cardPlay);
    }

    private async Task TriggerPokerEffects(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay)
    {
        if (_pokerHandForThisPlay == null || !_pokerHandForThisPlay.Cards.Any(pokerCard => pokerCard.Value.Enchantment == this))
        {
            return;
        }

        // Poker hands are independent of both run-wide and per-combat Suit trigger limits.
        await TriggerPokerHand(choiceContext, owner, cardPlay, _pokerHandForThisPlay);
    }

    private bool TrySpendPersistentTrigger()
    {
        PlayingCardSuitEnchantment persistentEnchantment = GetPersistentEnchantment();
        persistentEnchantment.EnsureOwnTriggerBudgetInitialized();
        if (persistentEnchantment.BlackSouls_RemainingTriggers <= 0)
        {
            SyncRemainingTriggersForDisplay();
            return false;
        }

        persistentEnchantment.BlackSouls_RemainingTriggers--;
        persistentEnchantment.SyncRemainingTriggersForDisplay();

        if (persistentEnchantment != this)
        {
            BlackSouls_RemainingTriggers = persistentEnchantment.BlackSouls_RemainingTriggers;
            BlackSouls_TriggerBudgetInitialized = true;
        }

        SyncRemainingTriggersForDisplay();
        return true;
    }

    private void EnsurePersistentTriggerBudgetInitialized()
    {
        PlayingCardSuitEnchantment persistentEnchantment = GetPersistentEnchantment();
        persistentEnchantment.EnsureOwnTriggerBudgetInitialized();

        if (persistentEnchantment != this)
        {
            BlackSouls_RemainingTriggers = persistentEnchantment.BlackSouls_RemainingTriggers;
            BlackSouls_TriggerBudgetInitialized = true;
        }
    }

    private void EnsureOwnTriggerBudgetInitialized()
    {
        if (BlackSouls_TriggerBudgetInitialized)
        {
            return;
        }

        BlackSouls_RemainingTriggers = Math.Clamp(Amount, MinTriggersPerCombat, MaxTriggersPerCombat);
        BlackSouls_TriggerBudgetInitialized = true;
        SyncRemainingTriggersForDisplay();
    }

    private PlayingCardSuitEnchantment GetPersistentEnchantment()
    {
        return Card.DeckVersion?.Enchantment as PlayingCardSuitEnchantment ?? this;
    }

    private void SyncRemainingTriggersForDisplay()
    {
        if (DynamicVars.TryGetValue("RemainingTriggers", out DynamicVar? remainingVar) && remainingVar is not null)
        {
            remainingVar.BaseValue = BlackSouls_RemainingTriggers;
        }
    }

    private PlayingCardPokerHand<CardModel>? FindPokerHandInHand()
    {
        return Card.Owner == null ? null : FindPokerHandIncludingWildcard(Card.Owner, Card);
    }

    private static IEnumerable<PlayingCardSuitEnchantment> GetSuitEnchantments(Player player)
    {
        return player.Deck.Cards
            .Select(card => card.Enchantment)
            .OfType<PlayingCardSuitEnchantment>()
            .GroupBy(enchantment => enchantment.PokerSuit)
            .Select(group => group.First());
    }

    private static bool TryEnchant(CardModel card, PlayingCardSuit suit, int rank)
    {
        switch (suit)
        {
            case PlayingCardSuit.Heart when ModelDb.Enchantment<HeartSuitEnchantment>().CanEnchant(card):
                CardCmd.Enchant<HeartSuitEnchantment>(card, rank);
                return true;
            case PlayingCardSuit.Diamond when ModelDb.Enchantment<DiamondSuitEnchantment>().CanEnchant(card):
                CardCmd.Enchant<DiamondSuitEnchantment>(card, rank);
                return true;
            case PlayingCardSuit.Club when ModelDb.Enchantment<ClubSuitEnchantment>().CanEnchant(card):
                CardCmd.Enchant<ClubSuitEnchantment>(card, rank);
                return true;
            case PlayingCardSuit.Spade when ModelDb.Enchantment<SpadeSuitEnchantment>().CanEnchant(card):
                CardCmd.Enchant<SpadeSuitEnchantment>(card, rank);
                return true;
            default:
                return false;
        }
    }

}

/// <summary>Implements the Hearts (Royalty) enchantment.</summary>
[RegisterEnchantment]
public sealed class HeartSuitEnchantment : PlayingCardSuitEnchantment
{
    private const string HeartIconPath = "res://bs_ancient/assets/images/enchantment/HeartSuitEnchantment.png";
    private bool _heartJackEventClaimed;

    protected override string SuitIconPath => HeartIconPath;

    internal override PlayingCardSuit PokerSuit => PlayingCardSuit.Heart;

    [SavedProperty]
    public bool BlackSouls_HeartJackEventClaimed
    {
        get => _heartJackEventClaimed;
        set
        {
            AssertMutable();
            _heartJackEventClaimed = value;
        }
    }

    /// <summary>Redirects the next event once for each Hearts Jack in the persistent deck.</summary>
    public override EventModel ModifyNextEvent(EventModel currentEvent)
    {
        return Amount == 11 && !BlackSouls_HeartJackEventClaimed
            ? ModelDb.Event<HeartJackEvent>()
            : currentEvent;
    }

    /// <summary>Consumes the first unclaimed Hearts Jack belonging to the event owner.</summary>
    public static bool ClaimNextEvent(Player owner)
    {
        HeartSuitEnchantment? heartJack = owner.Deck.Cards
            .Select(card => card.Enchantment)
            .OfType<HeartSuitEnchantment>()
            .FirstOrDefault(enchantment => enchantment.Amount == 11 && !enchantment.BlackSouls_HeartJackEventClaimed);
        if (heartJack == null)
        {
            return false;
        }

        heartJack.BlackSouls_HeartJackEventClaimed = true;
        return true;
    }

    protected override Task TriggerSuitEffect(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay)
    {
        return CreatureCmd.Heal(owner, 1);
    }
}

/// <summary>Implements the Diamonds (Nobility) enchantment.</summary>
[RegisterEnchantment]
public sealed class DiamondSuitEnchantment : PlayingCardSuitEnchantment
{
    private const string DiamondIconPath = "res://bs_ancient/assets/images/enchantment/DiamondSuitEnchantment.png";

    protected override string SuitIconPath => DiamondIconPath;

    internal override PlayingCardSuit PokerSuit => PlayingCardSuit.Diamond;

    protected override Task TriggerSuitEffect(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay)
    {
        return PlayerCmd.GainGold(5, owner.Player!);
    }
}

/// <summary>Implements the Clubs (Soldiers) enchantment.</summary>
[RegisterEnchantment]
public sealed class ClubSuitEnchantment : PlayingCardSuitEnchantment
{
    private const string ClubIconPath = "res://bs_ancient/assets/images/enchantment/ClubSuitEnchantment.png";

    protected override string SuitIconPath => ClubIconPath;

    internal override PlayingCardSuit PokerSuit => PlayingCardSuit.Club;

    protected override Task TriggerSuitEffect(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay)
    {
        return PowerCmd.Apply<FlexPotionPower>(choiceContext, owner, 2, owner, Card, false);
    }
}

/// <summary>Implements the Spades (Servants) enchantment.</summary>
[RegisterEnchantment]
public sealed class SpadeSuitEnchantment : PlayingCardSuitEnchantment
{
    private const string SpadeIconPath = "res://bs_ancient/assets/images/enchantment/SpadeSuitEnchantment.png";

    protected override string SuitIconPath => SpadeIconPath;

    internal override PlayingCardSuit PokerSuit => PlayingCardSuit.Spade;

    protected override Task TriggerSuitEffect(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay)
    {
        return CreatureCmd.GainBlock(owner, 3, ValueProp.Move, cardPlay);
    }
}

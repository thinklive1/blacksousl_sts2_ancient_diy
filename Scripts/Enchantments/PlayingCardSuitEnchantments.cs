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

namespace BlackSouls.Scripts;

/// <summary>Provides persistent and per-combat trigger limits for playing-card suit enchantments.</summary>
public abstract class PlayingCardSuitEnchantment : ModEnchantmentTemplate
{
    public const int MinTriggersPerCombat = 1;
    public const int MaxTriggersPerCombat = 13;

    private int _remainingTriggers;
    private bool _triggerBudgetInitialized;
    private bool _triggeredThisCombat;
    private PlayingCardPokerHand<PlayingCardSuitEnchantment>? _pokerHandForThisPlay;
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
            && _pokerHandForThisPlay?.Cards.Any(pokerCard => pokerCard.Value == this) == true)
        {
            _resolvedPokerEffectForCardPlay = true;
            await TriggerPokerEffects(choiceContext, owner, cardPlay);
            return;
        }

        await TryTriggerSuitEffect(choiceContext, owner, cardPlay, consumePersistentTrigger: true);
    }

    protected abstract Task TriggerSuitEffect(PlayerChoiceContext choiceContext, Creature owner, CardPlay? cardPlay);

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
        if (_pokerHandForThisPlay == null || !_pokerHandForThisPlay.Cards.Any(pokerCard => pokerCard.Value == this))
        {
            return;
        }

        IEnumerable<PlayingCardSuitEnchantment> enchantments = _pokerHandForThisPlay.Cards
            .Select(pokerCard => pokerCard.Value)
            .Distinct();
        foreach (PlayingCardSuitEnchantment enchantment in enchantments)
        {
            // Poker hands are independent of both run-wide and per-combat Suit trigger limits.
            for (int triggerIndex = 0; triggerIndex < _pokerHandForThisPlay.SuitEffectTriggerCount; triggerIndex++)
            {
                await enchantment.TriggerSuitEffect(choiceContext, owner, cardPlay);
            }
        }
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

    private PlayingCardPokerHand<PlayingCardSuitEnchantment>? FindPokerHandInHand()
    {
        if (Card.Owner == null)
        {
            return null;
        }

        List<CardModel> cards = PileType.Hand.GetPile(Card.Owner).Cards.ToList();
        if (!cards.Contains(Card))
        {
            cards.Add(Card);
        }

        List<PlayingCardPokerCard<PlayingCardSuitEnchantment>> pokerCards = cards
            .Select(card => card.Enchantment as PlayingCardSuitEnchantment)
            .OfType<PlayingCardSuitEnchantment>()
            .Select(enchantment => new PlayingCardPokerCard<PlayingCardSuitEnchantment>(
                enchantment,
                Math.Clamp(enchantment.Amount, MinTriggersPerCombat, MaxTriggersPerCombat),
                enchantment.PokerSuit))
            .ToList();

        return PlayingCardPokerHandEvaluator.FindBestHand(pokerCards);
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

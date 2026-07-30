using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Predicts the owner's next card by the names played throughout the run.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class ButchersMathClassRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/scripts.png";
    private const decimal PredictionStrength = 1.25m;
    private const decimal MinimumMultiplier = 0.5m;
    private const decimal MaximumMultiplier = 2.5m;

    [SavedProperty]
    public string BlackSouls_PlayCounts
    {
        get => _playCountsSerialized;
        set
        {
            AssertMutable();
            _playCountsSerialized = value ?? string.Empty;
            _playCounts = null;
        }
    }

    [SavedProperty]
    public string BlackSouls_PredictionCounts
    {
        get => _predictionCountsSerialized;
        set
        {
            AssertMutable();
            _predictionCountsSerialized = value ?? string.Empty;
            _predictionCounts = null;
        }
    }

    [SavedProperty]
    public int BlackSouls_LastCrossEntropyMilli
    {
        get => _lastCrossEntropyMilli;
        set
        {
            AssertMutable();
            _lastCrossEntropyMilli = Math.Max(0, value);
        }
    }

    private string _playCountsSerialized = string.Empty;
    private string _predictionCountsSerialized = string.Empty;
    private int _lastCrossEntropyMilli;
    private Dictionary<string, int>? _playCounts;
    private Dictionary<string, int>? _predictionCounts;
    private readonly Dictionary<string, int> _turnPlayCounts = [];

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return !SnarkPageRelicTrackerModifier.HasAppearedOrOwned<ButchersMathClassRelic>(runState);
    }

    public override Task AfterObtained()
    {
        if (Owner != null)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<ButchersMathClassRelic>(Owner);
        }

        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        _turnPlayCounts.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        // Freeze the run-wide model for this turn so every card uses one prediction.
        SetPredictionCounts(new Dictionary<string, int>(PlayCounts));
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.PlayIndex != 0)
        {
            return Task.CompletedTask;
        }

        string cardId = GetCardId(cardPlay.Card);
        Increment(PlayCounts, cardId);
        Increment(_turnPlayCounts, cardId);
        SavePlayCounts();
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner)
        {
            ButchersMathClassExecutionContext.Push(cardPlay.Card);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner)
        {
            ButchersMathClassExecutionContext.Pop(cardPlay.Card);
        }

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner?.Creature is not { } ownerCreature || side != ownerCreature.Side || !participants.Contains(ownerCreature))
        {
            return Task.CompletedTask;
        }

        int total = _turnPlayCounts.Values.Sum();
        if (total > 0)
        {
            decimal entropy = _turnPlayCounts.Sum(entry => entry.Value * -DecimalLog(GetProbability(entry.Key)));
            BlackSouls_LastCrossEntropyMilli = (int)Math.Round(entropy / total * 1_000m, MidpointRounding.AwayFromZero);
        }

        _turnPlayCounts.Clear();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return IsAffectedCard(cardSource) && dealer == Owner?.Creature ? GetMultiplier(cardSource!, amount) : 1m;
    }

    public override decimal ModifyBlockMultiplicative(Creature? target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        return IsAffectedCard(cardSource) && target == Owner?.Creature ? GetMultiplier(cardSource!, block) : 1m;
    }

    public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        return IsAffectedCard(cardSource) && giver == Owner?.Creature ? GetMultiplier(cardSource!, amount) : 1m;
    }

    public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
    {
        return source is CardModel card && IsAffectedCard(card) && summoner == Owner ? amount * GetMultiplier(card, amount) : amount;
    }

    public override int ModifyXValue(CardModel card, int originalValue)
    {
        return !IsAffectedCard(card)
            ? originalValue
            : Math.Max(0, (int)Math.Round(originalValue * GetMultiplier(card, originalValue), MidpointRounding.AwayFromZero));
    }

    public bool Affects(CardModel card)
    {
        return IsAffectedCard(card);
    }

    internal decimal ScaleDirectAmount(CardModel card, decimal amount)
    {
        return Math.Max(0m, Math.Round(amount * GetMultiplier(card, amount), MidpointRounding.AwayFromZero));
    }

    private Dictionary<string, int> PlayCounts => _playCounts ??= DeserializeCounts(BlackSouls_PlayCounts);
    private Dictionary<string, int> PredictionCounts => _predictionCounts ??= DeserializeCounts(BlackSouls_PredictionCounts);

    private void SavePlayCounts()
    {
        BlackSouls_PlayCounts = SerializeCounts(PlayCounts);
    }

    private void SetPredictionCounts(Dictionary<string, int> counts)
    {
        _predictionCounts = counts;
        BlackSouls_PredictionCounts = SerializeCounts(counts);
        _predictionCounts = counts;
    }

    private bool IsAffectedCard(CardModel? card)
    {
        return card?.Owner?.GetRelic<ButchersMathClassRelic>() == this;
    }

    private decimal GetMultiplier(CardModel card, decimal amount = 0m)
    {
        decimal probability = GetProbability(GetCardId(card));
        decimal baseline = GetBaselineProbability();
        decimal relativeSurprise = (baseline - probability) / baseline;
        decimal multiplier = Math.Clamp(1m + relativeSurprise * PredictionStrength, MinimumMultiplier, MaximumMultiplier);

        // Small integer effects need a larger swing to remain visible after rounding.
        if (relativeSurprise > 0m && amount is > 0m and <= 2m)
        {
            multiplier = Math.Max(multiplier, amount == 1m ? 3m : 2m);
        }

        return multiplier;
    }

    private decimal GetProbability(string cardId)
    {
        Dictionary<string, int> counts = PredictionCounts;
        int total = counts.Values.Sum();
        int knownCardNames = Math.Max(GetDeckCardIdCount(), counts.Count + (counts.ContainsKey(cardId) ? 0 : 1));
        int count = counts.GetValueOrDefault(cardId);
        return (count + 1m) / (total + knownCardNames);
    }

    private static void Increment(Dictionary<string, int> counts, string cardId)
    {
        counts[cardId] = counts.GetValueOrDefault(cardId) + 1;
    }

    private static string GetCardId(CardModel card)
    {
        return card.Id.ToString();
    }

    private decimal GetBaselineProbability()
    {
        return 1m / GetDeckCardIdCount();
    }

    private int GetDeckCardIdCount()
    {
        return Math.Max(1, Owner?.Deck.Cards.Select(GetCardId).Distinct().Count() ?? 0);
    }

    private static Dictionary<string, int> DeserializeCounts(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(serialized) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeCounts(Dictionary<string, int> counts)
    {
        return JsonSerializer.Serialize(counts.Where(entry => entry.Value > 0).OrderBy(entry => entry.Key).ToDictionary());
    }

    private static decimal DecimalLog(decimal value)
    {
        return (decimal)Math.Log((double)Math.Max(value, 0.0001m));
    }
}

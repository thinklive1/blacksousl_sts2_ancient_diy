using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Tracks Snark page relics that have already appeared this run.</summary>
public sealed class SnarkPageRelicTrackerModifier : ModModifierTemplate
{
    private const char RelicIdSeparator = '|';
    private const string TransparentIconPath = "res://bs_ancient/assets/images/modifiers/TransparentModifier.png";
    private string _appearedRelicIds = string.Empty;
    private string _hiddenOptionOutcomesSerialized = string.Empty;
    private Dictionary<string, int>? _hiddenOptionOutcomes;

    public override ModifierAssetProfile AssetProfile => new(TransparentIconPath);

    [SavedProperty]
    public string BlackSouls_AppearedSnarkPageRelicIds
    {
        get => _appearedRelicIds;
        set
        {
            AssertMutable();
            _appearedRelicIds = value ?? string.Empty;
        }
    }

    [SavedProperty]
    public string BlackSouls_HiddenSnarkOptionOutcomes
    {
        get => _hiddenOptionOutcomesSerialized;
        set
        {
            AssertMutable();
            _hiddenOptionOutcomesSerialized = value ?? string.Empty;
            _hiddenOptionOutcomes = null;
        }
    }

    public static bool HasAppearedOrOwned<T>(Player player) where T : RelicModel
    {
        return player.GetRelic<T>() != null || HasAppeared<T>(player.RunState);
    }

    public static bool HasAppearedOrOwned<T>(IRunState runState) where T : RelicModel
    {
        return runState.Players.Any(player => player.GetRelic<T>() != null) || HasAppeared<T>(runState);
    }

    public static void MarkAppeared<T>(Player player) where T : RelicModel
    {
        MarkAppeared(player.RunState, ModelDb.Relic<T>().Id.Entry);
    }

    public static void MarkAppeared<T>(IRunState runState) where T : RelicModel
    {
        MarkAppeared(runState, ModelDb.Relic<T>().Id.Entry);
    }

    /// <summary>Rolls one hidden relic option once for each eligible event instance.</summary>
    public static bool ShouldOfferHiddenOption<T>(EventModel eventModel, int chancePercent) where T : RelicModel
    {
        Player? owner = eventModel.Owner;
        if (owner == null || owner.GetRelic<T>() != null)
        {
            return false;
        }

        string optionId = ModelDb.Relic<T>().Id.Entry;
        int outcome = GetOrCreateHiddenOptionOutcome(eventModel, optionId, () =>
        {
            if (HasAppeared<T>(owner.RunState))
            {
                return 0;
            }

            bool shouldAppear = owner.RunState.Rng.Niche.NextInt(100) < Math.Clamp(chancePercent, 0, 100);
            if (shouldAppear)
            {
                MarkAppeared<T>(owner);
            }

            return shouldAppear ? 1 : 0;
        });
        return outcome == 1;
    }

    /// <summary>Persists a custom hidden-option outcome for an event and reuses it after page refreshes.</summary>
    public static int GetOrCreateHiddenOptionOutcome(EventModel eventModel, string optionId, Func<int> createOutcome)
    {
        if (eventModel.Owner?.RunState is not { } runState)
        {
            return 0;
        }

        SnarkPageRelicTrackerModifier? tracker = FindOrCreate(runState);
        return tracker?.GetOrCreateHiddenOptionOutcome(eventModel.Id.Entry, optionId, createOutcome) ?? 0;
    }

    private static bool HasAppeared<T>(IRunState runState) where T : RelicModel
    {
        string relicId = ModelDb.Relic<T>().Id.Entry;
        return Find(runState)?.GetAppearedRelicIds().Contains(relicId) == true;
    }

    private static void MarkAppeared(IRunState runState, string relicId)
    {
        SnarkPageRelicTrackerModifier? tracker = FindOrCreate(runState);
        if (tracker == null)
        {
            return;
        }

        HashSet<string> appearedRelicIds = tracker.GetAppearedRelicIds();
        if (appearedRelicIds.Add(relicId))
        {
            tracker.BlackSouls_AppearedSnarkPageRelicIds = string.Join(
                RelicIdSeparator,
                appearedRelicIds.Order(StringComparer.Ordinal));
        }
    }

    private static SnarkPageRelicTrackerModifier? Find(IRunState runState)
    {
        return runState.Modifiers.OfType<SnarkPageRelicTrackerModifier>().FirstOrDefault();
    }

    private static SnarkPageRelicTrackerModifier? FindOrCreate(IRunState runState)
    {
        SnarkPageRelicTrackerModifier? existing = Find(runState);
        if (existing != null)
        {
            return existing;
        }

        if (runState is not RunState mutableRunState)
        {
            return null;
        }

        SnarkPageRelicTrackerModifier tracker =
            (SnarkPageRelicTrackerModifier)ModelDb.Modifier<SnarkPageRelicTrackerModifier>().ToMutable();
        tracker.OnRunLoaded(mutableRunState);
        mutableRunState.AddModifierDebug(tracker);
        return tracker;
    }

    private HashSet<string> GetAppearedRelicIds()
    {
        return BlackSouls_AppearedSnarkPageRelicIds
            .Split(RelicIdSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private int GetOrCreateHiddenOptionOutcome(string eventId, string optionId, Func<int> createOutcome)
    {
        Dictionary<string, int> outcomes = HiddenOptionOutcomes;
        int outcome = HiddenOptionRollLedger.GetOrCreate(outcomes, eventId, optionId, createOutcome);
        BlackSouls_HiddenSnarkOptionOutcomes = JsonSerializer.Serialize(
            outcomes.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToDictionary());
        _hiddenOptionOutcomes = outcomes;
        return outcome;
    }

    private Dictionary<string, int> HiddenOptionOutcomes =>
        _hiddenOptionOutcomes ??= DeserializeHiddenOptionOutcomes(BlackSouls_HiddenSnarkOptionOutcomes);

    private static Dictionary<string, int> DeserializeHiddenOptionOutcomes(string serialized)
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
}

/// <summary>Stores deterministic hidden-option outcomes by event and option identity.</summary>
internal static class HiddenOptionRollLedger
{
    private const char KeySeparator = '\u001f';

    public static int GetOrCreate(
        IDictionary<string, int> outcomes,
        string eventId,
        string optionId,
        Func<int> createOutcome)
    {
        string key = $"{eventId}{KeySeparator}{optionId}";
        if (outcomes.TryGetValue(key, out int existing))
        {
            return existing;
        }

        int outcome = createOutcome();
        outcomes[key] = outcome;
        return outcome;
    }
}

using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Tracks Snark page relics that have already appeared this run.</summary>
public sealed class SnarkPageRelicTrackerModifier : ModModifierTemplate
{
    private const char RelicIdSeparator = '|';
    private const string TransparentIconPath = "res://bs_ancient/assets/images/modifiers/TransparentModifier.png";
    private string _appearedRelicIds = string.Empty;

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
}

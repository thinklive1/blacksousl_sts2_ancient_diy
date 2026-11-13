using MegaCrit.Sts2.Core.Runs;

namespace BlackSouls.Scripts;

/// <summary>Stores per-run BS Ancient option overrides.</summary>
public static class BsAncientRunOptions
{
    private static readonly object SyncRoot = new();
    private static bool? _pendingFairyTaleModeOverride;

    public static bool FairyTaleModeForNextRun
    {
        get
        {
            lock (SyncRoot)
            {
                return _pendingFairyTaleModeOverride ?? BsAncientConfig.EnableFairyTaleMode;
            }
        }
        set
        {
            lock (SyncRoot)
            {
                _pendingFairyTaleModeOverride = value;
            }
        }
    }

    public static void CaptureFairyTaleModeForRun(IRunState runState)
    {
        _ = BsAncientRunConfig.ForRun(runState);
        _ = ConsumePendingSelection(false);
    }

    public static bool IsFairyTaleModeEnabled(IRunState runState)
    {
        return BsAncientRunConfig.ForRun(runState).EnableFairyTaleMode;
    }

    internal static bool ConsumePendingSelection(bool defaultValue)
    {
        lock (SyncRoot)
        {
            bool enabled = _pendingFairyTaleModeOverride ?? defaultValue;
            _pendingFairyTaleModeOverride = null;
            return enabled;
        }
    }

    internal static void ResetForTests()
    {
        lock (SyncRoot)
        {
            _pendingFairyTaleModeOverride = null;
        }
    }

}

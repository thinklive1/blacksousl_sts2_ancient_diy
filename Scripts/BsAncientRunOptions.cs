using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Runs;

namespace BlackSouls.Scripts;

/// <summary>Stores per-run BS Ancient option overrides.</summary>
public static class BsAncientRunOptions
{
    private static readonly object SyncRoot = new();
    private static ConditionalWeakTable<object, FairyTaleModeSelection> _fairyTaleModeByRun = new();
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
        _ = ResolveFairyTaleModeForRun(runState, BsAncientConfig.EnableFairyTaleMode);
    }

    public static bool IsFairyTaleModeEnabled(IRunState runState)
    {
        return ResolveFairyTaleModeForRun(runState, BsAncientConfig.EnableFairyTaleMode);
    }

    internal static bool ResolveFairyTaleModeForRun(object runKey, bool defaultValue)
    {
        ArgumentNullException.ThrowIfNull(runKey);
        lock (SyncRoot)
        {
            if (_fairyTaleModeByRun.TryGetValue(runKey, out FairyTaleModeSelection? existing))
            {
                return existing.Enabled;
            }

            bool enabled = _pendingFairyTaleModeOverride ?? defaultValue;
            _pendingFairyTaleModeOverride = null;
            _fairyTaleModeByRun.Add(runKey, new FairyTaleModeSelection(enabled));
            return enabled;
        }
    }

    internal static void ResetForTests()
    {
        lock (SyncRoot)
        {
            _pendingFairyTaleModeOverride = null;
            _fairyTaleModeByRun = new ConditionalWeakTable<object, FairyTaleModeSelection>();
        }
    }

    private sealed record FairyTaleModeSelection(bool Enabled);
}

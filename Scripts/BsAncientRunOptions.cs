namespace BlackSouls.Scripts;

/// <summary>Stores per-run BS Ancient option overrides.</summary>
public static class BsAncientRunOptions
{
    private static bool? _fairyTaleModeOverride;

    public static bool FairyTaleModeForNextRun
    {
        get => _fairyTaleModeOverride ?? BsAncientConfig.EnableFairyTaleMode;
        set => _fairyTaleModeOverride = value;
    }

    public static bool HasFairyTaleModeOverride => _fairyTaleModeOverride.HasValue;

    public static void ResetFairyTaleModeOverride()
    {
        _fairyTaleModeOverride = null;
    }
}

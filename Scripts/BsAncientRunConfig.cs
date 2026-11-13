using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace BlackSouls.Scripts;

/// <summary>Persists host-owned gameplay settings without changing anyone's local configuration.</summary>
internal static class BsAncientRunConfig
{
    internal static readonly RunSavedData<BsAncientConfig.FileConfig> SavedConfig =
        RunSavedDataStore.For(Entry.ModId).Register<BsAncientConfig.FileConfig>(
            "gameplay_config", options: new RunSavedDataOptions { SchemaVersion = 1 });
    private static bool _initialized;

    internal static BsAncientConfig.FileConfig Current => ForRun(RunManager.Instance.DebugOnlyGetState());

    internal static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(OnLobbyStaging);
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(OnRunPreparing);
    }

    internal static void OnLobbyStaging(RunSavedDataLobbyStagingEvent args)
    {
        if (args.Reason == RunSavedDataLobbyStagingReason.Committing
            && (!args.IsMultiplayer || args.IsHost)
            && !SavedConfig.Lobby.TryGet(args.Lobby, out _))
        {
            SavedConfig.Lobby.Set(args.Lobby, CaptureNewRun());
        }
    }

    internal static void OnRunPreparing(RunSavedDataPreparingEvent args)
    {
        // Multiplayer values have already been imported from the host's start-run payload.
        if (!SavedConfig.TryGet(args.RunState, out _))
        {
            SavedConfig.Set(args.RunState, args.IsMultiplayer ? CreateLegacyMultiplayerDefaults() : CaptureNewRun());
        }
    }

    internal static BsAncientConfig.FileConfig CaptureNewRun()
    {
        BsAncientConfig.FileConfig config = BsAncientConfig.CaptureCurrent();
        config.EnableFairyTaleMode = BsAncientRunOptions.ConsumePendingSelection(config.EnableFairyTaleMode);
        return config;
    }

    internal static BsAncientConfig.FileConfig ForRun(IRunState? runState)
    {
        if (runState is not RunState run)
        {
            return BsAncientConfig.CaptureCurrent();
        }

        if (SavedConfig.TryGet(run, out BsAncientConfig.FileConfig? config))
        {
            return config;
        }

        // Old multiplayer saves have no shared settings. Never fall back to peer-local values.
        config = run.Players.Count > 1 ? CreateLegacyMultiplayerDefaults() : BsAncientConfig.CaptureCurrent();
        config.EnableFairyTaleMode |= run.Players.Any(player => player.Relics.Any(relic => relic is UnnamedFairyTaleBookRelic));
        SavedConfig.Set(run, config);
        return config;
    }

    internal static BsAncientConfig.FileConfig CreateLegacyMultiplayerDefaults() => new();
}

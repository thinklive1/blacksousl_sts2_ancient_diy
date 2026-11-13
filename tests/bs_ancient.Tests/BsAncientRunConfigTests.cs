using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace BsAncient.Tests;

public sealed class BsAncientRunConfigTests : IDisposable
{
    private readonly BsAncientConfig.FileConfig _local = BsAncientConfig.CaptureCurrent();

    public BsAncientRunConfigTests() => BsAncientRunOptions.ResetForTests();

    public void Dispose()
    {
        typeof(BsAncientConfig).GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [_local]);
        BsAncientRunOptions.ResetForTests();
    }

    [Fact]
    public void CapturedSnapshotHonorsAndConsumesTheRunOverrideWithoutChangingLocalSettings()
    {
        BsAncientConfig.EnableFairyTaleMode = false;
        BsAncientRunOptions.FairyTaleModeForNextRun = true;

        Assert.True(BsAncientRunConfig.CaptureNewRun().EnableFairyTaleMode);
        Assert.False(BsAncientRunConfig.CaptureNewRun().EnableFairyTaleMode);
        Assert.False(BsAncientConfig.EnableFairyTaleMode);
    }

    [Fact]
    public void ImportedHostSnapshotCannotBeOverwrittenByClientPreparingOrLocalEdits()
    {
        RunState run = CreateRun();
        var host = new BsAncientConfig.FileConfig
        {
            EnableFairyTaleMode = true,
            EnableNegativeFairyTaleRelics = false,
            GrandGuignolInitialRelicChance = 100
        };
        BsAncientRunConfig.SavedConfig.Set(run, host);
        BsAncientConfig.EnableFairyTaleMode = false;
        BsAncientConfig.GrandGuignolInitialRelicChance = 0;

        BsAncientRunConfig.OnRunPreparing(new(run, true, DateTimeOffset.UtcNow));

        Assert.Same(host, BsAncientRunConfig.ForRun(run));
        Assert.True(BsAncientRunOptions.IsFairyTaleModeEnabled(run));
        Assert.False(BsAncientRunConfig.ForRun(run).EnableNegativeFairyTaleRelics);
        Assert.Equal(100, BsAncientRunConfig.ForRun(run).GrandGuignolInitialRelicChance);
        Assert.False(BsAncientConfig.EnableFairyTaleMode);
        Assert.Equal(0, BsAncientConfig.GrandGuignolInitialRelicChance);
        Assert.Empty(run.Modifiers);
    }

    [Fact]
    public void ClientLobbyCannotContributeItsOwnRunSettings()
    {
        StartRunLobby lobby = CreateLobby();
        BsAncientRunOptions.FairyTaleModeForNextRun = true;
        BsAncientRunConfig.OnLobbyStaging(new(lobby, true, false,
            RunSavedDataLobbyStagingReason.Committing, DateTimeOffset.UtcNow));

        Assert.False(BsAncientRunConfig.SavedConfig.Lobby.TryGet(lobby, out _));
        Assert.True(BsAncientRunOptions.ConsumePendingSelection(false));
    }

    [Fact]
    public void RitsuLobbyPayloadRoundTripPreservesEveryGameplaySetting()
    {
        StartRunLobby lobby = CreateLobby();
        BsAncientConfig.AllowShadowDemonessMyth = true;
        BsAncientConfig.AllowPeterPan = true;
        BsAncientConfig.EnableLorinaAncient = false;
        BsAncientConfig.EnableModEvents = false;
        BsAncientConfig.GrandGuignolInitialRelicChance = 73;
        BsAncientRunOptions.FairyTaleModeForNextRun = true;
        BsAncientRunConfig.OnLobbyStaging(new(lobby, true, true,
            RunSavedDataLobbyStagingReason.Committing, DateTimeOffset.UtcNow));
        Assert.True(BsAncientRunConfig.SavedConfig.Lobby.TryGet(lobby, out var expected));

        Type registry = typeof(RunSavedDataStore).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRegistry", true)!;
        string payload = (string)registry.GetMethod("BuildLobbyStagingPayload")!.Invoke(null, [lobby])!;
        RunState restored = CreateRun();
        BsAncientRunConfig.SavedConfig.Set(restored, new());
        registry.GetMethod("ImportPayloadIntoRun")!.Invoke(null, [restored, payload]);

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(BsAncientRunConfig.ForRun(restored)));
        Assert.Empty(restored.Modifiers);
    }

    [Fact]
    public void RepeatedHostCommitKeepsTheSameSnapshot()
    {
        StartRunLobby lobby = CreateLobby();
        var committing = new RunSavedDataLobbyStagingEvent(lobby, true, true,
            RunSavedDataLobbyStagingReason.Committing, DateTimeOffset.UtcNow);
        BsAncientRunOptions.FairyTaleModeForNextRun = true;
        BsAncientRunConfig.OnLobbyStaging(committing);
        Assert.True(BsAncientRunConfig.SavedConfig.Lobby.TryGet(lobby, out var first));
        BsAncientRunOptions.FairyTaleModeForNextRun = false;

        BsAncientRunConfig.OnLobbyStaging(committing);

        Assert.True(BsAncientRunConfig.SavedConfig.Lobby.TryGet(lobby, out var second));
        Assert.Same(first, second);
        Assert.True(second.EnableFairyTaleMode);
    }

    [Fact]
    public void SingleplayerPreparingCapturesOnlyOnce()
    {
        RunState run = CreateRun();
        BsAncientRunOptions.FairyTaleModeForNextRun = true;
        BsAncientRunConfig.OnRunPreparing(new(run, false, DateTimeOffset.UtcNow));
        BsAncientRunOptions.FairyTaleModeForNextRun = false;
        BsAncientRunConfig.OnRunPreparing(new(run, false, DateTimeOffset.UtcNow));

        Assert.True(BsAncientRunOptions.IsFairyTaleModeEnabled(run));
    }

    [Fact]
    public void SavingLocalSettingsDoesNotWriteTheHostSnapshot()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"bs_ancient_config_sync_{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "config.cfg");
        try
        {
            BsAncientConfig.LoadFromPath(path);
            BsAncientConfig.EnableFairyTaleMode = false;
            BsAncientConfig.GrandGuignolInitialRelicChance = 4;
            RunState run = CreateRun();
            BsAncientRunConfig.SavedConfig.Set(run, new()
            {
                EnableFairyTaleMode = true,
                GrandGuignolInitialRelicChance = 91
            });
            Assert.True(BsAncientRunConfig.ForRun(run).EnableFairyTaleMode);
            BsAncientConfig.Save();

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.False(document.RootElement.GetProperty("EnableFairyTaleMode").GetBoolean());
            Assert.Equal(4, document.RootElement.GetProperty("GrandGuignolInitialRelicChance").GetInt32());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void MissingMultiplayerSnapshotUsesSharedDefaultsInsteadOfLocalConfig()
    {
        BsAncientConfig.AllowPeterPan = true;
        BsAncientConfig.EnableFairyTaleMode = true;
        BsAncientConfig.GrandGuignolInitialRelicChance = 99;
        var defaults = BsAncientRunConfig.CreateLegacyMultiplayerDefaults();

        Assert.False(defaults.AllowPeterPan);
        Assert.False(defaults.EnableFairyTaleMode);
        Assert.Equal(30, defaults.GrandGuignolInitialRelicChance);
    }

    private static RunState CreateRun()
    {
        RunState run = (RunState)RuntimeHelpers.GetUninitializedObject(typeof(RunState));
        typeof(RunState).GetProperty(nameof(RunState.Modifiers))!.SetValue(run, Array.Empty<ModifierModel>());
        return run;
    }

    private static StartRunLobby CreateLobby()
    {
        StartRunLobby lobby = (StartRunLobby)RuntimeHelpers.GetUninitializedObject(typeof(StartRunLobby));
        PropertyInfo players = typeof(StartRunLobby).GetProperty(nameof(StartRunLobby.Players))!;
        typeof(StartRunLobby).GetField("<Players>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(lobby, Activator.CreateInstance(players.PropertyType));
        return lobby;
    }
}

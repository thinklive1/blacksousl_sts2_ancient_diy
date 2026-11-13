using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace BlackSouls.Scripts;

/// <summary>Refreshes copies only after the acquired relic has finished its own initialization.</summary>
public sealed class SuspiciousHatRelicObtainedPatch : IPatchMethod
{
    public static string PatchId => "suspicious_hat_relic_obtained";
    public static string Description => "Refresh Suspicious Hat after relic acquisition completes.";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)])];

    public static void Prefix(Player player, out IDisposable __state) =>
        __state = SuspiciousHatAcquisitionScope.Begin(player);

    public static void Postfix(ref Task<RelicModel> __result, IDisposable __state) =>
        __result = SuspiciousHatAcquisitionScope.Complete(__result, __state,
            relic => SuspiciousHatRelic.RefreshForPlayer(relic.Owner));

    public static Exception? Finalizer(Exception? __exception, IDisposable? __state)
    {
        if (__exception != null)
        {
            __state?.Dispose();
        }

        return __exception;
    }
}

/// <summary>Commits removal and melting transitions without relying on local inventory UI events.</summary>
public sealed class SuspiciousHatRelicRemovedPatch : IPatchMethod
{
    public static string PatchId => "suspicious_hat_relic_removed";
    public static string Description => "Refresh Suspicious Hat after relic removal or melting completes.";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicCmd), nameof(RelicCmd.Remove), [typeof(RelicModel)]),
         new(typeof(RelicCmd), nameof(RelicCmd.Melt), [typeof(RelicModel)])];

    public static void Postfix(RelicModel relic, ref Task __result) =>
        __result = HarmonyAsyncTaskBridge.After(__result, () => SuspiciousHatRelic.RefreshForPlayer(relic.Owner));
}

/// <summary>Restores copies after the complete remote inventory has been deserialized.</summary>
public sealed class SuspiciousHatPlayerRestoredPatch : IPatchMethod
{
    public static string PatchId => "suspicious_hat_player_restored";
    public static string Description => "Restore Suspicious Hat after multiplayer player synchronization.";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Player), nameof(Player.SyncWithSerializedPlayer), [typeof(SerializablePlayer)])];

    public static void Postfix(Player __instance) => SuspiciousHatRelic.RefreshForPlayer(__instance);
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies behavior patches for Rapunzel Power Set Amount.</summary>
public sealed class RapunzelPowerSetAmountPatch : IPatchMethod
{
    public static string PatchId => "rapunzel_power_set_amount";
    public static string Description => "Prevent visible player powers from losing stacks during Rapunzel fairy tale protection.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [
            new(
                typeof(PowerModel),
                nameof(PowerModel.SetAmount),
                [typeof(int), typeof(bool)],
                ignoreIfMissing: true)
        ];

    [HarmonyPrefix]
    public static void Prefix(PowerModel __instance, ref int amount)
    {
        RapunzelFairyTaleRelic.TryPreventDirectPowerAmountLoss(__instance, ref amount);
    }
}

/// <summary>Applies behavior patches for Rapunzel Power Remove.</summary>
public sealed class RapunzelPowerRemovePatch : IPatchMethod
{
    public static string PatchId => "rapunzel_power_remove";
    public static string Description => "Prevent visible player powers from being removed during Rapunzel fairy tale protection.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [
            new(
                typeof(PowerCmd),
                nameof(PowerCmd.Remove),
                [typeof(PowerModel)],
                ignoreIfMissing: true)
        ];

    [HarmonyPrefix]
    public static bool Prefix(PowerModel? power, ref Task __result)
    {
        if (!RapunzelFairyTaleRelic.TryPreventDirectPowerRemoval(power))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Patches;

/// <summary>Suppresses only Doom's lethal check for Shared Doom protection or an active Reaper Form.</summary>
[HarmonyPatch(typeof(DoomPower), "IsOwnerDoomed")]
internal static class SharedDoomProtectionPatch
{
    private static void Postfix(DoomPower __instance, ref bool __result)
    {
        if (__result && (__instance.Owner.GetPower<SharedDoomTransferPower>()?.Sts2Diy_DoomProtection == true
            || __instance.Owner.GetPowerAmount<ReaperFormPower>() > 0))
        {
            __result = false;
        }
    }
}

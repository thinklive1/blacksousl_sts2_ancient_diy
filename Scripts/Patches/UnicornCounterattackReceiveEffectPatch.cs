using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlackSouls.Scripts;

[HarmonyPatch]
public static class UnicornCounterattackReceiveEffectPatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDamageReceived))]
    [HarmonyPrefix]
    public static bool HookBeforeDamageReceivedPrefix(ref Task __result)
    {
        if (!UnicornRoyalCrestRelic.IsCounterattacking)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
    [HarmonyPrefix]
    public static bool HookAfterDamageReceivedPrefix(ref Task __result)
    {
        if (!UnicornRoyalCrestRelic.IsCounterattacking)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }

    [HarmonyPatch(typeof(ThornsPower), nameof(ThornsPower.BeforeDamageReceived))]
    [HarmonyPrefix]
    public static bool ThornsBeforeDamageReceivedPrefix()
    {
        return !UnicornRoyalCrestRelic.IsCounterattacking;
    }

    [HarmonyPatch(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived))]
    [HarmonyPrefix]
    public static bool PersonalHiveAfterDamageReceivedPrefix()
    {
        return !UnicornRoyalCrestRelic.IsCounterattacking;
    }
}

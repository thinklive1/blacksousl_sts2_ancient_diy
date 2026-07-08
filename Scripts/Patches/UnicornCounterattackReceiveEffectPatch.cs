using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Unicorn Counterattack Receive Effect.</summary>
public static class UnicornCounterattackReceiveEffectPatch
{
    public static bool HookBeforeDamageReceivedPrefix(ref Task __result)
    {
        if (!UnicornRoyalCrestRelic.IsCounterattacking)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }

    public static bool HookAfterDamageReceivedPrefix(ref Task __result)
    {
        if (!UnicornRoyalCrestRelic.IsCounterattacking)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }

    public static bool ThornsBeforeDamageReceivedPrefix()
    {
        return !UnicornRoyalCrestRelic.IsCounterattacking;
    }

    public static bool PersonalHiveAfterDamageReceivedPrefix()
    {
        return !UnicornRoyalCrestRelic.IsCounterattacking;
    }
}

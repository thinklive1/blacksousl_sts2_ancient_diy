using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using System.Reflection;

namespace BlackSouls.Scripts.Patches;

[HarmonyPatch]
public static class UnlockEnchantmentCanPlayPatch
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]);
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel? preventer)
    {
        if (__result || __instance.Enchantment is not UnlockEnchantment unlock || !unlock.IsUnlockAvailable(__instance))
        {
            return;
        }

        reason &= ~UnplayableReason.BlockedByCardLogic;
        reason &= ~UnplayableReason.EnergyCostTooHigh;
        reason &= ~UnplayableReason.StarCostTooHigh;

        if (reason == UnplayableReason.None)
        {
            preventer = null;
            __result = true;
        }
    }
}

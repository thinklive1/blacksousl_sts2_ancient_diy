using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Sts2Diy.Scripts.Patches;

/// <summary>Retires Haunt from playable pools without breaking existing saves or model lookup.</summary>
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class SoulPhantomPatches
{
    private static void Postfix(CardPoolModel __instance, ref IEnumerable<CardModel> __result)
    {
        if (__instance is NecrobinderCardPool) __result = __result.Where(card => card is not Haunt);
    }
}

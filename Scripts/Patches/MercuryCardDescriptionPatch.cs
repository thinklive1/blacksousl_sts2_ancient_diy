using BlackSouls.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), [typeof(PileType), typeof(Creature)])]
public static class MercuryCardDescriptionPatch
{
    [HarmonyPriority(Priority.High)]
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is MercuryCard mercury && !string.IsNullOrWhiteSpace(mercury.BlackSouls_CopiedDescription))
        {
            __result = mercury.BlackSouls_CopiedDescription;
        }
    }
}

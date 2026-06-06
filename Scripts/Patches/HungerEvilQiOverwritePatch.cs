using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlackSouls.Scripts;

[HarmonyPatch]
public static class HungerEvilQiOverwritePatch
{
    [HarmonyPatch(typeof(HungerPower), "Afflict")]
    [HarmonyPrefix]
    public static bool AfflictPrefix(HungerPower __instance, CardModel card, ref Task __result)
    {
        if (card.Affliction is not EvilQiAffliction)
        {
            return true;
        }

        __result = AfflictDevoured(__instance, card);
        return false;
    }

    [HarmonyPatch(typeof(HungerPower), nameof(HungerPower.AfterCardEnteredCombat))]
    [HarmonyPrefix]
    public static bool AfterCardEnteredCombatPrefix(HungerPower __instance, CardModel card, ref Task __result)
    {
        if (card.Affliction is not EvilQiAffliction)
        {
            return true;
        }

        __result = AfflictDevoured(__instance, card);
        return false;
    }

    private static async Task AfflictDevoured(HungerPower power, CardModel card)
    {
        Devoured? devoured = await CardCmd.Afflict<Devoured>(card, power.Amount);
        if (devoured != null && !card.Keywords.Contains(CardKeyword.Exhaust))
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
            devoured.AppliedExhaust = true;
        }
    }
}

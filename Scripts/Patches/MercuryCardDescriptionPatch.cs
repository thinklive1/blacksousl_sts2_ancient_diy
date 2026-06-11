using BlackSouls.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

namespace BlackSouls.Scripts.Patches;

public static class MercuryCardDescriptionPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(CardModel))
            .Where(method => method.Name == nameof(CardModel.GetDescriptionForPile));
    }

    public static void Postfix(CardModel __instance, ref string __result)
    {
        ApplyCopiedDescription(__instance, ref __result);
    }

    private static void ApplyCopiedDescription(CardModel card, ref string description)
    {
        if (card is MercuryCard mercury && !string.IsNullOrWhiteSpace(mercury.BlackSouls_CopiedDescription))
        {
            description = mercury.BlackSouls_CopiedDescription;
        }
    }
}

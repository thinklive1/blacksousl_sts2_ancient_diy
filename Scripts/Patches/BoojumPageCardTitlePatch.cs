using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace BlackSouls.Scripts.Patches;

/// <summary>Restricts Boojum's page to visual card-title erasure during an active combat.</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class BoojumPageCardTitlePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not DeprecatedCard && IsBoojumPageActive())
        {
            __result = string.Empty;
        }
    }

    private static bool IsBoojumPageActive()
    {
        ICombatState? combatState = CombatManager.Instance?.DebugOnlyGetState();
        return CombatManager.Instance?.IsInProgress == true
            && combatState?.Players.Any(player => player.GetRelic<BoojumPageRelic>() != null) == true;
    }
}

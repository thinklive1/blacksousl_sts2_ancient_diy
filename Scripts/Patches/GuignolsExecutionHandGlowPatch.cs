using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace BlackSouls.Scripts.Patches;

/// <summary>Keeps execution-ready Attack cards glowing red even when they cannot currently be played.</summary>
[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
public static class GuignolsExecutionHandGlowPatch
{
    [HarmonyPostfix]
    public static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        NCard? cardNode = __instance.CardNode;
        if (cardNode == null || !GuignolsApplauseExecutionPower.ShouldHighlightExecution(cardNode.Model))
        {
            return;
        }

        cardNode.CardHighlight.Modulate = NCardHighlight.red;
        cardNode.CardHighlight.AnimShow();
    }
}

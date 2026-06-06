using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(ChainsOfBindingPower), nameof(ChainsOfBindingPower.AfterCardDrawn))]
public static class ChainsOfBindingEvilQiOverwritePatch
{
    public static bool Prefix(
        ChainsOfBindingPower __instance,
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw,
        ref Task __result)
    {
        if (card.Affliction is not EvilQiAffliction)
        {
            return true;
        }

        __result = AfflictBoundIfAllowed(__instance, card);
        return false;
    }

    private static async Task AfflictBoundIfAllowed(ChainsOfBindingPower power, CardModel card)
    {
        if (card.Owner != power.Owner.Player || power.CombatState.CurrentSide != power.Owner.Side)
        {
            return;
        }

        AfflictionModel bound = ModelDb.Affliction<Bound>();
        if (!bound.CanAfflictCardType(card.Type)
            || (card.Keywords.Contains(CardKeyword.Unplayable) && !bound.CanAfflictUnplayableCards))
        {
            return;
        }

        int boundThisTurn = CombatManager.Instance.History.Entries
            .OfType<CardAfflictedEntry>()
            .Count(entry => entry.HappenedThisTurn(power.CombatState)
                && entry.Actor == power.Owner
                && entry.Affliction is Bound);

        if (boundThisTurn < power.Amount)
        {
            await CardCmd.AfflictAndPreview<Bound>([card], power.Amount, CardPreviewStyle.None);
        }
    }
}

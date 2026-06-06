using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Afflict), [typeof(AfflictionModel), typeof(CardModel), typeof(decimal)])]
public static class EvilQiAfflictionOverwritePatch
{
    public static bool Prefix(AfflictionModel affliction, CardModel card, decimal amount, ref Task<AfflictionModel?> __result)
    {
        if (card.Affliction is not EvilQiAffliction || affliction is EvilQiAffliction)
        {
            return true;
        }

        if (CombatManager.Instance.IsOverOrEnding)
        {
            CardPile? pile = card.Pile;
            if (pile != null && pile.IsCombatPile)
            {
                __result = Task.FromResult<AfflictionModel?>(null);
                return false;
            }
        }

        affliction.AssertMutable();
        CombatState? combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        if (combatState == null || !Hook.ShouldAfflict(combatState, card, affliction))
        {
            __result = Task.FromResult<AfflictionModel?>(null);
            return false;
        }

        card.ClearAfflictionInternal();
        if (!affliction.CanAfflict(card))
        {
            __result = Task.FromResult<AfflictionModel?>(null);
            return false;
        }

        card.AfflictInternal(affliction, amount);
        affliction.AfterApplied();
        CombatManager.Instance.History.CardAfflicted(combatState, card, affliction);
        __result = Task.FromResult<AfflictionModel?>(card.Affliction);
        return false;
    }
}

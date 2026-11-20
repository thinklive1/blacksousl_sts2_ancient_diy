using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Patches;

/// <summary>
/// Put Harmony patches for vanilla cards here.
/// Keep each gameplay change in its own patch class once this file grows.
/// </summary>
public static class VanillaCardPatches
{
    /// <summary>Reworks the vanilla Shroud card into Calamity Raiment.</summary>
    [HarmonyPatch(typeof(CardModel), "get_Rarity")]
    private static class ShroudRarityPatch
    {
        private static bool Prefix(CardModel __instance, ref CardRarity __result)
        {
            if (__instance is not Shroud)
            {
                return true;
            }

            __result = CardRarity.Rare;
            return false;
        }
    }

    [HarmonyPatch(typeof(Shroud), "OnPlay")]
    private static class ShroudPlayPatch
    {
        private static bool Prefix(Shroud __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            __result = PowerCmd.Apply<CalamityRaimentPower>(choiceContext, __instance.Owner.Creature, 1m, __instance.Owner.Creature, __instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Shroud), "OnUpgrade")]
    private static class ShroudUpgradePatch
    {
        private static bool Prefix(Shroud __instance)
        {
            // The original cost is already 1. Do not mutate the canonical card during construction.
            return false;
        }
    }

    /// <summary>Changes Negative Pulse values while retaining the vanilla card identity.</summary>
    [HarmonyPatch(typeof(NegativePulse), "get_CanonicalVars")]
    private static class NegativePulseBaseValuesPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            DynamicVar[] vars = __result.ToArray();
            foreach (DynamicVar variable in vars)
            {
                if (variable is BlockVar) variable.BaseValue = 7m;
                else if (variable is PowerVar<DoomPower>) variable.BaseValue = 11m;
            }
            __result = vars;
        }
    }

    [HarmonyPatch(typeof(NegativePulse), "OnUpgrade")]
    private static class NegativePulseUpgradePatch
    {
        private static void Postfix(NegativePulse __instance)
        {
            // Vanilla adds 1 Block and 4 Doom; only one extra Block is needed for 9/15.
            __instance.DynamicVars.Block.UpgradeValueBy(1m);
        }
    }

    [HarmonyPatch(typeof(NegativePulse), "OnPlay")]
    private static class NegativePulsePlayPatch
    {
        private static bool Prefix(NegativePulse __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            __result = ApplyToAllNonOsty(__instance, choiceContext, cardPlay);
            return false;
        }

        private static async Task ApplyToAllNonOsty(NegativePulse card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            decimal block = card.DynamicVars.Block.BaseValue;
            decimal doom = card.DynamicVars.Doom.BaseValue;
            await CreatureCmd.GainBlock(card.Owner.Creature, block, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move, cardPlay);

            ICombatState? combatState = card.CombatState;
            if (combatState == null)
            {
                return;
            }

            IEnumerable<Creature> targets = combatState.Players
                .Select(player => player.Creature)
                .Concat(combatState.HittableEnemies)
                .Where(creature => creature != card.Owner.Osty && creature.IsAlive);

            await DoomRework.ApplyMultiple(choiceContext, targets, doom, card.Owner.Creature, card);
        }
    }
}

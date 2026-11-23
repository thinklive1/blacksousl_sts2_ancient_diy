using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Sts2Diy.Scripts.Patches;

/// <summary>Replaces Blight Strike's Doom application with damage based on the owner's Doom.</summary>
internal static class BlightStrikePatches
{
    [HarmonyPatch(typeof(BlightStrike), "get_CanonicalVars")]
    private static class BaseValuesPatch
    {
        private static bool Prefix(ref IEnumerable<DynamicVar> __result)
        {
            __result = [
                new CalculationBaseVar(9m),
                new ExtraDamageVar(1m),
                new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                    static (card, _) => Math.Max(0, card.Owner.Creature.GetPowerAmount<DoomPower>()) / 2)
            ];
            return false;
        }
    }

    [HarmonyPatch(typeof(BlightStrike), "OnUpgrade")]
    private static class UpgradePatch
    {
        private static bool Prefix(BlightStrike __instance)
        {
            __instance.DynamicVars.CalculationBase.UpgradeValueBy(3m);
            return false;
        }
    }

    [HarmonyPatch(typeof(BlightStrike), "OnPlay")]
    private static class PlayPatch
    {
        private static bool Prefix(BlightStrike __instance, PlayerChoiceContext choiceContext,
            CardPlay cardPlay, ref Task __result)
        {
            __result = Play(__instance, choiceContext, cardPlay);
            return false;
        }

        private static async Task Play(BlightStrike card, PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            // Use the same calculated variable for native damage preview and actual execution.
            await DamageCmd.Attack(card.DynamicVars.CalculatedDamage)
                .FromCard(card, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);
        }
    }
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Sts2Diy.Scripts.Patches;

/// <summary>Replaces Soul scaling damage with Soul generation, retaining the vanilla card identity.</summary>
public static class SoulStormPatches
{
    [HarmonyPatch(typeof(SoulStorm), "get_CanonicalVars")]
    private static class Values
    {
        private static bool Prefix(ref IEnumerable<DynamicVar> __result)
        {
            __result = [new DamageVar(6m, ValueProp.Move)];
            return false;
        }
    }

    [HarmonyPatch(typeof(SoulStorm), "OnUpgrade")]
    private static class Upgrade
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(SoulStorm), "OnPlay")]
    private static class Play
    {
        private static bool Prefix(SoulStorm __instance, PlayerChoiceContext choiceContext,
            CardPlay cardPlay, ref Task __result)
        {
            __result = Execute(__instance, choiceContext, cardPlay);
            return false;
        }
    }

    private static async Task Execute(SoulStorm card, PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        if (card.Owner.Creature.IsDead || CombatManager.Instance.IsOverOrEnding) return;
        // Freeze the count before damage reactions can change the Exhaust Pile.
        int souls = card.Owner.PlayerCombatState?.ExhaustPile.Cards.Count(c => c is Soul) ?? 0;
        await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).FromCard(card, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(context);
        for (int i = 0; i < souls; i++)
        {
            if (card.Owner.Creature.IsDead || CombatManager.Instance.IsOverOrEnding || card.CombatState == null) return;
            CardModel soul = card.CombatState.CreateCard<Soul>(card.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(soul,
                card.IsUpgraded ? PileType.Draw : PileType.Discard, card.Owner, CardPilePosition.Random);
        }
    }
}

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using BlackSouls.Scripts.Services;

namespace BlackSouls.Scripts.Patches;

/// <summary>Runs Croquet Mallet behavior through global combat hooks.</summary>
public sealed class CroquetMalletAfterCardDrawnPatch : IPatchMethod
{
    public static string PatchId => "croquet_mallet_recenter_after_draw";
    public static string Description => "Keep the Croquet Mallet centered after all draw sources.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.AfterCardDrawn))];

    public static void Postfix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw,
        ref Task __result) =>
        __result = Continue(__result, card);

    private static async Task Continue(Task original, CardModel card)
    {
        await original;
        CroquetMalletCombatService.RecenterHand(card.Owner);
    }
}

/// <summary>Snapshots an opposite-side attack before the triggering card leaves the hand.</summary>
public sealed class CroquetMalletBeforeCardPlayWrapperPatch : IPatchMethod
{
    public static string PatchId => "croquet_mallet_snapshot_before_play_wrapper";
    public static string Description => "Capture Croquet Mallet opposite-side attacks before CardModel moves the card out of hand.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [
            new(
                typeof(CardModel),
                nameof(CardModel.OnPlayWrapper),
                [typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool)])
        ];

    public static void Prefix(CardModel __instance, bool isAutoPlay) =>
        CroquetMalletCombatService.QueueTrigger(__instance, isAutoPlay);
}

/// <summary>Auto-plays the captured opposite-side attack after the triggering attack resolves.</summary>
public sealed class CroquetMalletAfterCardPlayedPatch : IPatchMethod
{
    public static string PatchId => "croquet_mallet_autoplay_after_play";
    public static string Description => "Resolve Croquet Mallet opposite-side attack replays.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.AfterCardPlayed))];

    public static void Postfix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) =>
        __result = Continue(__result, choiceContext, cardPlay);

    private static async Task Continue(Task original, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await original;
        await CroquetMalletCombatService.ResolveTrigger(choiceContext, cardPlay);
    }
}

/// <summary>Resets each Croquet Mallet's per-turn limit at the start of its owner's turn.</summary>
public sealed class CroquetMalletAfterPlayerTurnStartPatch : IPatchMethod
{
    public static string PatchId => "croquet_mallet_reset_turn_limit";
    public static string Description => "Reset Croquet Mallet trigger limits each player turn.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))];

    public static void Postfix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player,
        ref Task __result) =>
        __result = Continue(__result, player);

    private static async Task Continue(Task original, Player player)
    {
        await original;
        CroquetMalletCombatService.ResetTurn(player);
    }
}

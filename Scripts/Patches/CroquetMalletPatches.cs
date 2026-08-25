using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using BlackSouls.Scripts.Services;

namespace BlackSouls.Scripts.Patches;

/// <summary>Runs draw-complete behavior shared by poker hands and the Croquet Mallet.</summary>
public sealed class SharedAfterCardDrawnPatch : IPatchMethod
{
    public static string PatchId => "shared_after_card_drawn";
    public static string Description => "Sort poker hands and keep the Croquet Mallet centered after draws.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(Hook),
            nameof(Hook.AfterCardDrawn),
            [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)],
            ignoreIfMissing: true)];

    public static void Postfix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw,
        ref Task __result) =>
        __result = Continue(__result, combatState, card);

    private static async Task Continue(Task original, ICombatState combatState, CardModel card)
    {
        await original;

        if (BalatroTrainingCombatService.IsActive(combatState)
            && card.Owner is Player player
            && card.Pile?.Type == PileType.Hand)
        {
            BalatroTrainingCombatService.SortPokerHand(player);
        }

        CroquetMalletCombatService.RecenterHand(card.Owner);
    }
}

/// <summary>Coordinates Judgment Order delays and Croquet Mallet snapshots before a card resolves.</summary>
public sealed class SharedCardPlayWrapperPatch : IPatchMethod
{
    public static string PatchId => "shared_card_play_wrapper";
    public static string Description => "Delay Judgment Order attacks or capture Croquet Mallet triggers before card resolution.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [
            new(
                typeof(CardModel),
                nameof(CardModel.OnPlayWrapper),
                [typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool)],
                ignoreIfMissing: true)
        ];

    public static bool Prefix(
        CardModel __instance,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        bool skipCardPileVisuals,
        ref Task __result)
    {
        if (JudgmentOrderCombatService.ShouldDefer(__instance))
        {
            __result = JudgmentOrderCombatService.DeferAttack(
                __instance,
                choiceContext,
                target,
                isAutoPlay,
                skipCardPileVisuals);
            return false;
        }

        CroquetMalletCombatService.QueueTrigger(__instance, isAutoPlay);
        return true;
    }
}

/// <summary>Auto-plays the captured opposite-side attack after the triggering attack resolves.</summary>
public sealed class CroquetMalletAfterCardPlayedPatch : IPatchMethod
{
    public static string PatchId => "croquet_mallet_autoplay_after_play";
    public static string Description => "Resolve Croquet Mallet opposite-side attack replays.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(Hook),
            nameof(Hook.AfterCardPlayed),
            [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardPlay)],
            ignoreIfMissing: true)];

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

/// <summary>Runs turn-start behavior shared by poker hands and the Croquet Mallet.</summary>
public sealed class SharedAfterPlayerTurnStartPatch : IPatchMethod
{
    public static string PatchId => "shared_after_player_turn_start";
    public static string Description => "Sort poker hands and reset Croquet Mallet trigger limits each player turn.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(Hook),
            nameof(Hook.AfterPlayerTurnStart),
            [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(Player)],
            ignoreIfMissing: true)];

    public static void Postfix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player,
        ref Task __result) =>
        __result = Continue(__result, combatState, player);

    private static async Task Continue(Task original, ICombatState combatState, Player player)
    {
        await original;

        if (BalatroTrainingCombatService.IsActive(combatState))
        {
            BalatroTrainingCombatService.SortPokerHand(player);
        }

        CroquetMalletCombatService.ResetTurn(player);
    }
}

/// <summary>Clears cached Croquet Mallet plays even when a card play or combat exits early.</summary>
public sealed class CroquetMalletAfterCombatEndPatch : IPatchMethod
{
    public static string PatchId => "croquet_mallet_clear_after_combat";
    public static string Description => "Discard Croquet Mallet cached card references after combat.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(Hook),
            nameof(Hook.AfterCombatEnd),
            [typeof(IRunState), typeof(ICombatState), typeof(CombatRoom)],
            ignoreIfMissing: true)];

    public static void Postfix(
        IRunState runState,
        ICombatState? combatState,
        CombatRoom room,
        ref Task __result) =>
        __result = Continue(__result);

    private static async Task Continue(Task original)
    {
        try
        {
            await original;
        }
        finally
        {
            CroquetMalletCombatService.ResetCombat();
        }
    }
}

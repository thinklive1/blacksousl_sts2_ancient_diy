using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides three combat-only right-click discard-and-draw actions.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsRedDeckRelic : ModRelicTemplate
{
    public const int DiscardsPerCombat = 3;
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsRedDeckRelic.png";

    private int _remainingDiscards;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int DisplayAmount => _remainingDiscards;

    [SavedProperty]
    public int BlackSouls_RemainingDiscards
    {
        get => _remainingDiscards;
        set
        {
            AssertMutable();
            _remainingDiscards = Math.Clamp(value, 0, DiscardsPerCombat);
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState)
    {
        // Lorina awards this relic directly; keep it out of random event relic pools.
        return false;
    }

    public override Task BeforeCombatStart()
    {
        BlackSouls_RemainingDiscards = DiscardsPerCombat;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    internal bool CanDiscardAndDraw(CardModel card)
    {
        return _remainingDiscards > 0
            && card.Owner == Owner
            && card.IsInCombat
            && card.Pile?.Type == PileType.Hand
            && Owner.PlayerCombatState?.Phase == PlayerTurnPhase.Play
            && Owner.Creature.IsAlive
            && card.CombatState == Owner.Creature.CombatState
            && CombatManager.Instance.IsInProgress
            && !CombatManager.Instance.IsOverOrEnding;
    }

    internal async Task TryDiscardAndDraw(CardModel card, PlayerChoiceContext? choiceContext)
    {
        // Never substitute an unsynchronized choice context for a queued right-click action.
        if (choiceContext is not GameActionPlayerChoiceContext queuedContext
            || queuedContext.Action.OwnerId != Owner.NetId)
        {
            Entry.Logger.Warn($"[RedDeck] Rejected discard without the owner's queued choice context. Player={Owner.NetId}");
            return;
        }

        if (!CanDiscardAndDraw(card))
        {
            Entry.Logger.Info($"[RedDeck] Skipped stale discard. Player={Owner.NetId} Card={card.Id} Phase={Owner.PlayerCombatState?.Phase} Remaining={_remainingDiscards}");
            return;
        }

        BlackSouls_RemainingDiscards--;
        InvokeDisplayAmountChanged();
        Flash();
        Entry.Logger.Info($"[RedDeck] Discard started. Player={Owner.NetId} Card={card.Id} Remaining={_remainingDiscards}");
        try
        {
            await CardCmd.DiscardAndDraw(queuedContext, [card], 1);
            Entry.Logger.Info($"[RedDeck] Discard finished. Player={Owner.NetId} Remaining={_remainingDiscards} Hand={Owner.PlayerCombatState?.Hand.Cards.Count} Draw={Owner.PlayerCombatState?.DrawPile.Cards.Count} Discard={Owner.PlayerCombatState?.DiscardPile.Cards.Count}");
        }
        catch (Exception exception)
        {
            // A partially executed discard must not be retried or locally rolled back.
            Entry.Logger.Warn($"[RedDeck] Discard failed. Player={Owner.NetId} Card={card.Id} Remaining={_remainingDiscards} Error={exception}");
            throw;
        }
    }
}

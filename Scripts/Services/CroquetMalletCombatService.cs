using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts.Services;

/// <summary>Coordinates Croquet Mallet hand placement and opposite-side attack replays.</summary>
internal static class CroquetMalletCombatService
{
    private static readonly Dictionary<CardModel, List<PendingTrigger>> PendingTriggers = [];

    internal static void RecenterHand(Player? player)
    {
        if (player?.Creature.CombatState == null)
        {
            return;
        }

        List<QueenOfHeartsCroquetMalletCard> mallets = PileType.Hand.GetPile(player).Cards
            .OfType<QueenOfHeartsCroquetMalletCard>()
            .ToList();

        foreach (QueenOfHeartsCroquetMalletCard mallet in mallets)
        {
            mallet.CenterInHand();
        }
    }

    internal static void ResetTurn(Player player)
    {
        if (player.Creature.CombatState == null)
        {
            return;
        }

        foreach (QueenOfHeartsCroquetMalletCard mallet in GetCombatMallets(player))
        {
            mallet.ResetTriggersForTurn();
        }

        RecenterHand(player);
    }

    internal static void QueueTrigger(CardModel playedCard, bool isAutoPlay)
    {
        PendingTriggers.Remove(playedCard);

        Player? player = playedCard.Owner;
        if (isAutoPlay
            || playedCard.Type != CardType.Attack
            || player?.Creature.CombatState == null)
        {
            return;
        }

        List<CardModel> handCards = PileType.Hand.GetPile(player).Cards.ToList();
        int playedCardIndex = handCards.IndexOf(playedCard);
        if (playedCardIndex < 0)
        {
            Entry.Logger.Info($"Croquet Mallet skipped {playedCard.Id.Entry}: the card was not in hand during the play snapshot.");
            return;
        }

        bool hasMalletInHand = handCards.OfType<QueenOfHeartsCroquetMalletCard>().Any();
        List<PendingTrigger> triggers = [];
        foreach (QueenOfHeartsCroquetMalletCard mallet in handCards.OfType<QueenOfHeartsCroquetMalletCard>())
        {
            if (!mallet.CanTrigger)
            {
                continue;
            }

            int malletIndex = handCards.IndexOf(mallet);
            if (malletIndex < 0 || malletIndex == playedCardIndex)
            {
                continue;
            }

            CardModel? oppositeAttack = playedCardIndex < malletIndex
                ? handCards.Skip(malletIndex + 1).FirstOrDefault(IsEligibleAutoPlayAttack)
                : handCards.Take(malletIndex).Reverse().FirstOrDefault(IsEligibleAutoPlayAttack);

            if (oppositeAttack != null)
            {
                triggers.Add(new PendingTrigger(mallet, oppositeAttack));
            }
        }

        if (triggers.Count > 0)
        {
            PendingTriggers[playedCard] = triggers;
            Entry.Logger.Info($"Croquet Mallet cached {triggers.Count} opposite-side attack trigger(s) for {playedCard.Id.Entry}.");
        }
        else if (hasMalletInHand)
        {
            Entry.Logger.Info($"Croquet Mallet found no opposite-side attack for {playedCard.Id.Entry}.");
        }
    }

    internal static async Task ResolveTrigger(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!PendingTriggers.Remove(cardPlay.Card, out List<PendingTrigger>? triggers))
        {
            RecenterHand(cardPlay.Card.Owner);
            return;
        }

        try
        {
            foreach (PendingTrigger trigger in triggers)
            {
                QueenOfHeartsCroquetMalletCard mallet = trigger.Mallet;
                CardModel oppositeAttack = trigger.OppositeAttack;
                if (!mallet.CanTrigger
                    || mallet.Pile?.Type != PileType.Hand
                    || oppositeAttack.Pile?.Type != PileType.Hand
                    || oppositeAttack.Owner?.Creature.IsDead != false)
                {
                    Entry.Logger.Info($"Croquet Mallet skipped cached target {oppositeAttack.Id.Entry}: it was no longer playable from hand.");
                    continue;
                }

                mallet.MarkTriggered();
                Entry.Logger.Info($"Croquet Mallet auto-playing {oppositeAttack.Id.Entry}.");
                await CardCmd.AutoPlay(choiceContext, oppositeAttack, GetAutoPlayTarget(oppositeAttack));
            }
        }
        finally
        {
            RecenterHand(cardPlay.Card.Owner);
        }
    }

    internal static void ResetCombat()
    {
        PendingTriggers.Clear();
    }

    private static IEnumerable<QueenOfHeartsCroquetMalletCard> GetCombatMallets(Player player)
    {
        return PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Hand.GetPile(player).Cards)
            .Concat(PileType.Discard.GetPile(player).Cards)
            .Concat(PileType.Exhaust.GetPile(player).Cards)
            .OfType<QueenOfHeartsCroquetMalletCard>();
    }

    private static Creature? GetAutoPlayTarget(CardModel card)
    {
        if (card.TargetType is not (TargetType.AnyEnemy or TargetType.RandomEnemy))
        {
            return null;
        }

        return card.Owner?.Creature.CombatState?.HittableEnemies
            .FirstOrDefault(creature => creature.IsAlive);
    }

    private static bool IsEligibleAutoPlayAttack(CardModel card)
    {
        return card.Type == CardType.Attack
            && !card.Keywords.Contains(CardKeyword.Unplayable)
            && !HasAdditionalPlayCondition(card);
    }

    private static bool HasAdditionalPlayCondition(CardModel card)
    {
        MethodInfo? getter = card.GetType().GetMethod(
            "get_IsPlayable",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return getter?.DeclaringType != typeof(CardModel);
    }

    private sealed record PendingTrigger(
        QueenOfHeartsCroquetMalletCard Mallet,
        CardModel OppositeAttack);
}

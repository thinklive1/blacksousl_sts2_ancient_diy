using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using BlackSouls.Scripts;

namespace BlackSouls.Scripts.Services;

/// <summary>Queues Attack cards deferred by the Queen's Judgment Order.</summary>
internal static class JudgmentOrderCombatService
{
    private static readonly Dictionary<Player, Queue<PendingAttack>> DelayedAttacks = [];
    private static readonly AsyncLocal<HashSet<CardModel>?> ImmediatePlays = new();

    internal static bool ShouldDefer(CardModel card)
    {
        return card.Type == CardType.Attack
            && card.Owner?.GetRelic<QueenOfHeartsJudgmentOrderRelic>() != null
            && card.Owner.Creature.CombatState != null
            && !CombatManager.Instance.IsOverOrEnding
            && !IsImmediatePlay(card);
    }

    internal static async Task DeferAttack(
        CardModel card,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        bool skipCardPileVisuals)
    {
        Player owner = card.Owner;
        choiceContext.PushModel(card);
        try
        {
            await CombatManager.Instance.WaitForUnpause();
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }

            // Manual plays have already paid their resources. Automatic plays have already
            // entered the play pile through CardCmd.AutoPlay. In both cases, keep the card in
            // discard until its delayed play.
            if (!isAutoPlay)
            {
                await CardPileCmd.AddDuringManualCardPlay(card);
            }
            else if (card.Pile?.Type != PileType.Play)
            {
                await CardPileCmd.Add(card, PileType.Play, skipVisuals: skipCardPileVisuals);
            }

            if (card.Pile?.Type == PileType.Play)
            {
                await CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Bottom, null, skipCardPileVisuals);
            }

            if (card.Pile?.Type != PileType.Discard)
            {
                return;
            }

            lock (DelayedAttacks)
            {
                if (!DelayedAttacks.TryGetValue(owner, out Queue<PendingAttack>? attacks))
                {
                    attacks = new Queue<PendingAttack>();
                    DelayedAttacks[owner] = attacks;
                }

                attacks.Enqueue(new PendingAttack(card, target));
            }

            owner.GetRelic<QueenOfHeartsJudgmentOrderRelic>()?.Flash();
            if (card.EnergyCost.AfterCardPlayedCleanup())
            {
                card.InvokeEnergyCostChanged();
            }

            await CombatManager.Instance.CheckForEmptyHand(choiceContext, owner);
        }
        finally
        {
            choiceContext.PopModel(card);
        }
    }

    internal static async Task ResolveDelayedAttacks(PlayerChoiceContext choiceContext, Player player)
    {
        Queue<PendingAttack>? attacks;
        lock (DelayedAttacks)
        {
            if (!DelayedAttacks.Remove(player, out attacks))
            {
                return;
            }
        }

        while (attacks.Count > 0)
        {
            PendingAttack pending = attacks.Dequeue();
            CardModel card = pending.Card;
            if (card.Owner != player
                || player.Creature.IsDead
                || card.Pile == null
                || card.Owner.Creature.CombatState == null
                || !card.Owner.Creature.CombatState.ContainsCard(card))
            {
                continue;
            }

            Creature? target = pending.Target is { IsAlive: true } ? pending.Target : null;
            using (AllowImmediatePlay(card))
            {
                await CardCmd.AutoPlay(
                    choiceContext,
                    card,
                    target,
                    skipXCapture: true);
            }
        }
    }

    internal static IDisposable AllowImmediatePlay(CardModel card)
    {
        HashSet<CardModel> plays = ImmediatePlays.Value ??= [];
        plays.Add(card);
        return new ImmediatePlayScope(plays, card);
    }

    internal static void Reset(Player player)
    {
        lock (DelayedAttacks)
        {
            DelayedAttacks.Remove(player);
        }
    }

    private sealed record PendingAttack(CardModel Card, Creature? Target);

    private static bool IsImmediatePlay(CardModel card)
    {
        return ImmediatePlays.Value?.Contains(card) == true;
    }

    private sealed class ImmediatePlayScope : IDisposable
    {
        private readonly HashSet<CardModel> _plays;
        private readonly CardModel _card;

        internal ImmediatePlayScope(HashSet<CardModel> plays, CardModel card)
        {
            _plays = plays;
            _card = card;
        }

        public void Dispose()
        {
            _plays.Remove(_card);
            if (_plays.Count == 0)
            {
                ImmediatePlays.Value = null;
            }
        }
    }
}

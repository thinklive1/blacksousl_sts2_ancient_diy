using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Blacksouls.Scripts;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public class EncoreNextTurnPower : ModPowerTemplate
{
    private const int ManagerAmount = 1;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => false;

    public override int DisplayAmount => NextTurnPlayCount;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png"
    );

    private int NextTurnPlayCount => PendingCards
        .Where(entry => IsActiveEncoreCard(entry.Key))
        .Sum(entry => Math.Max(0, entry.Value));

    private Dictionary<CardModel, int> PendingCards => GetInternalData<Data>().PendingCards;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay
            || cardPlay.Card.Owner != Owner.Player
            || !cardPlay.Card.Keywords.Contains(MyKeywords.Encore)
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        await ScheduleNextTurnPlay(cardPlay);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        List<KeyValuePair<CardModel, int>> cardsToPlay = PendingCards
            .Where(entry => IsActiveEncoreCard(entry.Key) && entry.Value > 0)
            .ToList();

        PendingCards.Clear();
        await SyncVisualPower(0);

        foreach ((CardModel card, int playCount) in cardsToPlay)
        {
            for (int i = 0; i < playCount; i++)
            {
                if (Owner.IsDead || CombatManager.Instance.IsOverOrEnding || !IsActiveEncoreCard(card))
                {
                    await SyncAmount();
                    return;
                }

                Creature? target = GetAutoPlayTarget(card);
                if (card.TargetType.IsSingleTarget() && card.TargetType != TargetType.Self && target == null)
                {
                    continue;
                }

                await CardCmd.AutoPlay(choiceContext, card, target);
            }
        }

        await SyncAmount();
    }

    public static async Task AddTrackedCard(Player player, CardModel card, int priorPlays)
    {
        EncoreNextTurnPower? manager = await EnsureManager(player);
        if (manager == null)
        {
            return;
        }

        manager.PruneInactiveCards();
        await manager.SyncAmount();
    }

    private async Task ScheduleNextTurnPlay(CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        int playCount = GetCombatPlayCount(card);
        if (!CombatManager.Instance.History.CardPlaysFinished.Any(entry => ReferenceEquals(entry.CardPlay, cardPlay)))
        {
            playCount++;
        }

        playCount = Math.Max(1, playCount);
        PendingCards[card] = Math.Max(PendingCards.GetValueOrDefault(card), playCount);
        await SyncAmount();
    }

    private async Task SyncAmount()
    {
        PruneInactiveCards();
        int nextTurnPlayCount = NextTurnPlayCount;
        int amount = ManagerAmount + nextTurnPlayCount;
        if (Amount != amount)
        {
            await PowerCmd.SetAmount<EncoreNextTurnPower>(Owner, amount, Owner, null);
        }

        await SyncVisualPower(nextTurnPlayCount);
    }

    private async Task SyncVisualPower(int nextTurnPlayCount)
    {
        EncoreNextTurnVisualPower? visual = Owner.GetPower<EncoreNextTurnVisualPower>();
        if (nextTurnPlayCount > 0)
        {
            if (visual?.Amount != nextTurnPlayCount)
            {
                await PowerCmd.SetAmount<EncoreNextTurnVisualPower>(Owner, nextTurnPlayCount, Owner, null);
            }
        }
        else if (visual != null)
        {
            await PowerCmd.Remove(visual);
        }
    }

    private static async Task<EncoreNextTurnPower?> EnsureManager(Player player)
    {
        EncoreNextTurnPower? manager = player.Creature.GetPower<EncoreNextTurnPower>();
        return manager ?? await PowerCmd.Apply<EncoreNextTurnPower>(
            player.Creature,
            ManagerAmount,
            player.Creature,
            null,
            silent: true);
    }

    private void PruneInactiveCards()
    {
        foreach (CardModel card in PendingCards.Keys.Where(card => !IsActiveEncoreCard(card)).ToList())
        {
            PendingCards.Remove(card);
        }
    }

    private static int GetCombatPlayCount(CardModel card)
    {
        return CombatManager.Instance.History.CardPlaysFinished
            .Count(entry => entry.CardPlay.Card == card);
    }

    private static bool IsActiveEncoreCard(CardModel card)
    {
        return card.Owner?.PlayerCombatState != null
            && card.Pile is { IsCombatPile: true }
            && card.Keywords.Contains(MyKeywords.Encore);
    }

    private static Creature? GetAutoPlayTarget(CardModel card)
    {
        CombatState? combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        return card.TargetType switch
        {
            TargetType.Self or TargetType.AnyPlayer => card.Owner.Creature,
            TargetType.AnyEnemy or TargetType.RandomEnemy => card.Owner.RunState.Rng.CombatTargets.NextItem(
                combatState?.HittableEnemies ?? Enumerable.Empty<Creature>()),
            TargetType.AnyAlly => card.Owner.RunState.Rng.CombatTargets.NextItem(
                combatState?.Allies.Where(creature => IsValidAllyTarget(creature, card.Owner.Creature)) ?? Enumerable.Empty<Creature>()),
            TargetType.Osty => card.Owner.Osty is { IsAlive: true } osty ? osty : null,
            _ => null
        };
    }

    private static bool IsValidAllyTarget(Creature creature, Creature owner)
    {
        return creature != owner && creature.IsAlive && creature.IsPlayer;
    }

    private sealed class Data
    {
        public Dictionary<CardModel, int> PendingCards { get; } = [];
    }
}

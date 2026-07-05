using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public sealed class ExecutionEnchantment : ModEnchantmentTemplate
{
    private const string DebugPrefix = "[ExecutionEnchantment]";
    private bool _returnedThisPlay;
    private readonly HashSet<Creature> _disabledDeathPreventionThisPlay = [];

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenGuillotineRelic.png"
    );

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return (pileType, position);
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (IsThisCard(cardPlay.Card))
        {
            _returnedThisPlay = false;
            _disabledDeathPreventionThisPlay.Clear();
            DebugLog($"BeforeCardPlayed: card={CardDebugName(cardPlay.Card)}, pile={cardPlay.Card.Pile?.Type.ToString() ?? "null"}");
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return IsThisCard(cardSource) && target?.Powers.Any(power => power is MinionPower) == true
            ? 2m
            : 1m;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!IsDamageFromThisCard(dealer, target, cardSource) || (!result.WasTargetKilled && !target.IsDead))
        {
            DebugLog(
                $"AfterDamageGiven ignored: source={CardDebugName(cardSource)}, enchantCard={CardDebugName(Card)}, " +
                $"dealerMatch={dealer == Card.Owner?.Creature}, target={target.LogName}, targetDead={target.IsDead}, " +
                $"wasKilled={result.WasTargetKilled}, targetSide={target.Side}, dealerSide={dealer?.Side.ToString() ?? "null"}");
            return Task.CompletedTask;
        }

        DebugLog($"AfterDamageGiven kill detected: source={CardDebugName(cardSource)}, target={target.LogName}");
        return HandleSuccessfulKill(choiceContext, target);
    }

    public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay == null
            || !IsThisCard(cardPlay.Card)
            || cardPlay.Target == null
            || cardPlay.Target.Side == Card.Owner?.Creature.Side
            || !cardPlay.Target.IsDead)
        {
            DebugLog(
                $"OnPlay ignored: playCard={CardDebugName(cardPlay?.Card)}, enchantCard={CardDebugName(Card)}, " +
                $"target={(cardPlay?.Target == null ? "null" : cardPlay.Target.LogName)}, " +
                $"targetDead={cardPlay?.Target?.IsDead.ToString() ?? "null"}, " +
                $"targetSide={cardPlay?.Target?.Side.ToString() ?? "null"}, ownerSide={Card.Owner?.Creature.Side.ToString() ?? "null"}");
            return Task.CompletedTask;
        }

        DebugLog($"OnPlay fallback kill detected: card={CardDebugName(cardPlay.Card)}, target={cardPlay.Target.LogName}");
        return HandleSuccessfulKill(choiceContext, cardPlay.Target);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel playedCard = cardPlay.Card;
        if (!IsThisCard(playedCard) || !_returnedThisPlay || playedCard.Pile?.Type != PileType.Play)
        {
            DebugLog(
                $"AfterCardPlayed no return: playCard={CardDebugName(playedCard)}, enchantCard={CardDebugName(Card)}, " +
                $"returned={_returnedThisPlay}, pile={playedCard.Pile?.Type.ToString() ?? "null"}");
            return;
        }

        DebugLog($"AfterCardPlayed returning killed card to hand: card={CardDebugName(playedCard)}");
        await CardPileCmd.Add(playedCard, PileType.Hand, CardPilePosition.Top);
    }

    public async Task HandleSuccessfulKill(PlayerChoiceContext choiceContext, Creature target)
    {
        if (_returnedThisPlay)
        {
            DebugLog($"HandleSuccessfulKill skipped duplicate: target={target.LogName}");
            return;
        }

        _returnedThisPlay = true;
        DebugLog($"HandleSuccessfulKill marked return: target={target.LogName}, targetAlive={target.IsAlive}, targetDead={target.IsDead}");
        await DisableRevivalPowers(target);

        if (target.IsAlive)
        {
            DebugLog($"HandleSuccessfulKill force killing revival target: target={target.LogName}");
            await CreatureCmd.Kill(target, force: true);
        }

    }

    public bool HasDisabledDeathPrevention(Creature creature)
    {
        return _disabledDeathPreventionThisPlay.Contains(creature);
    }

    private async Task DisableRevivalPowers(Creature creature)
    {
        PowerModel? reattach = creature.GetPower<ReattachPower>();
        PowerModel? adaptable = creature.GetPower<AdaptablePower>();
        PowerModel? illusion = creature.GetPower<IllusionPower>();

        if (reattach != null || adaptable != null || illusion != null)
        {
            _disabledDeathPreventionThisPlay.Add(creature);
        }

        await PowerCmd.Remove(reattach);
        await PowerCmd.Remove(adaptable);
        await PowerCmd.Remove(illusion);
    }

    private bool IsDamageFromThisCard(Creature? dealer, Creature target, CardModel? cardSource)
    {
        return dealer == Card.Owner?.Creature
            && target.Side != dealer?.Side
            && IsThisCard(cardSource);
    }

    private bool IsThisCard(CardModel? card)
    {
        return card != null
            && (card == Card || card.DeckVersion == Card || card == Card.DeckVersion);
    }

    private static string CardDebugName(CardModel? card)
    {
        return card == null
            ? "null"
            : $"{card.Id}@{card.GetHashCode():X}[pile={card.Pile?.Type.ToString() ?? "null"}, deck={card.DeckVersion?.GetHashCode().ToString("X") ?? "null"}]";
    }

    private static void DebugLog(string message)
    {
        Entry.Logger.Debug($"{DebugPrefix} {message}");
    }
}

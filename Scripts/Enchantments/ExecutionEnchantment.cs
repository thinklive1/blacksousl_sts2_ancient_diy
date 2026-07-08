using MegaCrit.Sts2.Core.Combat;
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

/// <summary>Implements the Execution enchantment.</summary>
[RegisterEnchantment]
public sealed class ExecutionEnchantment : ModEnchantmentTemplate
{
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

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (IsThisCard(cardPlay.Card))
        {
            _returnedThisPlay = false;
            _disabledDeathPreventionThisPlay.Clear();
        }

        return Task.CompletedTask;
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
            return Task.CompletedTask;
        }

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
            return Task.CompletedTask;
        }

        return HandleSuccessfulKill(choiceContext, cardPlay.Target);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!IsThisCard(cardPlay.Card)
            || !_returnedThisPlay
            || cardPlay.Card.Pile?.Type != PileType.Play
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        await CardPileCmd.Add(cardPlay.Card, PileType.Hand, CardPilePosition.Top);
    }

    public async Task HandleSuccessfulKill(PlayerChoiceContext choiceContext, Creature target)
    {
        if (_returnedThisPlay)
        {
            return;
        }

        _returnedThisPlay = true;
        await DisableRevivalPowers(target);

        if (target.IsAlive)
        {
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
}

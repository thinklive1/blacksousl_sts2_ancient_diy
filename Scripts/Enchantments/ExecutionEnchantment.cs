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
        return card == Card ? (PileType.Exhaust, position) : (pileType, position);
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == Card)
        {
            _returnedThisPlay = false;
            _disabledDeathPreventionThisPlay.Clear();
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return cardSource == Card && target?.Powers.Any(power => power is MinionPower) == true
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
        if (cardSource != Card || target.Side == dealer?.Side || !result.WasTargetKilled)
        {
            return Task.CompletedTask;
        }

        return HandleSuccessfulKill(choiceContext, target);
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

        if (Card.Pile?.Type != PileType.Hand)
        {
            await CardPileCmd.Add(Card, PileType.Hand);
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
}

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
    private HashSet<Creature> _executionTargetsThisPlay = [];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _executionTargetsThisPlay = [];
        _returnedThisPlay = false;
    }

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
            _executionTargetsThisPlay.Clear();
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if BS_BETA_1110
        , CardPlay? cardPlay
#endif
        )
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
        if (!IsDamageFromThisCard(target, cardSource) || (!result.WasTargetKilled && !target.IsDead))
        {
            return Task.CompletedTask;
        }

        return HandleSuccessfulKill(choiceContext, target);
    }

    public override Task BeforeDeath(Creature creature)
    {
        if (!creature.IsDead || !_executionTargetsThisPlay.Remove(creature))
        {
            return Task.CompletedTask;
        }

        // Remove revival before native death snapshots its hooks, never after a revive move is queued.
        return DisableRevivalPowers(creature);
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

    public Task HandleSuccessfulKill(PlayerChoiceContext choiceContext, Creature target)
    {
        if (!target.IsDead)
        {
            return Task.CompletedTask;
        }

        _returnedThisPlay = true;
        _executionTargetsThisPlay.Add(target);
        return Task.CompletedTask;
    }

    private async Task DisableRevivalPowers(Creature creature)
    {
        foreach (PowerModel power in GetRevivalPowers(creature))
        {
            await PowerCmd.Remove(power);
        }
    }

    internal static PowerModel[] GetRevivalPowers(Creature creature) =>
        creature.Powers.Where(power => power is ReattachPower or AdaptablePower or IllusionPower).ToArray();

    private bool IsDamageFromThisCard(Creature target, CardModel? cardSource)
    {
        // Osty attacks retain the played card as their source but use Osty as the damage dealer.
        return Card.Owner is { } owner
            && target.Side != owner.Creature.Side
            && IsThisCard(cardSource);
    }

    private bool IsThisCard(CardModel? card)
    {
        return card != null
            && (card == Card || card.DeckVersion == Card || card == Card.DeckVersion);
    }
}

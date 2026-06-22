using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public sealed class BrutalityEnchantment : ModEnchantmentTemplate
{
    private const string BrutalityIconPath = "res://bs_ancient/assets/images/enchantment/BrutalityEnchantment.png";

    private bool _isEmpoweredThisPlay;
    private bool _isPlayingThisCard;
    private readonly HashSet<Creature> _damagedTargetsThisPlay = [];

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(IconPath: BrutalityIconPath);

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return HasPendingEmpowerment() ? 2m : 1m;
    }

    public override Task BeforeCombatStart()
    {
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _isEmpoweredThisPlay = false;
        _isPlayingThisCard = false;
        _damagedTargetsThisPlay.Clear();
        return Task.CompletedTask;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == Card)
        {
            _isPlayingThisCard = true;
            _damagedTargetsThisPlay.Clear();
            _isEmpoweredThisPlay = await ConsumePendingEmpowerment();
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return IsDamageFromThisCard(dealer, null, cardSource) && _isEmpoweredThisPlay ? 2m : 1m;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (IsDamageFromThisCard(dealer, target, cardSource) && result.TotalDamage > 0)
        {
            _damagedTargetsThisPlay.Add(target);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card)
        {
            return;
        }

        if (_damagedTargetsThisPlay.Any(target => target.IsDead))
        {
            await SetPendingEmpowerment();
        }

        _isEmpoweredThisPlay = false;
        _isPlayingThisCard = false;
        _damagedTargetsThisPlay.Clear();
    }

    private bool IsDamageFromThisCard(Creature? dealer, Creature? target, CardModel? cardSource)
    {
        return _isPlayingThisCard
            && dealer == Card.Owner?.Creature
            && (target == null || target.Side != dealer?.Side)
            && (cardSource == Card || cardSource?.DeckVersion == Card || cardSource == Card.DeckVersion);
    }

    private async Task SetPendingEmpowerment()
    {
        Creature? owner = Card.Owner?.Creature;
        if (owner == null)
        {
            return;
        }

        await PowerCmd.Apply<BrutalityPower>(
            new ThrowingPlayerChoiceContext(),
            owner,
            1,
            owner,
            Card,
            false);
    }

    private async Task<bool> ConsumePendingEmpowerment()
    {
        Creature? owner = Card.Owner?.Creature;
        BrutalityPower? power = owner?.GetPower<BrutalityPower>();
        if (owner == null || power == null || power.Amount <= 0)
        {
            return false;
        }

        if (power.Amount <= 1)
        {
            await PowerCmd.Remove(power);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                new ThrowingPlayerChoiceContext(),
                power,
                -1,
                owner,
                Card,
                false);
        }

        return true;
    }

    private bool HasPendingEmpowerment()
    {
        return Card.Owner?.Creature.GetPower<BrutalityPower>()?.Amount > 0;
    }
}

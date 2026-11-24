using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Preserves native Osty damage and death while cancelling one hit's overflow to its owner.</summary>
[RegisterPower]
public sealed class PartnerGuardPower : ModPowerTemplate
{
    private bool _reserved;
    private Creature? _dealer;
    private CardModel? _cardSource;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/PartnerGuardPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/PartnerGuardPower.png");

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        ClearReservation();
    }

    public override Task BeforeDamageReceived(PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_reserved && Amount > 0 && target == Owner && amount > 0 && props.IsPoweredAttack()
            && dealer?.Side != Owner.Side && Owner.Player?.Osty is { IsAlive: true })
        {
            _reserved = true;
            _dealer = dealer;
            _cardSource = cardSource;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        MatchesHit(target, props, dealer, cardSource) ? 0m : amount;

    public override Task AfterModifyingHpLostAfterOsty() => SpendReservation();

    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource) =>
        MatchesHit(target, props, dealer, cardSource) ? SpendReservation() : Task.CompletedTask;

    private bool MatchesHit(Creature target, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        _reserved && target == Owner && props.IsPoweredAttack()
        && ReferenceEquals(dealer, _dealer) && ReferenceEquals(cardSource, _cardSource);

    private Task SpendReservation()
    {
        if (!_reserved) return Task.CompletedTask;
        // Clear before the native command invokes more hooks; this protection is for one hit only.
        ClearReservation();
        return PowerCmd.Decrement(this);
    }

    private void ClearReservation()
    {
        _reserved = false;
        _dealer = null;
        _cardSource = null;
    }
}

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public class ViolenceDemonPower : ModPowerTemplate
{
    private const int SelfDamagePercent = 50;

    private bool _hasStartedTurn;
    private bool _playedAttackThisTurn;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("SelfDamagePercent", SelfDamagePercent)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/ViolenceDemonPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/ViolenceDemonPower.png"
    );

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return target == Owner ? 0m : 1m;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Type == CardType.Attack)
        {
            _playedAttackThisTurn = true;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if ((dealer != Owner && dealer?.PetOwner?.Creature != Owner) || target.Side == Owner.Side || !props.IsPoweredAttack())
        {
            return;
        }

        _playedAttackThisTurn = true;

        int heal = result.UnblockedDamage + result.OverkillDamage;
        if (heal <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner, heal);
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        if (_hasStartedTurn && !_playedAttackThisTurn)
        {
            decimal selfDamage = Math.Ceiling(Owner.CurrentHp * DynamicVars["SelfDamagePercent"].BaseValue / 100m);
            if (selfDamage > 0m)
            {
                Flash();
                await CreatureCmd.Damage(
                    new ThrowingPlayerChoiceContext(),
                    Owner,
                    selfDamage,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    Owner,
                    null);
            }
        }

        _playedAttackThisTurn = false;
        _hasStartedTurn = true;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _hasStartedTurn = false;
        _playedAttackThisTurn = false;
        return Task.CompletedTask;
    }
}

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

    private int _ownerTurnCount;

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

        int heal = result.UnblockedDamage + result.OverkillDamage;
        if (heal <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner, heal);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        _ownerTurnCount++;
        if (_ownerTurnCount >= 2)
        {
            decimal selfDamage = Math.Ceiling(Owner.CurrentHp * DynamicVars["SelfDamagePercent"].BaseValue / 100m);
            selfDamage = Math.Min(selfDamage, Math.Max(Owner.CurrentHp - 1, 0));
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
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _ownerTurnCount = 0;
        return Task.CompletedTask;
    }
}

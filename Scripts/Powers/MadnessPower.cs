using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public class MadnessPower : ModPowerTemplate
{
    private const int DamageMultiplier = 2;
    private const int SelfDamagePercent = 50;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("DamageMultiplier", DamageMultiplier),
        new DynamicVar("SelfDamagePercent", SelfDamagePercent)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/Madness.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/Madness.png"
    );

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if ((dealer != Owner && dealer != Owner.Player?.Osty) || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return DynamicVars["DamageMultiplier"].BaseValue;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        decimal selfDamage = Math.Ceiling(Owner.CurrentHp * DynamicVars["SelfDamagePercent"].BaseValue / 100m);
        selfDamage = Math.Min(selfDamage, Math.Max(Owner.CurrentHp - 1, 0));

        if (selfDamage <= 0m)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner,
            selfDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner,
            null
        );
    }
}

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

/// <summary>Implements the Little Prince Countdown power.</summary>
[RegisterPower]
public sealed class LittlePrinceCountdownPower : ModPowerTemplate
{
    private const int MeteorDamage = 30;
    private const string PowerIconPath = "res://bs_ancient/assets/images/powers/LittlePrinceCountdownPower.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("MeteorDamage", MeteorDamage)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath
    );

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        if (Amount <= 1)
        {
            Flash();
            await DamageAllCreatures(combatState);
            await PowerCmd.Remove(this);
            return;
        }

        SetAmount(Amount - 1, silent: true);
    }

    private async Task DamageAllCreatures(ICombatState combatState)
    {
        foreach (Creature creature in combatState.Creatures.Where(creature => creature.IsAlive).ToList())
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                creature,
                DynamicVars["MeteorDamage"].BaseValue,
                ValueProp.Move,
                Owner,
                null);
        }
    }
}

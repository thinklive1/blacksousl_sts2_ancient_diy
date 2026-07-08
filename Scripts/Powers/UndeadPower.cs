using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Undead power.</summary>
[RegisterPower]
public class UndeadPower : ModPowerTemplate
{
    private const int HealPerStack = 10;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(HealPerStack)];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/Undead.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/Undead.png"
    );

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner || Amount <= 0)
        {
            return true;
        }

        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner, 1m, playAnim: false);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        await PowerCmd.Decrement(this);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.IsPlayer && Amount > 0)
        {
            await CreatureCmd.Heal(Owner, Amount * HealPerStack);
        }
    }
}

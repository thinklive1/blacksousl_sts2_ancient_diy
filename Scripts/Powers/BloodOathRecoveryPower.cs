using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Sts2Diy.Scripts.Cards;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Repays only this player's actual Blood Oath HP loss, once per combat.</summary>
[RegisterPower]
public sealed class BloodOathRecoveryPower : ModPowerTemplate
{
    private int _hpToRestore;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => Sts2Diy_HpToRestore;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/BloodOathRecoveryPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/BloodOathRecoveryPower.png");

    [SavedProperty]
    public int Sts2Diy_HpToRestore
    {
        get => _hpToRestore;
        set
        {
            AssertMutable();
            _hpToRestore = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("HpToRestore", IsMutable ? Sts2Diy_HpToRestore : 0);
            return description;
        }
    }

    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.Receiver == Owner && cardSource is BloodOathCard
            && cardSource.Owner.Creature == Owner && result.UnblockedDamage > 0)
        {
            // UnblockedDamage is actual HP lost; OverkillDamage is separate and must not be repaid.
            Sts2Diy_HpToRestore += result.UnblockedDamage;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        int refund = Sts2Diy_HpToRestore;
        Sts2Diy_HpToRestore = 0;
        if (refund > 0 && Owner.IsPlayer && Owner.IsAlive)
        {
            await CreatureCmd.Heal(Owner, refund);
        }
    }
}

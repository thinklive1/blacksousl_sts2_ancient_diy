using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Evil Qi Pending power.</summary>
[RegisterPower]
public sealed class EvilQiPendingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/EvilQiEnchantment.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/EvilQiEnchantment.png"
    );

    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Amount <= 0 || Owner.IsDead)
        {
            return;
        }

        int amount = Amount;
        await EvilQiEffect.Resolve(choiceContext, Owner, amount);
        await PowerCmd.Remove(this);
    }
}

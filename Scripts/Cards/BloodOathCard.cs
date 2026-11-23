using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Trades HP equal to current Doom for Block, with repayment after combat.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class BloodOathCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/BloodOathCard.jpg");

    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<BloodOathRecoveryPower>()];

    public BloodOathCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var creature = Owner.Creature;
        int doom = Math.Max(0, creature.GetPowerAmount<DoomPower>());
        if (doom == 0 || creature.IsDead) return;

        // Install the ledger first: damage reactions can finish the combat before Damage returns.
        BloodOathRecoveryPower? recovery = creature.GetPower<BloodOathRecoveryPower>()
            ?? await PowerCmd.Apply<BloodOathRecoveryPower>(context, creature, 1m, creature, this);
        if (recovery == null) return;

        await CreatureCmd.Damage(context, creature, doom,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
        if (creature.IsAlive)
        {
            await CreatureCmd.GainBlock(creature, doom, ValueProp.Unpowered, cardPlay);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

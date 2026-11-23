using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Cards;

/// <summary>One-turn summon and Osty-block trigger card.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class DeathLifePactCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/DeathLifePactCard.jpg"
    );

    public DeathLifePactCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        return Owner == null
            ? Task.CompletedTask
            : PowerCmd.Apply<DeathLifePactPower>(context, Owner.Creature, 2m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>Absorbs half of the chosen enemy's Doom into the player and converts it to Block.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class NegativeEnergyAbsorptionCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/NegativeEnergyAbsorptionCard.jpg"
    );

    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];

    public NegativeEnergyAbsorptionCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        Creature enemy = cardPlay.Target;
        if (enemy.Side == Owner.Creature.Side || !enemy.IsHittable) return;
        int absorbed = enemy.GetPowerAmount<DoomPower>() / 2;
        if (absorbed <= 0) return;
        DoomPower? doom = enemy.GetPower<DoomPower>();
        if (doom == null) return;
        await PowerCmd.ModifyAmount(context, doom, -absorbed, Owner.Creature, this);
        await PowerCmd.Apply<DoomPower>(context, Owner.Creature, absorbed, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, absorbed, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, cardPlay);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

/// <summary>Doubles player Doom, then shares it with all enemies next turn.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class SharedDoomCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/SharedDoomCard.jpg"
    );

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];

    public SharedDoomCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        SharedDoomTransferPower? transfer = await PowerCmd.Apply<SharedDoomTransferPower>(
            context, Owner.Creature, 1m, Owner.Creature, this);
        if (IsUpgraded && transfer != null)
        {
            transfer.Sts2Diy_DoomProtection = true;
        }

        int doom = Owner.Creature.GetPowerAmount<DoomPower>();
        if (doom > 0)
        {
            await PowerCmd.Apply<DoomPower>(context, Owner.Creature, doom, Owner.Creature, this);
        }
    }
}

/// <summary>Arms a stronger Osty attack on the turn following an Osty death.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class BitterHatredCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/BitterHatredCard.jpg"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BitterHatredPower", 50m)];

    public BitterHatredCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<BitterHatredPower>(context, Owner.Creature, DynamicVars["BitterHatredPower"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["BitterHatredPower"].UpgradeValueBy(25m);
}

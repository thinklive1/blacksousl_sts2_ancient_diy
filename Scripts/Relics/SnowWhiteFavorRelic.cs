using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Snow White Favor relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class SnowWhiteFavorRelic : ModRelicTemplate
{
    private const int DexterityLoss = 5;
    private const decimal EnemyAttackDamageMultiplier = 0.5m;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DexterityPower>(DexterityLoss)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/SnowWhiteFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/SnowWhiteFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/SnowWhiteFavorRelic.png"
    );

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<DexterityPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, -DynamicVars["DexterityPower"].BaseValue, Owner.Creature, null, false);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!IsProtectedTarget(target)
            || !props.IsPoweredAttack()
            || dealer?.Side == target?.Side)
        {
            return 1m;
        }

        return EnemyAttackDamageMultiplier;
    }

    private bool IsProtectedTarget(Creature? target)
    {
        return target == Owner.Creature
            || target == Owner.Osty
            || target?.PetOwner == Owner
            || target?.Player == Owner
            || target?.Player?.NetId == Owner.NetId;
    }
}

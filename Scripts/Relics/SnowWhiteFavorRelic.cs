using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class SnowWhiteFavorRelic : ModRelicTemplate
{
    private const int DexterityLoss = 5;
    private const decimal EnemyAttackDamageMultiplier = 0.5m;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DexterityPower>(DexterityLoss)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/SnowWhiteFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/SnowWhiteFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/SnowWhiteFavorRelic.png"
    );

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, -DynamicVars["DexterityPower"].BaseValue, Owner.Creature, null);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature || dealer is null || dealer.Side == Owner.Creature.Side || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return EnemyAttackDamageMultiplier;
    }
}

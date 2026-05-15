using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class LittleMermaidFavorRelic : ModRelicTemplate
{
    private const int MaxHpLoss = 30;
    private const int EnergyGain = 1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MaxHpVar(MaxHpLoss),
        new EnergyVar(EnergyGain)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/LittleMermaidFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/LittleMermaidFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/LittleMermaidFavorRelic.png"
    );

    public override async Task AfterObtained()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.MaxHp.BaseValue,
            isFromCard: false
        );
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}

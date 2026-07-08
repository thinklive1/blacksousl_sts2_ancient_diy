using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Covenant Of Node relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class CovenantOfNodeRelic : ModRelicTemplate
{
    private const int EnergyGain = 1;
    private const int RegenGain = 3;

    private bool _hasTriggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(EnergyGain),
        new PowerVar<RegenPower>(RegenGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<RegenPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/CovenantOfNodeRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/CovenantOfNodeRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/CovenantOfNodeRelic.png"
    );

    public override Task BeforeCombatStart()
    {
        _hasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side || !IsOwnerBelowHalfHealth())
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

        if (_hasTriggeredThisCombat)
        {
            return;
        }

        _hasTriggeredThisCombat = true;
        await PowerCmd.Apply<RegenPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["RegenPower"].BaseValue, Owner.Creature, null, false);
    }

    private bool IsOwnerBelowHalfHealth()
    {
        return Owner.Creature.CurrentHp * 2 < Owner.Creature.MaxHp;
    }
}

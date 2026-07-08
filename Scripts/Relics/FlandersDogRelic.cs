using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Flanders Dog relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class FlandersDogRelic : ModRelicTemplate
{
    private const int TrackingAmount = 1;
    private const int WeakDamageTakenMultiplier = 2;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<FlandersTrackingPower>(TrackingAmount),
        new DynamicVar("WeakDamageTakenMultiplier", WeakDamageTakenMultiplier)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FlandersTrackingPower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<FlandersTrackingPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["FlandersTrackingPower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }
}

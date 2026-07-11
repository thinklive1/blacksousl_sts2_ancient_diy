using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Black Thing Myth relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class BlackThingMythRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MythRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    // This relic is granted only by Fairy Tale Mode, never by ordinary event rewards.
    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task AfterObtained()
    {
        NMapScreen.Instance?.RefreshAllPointVisuals();
        return Task.CompletedTask;
    }
}

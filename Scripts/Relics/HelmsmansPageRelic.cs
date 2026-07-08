using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Helmsmans Page relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class HelmsmansPageRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/scripts.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return !SnarkPageRelicTrackerModifier.HasAppearedOrOwned<HelmsmansPageRelic>(runState);
    }

    public override Task AfterObtained()
    {
        if (Owner != null)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<HelmsmansPageRelic>(Owner);
        }

        return Task.CompletedTask;
    }
}

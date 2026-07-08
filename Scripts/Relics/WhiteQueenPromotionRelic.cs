using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the White Queen Promotion relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class WhiteQueenPromotionRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/WhiteQueenPromotionRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/WhiteQueenPromotionRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/WhiteQueenPromotionRelic.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override bool ShouldAllowFreeTravel()
    {
        return true;
    }
}

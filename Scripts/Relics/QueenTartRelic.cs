using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Queen Tart relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenTartRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/QueenTartRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/QueenTartRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/QueenTartRelic.png"
    );

    public override EventModel ModifyNextEvent(EventModel currentEvent)
    {
        return BsAncientConfig.EnableModEvents
            && Owner.RunState.CurrentActIndex == 1
            ? ModelDb.Event<QueenOfHeartsEvent>()
            : currentEvent;
    }
}

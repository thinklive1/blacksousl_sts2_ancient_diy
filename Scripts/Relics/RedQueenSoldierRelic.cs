using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RedQueenSoldierRelic : ModRelicTemplate
{
    private const int TrialCount = 5;

    private static readonly MapPointType[] TrialRooms =
    [
        MapPointType.Monster,
        MapPointType.Monster,
        MapPointType.Monster,
        MapPointType.Elite,
        MapPointType.Elite,
    ];

    private int _trialActIndex = -1;
    private int _completedTrials;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, TrialCount - CompletedTrials);

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenSoldierRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenSoldierRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenSoldierRelic.png"
    );

    [SavedProperty]
    public int TrialActIndex
    {
        get => _trialActIndex;
        set
        {
            AssertMutable();
            _trialActIndex = value;
        }
    }

    [SavedProperty]
    public int CompletedTrials
    {
        get => _completedTrials;
        set
        {
            AssertMutable();
            _completedTrials = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override async Task AfterObtained()
    {
        TrialActIndex = Owner.RunState.CurrentActIndex;
        CompletedTrials = 0;
        ApplyTrialRooms(Owner.RunState.Map);
        await Task.CompletedTask;
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex != TrialActIndex || CompletedTrials >= TrialCount)
        {
            return map;
        }

        return ApplyTrialRooms(map);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.RunState.CurrentActIndex != TrialActIndex || CompletedTrials >= TrialCount)
        {
            return;
        }

        if (room.RoomType is not (RoomType.Monster or RoomType.Elite))
        {
            return;
        }

        CompletedTrials++;
        Flash();

        if (CompletedTrials >= TrialCount)
        {
            await RelicCmd.Replace(this, ModelDb.Relic<RedQueenPromotionRelic>().ToMutable());
        }
    }

    private ActMap ApplyTrialRooms(ActMap map)
    {
        for (int row = 1; row <= TrialRooms.Length; row++)
        {
            foreach (MapPoint point in map.GetPointsInRow(row))
            {
                point.PointType = TrialRooms[row - 1];
            }
        }

        return map;
    }
}

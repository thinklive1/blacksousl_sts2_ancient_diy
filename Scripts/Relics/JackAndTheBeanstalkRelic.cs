using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Jack And The Beanstalk relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class JackAndTheBeanstalkRelic : ModRelicTemplate
{
    private const int HealthLossPerNode = 5;
    private const int RewardNode = 6;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _remainingNodes = RewardNode;
    private int _totalHealthLost;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_RemainingNodes;

    public override bool IsUsedUp => BlackSouls_RemainingNodes <= 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Health", HealthLossPerNode),
        new DynamicVar("Nodes", RewardNode)
    ];

    [SavedProperty]
    public int BlackSouls_RemainingNodes
    {
        get => _remainingNodes;
        set
        {
            AssertMutable();
            _remainingNodes = Math.Max(0, value);
            RefreshCounter();
        }
    }

    [SavedProperty]
    public int BlackSouls_TotalHealthLost
    {
        get => _totalHealthLost;
        set
        {
            AssertMutable();
            _totalHealthLost = Math.Max(0, value);
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsCombatRoom(room) || IsCurrentPointCombat() || !ShouldCountCurrentPoint())
        {
            return;
        }

        await CountNode();
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        return IsCombatRoom(room) ? CountNode() : Task.CompletedTask;
    }

    private async Task CountNode()
    {
        if (Owner?.Creature == null || BlackSouls_RemainingNodes <= 0)
        {
            return;
        }

        Flash();
        BlackSouls_RemainingNodes--;

        if (BlackSouls_RemainingNodes > 0)
        {
            int healthLost = Math.Min(HealthLossPerNode, Math.Max(Owner.Creature.CurrentHp - 1, 0));
            if (healthLost <= 0)
            {
                return;
            }

            Owner.Creature.LoseHpInternal(
                healthLost,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim);
            BlackSouls_TotalHealthLost += healthLost;
            return;
        }

        if (BlackSouls_TotalHealthLost > 0)
        {
            await CreatureCmd.GainMaxHp(Owner.Creature, BlackSouls_TotalHealthLost);
        }

        RefreshCounter();
    }

    private bool ShouldCountCurrentPoint()
    {
        MapPoint? currentPoint = Owner?.RunState.CurrentMapPoint;
        return currentPoint is { PointType: not MapPointType.Unassigned };
    }

    private bool IsCurrentPointCombat()
    {
        MapPoint? currentPoint = Owner?.RunState.CurrentMapPoint;
        return currentPoint?.PointType is MapPointType.Monster or MapPointType.Elite;
    }

    private static bool IsCombatRoom(CombatRoom room)
    {
        return room.RoomType is RoomType.Monster or RoomType.Elite;
    }

    private static bool IsCombatRoom(AbstractRoom room)
    {
        return room is CombatRoom || room.RoomType is RoomType.Monster or RoomType.Elite;
    }

    private void RefreshCounter()
    {
        Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Alice Handkerchief modifier.</summary>
public sealed class AliceHandkerchiefModifier : ModModifierTemplate
{
    private const string ModifierIconPath = AliceHandkerchiefRelic.RelicIconPath;
    private const int BuffAmount = 3;
    private const int AffectedNodeCount = 5;

    private int _actIndex = -1;
    private int _remainingNodes;
    private bool _shouldBuffCurrentCombat;

    [SavedProperty]
    public int BlackSouls_ActIndex
    {
        get => _actIndex;
        set
        {
            AssertMutable();
            _actIndex = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_RemainingNodes
    {
        get => _remainingNodes;
        set
        {
            AssertMutable();
            _remainingNodes = Math.Max(0, value);
            RefreshRelicCounters();
        }
    }

    [SavedProperty]
    public bool BlackSouls_ShouldBuffCurrentCombat
    {
        get => _shouldBuffCurrentCombat;
        set
        {
            AssertMutable();
            _shouldBuffCurrentCombat = value;
        }
    }

    public override ModifierAssetProfile AssetProfile => new(ModifierIconPath);

    public void Configure(IRunState runState)
    {
        AssertMutable();
        BlackSouls_ActIndex = runState.CurrentActIndex + 1;
        BlackSouls_RemainingNodes = AffectedNodeCount;
        BlackSouls_ShouldBuffCurrentCombat = false;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (RunState.CurrentActIndex != BlackSouls_ActIndex || BlackSouls_RemainingNodes <= 0)
        {
            BlackSouls_ShouldBuffCurrentCombat = false;
            return Task.CompletedTask;
        }

        MapPoint? currentPoint = RunState.CurrentMapPoint;
        if (currentPoint == null || !ShouldCountPoint(currentPoint))
        {
            BlackSouls_ShouldBuffCurrentCombat = false;
            return Task.CompletedTask;
        }

        BlackSouls_ShouldBuffCurrentCombat = IsCombatPoint(currentPoint);
        BlackSouls_RemainingNodes--;
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (RunState.CurrentActIndex != BlackSouls_ActIndex || !BlackSouls_ShouldBuffCurrentCombat)
        {
            return;
        }

        foreach (MegaCrit.Sts2.Core.Entities.Players.Player player in RunState.Players)
        {
            await PowerCmd.Apply<StrengthPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), player.Creature, BuffAmount, player.Creature, null, false);
            await PowerCmd.Apply<DexterityPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), player.Creature, BuffAmount, player.Creature, null, false);
        }
    }

    private static bool IsCombatPoint(MapPoint point)
    {
        return point.PointType is MapPointType.Monster or MapPointType.Elite;
    }

    private static bool ShouldCountPoint(MapPoint point)
    {
        return point.PointType is not MapPointType.Ancient and not MapPointType.Unassigned;
    }

    private void RefreshRelicCounters()
    {
        if (RunState == null)
        {
            return;
        }

        foreach (MegaCrit.Sts2.Core.Entities.Players.Player player in RunState.Players)
        {
            player.GetRelic<AliceHandkerchiefRelic>()?.RefreshCounter();
        }
    }
}

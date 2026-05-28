using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlackSouls.Scripts;

public sealed class BirdSingerModifier : ModifierModel
{
    private const int AffectedNodeCount = 7;
    private const decimal HealthMultiplier = 0.5m;

    private int _actIndex = -1;
    private bool _showMarkers;
    private int[] _coordCols = [];
    private int[] _coordRows = [];

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
    public bool BlackSouls_ShowMarkers
    {
        get => _showMarkers;
        set
        {
            AssertMutable();
            _showMarkers = value;
        }
    }

    [SavedProperty]
    public int[] BlackSouls_CoordCols
    {
        get => _coordCols;
        set
        {
            AssertMutable();
            _coordCols = value ?? [];
        }
    }

    [SavedProperty]
    public int[] BlackSouls_CoordRows
    {
        get => _coordRows;
        set
        {
            AssertMutable();
            _coordRows = value ?? [];
        }
    }

    public void Configure(IRunState runState, bool showMarkers)
    {
        AssertMutable();
        BlackSouls_ActIndex = runState.CurrentActIndex;
        BlackSouls_ShowMarkers = showMarkers;

        MapPoint? currentPoint = runState.CurrentMapPoint;
        int minRow = currentPoint?.coord.row + 1 ?? 1;
        Rng rng = new((uint)((int)runState.Rng.Seed + StringHelper.GetDeterministicHashCode(nameof(BirdSingerModifier)) + runState.CurrentActIndex));
        List<MapPoint> candidates = runState.Map.GetAllMapPoints()
            .Where(point => point.PointType == MapPointType.Monster && point.coord.row >= minRow)
            .ToList();

        if (candidates.Count < AffectedNodeCount)
        {
            candidates = runState.Map.GetAllMapPoints()
                .Where(point => point.PointType == MapPointType.Monster)
                .ToList();
        }

        List<MapPoint> selected = candidates
            .UnstableShuffle(rng)
            .Take(AffectedNodeCount)
            .ToList();

        BlackSouls_CoordCols = selected.Select(point => point.coord.col).ToArray();
        BlackSouls_CoordRows = selected.Select(point => point.coord.row).ToArray();
        AddMarkedRooms(runState.Map);
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex == BlackSouls_ActIndex)
        {
            AddMarkedRooms(map);
        }

        return map;
    }

    public override async Task BeforeCombatStart()
    {
        if (!ShouldAffectCurrentCombat())
        {
            return;
        }

        CombatState? combatState = RunState.Players.FirstOrDefault()?.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await ReduceEnemyHealth(enemy);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side == CombatSide.Enemy && ShouldAffectCurrentCombat())
        {
            await ReduceEnemyHealth(creature);
        }
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        if (runState.CurrentActIndex == BlackSouls_ActIndex)
        {
            AddMarkedRooms(runState.Map);
        }
    }

    private bool ShouldAffectCurrentCombat()
    {
        if (RunState.CurrentActIndex != BlackSouls_ActIndex || RunState.CurrentRoom is not CombatRoom)
        {
            return false;
        }

        MapPoint? currentPoint = RunState.CurrentMapPoint;
        return currentPoint != null && GetMarkedCoords().Contains(currentPoint.coord);
    }

    private async Task ReduceEnemyHealth(Creature enemy)
    {
        int newHp = Math.Max(1, (int)Math.Ceiling(enemy.CurrentHp * HealthMultiplier));
        if (newHp < enemy.CurrentHp)
        {
            await CreatureCmd.SetCurrentHp(enemy, newHp);
        }
    }

    private void AddMarkedRooms(ActMap map)
    {
        if (!BlackSouls_ShowMarkers)
        {
            return;
        }

        foreach (MapCoord coord in GetMarkedCoords())
        {
            MapPoint? point = map.GetPoint(coord);
            if (point != null && !point.Quests.Any(quest => quest is BirdSingerModifier))
            {
                point.AddQuest(this);
            }
        }
    }

    private List<MapCoord> GetMarkedCoords()
    {
        List<MapCoord> coords = [];
        int count = Math.Min(BlackSouls_CoordCols.Length, BlackSouls_CoordRows.Length);
        for (int index = 0; index < count; index++)
        {
            coords.Add(new MapCoord
            {
                col = BlackSouls_CoordCols[index],
                row = BlackSouls_CoordRows[index]
            });
        }

        return coords;
    }
}

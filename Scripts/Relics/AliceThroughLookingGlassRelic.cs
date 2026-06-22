using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class AliceThroughLookingGlassRelic : ModRelicTemplate
{
    private const int EnchantCount = 4;
    private const int StraightRouteCount = 3;
    private const int SingleNodeMergeThreshold = 2;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _targetActIndex = -1;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(EnchantCount)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<AscensionEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    [SavedProperty]
    public int BlackSouls_TargetActIndex
    {
        get => _targetActIndex;
        set
        {
            AssertMutable();
            _targetActIndex = value;
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        BlackSouls_TargetActIndex = Owner.RunState.CurrentActIndex + 1;

        foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, DynamicVars.Cards.IntValue),
            player: Owner,
            enchantment: ModelDb.Enchantment<AscensionEnchantment>(),
            amount: DynamicVars.Cards.IntValue))
        {
            CardCmd.Enchant<AscensionEnchantment>(card, 1m);
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex == BlackSouls_TargetActIndex)
        {
            ApplyStraightRoutes(map);
        }

        return map;
    }

    private static void ApplyStraightRoutes(ActMap map)
    {
        MapPoint[,]? grid = TryGetGrid(map);
        if (grid == null)
        {
            return;
        }

        int columnCount = grid.GetLength(0);
        int rowCount = grid.GetLength(1);
        int[] routeColumns = GetRouteColumns(columnCount);
        List<List<MapPoint>> activeRows = [];
        HashSet<MapPoint> activePoints = [];

        foreach (MapPoint point in map.GetAllMapPoints())
        {
            foreach (MapPoint child in point.Children.ToList())
            {
                point.RemoveChildPoint(child);
            }
        }

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            List<MapPoint> row = map.GetPointsInRow(rowIndex)
                .OrderBy(point => point.coord.col)
                .ToList();
            if (row.Count == 0)
            {
                activeRows.Add([]);
                continue;
            }

            int routeCount = row.Count <= SingleNodeMergeThreshold
                ? 1
                : Math.Min(StraightRouteCount, row.Count);
            List<MapPoint> selectedPoints = SelectRoutePoints(row, routeColumns, routeCount);
            MovePointsToRouteColumns(grid, selectedPoints, routeColumns, rowIndex);

            foreach (MapPoint point in row.Except(selectedPoints))
            {
                RemovePointFromGrid(grid, point);
            }

            activeRows.Add(selectedPoints.OrderBy(point => point.coord.col).ToList());
            foreach (MapPoint point in selectedPoints)
            {
                activePoints.Add(point);
            }
        }

        foreach (MapPoint point in map.startMapPoints.ToList())
        {
            if (!activePoints.Contains(point))
            {
                map.startMapPoints.Remove(point);
            }
        }

        for (int rowIndex = 0; rowIndex < activeRows.Count - 1; rowIndex++)
        {
            List<MapPoint> currentRow = activeRows[rowIndex];
            List<MapPoint> nextRow = activeRows[rowIndex + 1];
            if (currentRow.Count == 0 || nextRow.Count == 0)
            {
                continue;
            }

            if (currentRow.Count == 1 || nextRow.Count == 1)
            {
                foreach (MapPoint current in currentRow)
                {
                    foreach (MapPoint next in nextRow)
                    {
                        current.AddChildPoint(next);
                    }
                }

                continue;
            }

            int routeCount = Math.Min(StraightRouteCount, Math.Min(currentRow.Count, nextRow.Count));

            for (int routeIndex = 0; routeIndex < routeCount; routeIndex++)
            {
                currentRow[routeIndex].AddChildPoint(nextRow[routeIndex]);
            }
        }

        ConnectLastRouteRowToBoss(map, activeRows);
    }

    private static MapPoint[,]? TryGetGrid(ActMap map)
    {
        System.Reflection.PropertyInfo? gridProperty = map.GetType().GetProperty(
            "Grid",
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic);
        return gridProperty?.GetValue(map) as MapPoint[,];
    }

    private static int[] GetRouteColumns(int columnCount)
    {
        if (columnCount <= 0)
        {
            return [];
        }

        if (columnCount == 1)
        {
            return [0];
        }

        int left = Math.Clamp(columnCount / 4, 0, columnCount - 1);
        int middle = Math.Clamp(columnCount / 2, 0, columnCount - 1);
        int right = Math.Clamp((columnCount - 1) * 3 / 4, 0, columnCount - 1);
        return new[] { left, middle, right }
            .Distinct()
            .Order()
            .ToArray();
    }

    private static List<MapPoint> SelectRoutePoints(List<MapPoint> row, int[] routeColumns, int routeCount)
    {
        List<MapPoint> selected = [];
        foreach (int routeColumn in routeColumns.Take(routeCount))
        {
            MapPoint? point = row
                .Except(selected)
                .OrderBy(candidate => Math.Abs(candidate.coord.col - routeColumn))
                .ThenBy(candidate => candidate.coord.col)
                .FirstOrDefault();
            if (point != null)
            {
                selected.Add(point);
            }
        }

        return selected
            .OrderBy(point => point.coord.col)
            .ToList();
    }

    private static void MovePointsToRouteColumns(
        MapPoint[,] grid,
        List<MapPoint> selectedPoints,
        int[] routeColumns,
        int rowIndex)
    {
        for (int routeIndex = 0; routeIndex < selectedPoints.Count; routeIndex++)
        {
            MapPoint point = selectedPoints[routeIndex];
            int targetColumn = selectedPoints.Count == 1
                ? routeColumns[Math.Min(routeColumns.Length - 1, routeColumns.Length / 2)]
                : routeColumns[Math.Min(routeIndex, routeColumns.Length - 1)];
            MovePoint(grid, point, targetColumn, rowIndex);
        }
    }

    private static void MovePoint(MapPoint[,] grid, MapPoint point, int targetColumn, int targetRow)
    {
        RemovePointFromGrid(grid, point);
        if (IsInGrid(grid, targetColumn, targetRow))
        {
            point.coord.col = targetColumn;
            point.coord.row = targetRow;
            grid[targetColumn, targetRow] = point;
        }
    }

    private static void RemovePointFromGrid(MapPoint[,] grid, MapPoint point)
    {
        if (IsInGrid(grid, point.coord.col, point.coord.row) && ReferenceEquals(grid[point.coord.col, point.coord.row], point))
        {
            grid[point.coord.col, point.coord.row] = null!;
            return;
        }

        for (int col = 0; col < grid.GetLength(0); col++)
        {
            for (int row = 0; row < grid.GetLength(1); row++)
            {
                if (ReferenceEquals(grid[col, row], point))
                {
                    grid[col, row] = null!;
                    return;
                }
            }
        }
    }

    private static bool IsInGrid(MapPoint[,] grid, int col, int row)
    {
        return col >= 0 && col < grid.GetLength(0) && row >= 0 && row < grid.GetLength(1);
    }

    private static void ConnectLastRouteRowToBoss(ActMap map, List<List<MapPoint>> activeRows)
    {
        List<MapPoint>? lastRouteRow = activeRows.LastOrDefault(row => row.Count > 0);
        if (lastRouteRow == null)
        {
            return;
        }

        HashSet<MapPoint> bosses = [];
        AddBossPoint(bosses, map.BossMapPoint);
        AddBossPoint(bosses, map.SecondBossMapPoint);
        bosses.ExceptWith(lastRouteRow);

        foreach (MapPoint current in lastRouteRow)
        {
            foreach (MapPoint boss in bosses)
            {
                current.AddChildPoint(boss);
            }
        }
    }

    private static void AddBossPoint(HashSet<MapPoint> bosses, MapPoint? point)
    {
        if (point is { PointType: MapPointType.Boss })
        {
            bosses.Add(point);
        }
    }
}

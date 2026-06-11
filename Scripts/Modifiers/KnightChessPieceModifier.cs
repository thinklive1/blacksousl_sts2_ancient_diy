using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

public sealed class KnightChessPieceModifier : ModModifierTemplate
{
    private const string ModifierIconPath = "res://bs_ancient/assets/images/relics/KnightChessPieceRelic.png";
    private const int MaxColumnDistance = 2;

    private bool _active;

    [SavedProperty]
    public bool BlackSouls_Active
    {
        get => _active;
        set
        {
            AssertMutable();
            _active = value;
        }
    }

    public override ModifierAssetProfile AssetProfile => new(ModifierIconPath);

    public void Configure(IRunState runState)
    {
        AssertMutable();
        BlackSouls_Active = true;
        AddKnightConnections(runState.Map);
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (BlackSouls_Active)
        {
            AddKnightConnections(map);
        }

        return map;
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        if (BlackSouls_Active)
        {
            AddKnightConnections(runState.Map);
        }
    }

    private static void AddKnightConnections(ActMap map)
    {
        foreach (MapPoint point in map.GetAllMapPoints())
        {
            int nextRow = point.coord.row + 1;
            foreach (MapPoint child in map.GetPointsInRow(nextRow))
            {
                if (Math.Abs(child.coord.col - point.coord.col) <= MaxColumnDistance)
                {
                    point.AddChildPoint(child);
                }
            }
        }
    }
}

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
    private const int MaxStraightRoutes = 8;
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
        List<List<MapPoint>> rows = Enumerable
            .Range(0, map.GetRowCount())
            .Select(row => map.GetPointsInRow(row).OrderBy(point => point.coord.col).ToList())
            .Where(row => row.Count > 0)
            .ToList();

        foreach (MapPoint point in rows.SelectMany(row => row))
        {
            foreach (MapPoint child in point.Children.ToList())
            {
                point.RemoveChildPoint(child);
            }
        }

        for (int rowIndex = 0; rowIndex < rows.Count - 1; rowIndex++)
        {
            List<MapPoint> currentRow = rows[rowIndex];
            List<MapPoint> nextRow = rows[rowIndex + 1];
            int routeCount = Math.Min(MaxStraightRoutes, Math.Min(currentRow.Count, nextRow.Count));

            for (int routeIndex = 0; routeIndex < routeCount; routeIndex++)
            {
                currentRow[routeIndex].AddChildPoint(nextRow[routeIndex]);
            }
        }
    }
}

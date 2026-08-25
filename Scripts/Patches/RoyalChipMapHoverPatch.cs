using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Shows the complete Royal Chip wager when hovering a marked map node.</summary>
public sealed class RoyalChipMapHoverPatch : IPatchMethod
{
    public static string PatchId => "royal_chip_map_hover";
    public static string Description => "Show Royal Chip wager, penalty, condition, and reward on marked nodes.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NMapPoint), "OnFocus", ignoreIfMissing: true)];

    [HarmonyPostfix]
    public static void Postfix(NMapPoint __instance)
    {
        if (__instance.Point == null
            || __instance.State == MapPointState.Traveled
            || !TryGetPendingGamble(__instance.Point, out RoyalChipGambleData gamble))
        {
            return;
        }

        LocString title = new("relics", "BS_ANCIENT_RELIC_ROYAL_CHIP_MAP_HOVER.title");
        LocString description = new("relics", "BS_ANCIENT_RELIC_ROYAL_CHIP_MAP_HOVER.description");
        description.Add("Wager", RoyalChipRelic.FormatWager(gamble));
        description.Add("Penalty", RoyalChipRelic.FormatPenalty(gamble));
        description.Add("Condition", RoyalChipRelic.FormatCondition(gamble));
        description.Add("Reward", RoyalChipRelic.FormatReward(gamble));

        HoverTip tip = new(title, description)
        {
            ShouldOverrideTextOverflow = false
        };
        NHoverTipSet.CreateAndShow(__instance, tip, HoverTip.GetHoverTipAlignment(__instance));
    }

    private static bool TryGetPendingGamble(MapPoint point, out RoyalChipGambleData gamble)
    {
        foreach (RoyalChipRelic relic in point.Quests.OfType<RoyalChipRelic>())
        {
            if (relic.TryGetPendingGamble(point.coord, out gamble))
            {
                return true;
            }
        }

        gamble = null!;
        return false;
    }
}

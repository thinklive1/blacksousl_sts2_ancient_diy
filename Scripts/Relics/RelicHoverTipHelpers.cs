using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides helper methods for Relic Hover Tip Helpers.</summary>
internal static class RelicHoverTipHelpers
{
    public static IHoverTip Details(ModRelicTemplate relic, string suffix = "details")
    {
        LocString title = new("relics", $"{relic.Id.Entry}.{suffix}.title");
        LocString description = new("relics", $"{relic.Id.Entry}.{suffix}.description");
        relic.DynamicVars.AddTo(description);
        return new HoverTip(title, description);
    }
}

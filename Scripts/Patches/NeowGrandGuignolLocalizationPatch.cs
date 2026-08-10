using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Neow Grand Guignol Localization.</summary>
[HarmonyPatch]
public static class NeowGrandGuignolLocalizationPatch
{
    private const string AncientTableName = "ancients";

    private static readonly Dictionary<string, string> ChineseReplacements = new()
    {
        ["NEOW.title"] = "古兰.吉涅尔",
        ["NEOW.epithet"] = "暗黑舞台装置",
        ["NEOW.pages.INITIAL.description"] = "帷幕升起，古兰.吉涅尔等待着你的选择。",
        ["NEOW.EVENT.description"] = "帷幕升起，古兰.吉涅尔等待着你的选择。",
        ["NEOW.talk.firstVisitEver.0-0.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.ANY.0-0r.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.ANY.1-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.ANY.2-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.ANY.3-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.ANY.4-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.IRONCLAD.0-0.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.IRONCLAD.1-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.IRONCLAD.2-0.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.IRONCLAD.2-1.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.IRONCLAD.2-2.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.SILENT.0-0.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.SILENT.1-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.SILENT.2-0.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.SILENT.2-2.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.DEFECT.0-0.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.DEFECT.1-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.DEFECT.2-0.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.DEFECT.2-2.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.NECROBINDER.0-0.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.NECROBINDER.0-2.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.NECROBINDER.1-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.NECROBINDER.2-1.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.REGENT.0-1.ancient"] = "出发……演员……你的剧本是杀死高塔顶端的傲慢之人",
        ["NEOW.talk.REGENT.1-0r.ancient"] = "帷幕……已经升起……",
        ["NEOW.talk.REGENT.2-1.ancient"] = "帷幕……已经升起……"
    };

    private static readonly Dictionary<string, string> EnglishReplacements = ChineseReplacements.Keys
        .ToDictionary(key => key, GetEnglishReplacement);

    [HarmonyPatch(typeof(LocTable), nameof(LocTable.GetRawText))]
    [HarmonyPrefix]
    public static bool GetRawTextPrefix(LocTable __instance, string key, ref string __result)
    {
        if (!TryGetReplacement(__instance, key, out string? replacement))
        {
            return true;
        }

        __result = replacement!;
        return false;
    }

    [HarmonyPatch(typeof(LocTable), nameof(LocTable.GetLocString))]
    [HarmonyPrefix]
    public static bool GetLocStringPrefix(LocTable __instance, string key, ref LocString __result)
    {
        if (!TryGetReplacement(__instance, key, out _))
        {
            return true;
        }

        __result = new LocString(AncientTableName, key);
        return false;
    }

    [HarmonyPatch(typeof(LocTable), nameof(LocTable.HasEntry))]
    [HarmonyPostfix]
    public static void HasEntryPostfix(LocTable __instance, string key, ref bool __result)
    {
        __result = __result || TryGetReplacement(__instance, key, out _);
    }

    [HarmonyPatch(typeof(LocTable), nameof(LocTable.IsLocalKey))]
    [HarmonyPostfix]
    public static void IsLocalKeyPostfix(LocTable __instance, string key, ref bool __result)
    {
        __result = __result || TryGetReplacement(__instance, key, out _);
    }

    [HarmonyPatch(typeof(LocTable), nameof(LocTable.GetLocStringsWithPrefix))]
    [HarmonyPostfix]
    public static void GetLocStringsWithPrefixPostfix(LocTable __instance, string keyPrefix, ref IReadOnlyList<LocString> __result)
    {
        if (!BsAncientConfig.ReplaceNeowAppearance || !IsAncientTable(__instance))
        {
            return;
        }

        IReadOnlyList<LocString> currentResult = __result;
        LocString[] additionalLocStrings = ChineseReplacements.Keys
            .Where(key => key.StartsWith(keyPrefix, StringComparison.Ordinal) && currentResult.All(loc => loc.LocEntryKey != key))
            .Select(key => new LocString(AncientTableName, key))
            .ToArray();

        if (additionalLocStrings.Length == 0)
        {
            return;
        }

        __result = __result.Concat(additionalLocStrings).ToArray();
    }

    private static bool TryGetReplacement(LocTable table, string key, out string? replacement)
    {
        replacement = null;
        Dictionary<string, string> replacements = GetCurrentReplacements();
        return BsAncientConfig.ReplaceNeowAppearance
            && IsAncientTable(table)
            && replacements.TryGetValue(key, out replacement);
    }

    private static bool IsAncientTable(LocTable table)
    {
        if (LocManager.Instance == null)
        {
            return false;
        }

        try
        {
            return ReferenceEquals(LocManager.Instance.GetTable(AncientTableName), table);
        }
        catch (LocException)
        {
            return false;
        }
    }

    private static Dictionary<string, string> GetCurrentReplacements()
    {
        string languageCode = I18N.ResolveCurrentLanguageCode();
        return languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || languageCode.Equals("zhs", StringComparison.OrdinalIgnoreCase)
            ? ChineseReplacements
            : EnglishReplacements;
    }

    private static string GetEnglishReplacement(string key)
    {
        return key switch
        {
            "NEOW.title" => "Grand Guignol",
            "NEOW.epithet" => "Dark Stage Device",
            "NEOW.pages.INITIAL.description" or "NEOW.EVENT.description" => "The curtain rises. Grand Guignol awaits your choice.",
            "NEOW.talk.firstVisitEver.0-0.ancient"
                or "NEOW.talk.ANY.0-0r.ancient"
                or "NEOW.talk.IRONCLAD.0-0.ancient"
                or "NEOW.talk.SILENT.0-0.ancient"
                or "NEOW.talk.DEFECT.0-0.ancient"
                or "NEOW.talk.NECROBINDER.0-0.ancient"
                or "NEOW.talk.NECROBINDER.0-2.ancient"
                or "NEOW.talk.REGENT.0-1.ancient"
                => "Go forth... actor... your script is to kill the arrogant one at the top of the Spire.",
            _ => "The curtain... has already risen..."
        };
    }
}

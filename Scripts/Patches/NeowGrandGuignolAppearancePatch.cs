using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace BlackSouls.Scripts;

[HarmonyPatch]
public static class NeowGrandGuignolAppearancePatch
{
    private const string BackgroundScenePath = "res://bs_ancient/assets/scenes/grand_guignol_ancient.tscn";
    private const string MapIconPath = "res://bs_ancient/assets/images/map/grand_guignol.png";
    private const string MapIconOutlinePath = "res://bs_ancient/assets/images/map/grand_guignol_outline.png";

    [HarmonyPatch(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))]
    [HarmonyPrefix]
    public static bool CreateBackgroundScenePrefix(EventModel __instance, ref PackedScene __result)
    {
        if (!BsAncientConfig.ReplaceNeowAppearance || __instance is not Neow)
        {
            return true;
        }

        __result = ResourceLoader.Load<PackedScene>(BackgroundScenePath);
        return false;
    }

    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.MapIcon), MethodType.Getter)]
    [HarmonyPostfix]
    public static void MapIconPostfix(AncientEventModel __instance, ref Texture2D __result)
    {
        if (BsAncientConfig.ReplaceNeowAppearance && __instance is Neow)
        {
            __result = ResourceLoader.Load<Texture2D>(MapIconPath);
        }
    }

    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.MapIconOutline), MethodType.Getter)]
    [HarmonyPostfix]
    public static void MapIconOutlinePostfix(AncientEventModel __instance, ref Texture2D __result)
    {
        if (BsAncientConfig.ReplaceNeowAppearance && __instance is Neow)
        {
            __result = ResourceLoader.Load<Texture2D>(MapIconOutlinePath);
        }
    }

    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.RunHistoryIcon), MethodType.Getter)]
    [HarmonyPostfix]
    public static void RunHistoryIconPostfix(AncientEventModel __instance, ref Texture2D __result)
    {
        if (BsAncientConfig.ReplaceNeowAppearance && __instance is Neow)
        {
            __result = ResourceLoader.Load<Texture2D>(MapIconPath);
        }
    }

    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.RunHistoryIconOutline), MethodType.Getter)]
    [HarmonyPostfix]
    public static void RunHistoryIconOutlinePostfix(AncientEventModel __instance, ref Texture2D __result)
    {
        if (BsAncientConfig.ReplaceNeowAppearance && __instance is Neow)
        {
            __result = ResourceLoader.Load<Texture2D>(MapIconOutlinePath);
        }
    }

    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.MapNodeAssetPaths), MethodType.Getter)]
    [HarmonyPostfix]
    public static void MapNodeAssetPathsPostfix(AncientEventModel __instance, ref IEnumerable<string> __result)
    {
        if (BsAncientConfig.ReplaceNeowAppearance && __instance is Neow)
        {
            __result = [MapIconPath, MapIconOutlinePath];
        }
    }

    [HarmonyPatch(typeof(EventModel), nameof(EventModel.GetAssetPaths))]
    [HarmonyPostfix]
    public static void GetAssetPathsPostfix(EventModel __instance, ref IEnumerable<string> __result)
    {
        if (BsAncientConfig.ReplaceNeowAppearance && __instance is Neow)
        {
            __result = __result.Append(BackgroundScenePath);
        }
    }
}

using BlackSouls.Scripts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BlackSouls.Scripts.Patches;

/// <summary>Renders map nodes as unknown nodes while the Black Thing is owned.</summary>
[HarmonyPatch]
public static class BlackThingMapVisualPatch
{
    private static readonly string UnknownIconPath = ImageHelper.GetImagePath(string.Concat(
        "atlases/ui_atlas.sprites/map/icons/", "map_unknown.tres"));

    private static readonly string UnknownOutlinePath = ImageHelper.GetImagePath(string.Concat(
        "atlases/compressed.sprites/map/", "map_unknown_outline.tres"));

    [HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Ready))]
    [HarmonyPostfix]
    public static void NormalMapPointReadyPostfix(NNormalMapPoint __instance)
    {
        ApplyUnknownVisuals(__instance);
    }

    [HarmonyPatch(typeof(NMapPoint), nameof(NMapPoint.RefreshVisualsInstantly))]
    [HarmonyPostfix]
    public static void RefreshVisualsPostfix(NMapPoint __instance)
    {
        if (__instance is NNormalMapPoint mapPoint)
        {
            ApplyUnknownVisuals(mapPoint);
        }
    }

    [HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.OnSelected))]
    [HarmonyPostfix]
    public static void MapPointSelectedPostfix(NNormalMapPoint __instance)
    {
        ApplyUnknownVisuals(__instance);
    }

    private static void ApplyUnknownVisuals(NNormalMapPoint mapPoint)
    {
        if (!HasBlackThing(mapPoint))
        {
            return;
        }

        TextureRect? icon = mapPoint.GetNodeOrNull<TextureRect>("%Icon");
        TextureRect? outline = mapPoint.GetNodeOrNull<TextureRect>("%Outline");
        TextureRect? questIcon = mapPoint.GetNodeOrNull<TextureRect>("%QuestIcon");
        if (icon == null || outline == null)
        {
            return;
        }

        icon.Texture = ResourceLoader.Load<Texture2D>(UnknownIconPath, null, ResourceLoader.CacheMode.Reuse);
        outline.Texture = ResourceLoader.Load<Texture2D>(UnknownOutlinePath, null, ResourceLoader.CacheMode.Reuse);
        if (questIcon != null)
        {
            questIcon.Visible = false;
        }
    }

    private static bool HasBlackThing(NNormalMapPoint mapPoint)
    {
        return mapPoint.Point != null
            && NMapScreen.Instance != null
            && mapPoint.GetTree() != null
            && MegaCrit.Sts2.Core.Runs.RunManager.Instance.DebugOnlyGetState()?.Players
                .Any(player => player.GetRelic<BlackThingMythRelic>() != null) == true;
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)]
public static class WormSmokeRestSiteIconPatch
{
    public static void Postfix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is WormSmokeRestSiteOption)
        {
            __result = ResourceLoader.Load<Texture2D>(WormSmokeRestSiteOption.SmokeIconPath);
        }
    }
}

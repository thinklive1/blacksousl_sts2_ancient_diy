using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

public class WormSmokeRestSiteIconPatch : IPatchMethod
{
    public static string PatchId => "worm_smoke_rest_site_icon";
    public static string Description => "Use Worm Smoke event image as the rest site option icon.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)];

    public static void Postfix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is WormSmokeRestSiteOption)
        {
            __result = ResourceLoader.Load<Texture2D>(WormSmokeRestSiteOption.SmokeIconPath);
        }
    }
}

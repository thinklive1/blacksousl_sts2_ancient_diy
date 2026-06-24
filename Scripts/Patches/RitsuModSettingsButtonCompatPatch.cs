using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace BlackSouls.Scripts.Patches;

[HarmonyPatch(typeof(NSettingsButton), "ConnectSignals")]
internal static class RitsuModSettingsButtonCompatPatch
{
    private const string RitsuSettingsButtonName = "RitsuLibModSettingsButton";
    private const string SelectionReticleNodeName = "SelectionReticle";

    public static void Prefix(NSettingsButton __instance)
    {
        if (__instance.Name.ToString() != RitsuSettingsButtonName ||
            __instance.HasNode(SelectionReticleNodeName))
        {
            return;
        }

        NSelectionReticle reticle = new()
        {
            Name = SelectionReticleNodeName,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        reticle.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        __instance.AddChild(reticle);
    }
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(ActModel), nameof(ActModel.SetSharedAncientSubset))]
public static class GrandGuignolAncientSpawnPatch
{
    public static void Prefix(List<AncientEventModel> sharedAncientSubset)
    {
        sharedAncientSubset.RemoveAll(ancient => ancient is GrandGuignolAncient);
    }
}

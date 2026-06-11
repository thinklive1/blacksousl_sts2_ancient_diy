using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

public class GrandGuignolAncientSpawnPatch : IPatchMethod
{
    public static string PatchId => "grand_guignol_ancient_spawn_filter";
    public static string Description => "Keep Grand Guignol collection-only ancient out of normal ancient subsets.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ActModel), nameof(ActModel.SetSharedAncientSubset))];

    public static void Prefix(List<AncientEventModel> sharedAncientSubset)
    {
        sharedAncientSubset.RemoveAll(ancient => ancient is GrandGuignolAncient);
    }
}

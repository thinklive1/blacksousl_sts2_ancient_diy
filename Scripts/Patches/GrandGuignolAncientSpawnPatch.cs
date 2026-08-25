using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Grand Guignol Ancient Spawn.</summary>
public class GrandGuignolAncientSpawnPatch : IPatchMethod
{
    public static string PatchId => "grand_guignol_ancient_spawn_filter";
    public static string Description => "Keep Grand Guignol collection-only ancient out of normal ancient subsets.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(ActModel),
            nameof(ActModel.SetSharedAncientSubset),
            [typeof(List<AncientEventModel>)],
            ignoreIfMissing: true)];

    public static void Prefix(List<AncientEventModel> sharedAncientSubset)
    {
        sharedAncientSubset.RemoveAll(ancient => ancient is GrandGuignolAncient);
    }
}

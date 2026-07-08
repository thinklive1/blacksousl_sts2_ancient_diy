using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Grand Guignol Relic Collection.</summary>
public class GrandGuignolRelicCollectionPatch : IPatchMethod
{
    public static string PatchId => "grand_guignol_relic_collection";
    public static string Description => "Create Grand Guignol ancient stats so its relics can be shown in the collection.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))];

    public static void Prefix(RelicRarity relicRarity, UnlockState unlockState)
    {
        if (relicRarity != RelicRarity.Ancient)
        {
            return;
        }

        SaveManager.Instance.Progress.GetOrCreateAncientStats(ModelDb.AncientEvent<GrandGuignolAncient>().Id);
    }
}

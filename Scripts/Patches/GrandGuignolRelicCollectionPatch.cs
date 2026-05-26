using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
public static class GrandGuignolRelicCollectionPatch
{
    public static void Prefix(RelicRarity relicRarity, UnlockState unlockState)
    {
        if (relicRarity != RelicRarity.Ancient)
        {
            return;
        }

        SaveManager.Instance.Progress.GetOrCreateAncientStats(ModelDb.AncientEvent<GrandGuignolAncient>().Id);
    }
}

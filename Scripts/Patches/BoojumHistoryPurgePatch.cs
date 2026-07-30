using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace BlackSouls.Scripts.Patches;

/// <summary>Deletes a lost Boojum run only after the game's normal history save has completed.</summary>
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRunHistory))]
public static class BoojumLostRunHistoryPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunHistory history)
    {
        BoojumHistoryPurge.PurgeCurrentRunHistoryAfterSave(history);
    }
}

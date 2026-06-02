using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(CardCmd))]
public static class EvilQiAfflictionProtectionPatch
{
    [HarmonyPatch(nameof(CardCmd.Afflict), [typeof(AfflictionModel), typeof(CardModel), typeof(decimal)])]
    [HarmonyPrefix]
    public static bool AfflictPrefix(AfflictionModel affliction, CardModel card, ref Task<AfflictionModel?> __result)
    {
        if (card.Affliction is EvilQiAffliction && affliction is not EvilQiAffliction)
        {
            __result = Task.FromResult<AfflictionModel?>(null);
            return false;
        }

        return true;
    }

    [HarmonyPatch(nameof(CardCmd.ClearAffliction))]
    [HarmonyPrefix]
    public static bool ClearAfflictionPrefix(CardModel card)
    {
        return card.Affliction is not EvilQiAffliction;
    }
}

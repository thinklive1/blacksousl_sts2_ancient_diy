using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

public sealed class BaphometFavorCanPlayPatch : IPatchMethod
{
    public static string PatchId => "baphomet_favor_can_play";
    public static string Description => "Allow Baphomet's Favor to play energy-cost cards without changing displayed cost.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [
            new(
                typeof(CardModel),
                nameof(CardModel.CanPlay),
                [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()],
                ignoreIfMissing: true)
        ];

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel? preventer)
    {
        if (__result
            || __instance.Owner?.GetRelic<BaphometFavorRelic>() == null
            || !CostsEnergy(__instance))
        {
            return;
        }

        reason &= ~UnplayableReason.EnergyCostTooHigh;

        if (reason == UnplayableReason.None)
        {
            preventer = null;
            __result = true;
        }
    }

    private static bool CostsEnergy(CardModel card)
    {
        return card.EnergyCost.CostsX || card.EnergyCost.GetWithModifiers(CostModifiers.Local) > 0;
    }
}

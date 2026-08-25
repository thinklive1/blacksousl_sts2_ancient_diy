using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies mod-specific exceptions to the vanilla card play restrictions.</summary>
public sealed class CardCanPlayCompatibilityPatch : IPatchMethod
{
    public static string PatchId => "card_can_play_compatibility";
    public static string Description => "Apply Unlock and Baphomet's Favor card-play exceptions.";
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
    public static void Postfix(
        CardModel __instance,
        ref bool __result,
        ref UnplayableReason reason,
        ref AbstractModel? preventer)
    {
        if (__result)
        {
            return;
        }

        if (__instance.Enchantment is UnlockEnchantment unlock && unlock.IsUnlockAvailable(__instance))
        {
            reason &= ~UnplayableReason.BlockedByCardLogic;
            reason &= ~UnplayableReason.EnergyCostTooHigh;
            reason &= ~UnplayableReason.StarCostTooHigh;
        }

        if (__instance.Owner?.GetRelic<BaphometFavorRelic>() != null && CostsEnergy(__instance))
        {
            reason &= ~UnplayableReason.EnergyCostTooHigh;
        }

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

using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies behavior patches for Unlock Enchantment Can Play.</summary>
public class UnlockEnchantmentCanPlayPatch : IPatchMethod
{
    public static string PatchId => "unlock_enchantment_can_play";
    public static string Description => "Allow Unlock-enchanted cards to bypass play restrictions once per combat.";
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
        if (__result || __instance.Enchantment is not UnlockEnchantment unlock || !unlock.IsUnlockAvailable(__instance))
        {
            return;
        }

        reason &= ~UnplayableReason.BlockedByCardLogic;
        reason &= ~UnplayableReason.EnergyCostTooHigh;
        reason &= ~UnplayableReason.StarCostTooHigh;

        if (reason == UnplayableReason.None)
        {
            preventer = null;
            __result = true;
        }
    }
}

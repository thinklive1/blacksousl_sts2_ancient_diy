using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Patches;

/// <summary>Retains the vanilla card identity, cost, rarity and artwork.</summary>
public static class DevourLifePatches
{
    [HarmonyPatch(typeof(DevourLife), "OnPlay")]
    private static class Play
    {
        private static bool Prefix(DevourLife __instance, PlayerChoiceContext choiceContext, ref Task __result)
        {
            __result = __instance.IsUpgraded
                ? PowerCmd.Apply<DevourLifeDrawPower>(choiceContext, __instance.Owner.Creature, 1m, __instance.Owner.Creature, __instance)
                : PowerCmd.Apply<DevourLifeDiscardPower>(choiceContext, __instance.Owner.Creature, 1m, __instance.Owner.Creature, __instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(DevourLife), "OnUpgrade")]
    private static class Upgrade
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(DevourLife), "get_ExtraHoverTips")]
    private static class HoverTips
    {
        private static void Postfix(ref IEnumerable<IHoverTip> __result) =>
            __result = [HoverTipFactory.FromCard<Soul>()];
    }
}

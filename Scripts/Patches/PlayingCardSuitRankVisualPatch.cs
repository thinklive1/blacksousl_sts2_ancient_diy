using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts.Patches;

/// <summary>Renders face-card labels for playing-card suit enchantments.</summary>
[HarmonyPatch(typeof(NCard), "UpdateEnchantmentVisuals")]
public static class PlayingCardSuitRankVisualPatch
{
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    [HarmonyPostfix]
    public static void CardVisualsPostfix(NCard __instance)
    {
        if (__instance.Model is not BalatroPlayingCard card
            || !__instance.IsNodeReady()
            || __instance.Visibility != ModelVisibility.Visible)
        {
            return;
        }

        TextureRect? portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null)
        {
            portrait.Texture = PreloadManager.Cache.GetTexture2D(card.PortraitPath);
        }
    }

    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        if (__instance.Model?.Enchantment is not PlayingCardSuitEnchantment suitEnchantment)
        {
            return;
        }

        Label? amountLabel = __instance.EnchantmentTab.GetNodeOrNull<Label>("Label");
        if (amountLabel != null)
        {
            amountLabel.Text = PlayingCardSuitEnchantment.GetRankDisplayText(suitEnchantment.Amount);
        }
    }
}

using BlackSouls.Scripts.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BlackSouls.Scripts.Patches;

[HarmonyPatch]
public static class AliceCurseEasterEggVisualPatch
{
    private const string GarbledLocKey = "BS_ANCIENT_POWER_ALICE_CURSE_EASTER_EGG_POWER.garbled";
    private const string AliceCursePortraitPath = "res://bs_ancient/assets/images/cards/AliceCurseCard.png";
    private const string CanvasGroupMaskMaterialPath = "res://scenes/cards/card_canvas_group_mask_material.tres";

    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientBorderRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientBorder");

    private static readonly AccessTools.FieldRef<NCard, Control> AncientBannerRef =
        AccessTools.FieldRefAccess<NCard, Control>("_ancientBanner");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientPortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientPortrait");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientTextBgRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientTextBg");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> BannerRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_banner");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> FrameRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_frame");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portrait");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitBorderRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portraitBorder");

    private static readonly AccessTools.FieldRef<NCard, CanvasGroup> PortraitCanvasGroupRef =
        AccessTools.FieldRefAccess<NCard, CanvasGroup>("_portraitCanvasGroup");

    private static readonly AccessTools.FieldRef<NCard, Material?> CanvasGroupMaskMaterialRef =
        AccessTools.FieldRefAccess<NCard, Material?>("_canvasGroupMaskMaterial");

    private static LocString GarbledLocString => new("powers", GarbledLocKey);

    private static string GarbledText => GarbledLocString.GetFormattedText();

    private static Texture2D AliceCursePortrait =>
        PreloadManager.Cache.GetTexture2D(AliceCursePortraitPath);

    private static bool IsActive()
    {
        ICombatState? combatState = CombatManager.Instance?.DebugOnlyGetState();
        return combatState?.Players.Any(player => player.Creature.Powers.OfType<AliceCurseEasterEggPower>().Any()) ?? false;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
    [HarmonyPostfix]
    public static void CardTitlePostfix(CardModel __instance, ref string __result)
    {
        if (ShouldGarbledCard(__instance))
        {
            __result = GarbledText;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), [typeof(PileType), typeof(Creature)])]
    [HarmonyPostfix]
    public static void CardDescriptionPostfix(CardModel __instance, ref string __result)
    {
        if (ShouldGarbledCard(__instance))
        {
            __result = GarbledText;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForUpgradePreview))]
    [HarmonyPostfix]
    public static void CardUpgradeDescriptionPostfix(CardModel __instance, ref string __result)
    {
        if (ShouldGarbledCard(__instance))
        {
            __result = GarbledText;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
    [HarmonyPostfix]
    public static void CardPortraitPostfix(CardModel __instance, ref Texture2D __result)
    {
        if (ShouldGarbledCard(__instance) && __instance is not AliceCurseCard)
        {
            __result = AliceCursePortrait;
        }
    }

    [HarmonyPatch(typeof(NCard), "Reload")]
    [HarmonyPostfix]
    public static void CardReloadPostfix(NCard __instance)
    {
        ApplyGarbledVisuals(__instance);
    }

    public static void RefreshVisibleCombatCards(Player owner)
    {
        foreach (CardModel card in owner.PlayerCombatState?.AllCards ?? Enumerable.Empty<CardModel>())
        {
            NCard? node = NCard.FindOnTable(card);
            if (node == null)
            {
                continue;
            }

            node.UpdateVisuals(node.DisplayingPile, CardPreviewMode.Normal);
            ApplyGarbledVisuals(node);
        }
    }

    private static void ApplyGarbledVisuals(NCard card)
    {
        if (!card.IsNodeReady()
            || card.Visibility != ModelVisibility.Visible
            || card.Model == null
            || !ShouldGarbledCard(card.Model))
        {
            return;
        }

        PortraitBorderRef(card).Visible = false;
        PortraitRef(card).Visible = false;
        FrameRef(card).Visible = false;
        BannerRef(card).Visible = false;

        AncientPortraitRef(card).Visible = true;
        AncientBorderRef(card).Visible = true;
        AncientTextBgRef(card).Visible = true;
        AncientBannerRef(card).Visible = true;
        AncientPortraitRef(card).Texture = AliceCursePortrait;
        AncientTextBgRef(card).Texture = GetAncientTextBg(card.Model.Type);

        ref Material? maskMaterial = ref CanvasGroupMaskMaterialRef(card);
        maskMaterial ??= PreloadManager.Cache.GetMaterial(CanvasGroupMaskMaterialPath);
        PortraitCanvasGroupRef(card).Material = maskMaterial;
    }

    private static bool ShouldGarbledCard(CardModel card)
    {
        return IsActive() && card is not DeprecatedCard;
    }

    private static Texture2D GetAncientTextBg(CardType cardType)
    {
        CardType visualType = cardType switch
        {
            CardType.None or CardType.Status or CardType.Curse => CardType.Skill,
            CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest => cardType,
            _ => CardType.Skill
        };

        string path = ImageHelper.GetImagePath(
            "atlases/compressed.sprites/card_template/ancient_card_text_bg_"
            + visualType.ToString().ToLowerInvariant()
            + ".tres"
        );
        return PreloadManager.Cache.GetTexture2D(path);
    }
}

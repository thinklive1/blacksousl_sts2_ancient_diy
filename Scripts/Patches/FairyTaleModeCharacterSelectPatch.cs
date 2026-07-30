using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Fairy Tale Mode Character Select.</summary>
public class FairyTaleModeCharacterSelectPatch : IPatchMethod
{
    private const string ToggleName = "BsAncientFairyTaleModeToggle";
    private const string TickedTexturePath = "res://images/atlases/ui_atlas.sprites/checkbox_ticked.tres";
    private const string UntickedTexturePath = "res://images/atlases/ui_atlas.sprites/checkbox_unticked.tres";
    private const string TickedSfx = "event:/sfx/ui/clicks/ui_checkbox_on";
    private const string UntickedSfx = "event:/sfx/ui/clicks/ui_checkbox_off";

    public static string PatchId => "fairy_tale_mode_character_select_toggle";
    public static string Description => "Add a Fairy Tale Mode toggle to character select (singleplayer and multiplayer).";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [
            new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeSingleplayer), ignoreIfMissing: true),
            new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost), ignoreIfMissing: true),
            new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient), ignoreIfMissing: true),
        ];

    public static void Postfix(NCharacterSelectScreen __instance)
    {
        if (__instance.GetNodeOrNull<Button>(ToggleName) != null
            || __instance.GetNodeOrNull<Label>($"{ToggleName}Warning") != null)
        {
            return;
        }

        Button toggle = new()
        {
            Name = ToggleName,
            ToggleMode = true,
            ButtonPressed = BsAncientConfig.EnableFairyTaleMode,
            Flat = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = "仅本局生效；优先于 Mod 设置中的默认值。"
        };

        TextureRect icon = new()
        {
            Name = "Icon",
            Texture = ToggleTexture(toggle.ButtonPressed),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        PositionIcon(icon);
        toggle.AddChild(icon);

        Label label = new()
        {
            Name = "Label",
            Text = "童话模式 / Fairy Tale",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        PositionLabel(label);
        toggle.AddChild(label);

        toggle.Pressed += () => OnTogglePressed(toggle, icon);
        PositionToggle(toggle);
        __instance.AddChild(toggle);

        Label warning = new()
        {
            Name = $"{ToggleName}Warning",
            Text = "多人模式可能存在恶性bug",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 0.8f, 0.4f, 0.9f)
        };
        PositionWarning(warning);
        __instance.AddChild(warning);

        BsAncientRunOptions.FairyTaleModeForNextRun = toggle.ButtonPressed;
    }

    private static void OnTogglePressed(Button toggle, TextureRect icon)
    {
        bool enabled = toggle.ButtonPressed;
        icon.Texture = ToggleTexture(enabled);
        BsAncientRunOptions.FairyTaleModeForNextRun = enabled;
        SfxCmd.Play(enabled ? TickedSfx : UntickedSfx);
    }

    private static Texture2D ToggleTexture(bool enabled)
    {
        return ResourceLoader.Load<Texture2D>(enabled ? TickedTexturePath : UntickedTexturePath);
    }

    private static void PositionIcon(TextureRect icon)
    {
        icon.SetAnchorsPreset(Control.LayoutPreset.CenterLeft);
        icon.OffsetLeft = 0f;
        icon.OffsetTop = -32f;
        icon.OffsetRight = 64f;
        icon.OffsetBottom = 32f;
    }

    private static void PositionLabel(Label label)
    {
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.OffsetLeft = 70f;
        label.OffsetTop = 0f;
        label.OffsetRight = 0f;
        label.OffsetBottom = 0f;
    }

    private static void PositionToggle(Button toggle)
    {
        toggle.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        toggle.CustomMinimumSize = new Vector2(320f, 64f);
        toggle.OffsetLeft = -360f;
        toggle.OffsetTop = -140f;
        toggle.OffsetRight = -48f;
        toggle.OffsetBottom = -76f;
    }

    private static void PositionWarning(Label warning)
    {
        warning.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        warning.OffsetLeft = -360f;
        warning.OffsetTop = -70f;
        warning.OffsetRight = -48f;
        warning.OffsetBottom = -46f;
    }
}

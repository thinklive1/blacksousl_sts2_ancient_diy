using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts;

[ModInitializer(nameof(Init))]
public class Entry
{
    // 你的modid
    public const string ModId = "bs_ancient";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);
    private static I18N? _settingsLocalization;

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        BsAncientConfig.Load(assembly);
        BsAncientRewardTypes.Register();
        _settingsLocalization = RitsuLibFramework.CreateModLocalization(
            ModId,
            "settings",
            [],
            [],
            ["res://bs_ancient/i18n/settings"],
            assembly);
        RegisterSettings();
        ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "core-patches");
        patcher.RegisterPatches<BsAncientPatchSet>();
        if (!patcher.PatchAll())
        {
            throw new InvalidOperationException("BS Ancient critical Ritsu patches failed.");
        }

        new Harmony($"{ModId}.patches").PatchAll(assembly);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
    }

    private static void RegisterSettings()
    {
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(S("displayName", "BS Ancient"))
            .WithTitle(S("title", "BS Ancient 设置"))
            .WithDescription(S("description", "这些开关会同步写入 bs_ancient_config.cfg。更改后需要重启游戏并从新一局开始生效。"))
            .AddSection("ancients", section => section
                .WithTitle(S("sections.ancients.title", "先古之民"))
                .AddToggle(
                    "only_use_mod_ancients",
                    S("onlyUseModAncients.title", "只生成 Mod 先古之民"),
                    BoolBinding(
                        "OnlyUseModAncients",
                        () => BsAncientConfig.OnlyUseModAncients,
                        value => BsAncientConfig.OnlyUseModAncients = value),
                    S("onlyUseModAncients.description", "开启后，第 2 层只会从诺登/梅贝尔中选择，第 3 层只会从普利凯特/梅贝尔中选择。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "disable_mod_ancients",
                    S("disableModAncients.title", "完全禁用 Mod 地图先古之民"),
                    BoolBinding(
                        "DisableModAncients",
                        () => BsAncientConfig.DisableModAncients,
                        value => BsAncientConfig.DisableModAncients = value),
                    S("disableModAncients.description", "开启后，诺登、普利凯特、梅贝尔不会出现在地图中。此项优先于“只生成 Mod 先古之民”。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "replace_neow_appearance",
                    S("replaceNeowAppearance.title", "替换涅奥外观"),
                    BoolBinding(
                        "ReplaceNeowAppearance",
                        () => BsAncientConfig.ReplaceNeowAppearance,
                        value => BsAncientConfig.ReplaceNeowAppearance = value),
                    S("replaceNeowAppearance.description", "开启后，将开局涅奥的外观、名称、标题和相关对话替换为古兰.吉涅尔。更改后需要重启游戏。"))
                .AddIntSlider(
                    "grand_guignol_initial_relic_chance",
                    S("grandGuignolInitialRelicChance.title", "古兰初始遗物出现概率"),
                    IntBinding(
                        "GrandGuignolInitialRelicChance",
                        () => BsAncientConfig.GrandGuignolInitialRelicChance,
                        value => BsAncientConfig.GrandGuignolInitialRelicChance = Math.Clamp(value, 0, 100)),
                    0,
                    100,
                    5,
                    value => $"{value}%",
                    S("grandGuignolInitialRelicChance.description", "控制开局正面选项被替换为古兰初始遗物的概率。更改后需要重启游戏并新开一局。"))));
    }

    private static ModSettingsText S(string key, string fallback)
    {
        return _settingsLocalization == null
            ? ModSettingsText.Literal(fallback)
            : ModSettingsText.I18N(_settingsLocalization, key, fallback);
    }

    private static IModSettingsValueBinding<bool> BoolBinding(
        string key,
        Func<bool> getter,
        Action<bool> setter)
    {
        return ModSettingsBindings.Callback(
            ModId,
            key,
            getter,
            setter,
            BsAncientConfig.Save,
            SaveScope.Global);
    }

    private static IModSettingsValueBinding<int> IntBinding(
        string key,
        Func<int> getter,
        Action<int> setter)
    {
        return ModSettingsBindings.Callback(
            ModId,
            key,
            getter,
            setter,
            BsAncientConfig.Save,
            SaveScope.Global);
    }
}

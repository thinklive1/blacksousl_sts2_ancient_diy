using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts;

/// <summary>Initializes BS Ancient settings, patches, scripts, and content registration.</summary>
[ModInitializer(nameof(Init))]
public class Entry
{
    // Stable id used for registration, logging, and persisted settings.
    public const string ModId = "bs_ancient";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);
    private static I18N? _settingsLocalization;

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        BsAncientConfig.Load(assembly);
        BsAncientRewardTypes.Register();
        // Modifiers are omitted from post-combat listeners while a CombatState exists.
        ModHelper.SubscribeForRunStateHooks(ModId, static runState =>
            runState.Modifiers.OfType<BoojumVictoryRewardModifier>());
        _settingsLocalization = RitsuLibFramework.CreateModLocalization(
            ModId,
            "settings",
            [],
            [],
            ["res://bs_ancient/i18n/settings"],
            assembly);
        RegisterSettings();
        RegisterFirstLaunchToast();
        ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "core-patches");
        patcher.RegisterPatches<BsAncientPatchSet>();
        if (!patcher.PatchAll())
        {
            throw new InvalidOperationException("BS Ancient critical Ritsu patches failed.");
        }

        new Harmony($"{ModId}.patches").PatchAll(assembly);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // Register all auto-discovered mod content.
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
    }

    private static void RegisterFirstLaunchToast()
    {
        RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(_ =>
        {
            if (BsAncientConfig.HasShownSettingsToast)
            {
                return;
            }

            RitsuToastService.ShowInfo("您可以在Mod设置内编辑各种选项");
            BsAncientConfig.HasShownSettingsToast = true;
            BsAncientConfig.Save();
        });
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
                    S("grandGuignolInitialRelicChance.description", "控制开局正面选项被替换为古兰初始遗物的概率。更改后需要重启游戏并新开一局。")))
            .AddSection("events", section => section
                .WithTitle(S("sections.events.title", "事件"))
                .AddToggle(
                    "enable_mod_events",
                    S("enableModEvents.title", "启用 Mod 事件"),
                    BoolBinding(
                        "EnableModEvents",
                        () => BsAncientConfig.EnableModEvents,
                        value => BsAncientConfig.EnableModEvents = value),
                    S("enableModEvents.description", "开启后，普通事件池中会出现 BS Ancient 的 Mod 事件。关闭后不会自然遇到这些事件。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "disable_testing_events",
                    S("disableTestingEvents.title", "禁用测试中事件"),
                    BoolBinding(
                        "DisableTestingEvents",
                        () => BsAncientConfig.DisableTestingEvents,
                        value => BsAncientConfig.DisableTestingEvents = value),
                    S("disableTestingEvents.description", "开启后，小丑、迷宫中的少女等 SAN/手镜相关测试事件不会自然出现。更改后需要重启游戏并新开一局。")))
            .AddSection("fairyTales", section => section
                .WithTitle(S("sections.fairyTales.title", "童话"))
                .WithDescription(S("sections.fairyTales.description", "童话模式包含大量强负面遗物，可能严重影响游戏体验。建议首次游玩时关闭该模式。"))
                .AddToggle(
                    "enable_fairy_tale_mode",
                    S("enableFairyTaleMode.title", "童话模式"),
                    BoolBinding(
                        "EnableFairyTaleMode",
                        () => BsAncientConfig.EnableFairyTaleMode,
                        value => BsAncientConfig.EnableFairyTaleMode = value),
                    S("enableFairyTaleMode.description", "开启后，每赢得 4 场战斗（包含 Boss 战），获得一本随机童话。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "enable_positive_fairy_tale_relics",
                    S("enablePositiveFairyTaleRelics.title", "允许正面童话"),
                    BoolBinding(
                        "EnablePositiveFairyTaleRelics",
                        () => BsAncientConfig.EnablePositiveFairyTaleRelics,
                        value => BsAncientConfig.EnablePositiveFairyTaleRelics = value),
                    S("enablePositiveFairyTaleRelics.description", "关闭后，童话模式不会随机获得正面童话遗物。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "enable_negative_fairy_tale_relics",
                    S("enableNegativeFairyTaleRelics.title", "允许负面童话"),
                    BoolBinding(
                        "EnableNegativeFairyTaleRelics",
                        () => BsAncientConfig.EnableNegativeFairyTaleRelics,
                        value => BsAncientConfig.EnableNegativeFairyTaleRelics = value),
                    S("enableNegativeFairyTaleRelics.description", "关闭后，童话模式不会随机获得负面、破坏性童话、神话和骇闻遗物。此开关优先于其他单个遗物开关。更改后需要重启游戏并新开一局。")))
            .AddSection("fairyTaleAllowList", section => section
                .WithTitle(S("sections.fairyTaleAllowList.title", "强负面童话/神话/骇闻出现开关"))
                .WithDescription(S("sections.fairyTaleAllowList.description", "以下童话、神话、骇闻默认不出现，需单独开启。更改后需要重启游戏并新开一局。"))
                .AddToggle("allow_alice_through_looking_glass",
                    S("allowAliceThroughLookingGlass.title", "童话-爱丽丝镜中棋缘"),
                    BoolBinding("AllowAliceThroughLookingGlass", () => BsAncientConfig.AllowAliceThroughLookingGlass, v => BsAncientConfig.AllowAliceThroughLookingGlass = v),
                    S("allowAliceThroughLookingGlass.description", "变化牌组"))
                .AddToggle("allow_cinderella",
                    S("allowCinderella.title", "童话-灰姑娘"),
                    BoolBinding("AllowCinderella", () => BsAncientConfig.AllowCinderella, v => BsAncientConfig.AllowCinderella = v),
                    S("allowCinderella.description", "删除牌组"))
                .AddToggle("allow_frog_princess",
                    S("allowFrogPrincess.title", "童话-青蛙公主"),
                    BoolBinding("AllowFrogPrincess", () => BsAncientConfig.AllowFrogPrincess, v => BsAncientConfig.AllowFrogPrincess = v),
                    S("allowFrogPrincess.description", "删除牌组"))
                .AddToggle("allow_greedy_dog",
                    S("allowGreedyDog.title", "童话-贪心的狗"),
                    BoolBinding("AllowGreedyDog", () => BsAncientConfig.AllowGreedyDog, v => BsAncientConfig.AllowGreedyDog = v),
                    S("allowGreedyDog.description", "变化牌组"))
                .AddToggle("allow_mermaid_princess",
                    S("allowMermaidPrincess.title", "童话-人鱼公主"),
                    BoolBinding("AllowMermaidPrincess", () => BsAncientConfig.AllowMermaidPrincess, v => BsAncientConfig.AllowMermaidPrincess = v),
                    S("allowMermaidPrincess.description", "删除牌组"))
                .AddToggle("allow_monkey_crab_battle",
                    S("allowMonkeyCrabBattle.title", "童话-猿蟹合战"),
                    BoolBinding("AllowMonkeyCrabBattle", () => BsAncientConfig.AllowMonkeyCrabBattle, v => BsAncientConfig.AllowMonkeyCrabBattle = v),
                    S("allowMonkeyCrabBattle.description", "加入诅咒"))
                .AddToggle("allow_north_wind_and_sun",
                    S("allowNorthWindAndSun.title", "童话-北风与太阳"),
                    BoolBinding("AllowNorthWindAndSun", () => BsAncientConfig.AllowNorthWindAndSun, v => BsAncientConfig.AllowNorthWindAndSun = v),
                    S("allowNorthWindAndSun.description", "变化牌组"))
                .AddToggle("allow_peter_pan",
                    S("allowPeterPan.title", "童话-彼得·潘"),
                    BoolBinding("AllowPeterPan", () => BsAncientConfig.AllowPeterPan, v => BsAncientConfig.AllowPeterPan = v),
                    S("allowPeterPan.description", "删除牌组"))
                .AddToggle("allow_ugly_duckling",
                    S("allowUglyDuckling.title", "童话-丑小鸭"),
                    BoolBinding("AllowUglyDuckling", () => BsAncientConfig.AllowUglyDuckling, v => BsAncientConfig.AllowUglyDuckling = v),
                    S("allowUglyDuckling.description", "加入牌组"))
                .AddToggle("allow_sleep_god_myth",
                    S("allowSleepGodMyth.title", "神话-睡眠之神"),
                    BoolBinding("AllowSleepGodMyth", () => BsAncientConfig.AllowSleepGodMyth, v => BsAncientConfig.AllowSleepGodMyth = v),
                    S("allowSleepGodMyth.description", "附魔牌组"))
                .AddToggle("allow_lake_god_myth",
                    S("allowLakeGodMyth.title", "神话-湖栖神"),
                    BoolBinding("AllowLakeGodMyth", () => BsAncientConfig.AllowLakeGodMyth, v => BsAncientConfig.AllowLakeGodMyth = v),
                    S("allowLakeGodMyth.description", "战斗负面状态"))
                .AddToggle("allow_dark_goat_of_the_woods_myth",
                    S("allowDarkGoatOfTheWoodsMyth.title", "神话-森之黑山羊"),
                    BoolBinding("AllowDarkGoatOfTheWoodsMyth", () => BsAncientConfig.AllowDarkGoatOfTheWoodsMyth, v => BsAncientConfig.AllowDarkGoatOfTheWoodsMyth = v),
                    S("allowDarkGoatOfTheWoodsMyth.description", "附魔牌组"))
                .AddToggle("allow_great_stag_goddess_myth",
                    S("allowGreatStagGoddessMyth.title", "神话-大鹿的女神"),
                    BoolBinding("AllowGreatStagGoddessMyth", () => BsAncientConfig.AllowGreatStagGoddessMyth, v => BsAncientConfig.AllowGreatStagGoddessMyth = v),
                    S("allowGreatStagGoddessMyth.description", "消耗堆循环"))
                .AddToggle("allow_black_thing_myth",
                    S("allowBlackThingMyth.title", "神话-黑物"),
                    BoolBinding("AllowBlackThingMyth", () => BsAncientConfig.AllowBlackThingMyth, v => BsAncientConfig.AllowBlackThingMyth = v),
                    S("allowBlackThingMyth.description", "地图节点隐藏"))
                .AddToggle("allow_shadow_demoness_myth",
                    S("allowShadowDemonessMyth.title", "神话-影之女恶魔"),
                    BoolBinding("AllowShadowDemonessMyth", () => BsAncientConfig.AllowShadowDemonessMyth, v => BsAncientConfig.AllowShadowDemonessMyth = v),
                    S("allowShadowDemonessMyth.description", "无法获得额外能量"))
                .AddToggle("allow_harald_shipman_news",
                    S("allowHaraldShipmanNews.title", "骇闻-暗医者"),
                    BoolBinding("AllowHaraldShipmanNews", () => BsAncientConfig.AllowHaraldShipmanNews, v => BsAncientConfig.AllowHaraldShipmanNews = v),
                    S("allowHaraldShipmanNews.description", "损失最大生命"))
                .AddToggle("allow_jack_ketch_news",
                    S("allowJackKetchNews.title", "骇闻-处刑人"),
                    BoolBinding("AllowJackKetchNews", () => BsAncientConfig.AllowJackKetchNews, v => BsAncientConfig.AllowJackKetchNews = v),
                    S("allowJackKetchNews.description", "第十回合重击"))));
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

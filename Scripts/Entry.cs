using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace BlackSouls.Scripts;

[ModInitializer(nameof(Init))]
public class Entry
{
    // 你的modid
    public const string ModId = "bs_ancient";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        // harmony可用，但是最好用ritsu的封装patch（TODO）
        var assembly = Assembly.GetExecutingAssembly();
        BsAncientConfig.Load(assembly);
        RegisterSettings();
        new Harmony($"{ModId}.patches").PatchAll(assembly);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
    }

    private static void RegisterSettings()
    {
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(ModSettingsText.Literal("BS Ancient"))
            .WithTitle(ModSettingsText.Literal("BS Ancient 设置"))
            .WithDescription(ModSettingsText.Literal("这些开关会同步写入 bs_ancient_config.cfg。更改后需要重启游戏并从新一局开始生效。"))
            .AddSection("ancients", section => section
                .WithTitle(ModSettingsText.Literal("先古之民"))
                .AddToggle(
                    "only_use_mod_ancients",
                    ModSettingsText.Literal("只生成 Mod 先古之民"),
                    BoolBinding(
                        "OnlyUseModAncients",
                        () => BsAncientConfig.OnlyUseModAncients,
                        value => BsAncientConfig.OnlyUseModAncients = value),
                    ModSettingsText.Literal("开启后，第 2 层只会从诺登/梅贝尔中选择，第 3 层只会从普利凯特/梅贝尔中选择。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "disable_mod_ancients",
                    ModSettingsText.Literal("完全禁用 Mod 地图先古之民"),
                    BoolBinding(
                        "DisableModAncients",
                        () => BsAncientConfig.DisableModAncients,
                        value => BsAncientConfig.DisableModAncients = value),
                    ModSettingsText.Literal("开启后，诺登、普利凯特、梅贝尔不会出现在地图中。此项优先于“只生成 Mod 先古之民”。更改后需要重启游戏并新开一局。"))
                .AddToggle(
                    "replace_neow_appearance",
                    ModSettingsText.Literal("替换涅奥外观"),
                    BoolBinding(
                        "ReplaceNeowAppearance",
                        () => BsAncientConfig.ReplaceNeowAppearance,
                        value => BsAncientConfig.ReplaceNeowAppearance = value),
                    ModSettingsText.Literal("开启后，将开局涅奥的外观、名称、标题和相关对话替换为古兰.吉涅尔。更改后需要重启游戏。"))));
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
}

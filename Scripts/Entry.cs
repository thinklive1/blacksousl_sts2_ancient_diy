using System.Reflection;
using BlackSouls.Scripts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace Blacksouls.Scripts;

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
        new Harmony($"{ModId}.patches").PatchAll(assembly);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
    }
}

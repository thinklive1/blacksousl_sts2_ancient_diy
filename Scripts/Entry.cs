using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace Sts2Diy.Scripts;

/// <summary>Initializes RitsuLib, Harmony patches, and auto-discovered mod content.</summary>
[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "sts2diy";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Harmony patches in Scripts/Patches can modify vanilla game behavior.
        new Harmony($"{ModId}.patches").PatchAll(assembly);

        // RitsuLib handles Godot scripts and content templates added later.
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        Logger.Info("STS2 DIY initialized.");
    }
}

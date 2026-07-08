using System.Reflection;
using System.Text.Json;

namespace BlackSouls.Scripts;

/// <summary>Stores and persists BS Ancient configuration values.</summary>
public static class BsAncientConfig
{
    private const string ConfigFileName = "bs_ancient_config.cfg";

    private static string? _configPath;

    public static bool OnlyUseModAncients = true;
    public static bool DisableModAncients = false;
    public static bool ReplaceNeowAppearance = true;
    public static bool EnableModEvents = true;
    public static bool DisableTestingEvents = true;
    public static bool EnableFairyTaleMode = false;
    public static bool HasShownSettingsToast = false;
    public static int GrandGuignolInitialRelicChance = 30;

    // Per-relic toggles for destructive fairy tales, all disabled by default.
    public static bool AllowAliceThroughLookingGlass = false;
    public static bool AllowCinderella = false;
    public static bool AllowFrogPrincess = false;
    public static bool AllowGreedyDog = false;
    public static bool AllowMermaidPrincess = false;
    public static bool AllowMonkeyCrabBattle = false;
    public static bool AllowNorthWindAndSun = false;
    public static bool AllowPeterPan = false;
    public static bool AllowUglyDuckling = false;
    public static bool AllowSleepGodMyth = false;
    public static bool AllowLakeGodMyth = false;
    public static bool AllowDarkGoatOfTheWoodsMyth = false;

    public static void Load(Assembly assembly)
    {
        string configPath = GetConfigPath(assembly);
        _configPath = configPath;
        if (!File.Exists(configPath))
        {
            SaveDefault(configPath);
            return;
        }

        string json = File.ReadAllText(configPath);
        bool shouldWriteBack = !HasAllConfigFields(json);
        FileConfig? config = JsonSerializer.Deserialize<FileConfig>(json);
        if (config == null)
        {
            SaveDefault(configPath);
            return;
        }

        OnlyUseModAncients = config.OnlyUseModAncients;
        DisableModAncients = config.DisableModAncients;
        ReplaceNeowAppearance = config.ReplaceNeowAppearance;
        EnableModEvents = config.EnableModEvents;
        DisableTestingEvents = config.DisableTestingEvents;
        EnableFairyTaleMode = config.EnableFairyTaleMode;
        HasShownSettingsToast = config.HasShownSettingsToast;
        GrandGuignolInitialRelicChance = Math.Clamp(config.GrandGuignolInitialRelicChance, 0, 100);
        AllowAliceThroughLookingGlass = config.AllowAliceThroughLookingGlass;
        AllowCinderella = config.AllowCinderella;
        AllowFrogPrincess = config.AllowFrogPrincess;
        AllowGreedyDog = config.AllowGreedyDog;
        AllowMermaidPrincess = config.AllowMermaidPrincess;
        AllowMonkeyCrabBattle = config.AllowMonkeyCrabBattle;
        AllowNorthWindAndSun = config.AllowNorthWindAndSun;
        AllowPeterPan = config.AllowPeterPan;
        AllowUglyDuckling = config.AllowUglyDuckling;
        AllowSleepGodMyth = config.AllowSleepGodMyth;
        AllowLakeGodMyth = config.AllowLakeGodMyth;
        AllowDarkGoatOfTheWoodsMyth = config.AllowDarkGoatOfTheWoodsMyth;

        if (GrandGuignolInitialRelicChance != config.GrandGuignolInitialRelicChance)
        {
            shouldWriteBack = true;
        }

        if (shouldWriteBack)
        {
            SaveCurrent(configPath);
        }
    }

    public static void Save()
    {
        if (string.IsNullOrWhiteSpace(_configPath))
        {
            return;
        }

        SaveCurrent(_configPath);
    }

    private static string GetConfigPath(Assembly assembly)
    {
        string? assemblyDirectory = Path.GetDirectoryName(assembly.Location);
        if (string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            assemblyDirectory = AppContext.BaseDirectory;
        }

        return Path.Combine(assemblyDirectory, ConfigFileName);
    }

    private static void SaveDefault(string configPath)
    {
        FileConfig config = new()
        {
            OnlyUseModAncients = OnlyUseModAncients,
            DisableModAncients = DisableModAncients,
            ReplaceNeowAppearance = ReplaceNeowAppearance,
            EnableModEvents = EnableModEvents,
            DisableTestingEvents = DisableTestingEvents,
            EnableFairyTaleMode = EnableFairyTaleMode,
            HasShownSettingsToast = HasShownSettingsToast,
            GrandGuignolInitialRelicChance = GrandGuignolInitialRelicChance,
            AllowAliceThroughLookingGlass = AllowAliceThroughLookingGlass,
            AllowCinderella = AllowCinderella,
            AllowFrogPrincess = AllowFrogPrincess,
            AllowGreedyDog = AllowGreedyDog,
            AllowMermaidPrincess = AllowMermaidPrincess,
            AllowMonkeyCrabBattle = AllowMonkeyCrabBattle,
            AllowNorthWindAndSun = AllowNorthWindAndSun,
            AllowPeterPan = AllowPeterPan,
            AllowUglyDuckling = AllowUglyDuckling,
            AllowSleepGodMyth = AllowSleepGodMyth,
            AllowLakeGodMyth = AllowLakeGodMyth,
            AllowDarkGoatOfTheWoodsMyth = AllowDarkGoatOfTheWoodsMyth,
        };
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private static void SaveCurrent(string configPath)
    {
        FileConfig config = new()
        {
            OnlyUseModAncients = OnlyUseModAncients,
            DisableModAncients = DisableModAncients,
            ReplaceNeowAppearance = ReplaceNeowAppearance,
            EnableModEvents = EnableModEvents,
            DisableTestingEvents = DisableTestingEvents,
            EnableFairyTaleMode = EnableFairyTaleMode,
            HasShownSettingsToast = HasShownSettingsToast,
            GrandGuignolInitialRelicChance = GrandGuignolInitialRelicChance,
            AllowAliceThroughLookingGlass = AllowAliceThroughLookingGlass,
            AllowCinderella = AllowCinderella,
            AllowFrogPrincess = AllowFrogPrincess,
            AllowGreedyDog = AllowGreedyDog,
            AllowMermaidPrincess = AllowMermaidPrincess,
            AllowMonkeyCrabBattle = AllowMonkeyCrabBattle,
            AllowNorthWindAndSun = AllowNorthWindAndSun,
            AllowPeterPan = AllowPeterPan,
            AllowUglyDuckling = AllowUglyDuckling,
            AllowSleepGodMyth = AllowSleepGodMyth,
            AllowLakeGodMyth = AllowLakeGodMyth,
            AllowDarkGoatOfTheWoodsMyth = AllowDarkGoatOfTheWoodsMyth,
        };
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private static bool HasAllConfigFields(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(nameof(FileConfig.OnlyUseModAncients), out _)
                && root.TryGetProperty(nameof(FileConfig.DisableModAncients), out _)
                && root.TryGetProperty(nameof(FileConfig.ReplaceNeowAppearance), out _)
                && root.TryGetProperty(nameof(FileConfig.EnableModEvents), out _)
                && root.TryGetProperty(nameof(FileConfig.DisableTestingEvents), out _)
                && root.TryGetProperty(nameof(FileConfig.EnableFairyTaleMode), out _)
                && root.TryGetProperty(nameof(FileConfig.HasShownSettingsToast), out _)
                && root.TryGetProperty(nameof(FileConfig.GrandGuignolInitialRelicChance), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowAliceThroughLookingGlass), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowCinderella), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowFrogPrincess), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowGreedyDog), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowMermaidPrincess), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowMonkeyCrabBattle), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowNorthWindAndSun), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowPeterPan), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowUglyDuckling), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowSleepGodMyth), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowLakeGodMyth), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowDarkGoatOfTheWoodsMyth), out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class FileConfig
    {
        public bool OnlyUseModAncients { get; set; } = true;

        public bool DisableModAncients { get; set; } = false;

        public bool ReplaceNeowAppearance { get; set; } = true;

        public bool EnableModEvents { get; set; } = true;

        public bool DisableTestingEvents { get; set; } = true;

        public bool EnableFairyTaleMode { get; set; } = false;

        public bool HasShownSettingsToast { get; set; } = false;

        public int GrandGuignolInitialRelicChance { get; set; } = 30;

        public bool AllowAliceThroughLookingGlass { get; set; } = false;

        public bool AllowCinderella { get; set; } = false;

        public bool AllowFrogPrincess { get; set; } = false;

        public bool AllowGreedyDog { get; set; } = false;

        public bool AllowMermaidPrincess { get; set; } = false;

        public bool AllowMonkeyCrabBattle { get; set; } = false;

        public bool AllowNorthWindAndSun { get; set; } = false;

        public bool AllowPeterPan { get; set; } = false;

        public bool AllowUglyDuckling { get; set; } = false;

        public bool AllowSleepGodMyth { get; set; } = false;

        public bool AllowLakeGodMyth { get; set; } = false;

        public bool AllowDarkGoatOfTheWoodsMyth { get; set; } = false;
    }
}

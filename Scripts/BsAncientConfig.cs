using System.Reflection;
using System.Text.Json;

namespace BlackSouls.Scripts;

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
            GrandGuignolInitialRelicChance = GrandGuignolInitialRelicChance
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
            GrandGuignolInitialRelicChance = GrandGuignolInitialRelicChance
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
                && root.TryGetProperty(nameof(FileConfig.GrandGuignolInitialRelicChance), out _);
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
    }
}

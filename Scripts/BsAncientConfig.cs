using System.Reflection;
using System.Text.Json;

namespace BlackSouls.Scripts;

public static class BsAncientConfig
{
    private const string ConfigFileName = "bs_ancient_config.cfg";

    public static bool OnlyUseModAncients = true;
    public static bool ReplaceNeowAppearance = true;

    public static void Load(Assembly assembly)
    {
        string configPath = GetConfigPath(assembly);
        if (!File.Exists(configPath))
        {
            SaveDefault(configPath);
            return;
        }

        string json = File.ReadAllText(configPath);
        FileConfig? config = JsonSerializer.Deserialize<FileConfig>(json);
        if (config == null)
        {
            SaveDefault(configPath);
            return;
        }

        OnlyUseModAncients = config.OnlyUseModAncients;
        ReplaceNeowAppearance = config.ReplaceNeowAppearance;
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
            ReplaceNeowAppearance = ReplaceNeowAppearance
        };
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private sealed class FileConfig
    {
        public bool OnlyUseModAncients { get; set; } = true;

        public bool ReplaceNeowAppearance { get; set; } = true;
    }
}

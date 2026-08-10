using System.Reflection;
using System.Text.Json;

namespace BlackSouls.Scripts;

/// <summary>Stores and persists BS Ancient configuration values.</summary>
public static class BsAncientConfig
{
    private const string ConfigFileName = "bs_ancient_config.cfg";
    private const string CorruptConfigSuffix = ".corrupt";
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private static string? _configPath;

    public static bool OnlyUseModAncients = true;
    public static bool DisableModAncients = false;
    public static bool ReplaceNeowAppearance = true;
    public static bool EnableModEvents = true;
    public static bool DisableTestingEvents = true;
    public static bool EnableFairyTaleMode = false;
    public static bool EnablePositiveFairyTaleRelics = true;
    public static bool EnableNegativeFairyTaleRelics = true;
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
    public static bool AllowGreatStagGoddessMyth = false;
    public static bool AllowBlackThingMyth = false;
    public static bool AllowShadowDemonessMyth = false;
    public static bool AllowHaraldShipmanNews = false;
    public static bool AllowJackKetchNews = false;

    public static void Load(Assembly assembly)
    {
        LoadFromPath(GetConfigPath(assembly), message => Entry.Logger.Warn(message));
    }

    internal static void LoadFromPath(string configPath, Action<string>? warn = null)
    {
        _configPath = configPath;
        ResetToDefaults();
        if (!File.Exists(configPath))
        {
            TrySaveCurrent(configPath, warn);
            return;
        }

        try
        {
            string json = File.ReadAllText(configPath);
            bool shouldWriteBack = !HasAllConfigFields(json);
            FileConfig config = JsonSerializer.Deserialize<FileConfig>(json)
                ?? throw new JsonException("The configuration root was null.");

            Apply(config);
            if (GrandGuignolInitialRelicChance != config.GrandGuignolInitialRelicChance)
            {
                shouldWriteBack = true;
            }

            if (shouldWriteBack)
            {
                TrySaveCurrent(configPath, warn);
            }
        }
        catch (JsonException exception)
        {
            RecoverCorruptConfig(configPath, exception, warn);
        }
        catch (NotSupportedException exception)
        {
            RecoverCorruptConfig(configPath, exception, warn);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            warn?.Invoke($"Could not read BS Ancient configuration '{configPath}'. Defaults will be used: {exception.Message}");
        }
    }

    public static void Save()
    {
        if (string.IsNullOrWhiteSpace(_configPath))
        {
            return;
        }

        TrySaveCurrent(_configPath, message => Entry.Logger.Warn(message));
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

    private static void Apply(FileConfig config)
    {
        OnlyUseModAncients = config.OnlyUseModAncients;
        DisableModAncients = config.DisableModAncients;
        ReplaceNeowAppearance = config.ReplaceNeowAppearance;
        EnableModEvents = config.EnableModEvents;
        DisableTestingEvents = config.DisableTestingEvents;
        EnableFairyTaleMode = config.EnableFairyTaleMode;
        EnablePositiveFairyTaleRelics = config.EnablePositiveFairyTaleRelics;
        EnableNegativeFairyTaleRelics = config.EnableNegativeFairyTaleRelics;
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
        AllowGreatStagGoddessMyth = config.AllowGreatStagGoddessMyth;
        AllowBlackThingMyth = config.AllowBlackThingMyth;
        AllowShadowDemonessMyth = config.AllowShadowDemonessMyth;
        AllowHaraldShipmanNews = config.AllowHaraldShipmanNews;
        AllowJackKetchNews = config.AllowJackKetchNews;
    }

    private static FileConfig CaptureCurrent()
    {
        return new FileConfig
        {
            OnlyUseModAncients = OnlyUseModAncients,
            DisableModAncients = DisableModAncients,
            ReplaceNeowAppearance = ReplaceNeowAppearance,
            EnableModEvents = EnableModEvents,
            DisableTestingEvents = DisableTestingEvents,
            EnableFairyTaleMode = EnableFairyTaleMode,
            EnablePositiveFairyTaleRelics = EnablePositiveFairyTaleRelics,
            EnableNegativeFairyTaleRelics = EnableNegativeFairyTaleRelics,
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
            AllowGreatStagGoddessMyth = AllowGreatStagGoddessMyth,
            AllowBlackThingMyth = AllowBlackThingMyth,
            AllowShadowDemonessMyth = AllowShadowDemonessMyth,
            AllowHaraldShipmanNews = AllowHaraldShipmanNews,
            AllowJackKetchNews = AllowJackKetchNews,
        };
    }

    private static void ResetToDefaults()
    {
        Apply(new FileConfig());
    }

    private static void RecoverCorruptConfig(string configPath, Exception exception, Action<string>? warn)
    {
        string backupPath = GetAvailableCorruptBackupPath(configPath);
        try
        {
            File.Move(configPath, backupPath);
            warn?.Invoke(
                $"Invalid BS Ancient configuration was moved to '{backupPath}'. "
                + $"Defaults were restored: {exception.Message}");
        }
        catch (Exception backupException) when (backupException is IOException or UnauthorizedAccessException)
        {
            warn?.Invoke(
                $"Invalid BS Ancient configuration could not be backed up. Defaults will replace it: "
                + $"{backupException.Message}");
        }

        ResetToDefaults();
        TrySaveCurrent(configPath, warn);
    }

    private static string GetAvailableCorruptBackupPath(string configPath)
    {
        string candidate = configPath + CorruptConfigSuffix;
        for (int suffix = 1; File.Exists(candidate); suffix++)
        {
            candidate = $"{configPath}{CorruptConfigSuffix}.{suffix}";
        }

        return candidate;
    }

    private static bool TrySaveCurrent(string configPath, Action<string>? warn)
    {
        string? directory = Path.GetDirectoryName(configPath);
        string temporaryPath = $"{configPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(CaptureCurrent(), SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, configPath, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            warn?.Invoke($"Could not save BS Ancient configuration '{configPath}': {exception.Message}");
            return false;
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                warn?.Invoke($"Could not remove temporary BS Ancient configuration '{temporaryPath}': {exception.Message}");
            }
        }
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
                && root.TryGetProperty(nameof(FileConfig.EnablePositiveFairyTaleRelics), out _)
                && root.TryGetProperty(nameof(FileConfig.EnableNegativeFairyTaleRelics), out _)
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
                && root.TryGetProperty(nameof(FileConfig.AllowDarkGoatOfTheWoodsMyth), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowGreatStagGoddessMyth), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowBlackThingMyth), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowShadowDemonessMyth), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowHaraldShipmanNews), out _)
                && root.TryGetProperty(nameof(FileConfig.AllowJackKetchNews), out _);
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

        public bool EnablePositiveFairyTaleRelics { get; set; } = true;

        public bool EnableNegativeFairyTaleRelics { get; set; } = true;

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

        public bool AllowGreatStagGoddessMyth { get; set; } = false;

        public bool AllowBlackThingMyth { get; set; } = false;

        public bool AllowShadowDemonessMyth { get; set; } = false;

        public bool AllowHaraldShipmanNews { get; set; } = false;

        public bool AllowJackKetchNews { get; set; } = false;
    }
}

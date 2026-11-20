using System.Text.Json;
using BlackSouls.Scripts;

namespace BsAncient.Tests;

public sealed class BsAncientConfigTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"bs_ancient_tests_{Guid.NewGuid():N}");

    public BsAncientConfigTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void MissingConfigCreatesCompleteDefaultsWithoutTemporaryFiles()
    {
        string path = ConfigPath();

        BsAncientConfig.LoadFromPath(path);

        Assert.True(File.Exists(path));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.True(document.RootElement.GetProperty("EnablePositiveFairyTaleRelics").GetBoolean());
        Assert.True(document.RootElement.GetProperty("EnableNegativeFairyTaleRelics").GetBoolean());
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void LegacyConfigKeepsValuesAndAddsNewDefaults()
    {
        string path = ConfigPath();
        File.WriteAllText(path, """
            {
              "EnableFairyTaleMode": true,
              "GrandGuignolInitialRelicChance": 999
            }
            """);

        BsAncientConfig.LoadFromPath(path);

        Assert.True(BsAncientConfig.EnableFairyTaleMode);
        Assert.Equal(100, BsAncientConfig.GrandGuignolInitialRelicChance);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.True(document.RootElement.TryGetProperty("AllowJackKetchNews", out _));
        Assert.True(document.RootElement.GetProperty("EnableNodeAncient").GetBoolean());
        Assert.True(document.RootElement.GetProperty("EnablePrickettAncient").GetBoolean());
        Assert.True(document.RootElement.GetProperty("EnableMabelAncient").GetBoolean());
        Assert.True(document.RootElement.GetProperty("EnableLorinaAncient").GetBoolean());
        Assert.Equal(100, document.RootElement.GetProperty("GrandGuignolInitialRelicChance").GetInt32());
    }

    [Fact]
    public void MalformedConfigIsBackedUpAndReplacedWithDefaults()
    {
        string path = ConfigPath();
        File.WriteAllText(path, "{ definitely not json");
        List<string> warnings = [];

        BsAncientConfig.LoadFromPath(path, warnings.Add);

        Assert.Single(Directory.GetFiles(_directory, "*.corrupt*"));
        Assert.True(BsAncientConfig.OnlyUseModAncients);
        Assert.Equal(30, BsAncientConfig.GrandGuignolInitialRelicChance);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Contains(warnings, warning => warning.Contains("Defaults were restored", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("0.107.1")]
    [InlineData("0.111.0")]
    [InlineData("0.113.0")]
    public void DesktopCompatibilityVersionsShareTheRootConfigWithoutResolvingGodotPaths(string version)
    {
        string assemblyPath = Path.Combine(_directory, "compat", version, "bs_ancient.impl.dll");
        string expected = ConfigPath();
        Assert.Equal(expected, BsAncientConfig.ResolveConfigPath(assemblyPath, false,
            () => throw new InvalidOperationException("Desktop must not need a user-data fallback.")));
    }

    [Fact]
    public void FlatDesktopInstallStillUsesItsAssemblyDirectory()
    {
        Assert.Equal(ConfigPath(), BsAncientConfig.ResolveConfigPath(Path.Combine(_directory, "bs_ancient.dll"),
            false, () => throw new InvalidOperationException("Desktop must not need a user-data fallback.")));
    }

    [Fact]
    public void NewerCompatibilitySettingsMigrateAndSubsequentSavesAndRestartsUseTheRoot()
    {
        string legacy = LegacyCompatibilityConfigPath();
        string current = ConfigPath();
        File.WriteAllText(current, """{"GrandGuignolInitialRelicChance":30}""");
        File.WriteAllText(legacy, """{"GrandGuignolInitialRelicChance":85,"EnableLorinaAncient":false}""");
        File.SetLastWriteTimeUtc(current, DateTime.UtcNow.AddDays(-3));
        File.SetLastWriteTimeUtc(legacy, DateTime.UtcNow.AddDays(-2));
        string legacyContents = File.ReadAllText(legacy);

        BsAncientConfig.LoadDesktopConfig(current, legacy);

        Assert.Equal(85, BsAncientConfig.GrandGuignolInitialRelicChance);
        Assert.False(BsAncientConfig.EnableLorinaAncient);
        BsAncientConfig.GrandGuignolInitialRelicChance = 75;
        BsAncientConfig.Save();
        BsAncientConfig.GrandGuignolInitialRelicChance = 30;
        BsAncientConfig.LoadDesktopConfig(current, legacy);
        Assert.Equal(75, BsAncientConfig.GrandGuignolInitialRelicChance);
        Assert.Equal(legacyContents, File.ReadAllText(legacy));
        using JsonDocument saved = JsonDocument.Parse(File.ReadAllText(current));
        Assert.Equal(75, saved.RootElement.GetProperty("GrandGuignolInitialRelicChance").GetInt32());
        Assert.True(saved.RootElement.TryGetProperty("AllowJackKetchNews", out _));
    }

    [Fact]
    public void MissingRootConfigMigratesCompatibilitySettingsWithoutChangingTheLegacyFile()
    {
        string legacy = LegacyCompatibilityConfigPath();
        File.WriteAllText(legacy, """{"GrandGuignolInitialRelicChance":95}""");
        string contents = File.ReadAllText(legacy);

        BsAncientConfig.LoadDesktopConfig(ConfigPath(), legacy);

        Assert.Equal(95, BsAncientConfig.GrandGuignolInitialRelicChance);
        Assert.True(File.Exists(ConfigPath()));
        Assert.Equal(contents, File.ReadAllText(legacy));
    }

    [Fact]
    public void OlderCompatibilityConfigDoesNotOverrideMoreRecentRootSettings()
    {
        string legacy = LegacyCompatibilityConfigPath();
        File.WriteAllText(legacy, """{"GrandGuignolInitialRelicChance":30}""");
        File.WriteAllText(ConfigPath(), """{"GrandGuignolInitialRelicChance":70}""");
        File.SetLastWriteTimeUtc(legacy, DateTime.UtcNow.AddDays(-2));
        File.SetLastWriteTimeUtc(ConfigPath(), DateTime.UtcNow.AddDays(-1));

        BsAncientConfig.LoadDesktopConfig(ConfigPath(), legacy);

        Assert.Equal(70, BsAncientConfig.GrandGuignolInitialRelicChance);
    }

    [Fact]
    public void CorruptCompatibilityConfigCannotReplaceValidRootSettings()
    {
        string legacy = LegacyCompatibilityConfigPath();
        File.WriteAllText(legacy, "{ invalid legacy json");
        File.WriteAllText(ConfigPath(), """{"GrandGuignolInitialRelicChance":65}""");
        File.SetLastWriteTimeUtc(ConfigPath(), DateTime.UtcNow.AddDays(-2));
        List<string> warnings = [];

        BsAncientConfig.LoadDesktopConfig(ConfigPath(), legacy, warnings.Add);

        Assert.Equal(65, BsAncientConfig.GrandGuignolInitialRelicChance);
        Assert.Contains(warnings, warning => warning.Contains("Could not migrate", StringComparison.Ordinal));
        Assert.Equal("{ invalid legacy json", File.ReadAllText(legacy));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("bs_ancient.impl.dll", false)]
    [InlineData("bs_ancient.impl.dll", true)]
    public void MissingOrRelativeAssemblyLocationUsesPersistentUserData(string? assemblyPath, bool mobile)
    {
        string expected = Path.Combine(_directory, "mod_data", "bs_ancient_config.cfg");
        Assert.Equal(expected, BsAncientConfig.ResolveConfigPath(assemblyPath, mobile, () => expected));
    }

    [Fact]
    public void MobileUsesUserDataEvenWhenItsAssemblyHasAnAbsolutePath()
    {
        string expected = Path.Combine(_directory, "user_data", "bs_ancient_config.cfg");
        Assert.Equal(expected, BsAncientConfig.ResolveConfigPath(Path.Combine(_directory, "bs_ancient.impl.dll"),
            true, () => expected));
    }

    [Fact]
    public void LegacyMobileConfigMigratesWithoutLosingSettingsAndFutureSavesUseTheNewPath()
    {
        string legacy = ConfigPath();
        string current = Path.Combine(_directory, "user_data", "bs_ancient_config.cfg");
        File.WriteAllText(legacy, """{"EnableFairyTaleMode":true,"EnableLorinaAncient":false}""");

        BsAncientConfig.LoadWithLegacyFallback(current, [legacy]);

        Assert.True(BsAncientConfig.EnableFairyTaleMode);
        Assert.False(BsAncientConfig.EnableLorinaAncient);
        Assert.True(File.Exists(current));
        string oldContents = File.ReadAllText(legacy);
        BsAncientConfig.EnableFairyTaleMode = false;
        BsAncientConfig.Save();
        Assert.Equal(oldContents, File.ReadAllText(legacy));
        using JsonDocument saved = JsonDocument.Parse(File.ReadAllText(current));
        Assert.False(saved.RootElement.GetProperty("EnableFairyTaleMode").GetBoolean());
        Assert.True(saved.RootElement.TryGetProperty("AllowJackKetchNews", out _));
    }

    [Fact]
    public void ExistingUserDataConfigIsNotOverwrittenByLegacySettings()
    {
        string legacy = Path.Combine(_directory, "legacy.cfg");
        string current = ConfigPath();
        File.WriteAllText(legacy, """{"EnableFairyTaleMode":true}""");
        File.WriteAllText(current, """{"EnableFairyTaleMode":false}""");

        BsAncientConfig.LoadWithLegacyFallback(current, [legacy]);

        Assert.False(BsAncientConfig.EnableFairyTaleMode);
    }

    [Fact]
    public void LegacyLookupDoesNotResolveRelativePathsAgainstTheWorkingDirectory()
    {
        Assert.Empty(BsAncientConfig.GetLegacyConfigPaths("bs_ancient.impl.dll", "mods/bs_ancient", ""));
    }

    private string ConfigPath()
    {
        return Path.Combine(_directory, "bs_ancient_config.cfg");
    }

    private string LegacyCompatibilityConfigPath()
    {
        string directory = Path.Combine(_directory, "compat", "0.113.0");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "bs_ancient_config.cfg");
    }
}

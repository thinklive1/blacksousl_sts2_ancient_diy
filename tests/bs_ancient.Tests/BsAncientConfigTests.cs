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

    private string ConfigPath()
    {
        return Path.Combine(_directory, "bs_ancient_config.cfg");
    }
}

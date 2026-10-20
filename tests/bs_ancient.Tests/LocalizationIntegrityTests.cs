using System.Text.Json;
using System.Text.RegularExpressions;

namespace BsAncient.Tests;

public sealed partial class LocalizationIntegrityTests
{
    [Fact]
    public void EnglishAndChineseLocalizationHaveMatchingFilesKeysAndVariables()
    {
        string root = FindRepositoryRoot();
        string englishDirectory = Path.Combine(root, "bs_ancient", "localization", "eng");
        string chineseDirectory = Path.Combine(root, "bs_ancient", "localization", "zhs");
        string[] englishFiles = Directory.GetFiles(englishDirectory, "*.json")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray()!;
        string[] chineseFiles = Directory.GetFiles(chineseDirectory, "*.json")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray()!;

        Assert.Equal(englishFiles, chineseFiles);
        foreach (string fileName in englishFiles)
        {
            Dictionary<string, string> english = ReadFlatLocalization(Path.Combine(englishDirectory, fileName));
            Dictionary<string, string> chinese = ReadFlatLocalization(Path.Combine(chineseDirectory, fileName));
            Assert.Equal(english.Keys.Order(StringComparer.Ordinal), chinese.Keys.Order(StringComparer.Ordinal));

            foreach (string key in english.Keys)
            {
                Assert.Equal(
                    ExtractFormatVariables(english[key]),
                    ExtractFormatVariables(chinese[key]));
            }
        }
    }

    [Fact]
    public void SettingsLocalizationHasMatchingKeysAndVariables()
    {
        string settingsDirectory = Path.Combine(FindRepositoryRoot(), "bs_ancient", "i18n", "settings");
        Dictionary<string, string> english = ReadFlatLocalization(Path.Combine(settingsDirectory, "eng.json"));
        Dictionary<string, string> chinese = ReadFlatLocalization(Path.Combine(settingsDirectory, "zhs.json"));

        Assert.Equal(english.Keys.Order(StringComparer.Ordinal), chinese.Keys.Order(StringComparer.Ordinal));
        foreach (string key in english.Keys)
        {
            Assert.Equal(ExtractFormatVariables(english[key]), ExtractFormatVariables(chinese[key]));
        }
    }

    [Theory]
    [InlineData("zhs", true, "本回合所有抽牌行为的数量")]
    [InlineData("zhs", false, "本回合暂时失去抽牌加成")]
    [InlineData("eng", true, "Whenever you draw cards this turn")]
    [InlineData("eng", false, "The draw bonus is temporarily disabled this turn")]
    public void DanceShoesDescriptionsFormatBothIndicatorStates(string language, bool enabled, string expected)
    {
        string path = Path.Combine(FindRepositoryRoot(), "bs_ancient", "localization", language, "powers.json");
        Dictionary<string, string> entries = ReadFlatLocalization(path);
        var formatter = SmartFormat.Smart.CreateDefaultSmartFormat();
        foreach (string suffix in new[] { "description", "smartDescription" })
        {
            string formatted = formatter.Format(entries[$"BS_ANCIENT_POWER_RED_DANCE_SHOES_POWER.{suffix}"],
                new Dictionary<string, object>
                {
                    ["Amount"] = BlackSouls.Scripts.RedDanceShoesPower.GetIndicatorAmount(enabled)
                });
            Assert.StartsWith(expected, formatted);
            Assert.DoesNotContain("{Amount", formatted);
        }
    }

    private static Dictionary<string, string> ReadFlatLocalization(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        return document.RootElement.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.GetString() ?? string.Empty,
            StringComparer.Ordinal);
    }

    private static string[] ExtractFormatVariables(string value)
    {
        return SmartFormatVariableRegex().Matches(value)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "bs_ancient.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the BS Ancient repository root.");
    }

    [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)(?::[^{}]*)?\}", RegexOptions.CultureInvariant)]
    private static partial Regex SmartFormatVariableRegex();
}

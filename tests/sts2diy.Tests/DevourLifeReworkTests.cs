using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class DevourLifeReworkTests
{
    [Theory]
    [InlineData(typeof(DevourLifeDiscardPower), PileType.Discard)]
    [InlineData(typeof(DevourLifeDrawPower), PileType.Draw)]
    public void UpgradedAndNormalCopiesKeepSeparateDestinations(Type type, PileType expected)
    {
        object power = RuntimeHelpers.GetUninitializedObject(type);
        Assert.Equal(expected, typeof(DevourLifeReworkBase).GetProperty("Destination",
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(power));
    }

    [Theory]
    [InlineData("zhs", "每有一名敌人")]
    [InlineData("eng", "for each enemy")]
    public void DescriptionsSpecifyOneSoulPerEnemy(string language, string expected)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string dir = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "cards.json")));
        string description = cards.RootElement.GetProperty("DEVOUR_LIFE.description").GetString()!;
        Assert.Contains(expected, description);
        Assert.Contains("{IfUpgraded:show:", description);
        using var powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "powers.json")));
        foreach (string destination in new[] { "DRAW", "DISCARD" })
            Assert.Contains(expected, powers.RootElement.GetProperty(
                $"STS2DIY_POWER_DEVOUR_LIFE_{destination}_POWER.description").GetString());
    }
}

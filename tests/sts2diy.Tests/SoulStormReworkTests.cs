using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Sts2Diy.Scripts.Patches;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class SoulStormReworkTests
{
    [Fact]
    public void DamageIsSixAndNoLongerScalesWithExhaustedSouls()
    {
        Type values = typeof(SoulStormPatches).GetNestedType("Values", BindingFlags.NonPublic)!;
        object?[] args = [null];
        Assert.Equal(false, values.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args));
        DynamicVar variable = Assert.Single(Assert.IsAssignableFrom<IEnumerable<DynamicVar>>(args[0]));
        Assert.IsType<DamageVar>(variable);
        Assert.Equal(6m, variable.BaseValue);
        Type upgrade = typeof(SoulStormPatches).GetNestedType("Upgrade", BindingFlags.NonPublic)!;
        Assert.Equal(false, upgrade.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null));
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void CardDescriptionUsesFixedDamageAndUpgradedDestination(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        string text = data.RootElement.GetProperty("SOUL_STORM.description").GetString()!;
        Assert.Contains("{Damage:diff()}", text);
        Assert.Contains("{IfUpgraded:show:", text);
        Assert.DoesNotContain("ExtraDamage", text);
        Assert.DoesNotContain("CalculatedDamage", text);
    }
}

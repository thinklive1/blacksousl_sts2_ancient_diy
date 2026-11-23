using System.Text.Json;
using System.Xml.Linq;
using Loader = Sts2Diy.Compatibility.Entry;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class CompatibilityLoaderTests
{
    [Theory]
    [InlineData(111, 0, "0.111.0")]
    [InlineData(111, 1, "0.111.0")]
    [InlineData(112, 0, "0.111.0")]
    [InlineData(113, 0, "0.113.0")]
    [InlineData(113, 1, "0.113.0")]
    public void LoaderSelectsExactlyOneImplementation(int minor, int patch, string folder) =>
        Assert.Equal(folder, Loader.SelectCompatibilityFolder(new(new Version(0, minor, patch), "test")));

    [Theory]
    [InlineData(107)]
    [InlineData(110)]
    [InlineData(114)]
    [InlineData(115)]
    public void UnsupportedApisAreRejectedInsteadOfGuessing(int minor) =>
        Assert.Throws<NotSupportedException>(() => Loader.SelectCompatibilityFolder(new(new Version(0, minor, 0), "test")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sts2.dll")]
    public void MissingOrRelativeAssemblyLocationsDoNotBecomeGameDirectories(string? path) =>
        Assert.Null(Loader.FindReleaseInfoPath(path));

    [Fact]
    public void BundleContainsTheLoaderAndBothImplementationsAndManifestHasMatchingMinimum()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        XDocument project = XDocument.Load(Path.Combine(root.FullName, "sts2diy.csproj"));
        string copy = Assert.Single(project.Root!.Elements("Target"), t => (string?)t.Attribute("Name") == "CopyMod").ToString();
        Assert.Contains("sts2diy.Loader", copy);
        Assert.Contains("compat\\0.111.0\\sts2diy.impl.dll", copy);
        Assert.Contains("compat\\0.113.0\\sts2diy.impl.dll", copy);
        Assert.DoesNotContain("RemoveDir", copy.Split("Copying mod to Slay the Spire 2 mods folder...")[1]);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "sts2diy.json")));
        Assert.Equal("0.111.0", manifest.RootElement.GetProperty("min_game_version").GetString());
    }
}

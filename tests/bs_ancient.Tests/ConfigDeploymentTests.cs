using System.Xml.Linq;

namespace BsAncient.Tests;

public sealed class ConfigDeploymentTests
{
    [Fact]
    public void DeploymentDoesNotDeleteTheInstalledCompatibilityTreeAndStillExcludesUploadConfigs()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "bs_ancient.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        XDocument project = XDocument.Load(Path.Combine(root.FullName, "bs_ancient.csproj"));
        XElement copy = Assert.Single(project.Root!.Elements("Target"), target => (string?)target.Attribute("Name") == "CopyMod");
        Assert.DoesNotContain(copy.Elements("RemoveDir"), task =>
            (string?)task.Attribute("Directories") is "$(ModInstallDir)" or "$(ModInstallDir)\\compat");
        Assert.DoesNotContain(copy.Elements("Delete"), task =>
            ((string?)task.Attribute("Files"))?.Contains("$(ModInstallDir)\\bs_ancient_config.cfg", StringComparison.Ordinal) == true);
        Assert.Contains(copy.Elements("Delete"), task =>
            (string?)task.Attribute("Files") == "$(WorkshopUploadContentDir)\\bs_ancient_config.cfg");
        Assert.Contains(copy.Elements("RemoveDir"), task =>
            (string?)task.Attribute("Directories") == "$(WorkshopUploadContentDir)\\compat");
        Assert.Contains(copy.Elements("Delete"), task =>
            (string?)task.Attribute("Files") == "$(ModInstallDir)\\compat\\0.107.1\\bs_ancient.impl.dll");
        Assert.DoesNotContain(copy.Elements("RemoveDir"), task =>
            ((string?)task.Attribute("Directories"))?.Contains("$(ModInstallDir)\\compat\\0.107.1", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void BundleAndManifestOnlySupportTheMaintainedGameApis()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "bs_ancient.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        XDocument project = XDocument.Load(Path.Combine(root.FullName, "bs_ancient.csproj"));
        XElement bundle = Assert.Single(project.Root!.Elements("Target"), t => (string?)t.Attribute("Name") == "PrepareCompatibilityBundle");
        Assert.DoesNotContain("0.107.1", bundle.ToString());
        Assert.Contains("compat\\0.111.0", bundle.ToString());
        Assert.Contains("compat\\0.113.0", bundle.ToString());
        Assert.DoesNotContain(project.Descendants("PackageReference"), p =>
            ((string?)p.Attribute("Include"))?.Contains("0.107.1", StringComparison.Ordinal) == true);
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "bs_ancient.json")));
        Assert.Equal("0.111.0", manifest.RootElement.GetProperty("min_game_version").GetString());
    }
}

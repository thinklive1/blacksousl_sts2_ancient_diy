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
    }
}

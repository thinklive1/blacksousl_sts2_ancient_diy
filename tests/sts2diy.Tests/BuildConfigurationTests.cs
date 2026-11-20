using System.Xml.Linq;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class BuildConfigurationTests
{
    [Fact]
    public void OrdinaryBuildDeploysOnlyTheExplicitPayloadAndDoesNotExportResources()
    {
        XElement project = ReadProject();
        XElement deploy = Target(project, "CopyMod");
        Assert.Equal("Build", (string?)deploy.Attribute("AfterTargets"));
        Assert.Contains("SkipModDeployment", (string)deploy.Attribute("Condition")!);
        Assert.Empty(deploy.Elements("Exec"));
        string files = (string)deploy.Descendants("_ModFiles").Single().Attribute("Include")!;
        Assert.Equal("$(TargetPath);$(MSBuildProjectDirectory)\\sts2diy.json;$(MSBuildProjectDirectory)\\README.md;$(MSBuildProjectDirectory)\\README_EN.md", files);
        Assert.Contains(deploy.Elements("Copy"), copy => (string?)copy.Attribute("DestinationFolder") == "$(WorkshopUploadContentDir)\\");
        Assert.Contains(deploy.Elements("Copy"), copy => (string?)copy.Attribute("DestinationFolder") == "$(ModInstallDir)\\");
    }

    [Fact]
    public void InnerGodotBuildSkipsDeploymentAndRuntimeDependenciesAreNotBundled()
    {
        XElement project = ReadProject();
        XElement inner = project.Elements("PropertyGroup").Single(group =>
            ((string?)group.Attribute("Condition"))?.Contains("IsInnerGodotExport") == true);
        Assert.Equal("true", inner.Element("SkipModDeployment")!.Value);
        Assert.Equal("false", project.Descendants("CopyLocalLockFileAssemblies").Single().Value);
        Assert.All(project.Descendants("Reference"), reference => Assert.Equal("false", reference.Element("Private")!.Value));
    }

    [Fact]
    public void ResourceExportHasATimeoutAndCopiesOnlyAfterSuccessfulCompletion()
    {
        XElement export = Target(ReadProject(), "ExportPck");
        XElement command = Assert.Single(export.Elements("Exec"));
        Assert.Equal("60000", (string?)command.Attribute("Timeout"));
        Assert.Null(command.Attribute("ContinueOnError"));
        Assert.Contains("$(PckExportDir)", (string)command.Attribute("Command")!);
        Assert.Equal(2, command.ElementsAfterSelf("Copy").Count());
        Assert.All(command.ElementsAfterSelf("Copy"), copy =>
            Assert.Equal("$(PckExportDir)\\sts2diy.pck", (string?)copy.Attribute("SourceFiles")));
    }

    [Fact]
    public void UploadedConfigurationAndDocsAreRemovedWithoutDeletingLocalPlayerSettings()
    {
        XElement deploy = Target(ReadProject(), "CopyMod");
        Assert.All(deploy.Elements("RemoveDir"), remove =>
            Assert.StartsWith("$(WorkshopUploadContentDir)", (string)remove.Attribute("Directories")!));
        Assert.All(deploy.Elements("Delete"), delete =>
            Assert.Equal("@(_UploadConfigFiles)", (string?)delete.Attribute("Files")));
        string patterns = (string)deploy.Descendants("_UploadConfigFiles").Single().Attribute("Include")!;
        Assert.Contains("**\\*.cfg", patterns);
        Assert.Contains("**\\*_config.json", patterns);
        Assert.DoesNotContain("ModInstallDir", patterns);
    }

    private static XElement Target(XElement project, string name) =>
        project.Elements("Target").Single(target => (string?)target.Attribute("Name") == name);

    private static XElement ReadProject()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        return XDocument.Load(Path.Combine(root.FullName, "sts2diy.csproj")).Root!;
    }
}

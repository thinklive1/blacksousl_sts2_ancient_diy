using BlackSouls.Compatibility;
using System.Reflection;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;

namespace BsAncient.Tests;

public sealed class CompatibilityLoaderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("sts2.dll")]
    [InlineData("data_sts2_windows_x86_64/sts2.dll")]
    public void EmptyAndRelativeGameAssemblyPathsHaveNoReleaseInfoPath(string? assemblyPath)
    {
        Assert.Null(Entry.FindReleaseInfoPath(assemblyPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sts2.dll")]
    public void MissingReleaseFileUsesTheHostVersionInsteadOfGuessingStable(string? assemblyPath)
    {
        var release = Entry.ReadGameRelease(assemblyPath, new SemanticVersion(0, 113, 0));
        Assert.Equal(new Version(0, 113, 0), release.Version);
        Assert.Equal("0.113.0", Entry.SelectCompatibilityFolder(release));
        Assert.Contains("host runtime", release.DisplayName);
    }

    [Fact]
    public void MissingReleaseFileAndHostVersionFailClearlyRatherThanLoadingTheWrongApi()
    {
        var error = Assert.Throws<NotSupportedException>(() => Entry.ReadGameRelease("sts2.dll", null));
        Assert.Contains("Refusing to guess", error.Message);
    }

    [Fact]
    public void FutureHostVersionStillCannotSelectAnUnsupportedImplementation()
    {
        var release = Entry.ReadGameRelease(null, new SemanticVersion(0, 114, 0));
        Assert.Throws<NotSupportedException>(() => Entry.SelectCompatibilityFolder(release));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bs_ancient.dll")]
    public void MobileAssemblyLocationsUseTheRegisteredModDirectory(string? assemblyPath)
    {
        string modDirectory = Path.Combine(Path.GetTempPath(), "bs_ancient_mobile_mod");
        Assert.Equal(Path.GetFullPath(modDirectory), Entry.ResolveModDirectory(
            assemblyPath, modDirectory, Path.GetTempPath()));
    }

    [Fact]
    public void AbsoluteDesktopAssemblyLocationKeepsItsOriginalPriority()
    {
        string modDirectory = Path.Combine(Path.GetTempPath(), "bs_ancient_desktop_mod");
        Assert.Equal(Path.GetFullPath(modDirectory), Entry.ResolveModDirectory(
            Path.Combine(modDirectory, "bs_ancient.dll"), Path.Combine(Path.GetTempPath(), "other_mod"), Path.GetTempPath()));
    }

    [Fact]
    public void UnregisteredMemoryLoadedModDoesNotGuessTheGameDirectory()
    {
        string missingBaseDirectory = Path.Combine(Path.GetTempPath(), $"bs_ancient_missing_{Guid.NewGuid():N}");
        Assert.Throws<DirectoryNotFoundException>(() => Entry.ResolveModDirectory("bs_ancient.dll", null, missingBaseDirectory));
    }

    [Fact]
    public void ApplicationDirectoryFallbackRequiresTheActualLoaderFile()
    {
        string directory = Directory.CreateTempSubdirectory("bs_ancient_loader_").FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "bs_ancient.dll"), []);
            Assert.Equal(Path.GetFullPath(directory), Entry.ResolveModDirectory(null, null, directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DesktopReleaseFileStillSelectsTheInstalledGameApi()
    {
        string gameDirectory = Directory.CreateTempSubdirectory("bs_ancient_release_").FullName;
        try
        {
            string dataDirectory = Path.Combine(gameDirectory, "data_sts2_windows_x86_64");
            Directory.CreateDirectory(dataDirectory);
            File.WriteAllText(Path.Combine(gameDirectory, "release_info.json"),
                """{"version":"v0.111.0","branch":"public"}""");
            var release = Entry.ReadGameRelease(Path.Combine(dataDirectory, "sts2.dll"), new SemanticVersion(0, 107, 1));
            Assert.Equal(new Version(0, 111, 0), release.Version);
            Assert.Equal("v0.111.0 (public)", release.DisplayName);
        }
        finally
        {
            Directory.Delete(gameDirectory, recursive: true);
        }
    }

    [Fact]
    public void HostVersionFallbackFieldHasTheExpectedType()
    {
        FieldInfo field = typeof(ModManager).GetField("_gameVersion", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.NotNull(field);
        Assert.Equal(typeof(SemanticVersion), field.FieldType);
    }

    [Theory]
    [InlineData(0, 107, 1, "0.107.1")]
    [InlineData(0, 107, 9, "0.107.1")]
    [InlineData(0, 111, 0, "0.111.0")]
    [InlineData(0, 111, 1, "0.111.0")]
    [InlineData(0, 112, 0, "0.111.0")]
    [InlineData(0, 113, 0, "0.113.0")]
    [InlineData(0, 113, 1, "0.113.0")]
    public void SelectsImplementationForGameApi(int major, int minor, int patch, string expected)
    {
        var release = new Entry.GameRelease(new Version(major, minor, patch), "test");

        Assert.Equal(expected, Entry.SelectCompatibilityFolder(release));
    }

    [Theory]
    [InlineData(0, 114, 0)]
    [InlineData(0, 115, 0)]
    public void RejectsGameApisWithoutAnImplementation(int major, int minor, int patch)
    {
        var release = new Entry.GameRelease(new Version(major, minor, patch), "test");

        var error = Assert.Throws<NotSupportedException>(() => Entry.SelectCompatibilityFolder(release));

        Assert.Contains("supported game branch", error.Message);
    }

    [Fact]
    public void ResolvesImplementationInLoaderAssemblyContext()
    {
        string loaderPath = typeof(Entry).Assembly.Location;
        var isolatedContext = new AssemblyLoadContext(
            $"bs-ancient-loader-test-{Guid.NewGuid():N}",
            isCollectible: true);

        try
        {
            Assembly isolatedLoader = isolatedContext.LoadFromAssemblyPath(loaderPath);
            Type isolatedEntry = isolatedLoader.GetType(
                typeof(Entry).FullName!,
                throwOnError: true)!;
            MethodInfo resolver = isolatedEntry.GetMethod(
                "ResolveImplementationLoadContext",
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var resolvedContext = Assert.IsType<AssemblyLoadContext>(
                resolver.Invoke(null, null));

            Assert.Same(isolatedContext, resolvedContext);
        }
        finally
        {
            isolatedContext.Unload();
        }
    }
}

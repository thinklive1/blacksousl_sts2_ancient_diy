using BlackSouls.Compatibility;
using System.Reflection;
using System.Runtime.Loader;

namespace BsAncient.Tests;

public sealed class CompatibilityLoaderTests
{
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

using System.Text.RegularExpressions;
using System.Reflection;

namespace BsAncient.Tests;

public sealed partial class MobileResourcePathTests
{
    [Fact]
    public void LoaderAndCurrentImplementationRemainManagedAnyCpuAssemblies()
    {
        foreach (Assembly assembly in new[] { typeof(BlackSouls.Compatibility.Entry).Assembly, typeof(BlackSouls.Scripts.Entry).Assembly })
        {
            assembly.ManifestModule.GetPEKind(out PortableExecutableKinds kind, out _);
            Assert.True(kind.HasFlag(PortableExecutableKinds.ILOnly), assembly.FullName);
            Assert.False(kind.HasFlag(PortableExecutableKinds.Required32Bit), assembly.FullName);
            Assert.False(kind.HasFlag(PortableExecutableKinds.PE32Plus), assembly.FullName);
        }
    }

    [Fact]
    public void LiteralModResourcePathsExistWithExactCase()
    {
        string root = FindRepositoryRoot();
        string contentRoot = Path.Combine(root, "bs_ancient");
        var paths = Directory.EnumerateFileSystemEntries(contentRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
        var failures = new SortedSet<string>(StringComparer.Ordinal);
        IEnumerable<string> sources = Directory.EnumerateFiles(Path.Combine(root, "Scripts"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(contentRoot, "assets"), "*", SearchOption.AllDirectories)
                .Where(path => Path.GetExtension(path) is ".tscn" or ".tres" or ".gdshader"));

        foreach (string source in sources)
        {
            foreach (Match match in ResourcePathRegex().Matches(File.ReadAllText(source)))
            {
                string relativePath = match.Value["res://".Length..];
                if (relativePath.Contains('{') || relativePath.Contains('}')) continue;
                if (!paths.Contains(relativePath))
                {
                    failures.Add($"{Path.GetRelativePath(root, source)}: {match.Value}");
                }
            }
        }

        Assert.True(failures.Count == 0, "Missing or incorrectly cased resource paths:\n" + string.Join('\n', failures));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "bs_ancient.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the BS Ancient repository root.");
    }

    [GeneratedRegex("res://bs_ancient/[^\"\\r\\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex ResourcePathRegex();
}

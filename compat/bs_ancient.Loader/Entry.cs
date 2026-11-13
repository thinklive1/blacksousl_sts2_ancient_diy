using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Text.Json;
using MegaCrit.Sts2.Core.Modding;

namespace BlackSouls.Compatibility;

/// <summary>Selects the implementation compiled for the running game API.</summary>
[ModInitializer(nameof(Init))]
public static class Entry
{
    private const string ImplementationEntryType = "BlackSouls.Scripts.Entry";
    private const string ImplementationEntryMethod = "Init";

    public static void Init()
    {
        string modDirectory = ResolveModDirectory();
        GameRelease release = ReadGameRelease();
        string compatibilityFolder = SelectCompatibilityFolder(release);
        string implementationPath = Path.Combine(
            modDirectory,
            "compat",
            compatibilityFolder,
            "bs_ancient.impl.dll");

        if (!File.Exists(implementationPath))
        {
            throw new FileNotFoundException(
                $"BS Ancient has no implementation for game {release.DisplayName}. Expected: {implementationPath}",
                implementationPath);
        }

        Console.WriteLine(
            $"[BS Ancient] Loading compatibility target {compatibilityFolder} for game {release.DisplayName}.");
        AssemblyLoadContext loadContext = ResolveImplementationLoadContext();
        Assembly implementation = loadContext.LoadFromAssemblyPath(
            Path.GetFullPath(implementationPath));
        Type entryType = implementation.GetType(ImplementationEntryType, throwOnError: true)!;
        MethodInfo init = entryType.GetMethod(
            ImplementationEntryMethod,
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(ImplementationEntryType, ImplementationEntryMethod);

        try
        {
            init.Invoke(null, null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    internal static string SelectCompatibilityFolder(GameRelease release)
    {
        if (release.Version >= new Version(0, 114, 0))
        {
            throw new NotSupportedException(
                $"BS Ancient has not been compiled for game {release.DisplayName}. " +
                "This release supports game 0.107.1, 0.111.0 and 0.113.0. Use a supported game branch until a compatible release is available.");
        }

        if (release.Version >= new Version(0, 113, 0))
        {
            return "0.113.0";
        }

        return release.Version >= new Version(0, 111, 0)
            ? "0.111.0"
            : "0.107.1";
    }

    internal static AssemblyLoadContext ResolveImplementationLoadContext()
    {
        return AssemblyLoadContext.GetLoadContext(typeof(Entry).Assembly)
            ?? AssemblyLoadContext.Default;
    }

    private static string ResolveModDirectory()
    {
        string? assemblyPath = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(assemblyPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
        }

        return AppContext.BaseDirectory;
    }

    private static GameRelease ReadGameRelease()
    {
        string? gameAssemblyPath = typeof(ModInitializerAttribute).Assembly.Location;
        string? dataDirectory = string.IsNullOrWhiteSpace(gameAssemblyPath)
            ? null
            : Path.GetDirectoryName(gameAssemblyPath);
        string? gameDirectory = dataDirectory == null
            ? null
            : Directory.GetParent(dataDirectory)?.FullName;
        string? releaseInfoPath = gameDirectory == null
            ? null
            : Path.Combine(gameDirectory, "release_info.json");

        if (releaseInfoPath == null || !File.Exists(releaseInfoPath))
        {
            return new GameRelease(new Version(0, 107, 1), "unknown (stable fallback)");
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(releaseInfoPath));
        string rawVersion = document.RootElement.TryGetProperty("version", out JsonElement versionElement)
            ? versionElement.GetString() ?? string.Empty
            : string.Empty;
        string branch = document.RootElement.TryGetProperty("branch", out JsonElement branchElement)
            ? branchElement.GetString() ?? string.Empty
            : string.Empty;

        string normalizedVersion = rawVersion.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(normalizedVersion, out Version? version))
        {
            version = new Version(0, 107, 1);
        }

        string displayName = string.IsNullOrWhiteSpace(branch)
            ? rawVersion
            : $"{rawVersion} ({branch})";
        return new GameRelease(version, displayName);
    }

    internal readonly record struct GameRelease(Version Version, string DisplayName);
}

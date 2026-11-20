using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Text.Json;
using MegaCrit.Sts2.Core.Debug;
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
        return ResolveModDirectory(
            Assembly.GetExecutingAssembly().Location,
            ModManager.Mods.FirstOrDefault(mod => mod.manifest?.id == "bs_ancient")?.path,
            AppContext.BaseDirectory);
    }

    internal static string ResolveModDirectory(
        string? assemblyPath,
        string? registeredModDirectory,
        string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(assemblyPath) && Path.IsPathFullyQualified(assemblyPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
        }

        // Mobile hosts can load assemblies from bytes and expose only a filename or an empty Location.
        if (!string.IsNullOrWhiteSpace(registeredModDirectory) && Path.IsPathFullyQualified(registeredModDirectory))
        {
            return Path.GetFullPath(registeredModDirectory);
        }

        if (File.Exists(Path.Combine(baseDirectory, "bs_ancient.dll")))
        {
            return Path.GetFullPath(baseDirectory);
        }

        throw new DirectoryNotFoundException(
            "BS Ancient could not locate its mod directory. The host must provide an assembly path or a registered mod path.");
    }

    private static GameRelease ReadGameRelease()
    {
        // Prefer the public release manager; mobile hosts may instead supply the version to ModManager.Initialize.
        SemanticVersion? hostVersion = ReleaseInfoManager.Instance.SemVer
            ?? typeof(ModManager).GetField("_gameVersion", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null) as SemanticVersion;
        return ReadGameRelease(typeof(ModInitializerAttribute).Assembly.Location, hostVersion);
    }

    internal static GameRelease ReadGameRelease(string? gameAssemblyPath, SemanticVersion? hostVersion)
    {
        string? releaseInfoPath = FindReleaseInfoPath(gameAssemblyPath);
        if (releaseInfoPath != null && File.Exists(releaseInfoPath))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(releaseInfoPath));
            string rawVersion = document.RootElement.TryGetProperty("version", out JsonElement versionElement)
                ? versionElement.GetString() ?? string.Empty
                : string.Empty;
            string branch = document.RootElement.TryGetProperty("branch", out JsonElement branchElement)
                ? branchElement.GetString() ?? string.Empty
                : string.Empty;

            if (Version.TryParse(rawVersion.Trim().TrimStart('v', 'V'), out Version? version))
            {
                string displayName = string.IsNullOrWhiteSpace(branch) ? rawVersion : $"{rawVersion} ({branch})";
                return new GameRelease(version, displayName);
            }
        }

        if (hostVersion != null)
        {
            return new GameRelease(
                new Version(hostVersion.Major, hostVersion.Minor, hostVersion.Patch),
                $"{hostVersion} (host runtime)");
        }

        throw new NotSupportedException(
            "BS Ancient could not identify the game version from release_info.json or the host runtime. " +
            "Refusing to guess a compatibility target.");
    }

    internal static string? FindReleaseInfoPath(string? gameAssemblyPath)
    {
        if (string.IsNullOrWhiteSpace(gameAssemblyPath) || !Path.IsPathFullyQualified(gameAssemblyPath))
        {
            return null;
        }

        string? dataDirectory = Path.GetDirectoryName(gameAssemblyPath);
        string? gameDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? null
            : Directory.GetParent(dataDirectory)?.FullName;
        return gameDirectory == null ? null : Path.Combine(gameDirectory, "release_info.json");
    }

    internal readonly record struct GameRelease(Version Version, string DisplayName);
}

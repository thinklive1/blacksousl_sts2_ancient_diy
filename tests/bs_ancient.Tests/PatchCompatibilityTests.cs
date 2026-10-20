using System.Collections;
using System.Reflection;
using BlackSouls.Scripts;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace BsAncient.Tests;

public sealed class PatchCompatibilityTests
{
    private static readonly BindingFlags DeclaredMethods =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    public void RitsuPatchTargetsResolveOnInstalledGame()
    {
        Assembly assembly = typeof(Entry).Assembly;
        var failures = new List<string>();
        int targetCount = 0;

        foreach (Type patchType in assembly.GetTypes()
                     .Where(type => !type.IsAbstract && typeof(IPatchMethod).IsAssignableFrom(type))
                     .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            MethodInfo? getTargets = patchType.GetMethod(
                nameof(IPatchMethod.GetTargets),
                BindingFlags.Public | BindingFlags.Static);
            if (getTargets == null)
            {
                failures.Add($"{patchType.FullName}: missing public static GetTargets()");
                continue;
            }

            if (getTargets.Invoke(null, null) is not IEnumerable targets)
            {
                failures.Add($"{patchType.FullName}: GetTargets() returned null");
                continue;
            }

            foreach (object? value in targets)
            {
                if (value is not ModPatchTarget target)
                {
                    failures.Add($"{patchType.FullName}: GetTargets() returned an unexpected value");
                    continue;
                }

                targetCount++;
                if (PatchTargetMethodResolver.Resolve(target) == null && !target.IgnoreIfMissing)
                {
                    failures.Add($"{patchType.FullName}: {target}");
                }
            }
        }

        Assert.True(targetCount > 0, "No Ritsu patch targets were discovered.");
        Assert.True(failures.Count == 0, "Unresolved Ritsu patch targets:\n" + string.Join("\n", failures));
    }

    [Fact]
    public void HarmonyAttributeTargetsResolveOnInstalledGame()
    {
        Assembly assembly = typeof(Entry).Assembly;
        var failures = new List<string>();
        int targetCount = 0;

        foreach (Type patchType in assembly.GetTypes()
                     .Where(HasHarmonyPatchMetadata)
                     .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            HarmonyMethod classMetadata = HarmonyMethodExtensions.GetMergedFromType(patchType);
            ResolveMetadataTarget(patchType, "class", classMetadata, failures, ref targetCount);

            foreach (MethodInfo method in patchType.GetMethods(DeclaredMethods)
                         .Where(HasHarmonyPatchMetadata)
                         .OrderBy(method => method.MetadataToken))
            {
                HarmonyMethod methodMetadata = HarmonyMethodExtensions.GetMergedFromMethod(method);
                HarmonyMethod merged = HarmonyMethodExtensions.Merge(classMetadata, methodMetadata);
                ResolveMetadataTarget(patchType, method.Name, merged, failures, ref targetCount);
            }

            ResolveDynamicTargets(patchType, failures, ref targetCount);
        }

        Assert.True(targetCount > 0, "No Harmony patch targets were discovered.");
        Assert.True(failures.Count == 0, "Unresolved Harmony patch targets:\n" + string.Join("\n", failures));
    }

    private static bool HasHarmonyPatchMetadata(MemberInfo member)
    {
        return member.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0;
    }

    private static void ResolveMetadataTarget(
        Type patchType,
        string source,
        HarmonyMethod metadata,
        ICollection<string> failures,
        ref int targetCount)
    {
        if (metadata.declaringType == null || string.IsNullOrWhiteSpace(metadata.methodName))
        {
            return;
        }

        targetCount++;
        MethodBase? target = PatchTargetMethodResolver.Resolve(
            metadata.declaringType,
            metadata.methodName,
            metadata.argumentTypes,
            metadata.methodType ?? MethodType.Normal);
        if (target == null)
        {
            failures.Add(
                $"{patchType.FullName}.{source}: {metadata.declaringType.FullName}.{metadata.methodName}");
        }
    }

    private static void ResolveDynamicTargets(
        Type patchType,
        ICollection<string> failures,
        ref int targetCount)
    {
        foreach (string methodName in new[] { "TargetMethod", "TargetMethods" })
        {
            MethodInfo? resolver = patchType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (resolver == null || resolver.GetParameters().Length != 0)
            {
                continue;
            }

            object? result = resolver.Invoke(null, null);
            IEnumerable values = result is MethodBase single
                ? new[] { single }
                : result as IEnumerable ?? Array.Empty<MethodBase>();
            bool foundAny = false;
            foreach (object? value in values)
            {
                if (value is MethodBase)
                {
                    foundAny = true;
                    targetCount++;
                }
            }

            if (!foundAny)
            {
                failures.Add($"{patchType.FullName}.{methodName}(): returned no targets");
            }
        }
    }
}

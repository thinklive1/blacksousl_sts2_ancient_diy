#if BS_STABLE_1071
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace BlackSouls.Scripts.Compatibility;

/// <summary>Adds the version-specific implementation assembly to the stable host's single-assembly mod scan.</summary>
[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
internal static class StableModTypeDiscoveryPatch
{
    private static readonly Assembly ImplementationAssembly = typeof(StableModTypeDiscoveryPatch).Assembly;

    [HarmonyPostfix]
    private static void Postfix(ref Type[] __result)
    {
        __result = IncludeImplementationTypes(__result, ImplementationAssembly);
    }

    internal static Type[] IncludeImplementationTypes(Type[] discoveredTypes, Assembly implementationAssembly)
    {
        ArgumentNullException.ThrowIfNull(discoveredTypes);
        ArgumentNullException.ThrowIfNull(implementationAssembly);

        if (discoveredTypes.Any(type => type?.Assembly == implementationAssembly))
        {
            return discoveredTypes;
        }

        Type[] implementationTypes;
        try
        {
            implementationTypes = implementationAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            implementationTypes = exception.Types.OfType<Type>().ToArray();
        }

        if (implementationTypes.Length == 0)
        {
            return discoveredTypes;
        }

        Type[] merged = new Type[discoveredTypes.Length + implementationTypes.Length];
        Array.Copy(discoveredTypes, merged, discoveredTypes.Length);
        Array.Copy(implementationTypes, 0, merged, discoveredTypes.Length, implementationTypes.Length);
        return merged;
    }
}
#endif

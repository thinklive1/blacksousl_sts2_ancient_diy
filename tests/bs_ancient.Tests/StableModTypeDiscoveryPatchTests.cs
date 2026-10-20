using BlackSouls.Scripts.Compatibility;

namespace bs_ancient.Tests;

public sealed class StableModTypeDiscoveryPatchTests
{
    [Fact]
    public void IncludeImplementationTypes_AppendsImplementationAssemblyOnce()
    {
        Type[] original = [typeof(string)];
        Type[] merged = StableModTypeDiscoveryPatch.IncludeImplementationTypes(
            original,
            typeof(StableModTypeDiscoveryPatch).Assembly);

        Assert.Contains(typeof(StableModTypeDiscoveryPatch), merged);

        Type[] secondPass = StableModTypeDiscoveryPatch.IncludeImplementationTypes(
            merged,
            typeof(StableModTypeDiscoveryPatch).Assembly);

        Assert.Same(merged, secondPass);
    }
}

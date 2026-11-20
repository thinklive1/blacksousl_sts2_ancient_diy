using System.Buffers.Binary;
using HarmonyLib;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class PowerArtTests
{
    [Theory]
    [InlineData(typeof(DeathLifePactPower))]
    [InlineData(typeof(SharedDoomTransferPower))]
    [InlineData(typeof(BitterHatredPower))]
    public void ApprovedPowerIconsHaveExplicitProfilesAndCompactRgbaPngs(Type power)
    {
        var getter = power.GetProperty("AssetProfile")!.GetMethod!;
        Assert.Equal(power, getter.DeclaringType);
        string expected = $"res://sts2diy/assets/images/powers/{power.Name}.png";
        Assert.Equal(2, PatchProcessor.GetOriginalInstructions(getter)
            .Count(i => i.operand is string path && path == expected));

        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string file = Path.Combine(root.FullName, expected[6..].Replace('/', Path.DirectorySeparatorChar));
        byte[] png = File.ReadAllBytes(file);
        Assert.InRange(png.Length, 1024, 512 * 1024);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);
        Assert.Equal(512, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(512, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        Assert.Equal(6, png[25]); // PNG color type 6 preserves the alpha channel.
    }
}

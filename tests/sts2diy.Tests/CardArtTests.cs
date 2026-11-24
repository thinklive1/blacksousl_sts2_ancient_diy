using System.Reflection;
using HarmonyLib;
using Sts2Diy.Scripts.Cards;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class CardArtTests
{
    [Theory]
    [InlineData(typeof(DeathLifePactCard))]
    [InlineData(typeof(NegativeEnergyAbsorptionCard))]
    [InlineData(typeof(SharedDoomCard))]
    [InlineData(typeof(BitterHatredCard))]
    [InlineData(typeof(BloodOathCard))]
    [InlineData(typeof(DarkRitualCard))]
    [InlineData(typeof(PoisonedDraughtCard))]
    [InlineData(typeof(BorrowedBodyCard))]
    [InlineData(typeof(SharedVengeanceCard))]
    [InlineData(typeof(PartnerCard))]
    [InlineData(typeof(VoidPassageCard))]
    [InlineData(typeof(NegativeEnergyInfusionCard))]
    public void ApprovedArtHasAProductionProfileAndACompactNativeSizeJpeg(Type card)
    {
        MethodInfo getter = card.GetProperty("AssetProfile")!.GetMethod!;
        Assert.Equal(card, getter.DeclaringType);
        string expected = $"res://sts2diy/assets/images/cards/{card.Name}.jpg";
        Assert.Contains(PatchProcessor.GetOriginalInstructions(getter), i => i.operand is string path && path == expected);
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string file = Path.Combine(root.FullName, expected[6..].Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(file), file);
        Assert.InRange(new FileInfo(file).Length, 1024, 256 * 1024);
        Assert.Equal((1000, 760), ReadJpegSize(file));
    }

    private static (int Width, int Height) ReadJpegSize(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);
        Assert.Equal(0xff, reader.ReadByte());
        Assert.Equal(0xd8, reader.ReadByte());
        while (stream.Position < stream.Length)
        {
            byte prefix = reader.ReadByte();
            if (prefix != 0xff) continue;
            byte marker;
            do { marker = reader.ReadByte(); } while (marker == 0xff);
            if (marker == 0x00 || marker == 0xd8 || marker is >= 0xd0 and <= 0xd7) continue;
            if (marker is 0xd9 or 0xda) break;
            int length = (reader.ReadByte() << 8) | reader.ReadByte();
            if (marker is 0xc0 or 0xc1 or 0xc2)
            {
                reader.ReadByte();
                int height = (reader.ReadByte() << 8) | reader.ReadByte();
                int width = (reader.ReadByte() << 8) | reader.ReadByte();
                return (width, height);
            }
            stream.Seek(length - 2, SeekOrigin.Current);
        }
        throw new InvalidDataException($"No supported JPEG frame in {path}.");
    }
}

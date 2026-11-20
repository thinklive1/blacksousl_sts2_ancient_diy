using System.Reflection;
using BlackSouls.Scripts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Core;

namespace BsAncient.Tests;

public sealed class AncientRoomGenerationPatchTests
{
    [Theory]
    [InlineData(typeof(LegacyAct), 3)]
    [InlineData(typeof(CurrentAct), 4)]
    [InlineData(typeof(BothSignaturesAct), 4)]
    public void RoomGenerationSupportsBothSignaturesAndPrefersTheCurrentImplementation(Type act, int count)
    {
        Type[] signature = OnlyUseModAncientsPatch.ResolveGenerateRoomsParameters(act);
        Assert.Equal(count, signature.Length);
        Assert.Equal(typeof(Rng), signature[0]);
        Assert.Equal(typeof(UnlockState), signature[1]);
        Assert.All(signature.Skip(2), type => Assert.Equal(typeof(bool), type));
        Assert.NotNull(act.GetMethod("GenerateRooms", signature));
    }

    [Fact]
    public void MissingSignaturesAreReportedRatherThanSilentlyIgnored()
    {
        Assert.Throws<MissingMethodException>(() =>
            OnlyUseModAncientsPatch.ResolveGenerateRoomsParameters(typeof(UnsupportedAct)));
    }

    [Fact]
    public void InstalledGameHasExactlyOneRequiredRoomGenerationTarget()
    {
        var target = Assert.Single(OnlyUseModAncientsPatch.GetTargets());
        Assert.False(target.IgnoreIfMissing);
        MethodBase? method = PatchTargetMethodResolver.Resolve(target);
        Assert.NotNull(method);
        Assert.Equal(typeof(ActModel), method.DeclaringType);
        Assert.Equal("GenerateRooms", method.Name);
        Assert.InRange(method.GetParameters().Length, 3, 4);
        foreach (ParameterInfo argument in typeof(OnlyUseModAncientsPatch).GetMethod("Postfix")!.GetParameters()
                     .Where(parameter => parameter.Name != "__instance"))
        {
            Assert.Contains(method.GetParameters(), parameter => parameter.Name == argument.Name
                && parameter.ParameterType == argument.ParameterType);
        }
    }

    [Fact]
    public void HarmonyCanAttachTheRoomFilterToTheInstalledGameWithoutStartingARun()
    {
        MethodBase method = PatchTargetMethodResolver.Resolve(Assert.Single(OnlyUseModAncientsPatch.GetTargets()))!;
        Harmony harmony = new("bs_ancient.tests.ancient_room_generation");
        try
        {
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(OnlyUseModAncientsPatch), "Postfix"));
            Assert.Contains(Harmony.GetPatchInfo(method)!.Postfixes, patch => patch.owner == harmony.Id);
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
    }

    public sealed class LegacyAct
    {
        public void GenerateRooms(Rng rng, UnlockState unlockState, bool isMultiplayer) { }
    }

    public sealed class CurrentAct
    {
        public void GenerateRooms(RunState runState) { }
        public void GenerateRooms(Rng rng, UnlockState unlockState, bool isMultiplayer, bool isBlitz) { }
    }

    public sealed class BothSignaturesAct
    {
        public void GenerateRooms(Rng rng, UnlockState unlockState, bool isMultiplayer) { }
        public void GenerateRooms(Rng rng, UnlockState unlockState, bool isMultiplayer, bool isBlitz) { }
    }

    public sealed class UnsupportedAct
    {
        public void GenerateRooms(RunState runState) { }
    }
}

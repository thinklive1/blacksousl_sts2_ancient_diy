using System.Reflection;
using System.Reflection.Emit;
using BlackSouls.Scripts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BsAncient.Tests;

public sealed class SuspiciousHatTests
{
    [Fact]
    public void SerializedCopyOwnsItsSavedCollectionsAndModelIds()
    {
        var original = new SerializableRelic
        {
            Id = new ModelId("RELIC", "PEN_NIB"),
            Props = new SavedProperties
            {
                ints = [new("AttacksPlayed", 7)],
                intArrays = [new("Counts", [1, 2, 3])],
                modelIds = [new("OtherId", new ModelId("CARD", "BASH"))]
            }
        };
        SerializableRelic clone = SuspiciousHatRelic.CloneSerializable(original);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal("BASH", clone.Props!.modelIds![0].value.Entry);
        clone.Props.intArrays![0].value[0] = 99;
        Assert.Equal(1, original.Props.intArrays[0].value[0]);
        Assert.NotSame(original.Props, clone.Props);
    }

    [Fact]
    public void SavedCopyRestoresItsOwnCounterInsteadOfTheRealRelicCounter()
    {
        var saved = SuspiciousHatRelic.DeserializeCopy("""
            { "TargetIndex": 2, "TargetFloor": 5,
              "Relic": { "id": { "Category": "RELIC", "Entry": "PEN_NIB" },
                         "props": { "ints": [{ "name": "AttacksPlayed", "value": 8 }] } } }
            """);
        Assert.NotNull(saved?.Relic);
        Assert.True(SuspiciousHatRelic.MatchesSource(saved, new ModelId("RELIC", "PEN_NIB"), 5, 2));
        Assert.Equal(8, saved.Relic.Props!.ints![0].value);
    }

    [Theory]
    [InlineData("PEN_NIB", 6, 2)]
    [InlineData("PEN_NIB", 5, 3)]
    [InlineData("OTHER_RELIC", 5, 2)]
    public void AChangedSourceCannotReuseAnOldCounter(string entry, int floor, int index)
    {
        var saved = new SuspiciousHatRelic.CopiedRelicSave
        {
            TargetFloor = 5, TargetIndex = 2,
            Relic = new SerializableRelic { Id = new ModelId("RELIC", "PEN_NIB") }
        };
        Assert.False(SuspiciousHatRelic.MatchesSource(saved, new ModelId("RELIC", entry), floor, index));
    }

    [Fact]
    public void CopyFactoryRestoresASerializedSnapshotWithoutRunningPickupEffects()
    {
        // RelicModel's static initializer requires Godot; inspect its integration without starting the engine.
        Type type = typeof(SuspiciousHatRelic);
        MethodInfo factory = type.GetMethod("CreateCopy", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var instructions = PatchProcessor.GetOriginalInstructions(factory);
        Assert.Contains(instructions, i => i.operand is MethodInfo m
            && m.DeclaringType == typeof(RelicModel) && m.Name == nameof(RelicModel.ToMutable));
        Assert.Contains(instructions, i => i.operand is MethodInfo m
            && m.DeclaringType == type && m.Name == nameof(SuspiciousHatRelic.BuildCopySnapshot));
        Assert.DoesNotContain(instructions, i => i.operand is MethodInfo m
            && m.Name == nameof(RelicModel.AfterObtained));
    }

    [Fact]
    public void HatClonesDoNotShareProxyObjects()
    {
        Type type = typeof(SuspiciousHatRelic);
        MethodInfo clone = type.GetMethod("DeepCloneFields", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var instructions = PatchProcessor.GetOriginalInstructions(clone);
        Assert.Contains(instructions, i => i.opcode == OpCodes.Newobj
            && i.operand is ConstructorInfo constructor
            && constructor.DeclaringType == typeof(SuspiciousHatCopyState<RelicModel>));
        Assert.Contains(instructions, i => i.opcode == OpCodes.Stfld
            && i.operand is FieldInfo field && field.Name == "_copyState");
    }

    [Fact]
    public void DisplayAndSaveGettersCannotReachCopyTransitions()
    {
        Type type = typeof(SuspiciousHatRelic);
        foreach (string name in new[] { "ShowCounter", "DisplayAmount", "SpawnsPets", "AddsPet", "AdditionalHoverTips", "BlackSouls_CopiedRelicState" })
        {
            MethodInfo getter = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetMethod!;
            var pending = new Stack<MethodInfo>();
            var seen = new HashSet<MethodInfo>();
            pending.Push(getter);
            while (pending.TryPop(out MethodInfo? method))
            {
                if (!seen.Add(method)) continue;
                Assert.DoesNotContain(method.Name, new[] { "RefreshCopiedRelic", "CreateCopy", "CopyForGameplayHook" });
                foreach (MethodInfo call in PatchProcessor.GetOriginalInstructions(method).Select(i => i.operand).OfType<MethodInfo>())
                {
                    if (call.DeclaringType == type) pending.Push(call);
                }
            }
        }
    }

    [Fact]
    public void FreshCopiesPreserveSourceTrainingHealthAndCountersWithoutSharingThem()
    {
        SerializableRelic source = Snapshot("GIRYA", ("TimesLifted", 3));
        SerializableRelic copy = SuspiciousHatRelic.BuildCopySnapshot(source, null, Identity("GIRYA"));
        Assert.Equal(3, GetInt(copy, "TimesLifted"));
        copy.Props!.ints![0] = new("TimesLifted", 1);
        Assert.Equal(3, GetInt(source, "TimesLifted"));

        source = Snapshot("WUPPY", ("MaxHp", 60), ("CurrentHp", 41));
        copy = SuspiciousHatRelic.BuildCopySnapshot(source, null, Identity("WUPPY"));
        Assert.Equal(60, GetInt(copy, "MaxHp"));
        Assert.Equal(41, GetInt(copy, "CurrentHp"));
    }

    [Fact]
    public void SavedCombatCounterTakesPrecedenceOverTheRealRelicsCurrentCounter()
    {
        SerializableRelic source = Snapshot("PEN_NIB", ("AttacksPlayed", 2));
        var saved = Saved(Snapshot("PEN_NIB", ("AttacksPlayed", 8)));
        SerializableRelic copy = SuspiciousHatRelic.BuildCopySnapshot(source, saved, Identity("PEN_NIB"));
        Assert.Equal(8, GetInt(copy, "AttacksPlayed"));
        Assert.Equal(2, GetInt(source, "AttacksPlayed"));
    }

    [Fact]
    public void OldZeroedGiryaProgressIsRepairedFromPermanentTraining()
    {
        SerializableRelic source = Snapshot("GIRYA", ("TimesLifted", 3));
        var saved = Saved(Snapshot("GIRYA", ("TimesLifted", 0)));
        Assert.Equal(3, GetInt(SuspiciousHatRelic.BuildCopySnapshot(source, saved, Identity("GIRYA")), "TimesLifted"));
    }

    [Fact]
    public void InvalidLegacyPetHealthIsRepairedButAValidDeadPetIsNotRevived()
    {
        SerializableRelic source = Snapshot("WUPPY", ("MaxHp", 60), ("CurrentHp", 41));
        var invalid = Saved(Snapshot("WUPPY", ("MaxHp", 0), ("CurrentHp", 0)));
        SerializableRelic repaired = SuspiciousHatRelic.BuildCopySnapshot(source, invalid, Identity("WUPPY"));
        Assert.Equal(60, GetInt(repaired, "MaxHp"));
        Assert.Equal(41, GetInt(repaired, "CurrentHp"));

        var dead = Saved(Snapshot("WUPPY", ("MaxHp", 60), ("CurrentHp", 0)));
        Assert.Equal(0, GetInt(SuspiciousHatRelic.BuildCopySnapshot(source, dead, Identity("WUPPY")), "CurrentHp"));
    }

    [Fact]
    public void SpentMawBankCannotBeReactivatedByAnOldCopySave()
    {
        SerializableRelic source = Snapshot("MAW_BANK");
        source.Props!.bools = [new("HasItemBeenBought", true)];
        SerializableRelic old = Snapshot("MAW_BANK");
        old.Props!.bools = [new("HasItemBeenBought", false)];
        var copy = SuspiciousHatRelic.BuildCopySnapshot(source, Saved(old), Identity("MAW_BANK"));
        Assert.True(copy.Props!.bools!.Single(p => p.name == "HasItemBeenBought").value);
        Assert.False(old.Props.bools[0].value);
    }

    [Fact]
    public void ACopySaveCannotLeakCountersAcrossPlayers()
    {
        SerializableRelic source = Snapshot("PEN_NIB", ("AttacksPlayed", 1));
        var saved = Saved(Snapshot("PEN_NIB", ("AttacksPlayed", 9)));
        saved.TargetOwnerNetId = 123;
        var copy = SuspiciousHatRelic.BuildCopySnapshot(source, saved, Identity("PEN_NIB") with { OwnerNetId = 456 });
        Assert.Equal(1, GetInt(copy, "AttacksPlayed"));
    }

    [Theory]
    [InlineData("AfterItemPurchased")]
    [InlineData("AfterPotionUsed")]
    [InlineData("AfterRestSiteHeal")]
    [InlineData("AfterRestSiteSmith")]
    public void RequiredLifecycleHooksReachThePreparedCopy(string name)
    {
        Type type = typeof(SuspiciousHatRelic);
        MethodInfo hook = type.GetMethod(name)!;
        Assert.Equal(type, hook.DeclaringType);
        var instructions = PatchProcessor.GetOriginalInstructions(hook);
        Assert.Contains(instructions, i => i.operand is MethodInfo method && method.Name == "CopyForGameplayHook");
        Assert.Contains(instructions, i => i.operand is MethodInfo method && method.Name == name && method.DeclaringType != type);
    }

    private static SuspiciousHatCopyIdentity Identity(string entry) => new(new ModelId("RELIC", entry), 5, 2);
    private static SuspiciousHatRelic.CopiedRelicSave Saved(SerializableRelic relic) => new()
    {
        TargetFloor = 5, TargetIndex = 2, Relic = relic
    };
    private static SerializableRelic Snapshot(string entry, params (string Name, int Value)[] values) => new()
    {
        Id = new ModelId("RELIC", entry),
        Props = new SavedProperties { ints = values.Select(v => new SavedProperties.SavedProperty<int>(v.Name, v.Value)).ToList() }
    };
    private static int GetInt(SerializableRelic relic, string name) => relic.Props!.ints!.Single(p => p.name == name).value;
}

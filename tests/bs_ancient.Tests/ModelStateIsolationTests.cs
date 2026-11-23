using System.Reflection;
using System.Reflection.Emit;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BsAncient.Tests;

public sealed class ModelStateIsolationTests
{
    [Theory]
    [InlineData(typeof(BrutalityEnchantment), "_damagedTargetsThisPlay")]
    [InlineData(typeof(ExecutionEnchantment), "_executionTargetsThisPlay")]
    [InlineData(typeof(FeedingEnchantment), "_damagedTargetsThisPlay")]
    [InlineData(typeof(MercuryCard), "_autoPlayedCopies")]
    [InlineData(typeof(MercuryCard), "_copiedCards")]
    [InlineData(typeof(MermaidTearCard), "_swallowedDeckCards")]
    public void MutableCollectionsAreNotSharedByModelClones(Type modelType, string fieldName)
    {
        AbstractModel canonical = CreateCanonical(modelType);
        AbstractModel first = canonical.MutableClone();
        AbstractModel second = canonical.MutableClone();
        AbstractModel copy = first.MutableClone();
        FieldInfo field = modelType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.NotNull(field);
        Assert.NotSame(field.GetValue(canonical), field.GetValue(first));
        Assert.NotSame(field.GetValue(first), field.GetValue(second));
        Assert.NotSame(field.GetValue(first), field.GetValue(copy));
    }

    [Theory]
    [InlineData(typeof(UnicornRoyalCrestRelic), "_incomingAttacks")]
    [InlineData(typeof(GreatStagGoddessMythRelic), "_cardsThatEnteredExhaust")]
    [InlineData(typeof(FoxAndSourGrapesRelic), "_affectedCreatures")]
    [InlineData(typeof(OldFilmB), "_triggeredTargets")]
    [InlineData(typeof(ButchersMathClassRelic), "_turnPlayCounts")]
    public void RelicCloneAllocatesItsOwnCollection(Type modelType, string fieldName)
    {
        // RelicModel initializes native StringNames; inspect clone IL without starting Godot.
        FieldInfo field = modelType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo? clone = modelType.GetMethod("DeepCloneFields",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(clone);
        List<CodeInstruction> instructions = PatchProcessor.GetOriginalInstructions(clone);
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Newobj
            && instruction.operand is ConstructorInfo constructor && constructor.DeclaringType == field.FieldType);
        Assert.Contains(instructions, instruction => instruction.opcode == OpCodes.Stfld
            && Equals(instruction.operand, field));
    }

    internal static T CreateMutable<T>() where T : AbstractModel, new() =>
        (T)CreateCanonical(typeof(T)).MutableClone();

    private static AbstractModel CreateCanonical(Type modelType)
    {
        AbstractModel model = (AbstractModel)Activator.CreateInstance(modelType)!;
        Type? baseType = model switch
        {
            CardModel => typeof(CardModel),
            _ => null
        };

        // Supply a canonical source without bootstrapping Godot's global ModelDb.
        baseType?.GetField("_canonicalInstance", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(model, model);
        return model;
    }
}

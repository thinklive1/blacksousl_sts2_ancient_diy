using System.Reflection;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Compatibility;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using HarmonyLib;

namespace BsAncient.Tests;

public sealed class Compatibility113Tests
{
    [Theory]
    [InlineData(typeof(GentleGiftEvent), "RefuseGift")]
    [InlineData(typeof(QueenOfHeartsEvent), "ApplyRandomSuit")]
    [InlineData(typeof(PlayingCardGardenersEvent), "HelpGardeners")]
    [InlineData(typeof(NeowRethinkPokerPatch), "Postfix")]
    public void IndependentEventChoicesDoNotConsumeTheSharedRunRng(Type type, string name)
    {
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var calls = PatchProcessor.GetOriginalInstructions(method)
            .Select(instruction => instruction.operand).OfType<MethodInfo>().ToList();
        Assert.Contains(calls, call => call.Name == "get_Rng" && call.DeclaringType == typeof(EventModel));
        Assert.DoesNotContain(calls, call => call.Name == "get_Rng" && call.DeclaringType == typeof(IRunState));
    }

    [Fact]
    public void PatchArgumentsStillMatchTheGameMethods()
    {
        var failures = new List<string>();
        foreach (var patch in typeof(BlackSouls.Scripts.Entry).Assembly.GetTypes()
                     .Where(type => !type.IsAbstract && typeof(IPatchMethod).IsAssignableFrom(type)))
        {
            var getTargets = patch.GetMethod("GetTargets", BindingFlags.Public | BindingFlags.Static);
            if (getTargets?.Invoke(null, null) is not ModPatchTarget[] targets)
                continue;
            foreach (var target in targets)
            {
                var original = PatchTargetMethodResolver.Resolve(target);
                if (original == null)
                    continue;
                foreach (string name in new[] { "Prefix", "Postfix", "Finalizer" })
                {
                    var hook = patch.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (hook == null)
                        continue;
                    foreach (var arg in hook.GetParameters())
                    {
                        Type hookType = arg.ParameterType.IsByRef ? arg.ParameterType.GetElementType()! : arg.ParameterType;
                        if (arg.Name == "__result" && original is MethodInfo method)
                        {
                            if (!hookType.IsAssignableFrom(method.ReturnType))
                                failures.Add($"{patch.Name}.{name}: result {hookType} != {method.ReturnType}");
                        }
                        else if (arg.Name?.StartsWith("__", StringComparison.Ordinal) == false
                                 && original.GetParameters().All(parameter => parameter.Name != arg.Name))
                        {
                            failures.Add($"{patch.Name}.{name}: missing argument {arg.Name} on {original}");
                        }
                    }
                }
            }
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void EligibilityRulesReachTheNativeInternalHook()
    {
        var hook = typeof(ModRelicTemplate113).GetMethod("IsAllowedInternal",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.Equal(typeof(RelicModel), hook.GetBaseDefinition().DeclaringType);
        var customRule = typeof(WonderlandCodeOfLawsRelic).GetMethod("IsAllowed", [typeof(IRunState)])!;
        Assert.Equal(typeof(ModRelicTemplate113), customRule.GetBaseDefinition().DeclaringType);
        Assert.False(typeof(RelicModel).GetMethod("IsAllowed")!.IsVirtual);
    }

    [Theory]
    [InlineData(typeof(SuspiciousHatRelic))]
    [InlineData(typeof(SurroundedVisualPower))]
    public void DeathHooksIncludeTheKillingCard(Type modelType)
    {
        var hook = modelType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.Name == "AfterDeath");
        Assert.Equal(typeof(CardModel), hook.GetParameters()[2].ParameterType);
        Assert.Equal(typeof(AbstractModel), hook.GetBaseDefinition().DeclaringType);
    }
}

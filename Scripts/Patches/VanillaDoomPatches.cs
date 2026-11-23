using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Sts2Diy.Scripts.Patches;

/// <summary>Changes only vanilla Doom producers, retaining their native targeting, VFX, and timing.</summary>
internal static class VanillaDoomPatches
{
    internal static readonly Type[] DirectCards =
        [typeof(Scourge), typeof(Deathbringer), typeof(EndOfDays), typeof(NoEscape), typeof(BlightStrike)
#if !STS2DIY_STABLE_1110
        , typeof(Death)
#endif
        ];
    internal static readonly Type[] DelayedPowers =
        [typeof(CountdownPower), typeof(OblivionPower), typeof(ReaperFormPower), typeof(NeurosurgePower), typeof(UnderworldPower)];

    [HarmonyPatch]
    private static class DoomProducerPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in DirectCards.Concat(DelayedPowers))
            {
                bool found = false;
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (typeof(CardModel).IsAssignableFrom(type) && method.Name != "OnPlay") continue;
                    Type? stateMachine = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
                    MethodInfo? moveNext = stateMachine?.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (moveNext == null || !PatchProcessor.GetOriginalInstructions(moveNext).Any(IsDoomApplication)) continue;
                    found = true;
                    yield return moveNext;
                }
                if (!found) throw new InvalidOperationException($"No vanilla Doom producer found on {type.FullName}.");
            }
        }

        private static bool IsDoomApplication(CodeInstruction instruction) =>
            instruction.operand is MethodInfo method && method.DeclaringType == typeof(PowerCmd)
            && method.Name == nameof(PowerCmd.Apply) && method.IsGenericMethod
            && method.GetGenericArguments().SequenceEqual([typeof(DoomPower)]);

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (IsDoomApplication(instruction))
                {
                    MethodInfo original = (MethodInfo)instruction.operand;
                    string replacement = original.GetParameters()[1].ParameterType == typeof(Creature)
                        ? nameof(DoomRework.ApplySingle) : nameof(DoomRework.ApplyMultiple);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(DoomRework), replacement);
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch]
    private static class AreaBacklashPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
            new[] { typeof(Deathbringer), typeof(EndOfDays) }.Select(type => AccessTools.DeclaredMethod(type, "OnPlay"));

        private static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result) =>
            __result = Complete(__result, __instance, choiceContext);

        private static async Task Complete(Task original, CardModel card, PlayerChoiceContext context)
        {
            await original;
            // Enemy count must not multiply the cost of a single area card.
            await DoomRework.ApplyBacklash(context, card.Owner.Creature, card.DynamicVars.Doom.BaseValue, card);
        }
    }

    [HarmonyPatch(typeof(CardModel), "get_Description")]
    private static class DoomDescriptionPatch
    {
        private static void Postfix(CardModel __instance, LocString __result)
        {
            string? key = __instance switch
            {
                Scourge or Deathbringer or EndOfDays or NegativePulse or Oblivion => "DoomPower",
#if !STS2DIY_STABLE_1110
                Death => "DoomPower",
#endif
                Countdown => "CountdownPower",
                Neurosurge => "NeurosurgePower",
                NoEscape => "CalculationBase",
                _ => null
            };
            if (key == null) return;
            decimal amount = __instance.DynamicVars[key].BaseValue;
            __result.Add("DiyDoom", DoomRework.Scale(amount));
            __result.Add("DiySelfDoom", DoomRework.BacklashForCard(amount, __instance));
        }
    }
}

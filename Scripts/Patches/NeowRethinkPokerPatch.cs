using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Neow Rethink Poker.</summary>
public class NeowRethinkPokerPatch : IPatchMethod
{
    public static string PatchId => "neow_grand_guignol_initial_relic_option";
    public static string Description => "Replace one positive Neow option with a Grand Guignol initial relic option.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Neow), "GenerateInitialOptions", ignoreIfMissing: true)];

    private const int PositiveOptionCount = 2;

    private static readonly MethodInfo? RelicOptionMethod =
        AccessTools.Method(
            typeof(AncientEventModel),
            "RelicOption",
            [typeof(RelicModel), typeof(string), typeof(string)]);
    private static bool _missingRelicOptionLogged;

    internal static bool CanCreateRelicOption => RelicOptionMethod != null;

    public static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        try
        {
            if (RelicOptionMethod == null)
            {
                if (!_missingRelicOptionLogged)
                {
                    _missingRelicOptionLogged = true;
                    Entry.Logger.Warn("Grand Guignol's Neow option was disabled because AncientEventModel.RelicOption is unavailable.");
                }

                return;
            }

            if (__instance.Owner == null || __instance.Owner.RunState.Modifiers.Count > 0)
            {
                return;
            }

            if (__result.Count < PositiveOptionCount)
            {
                return;
            }

            List<RelicModel> candidates = [
                ModelDb.Relic<RethinkPokerRelic>().ToMutable(),
                ModelDb.Relic<WormSmokeRelic>().ToMutable(),
                ModelDb.Relic<MargaretRelic>().ToMutable(),
                ModelDb.Relic<AngelFeatherRelic>().ToMutable(),
                ModelDb.Relic<MabelSoldierRelic>().ToMutable(),
                ModelDb.Relic<GuignolsDollRelic>().ToMutable()
            ];
            candidates.RemoveAll(relic => !relic.IsAllowed(__instance.Owner.RunState));
            if (candidates.Count == 0)
            {
                return;
            }

            if (__instance.Owner.RunState.Rng.Niche.NextInt(100) >= BsAncientConfig.GrandGuignolInitialRelicChance)
            {
                return;
            }

            List<EventOption> options = __result.ToList();
            RelicModel? relic = __instance.Owner.RunState.Rng.Niche.NextItem(candidates);
            if (relic == null)
            {
                return;
            }

            int replacementIndex = __instance.Owner.RunState.Rng.Niche.NextInt(PositiveOptionCount);
            options[replacementIndex] = CreateRethinkPokerOption(__instance, relic);
            __result = options;
        }
        catch (Exception exception)
        {
            Entry.Logger.Warn($"Grand Guignol's Neow option was left unchanged: {exception.Message}");
        }
    }

    private static EventOption CreateRethinkPokerOption(Neow neow, RelicModel relic)
    {
        return (EventOption)RelicOptionMethod!.Invoke(
            neow,
            [relic, "INITIAL", "NEOW.pages.DONE.POSITIVE.description"])!;
    }
}

/// <summary>Obtains the Fairy Tale Book before Neow starts instead of launching an unobserved task.</summary>
public sealed class FairyTaleBookBeforeNeowPatch : IPatchMethod
{
    public static string PatchId => "fairy_tale_book_before_neow";
    public static string Description => "Await Fairy Tale Book acquisition before entering Neow's event.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(Hook),
            nameof(Hook.BeforeRoomEntered),
            [typeof(IRunState), typeof(AbstractRoom)],
            ignoreIfMissing: true)];

    public static void Postfix(IRunState runState, AbstractRoom room, ref Task __result) =>
        __result = Continue(__result, runState, room);

    private static async Task Continue(Task original, IRunState runState, AbstractRoom room)
    {
        await original;
        if (room is not EventRoom { CanonicalEvent: Neow }
            || !BsAncientRunOptions.IsFairyTaleModeEnabled(runState))
        {
            return;
        }

        foreach (var player in runState.Players)
        {
            if (player.GetRelic<UnnamedFairyTaleBookRelic>() == null)
            {
                await RelicCmd.Obtain<UnnamedFairyTaleBookRelic>(player);
            }
        }
    }
}

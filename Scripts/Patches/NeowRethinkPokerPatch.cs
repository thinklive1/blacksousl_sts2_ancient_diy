using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
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

    private static readonly MethodInfo RelicOptionMethod =
        AccessTools.Method(
            typeof(AncientEventModel),
            "RelicOption",
            [typeof(RelicModel), typeof(string), typeof(string)]);

    public static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        try
        {
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
        finally
        {
            if (__instance.Owner != null)
            {
                TryObtainFairyTaleBook(__instance);
            }
        }
    }

    private static void TryObtainFairyTaleBook(Neow neow)
    {
        if (neow.Owner == null
            || !BsAncientRunOptions.IsFairyTaleModeEnabled(neow.Owner.RunState)
            || neow.Owner.GetRelic<UnnamedFairyTaleBookRelic>() != null)
        {
            return;
        }

        _ = ObtainFairyTaleBook(neow);
    }

    private static async Task ObtainFairyTaleBook(Neow neow)
    {
        try
        {
            if (neow.Owner == null || neow.Owner.GetRelic<UnnamedFairyTaleBookRelic>() != null)
            {
                return;
            }

            await RelicCmd.Obtain<UnnamedFairyTaleBookRelic>(neow.Owner);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
        }
    }

    private static EventOption CreateRethinkPokerOption(Neow neow, RelicModel relic)
    {
        return (EventOption)RelicOptionMethod.Invoke(
            neow,
            [relic, "INITIAL", "NEOW.pages.DONE.POSITIVE.description"])!;
    }
}

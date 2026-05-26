using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BlackSouls.Scripts;

[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
public static class NeowRethinkPokerPatch
{
    private const int PositiveOptionCount = 2;
    private const int ApproximatePositiveOptionPoolSize = 14;

    private static readonly MethodInfo RelicOptionMethod =
        AccessTools.Method(
            typeof(AncientEventModel),
            "RelicOption",
            [typeof(RelicModel), typeof(string), typeof(string)]);

    public static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
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
            ModelDb.Relic<MargaretRelic>().ToMutable()
        ];
        candidates.RemoveAll(relic => !relic.IsAllowed(__instance.Owner.RunState));
        if (candidates.Count == 0)
        {
            return;
        }

        if (__instance.Owner.RunState.Rng.Niche.NextInt(ApproximatePositiveOptionPoolSize) != 0)
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

    private static EventOption CreateRethinkPokerOption(Neow neow, RelicModel relic)
    {
        return (EventOption)RelicOptionMethod.Invoke(
            neow,
            [relic, "INITIAL", "NEOW.pages.DONE.POSITIVE.description"])!;
    }
}

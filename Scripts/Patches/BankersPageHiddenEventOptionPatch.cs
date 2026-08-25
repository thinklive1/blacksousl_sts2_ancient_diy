using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies behavior patches for Bankers Page Hidden Event Option.</summary>
public static class BankersPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BANKERS_PAGE_OPTION";
    private const int AppearanceChancePercent = 30;

    public static void SetEventStatePrefix(EventModel __instance, ref IEnumerable<EventOption> eventOptions)
    {
        if (__instance.Owner == null || __instance.IsFinished)
        {
            return;
        }

        List<EventOption> options = eventOptions.ToList();
        if (!HiddenEventOptionSupport.CanFinishEvents
            || options.Count == 0
            || options.Any(option => option.TextKey == HiddenOptionKey)
            || !options.Any(IsGoldGainOption)
            || !SnarkPageRelicTrackerModifier.ShouldOfferHiddenOption<BankersPageRelic>(
                __instance,
                AppearanceChancePercent))
        {
            eventOptions = options;
            return;
        }

        options.Add(CreateHiddenOption(__instance));
        eventOptions = options;
    }

    private static EventOption CreateHiddenOption(EventModel eventModel)
    {
        EventOption option = EventOption.FromRelic(
                ModelDb.Relic<BankersPageRelic>().ToMutable(),
                eventModel,
                () => ObtainBankersPageAndFinish(eventModel),
                HiddenOptionKey)
            .ThatWontSaveToChoiceHistory();

        if (option.Relic != null && eventModel.Owner != null)
        {
            option.Relic.Owner = eventModel.Owner;
        }

        return option;
    }

    private static async Task ObtainBankersPageAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<BankersPageRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BankersPageRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }

    private static bool IsGoldGainOption(EventOption option)
    {
        return option.TextKey switch
        {
            string key when key.Contains("COLOSSAL_FLOWER.pages.") && key.Contains(".options.EXTRACT_CURRENT_PRIZE_") => true,
            "COLOSSAL_FLOWER.pages.REACH_DEEPER_2.options.EXTRACT_INSTEAD" => true,
            "DENSE_VEGETATION.pages.INITIAL.options.TRUDGE_ON" => true,
            "ENDLESS_CONVEYOR.pages.ALL.options.GOLDEN_FYSH" => true,
            "JUNGLE_MAZE_ADVENTURE.pages.INITIAL.options.SOLO_QUEST" => true,
            "JUNGLE_MAZE_ADVENTURE.pages.INITIAL.options.JOIN_FORCES" => true,
            "LOST_WISP.pages.INITIAL.options.SEARCH" => true,
            "SUNKEN_STATUE.pages.INITIAL.options.DIVE_INTO_WATER" => true,
            "SUNKEN_TREASURY.pages.INITIAL.options.FIRST_CHEST" => true,
            "SUNKEN_TREASURY.pages.INITIAL.options.SECOND_CHEST" => true,
            "THE_LANTERN_KEY.pages.INITIAL.options.RETURN_THE_KEY" => true,
            "THIS_OR_THAT.pages.INITIAL.options.PLAIN" => true,
            "TRASH_HEAP.pages.INITIAL.options.GRAB" => true,
            "TRIAL.pages.NOBLE.options.INNOCENT" => true,
            "BS_ANCIENT_EVENT_QUEEN_OF_HEARTS_EVENT.pages.INITIAL.options.GOLD" => true,
            _ => false
        };
    }
}

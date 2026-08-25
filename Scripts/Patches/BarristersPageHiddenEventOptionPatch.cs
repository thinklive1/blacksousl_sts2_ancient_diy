using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies behavior patches for Barristers Page Hidden Event Option.</summary>
public static class BarristersPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BARRISTERS_PAGE_OPTION";
    private const int AppearanceChancePercent = 50;

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
            || !options.Any(IsDeathOption)
            || !SnarkPageRelicTrackerModifier.ShouldOfferHiddenOption<BarristersPageRelic>(
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
                ModelDb.Relic<BarristersPageRelic>().ToMutable(),
                eventModel,
                () => ObtainBarristersPageAndFinish(eventModel),
                HiddenOptionKey)
            .ThatWontSaveToChoiceHistory();

        if (option.Relic != null && eventModel.Owner != null)
        {
            option.Relic.Owner = eventModel.Owner;
        }

        return option;
    }

    private static async Task ObtainBarristersPageAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<BarristersPageRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BarristersPageRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }

    private static bool IsDeathOption(EventOption option)
    {
        return option.TextKey switch
        {
            // Trial - any option can lead to Double Down (instant abandon run)
            string key when key.StartsWith("TRIAL.pages.INITIAL.options.") => true,
            // Tablet of Truth - deciphering can directly call CreatureCmd.Kill()
            string key when key.StartsWith("TABLET_OF_TRUTH.pages.") && key.Contains(".options.DECIPHER") => true,
            // Abyssal Baths - immersive/linger damage can kill
            "ABYSSAL_BATHS.pages.INITIAL.options.IMMERSE" => true,
            "ABYSSAL_BATHS.pages.ALL.options.LINGER" => true,
            // Drowning Beacon - climb = lose 13 Max HP, can kill
            "DROWNING_BEACON.pages.INITIAL.options.CLIMB" => true,
            // Unrest Site - "Kill" option loses 8 Max HP
            "UNREST_SITE.pages.INITIAL.options.KILL" => true,
            _ => false
        };
    }
}

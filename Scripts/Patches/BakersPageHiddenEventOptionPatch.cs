using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds the hidden Baker's Page option to events about forgotten things and discarded memories.</summary>
public static class BakersPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BAKERS_PAGE_OPTION";
    private const int AppearanceChancePercent = 20;

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
            || !options.Any(IsBakerEventOption)
            || !SnarkPageRelicTrackerModifier.ShouldOfferHiddenOption<BakersPageRelic>(
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
                ModelDb.Relic<BakersPageRelic>().ToMutable(),
                eventModel,
                () => ObtainBakersPageAndFinish(eventModel),
                HiddenOptionKey)
            .ThatWontSaveToChoiceHistory();

        if (option.Relic != null && eventModel.Owner != null)
        {
            option.Relic.Owner = eventModel.Owner;
        }

        return option;
    }

    private static async Task ObtainBakersPageAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<BakersPageRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BakersPageRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }

    private static bool IsBakerEventOption(EventOption option)
    {
        return option.TextKey is
            "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.ACCEPT"
            or "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.CONFRONT"
            or "TRASH_HEAP.pages.INITIAL.options.DIVE_IN";
    }
}

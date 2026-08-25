using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds the hidden Bellman's Page option to symmetry and card-crafting events.</summary>
public static class BellmansPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BELLMANS_PAGE_OPTION";
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
            || !options.Any(IsBellmanEventOption)
            || !SnarkPageRelicTrackerModifier.ShouldOfferHiddenOption<BellmansPageRelic>(
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
                ModelDb.Relic<BellmansPageRelic>().ToMutable(),
                eventModel,
                () => ObtainBellmansPageAndFinish(eventModel),
                HiddenOptionKey)
            .ThatWontSaveToChoiceHistory();

        if (option.Relic != null && eventModel.Owner != null)
        {
            option.Relic.Owner = eventModel.Owner;
        }

        return option;
    }

    private static async Task ObtainBellmansPageAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<BellmansPageRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BellmansPageRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }

    private static bool IsBellmanEventOption(EventOption option)
    {
        string key = option.TextKey;
        return key switch
        {
            "AMALGAMATOR.pages.INITIAL.options.COMBINE_STRIKES" => true,
            "AMALGAMATOR.pages.INITIAL.options.COMBINE_DEFENDS" => true,
            "BS_ANCIENT_EVENT_WAX_STATUE_EVENT.pages.INITIAL.options.TWIN" => true,
            "BS_ANCIENT_EVENT_WAX_STATUE_EVENT.pages.INITIAL.options.LONELY" => true,
            string colorKey when colorKey.StartsWith("COLORFUL_PHILOSOPHERS.pages.INITIAL.options.") => true,
            "SPIRALING_WHIRLPOOL.pages.INITIAL.options.OBSERVE" => true,
            "TINKER_TIME.pages.INITIAL.options.CHOOSE_CARD_TYPE" => true,
            "THE_FUTURE_OF_POTIONS.pages.INITIAL.options.POTION" => true,
            "BS_ANCIENT_EVENT_ENDLESS_TEA_PARTY_EVENT.pages.INITIAL.options.ANSWER" => true,
            "BS_ANCIENT_EVENT_ENDLESS_TEA_PARTY_EVENT.pages.INITIAL.options.ASK" => true,
            "BS_ANCIENT_EVENT_ENDLESS_TEA_PARTY_EVENT.pages.INITIAL.options.QUESTION" => true,
            _ => false
        };
    }
}

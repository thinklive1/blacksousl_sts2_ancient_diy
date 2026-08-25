using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds the hidden Helmsman's Page option to voice and water themed events.</summary>
public static class HelmsmansPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_HELMSMANS_PAGE_OPTION";
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
            || !options.Any(IsHelmsmanEventOption)
            || !SnarkPageRelicTrackerModifier.ShouldOfferHiddenOption<HelmsmansPageRelic>(
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
                ModelDb.Relic<HelmsmansPageRelic>().ToMutable(),
                eventModel,
                () => ObtainHelmsmansPageAndFinish(eventModel),
                HiddenOptionKey)
            .ThatWontSaveToChoiceHistory();

        if (option.Relic != null && eventModel.Owner != null)
        {
            option.Relic.Owner = eventModel.Owner;
        }

        return option;
    }

    private static async Task ObtainHelmsmansPageAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<HelmsmansPageRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<HelmsmansPageRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }

    private static bool IsHelmsmanEventOption(EventOption option)
    {
        string key = option.TextKey;
        return key switch
        {
            "DROWNING_BEACON.pages.INITIAL.options.BOTTLE" => true,
            "DROWNING_BEACON.pages.INITIAL.options.CLIMB" => true,
            "WATERLOGGED_SCRIPTORIUM.pages.INITIAL.options.BLOODY_INK" => true,
            "WATERLOGGED_SCRIPTORIUM.pages.INITIAL.options.TENTACLE_QUILL" => true,
            "WATERLOGGED_SCRIPTORIUM.pages.INITIAL.options.PRICKLY_SPONGE" => true,
            "BRAIN_LEECH.pages.INITIAL.options.SHARE_KNOWLEDGE" => true,
            "DOLL_ROOM.pages.INITIAL.options.RANDOM" => true,
            "DOLL_ROOM.pages.INITIAL.options.TAKE_SOME_TIME" => true,
            "DOLL_ROOM.pages.INITIAL.options.EXAMINE" => true,
            "THE_LANTERN_KEY.pages.INITIAL.options.RETURN_THE_KEY" => true,
            "BS_ANCIENT_EVENT_BIRD_SINGER_EVENT.pages.INITIAL.options.WAIT" => true,
            "BS_ANCIENT_EVENT_BIRD_SINGER_EVENT.pages.INITIAL.options.FLEE" => true,
            _ => false
        };
    }
}

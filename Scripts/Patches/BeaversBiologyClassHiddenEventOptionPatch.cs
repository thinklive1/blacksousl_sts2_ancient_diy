using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds one hidden lesson option to the History Course event.</summary>
public static class BeaversBiologyClassHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BEVERS_BIOLOGY_CLASS_OPTION";
    private const string ButchersHiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BUTCHERS_MATH_CLASS_OPTION";
    private const string HiddenLessonRollId = "BS_ANCIENT_HIDDEN_HISTORY_LESSON";
    private const string HistoryCourseOptionKey = "WAR_HISTORIAN_REPY.pages.INITIAL.options.UNLOCK_CAGE";
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
            || options.Any(option => option.TextKey is HiddenOptionKey or ButchersHiddenOptionKey)
            || !options.Any(option => option.TextKey == HistoryCourseOptionKey)
            || __instance.Owner.GetRelic<BeaversBiologyClassRelic>() != null
            || __instance.Owner.GetRelic<ButchersMathClassRelic>() != null)
        {
            eventOptions = options;
            return;
        }

        int lesson = SnarkPageRelicTrackerModifier.GetOrCreateHiddenOptionOutcome(
            __instance,
            HiddenLessonRollId,
            () => RollHiddenLesson(__instance));
        if (lesson == 0)
        {
            eventOptions = options;
            return;
        }

        bool chooseBiologyClass = lesson == 1;
        options.Add(CreateHiddenOption(__instance, chooseBiologyClass));
        eventOptions = options;
    }

    private static int RollHiddenLesson(EventModel eventModel)
    {
        if (eventModel.Owner == null
            || SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BeaversBiologyClassRelic>(eventModel.Owner)
            || SnarkPageRelicTrackerModifier.HasAppearedOrOwned<ButchersMathClassRelic>(eventModel.Owner)
            || eventModel.Owner.RunState.Rng.Niche.NextInt(100) >= AppearanceChancePercent)
        {
            return 0;
        }

        bool chooseBiologyClass = eventModel.Owner.RunState.Rng.Niche.NextInt(2) == 0;
        if (chooseBiologyClass)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<BeaversBiologyClassRelic>(eventModel.Owner);
            return 1;
        }

        SnarkPageRelicTrackerModifier.MarkAppeared<ButchersMathClassRelic>(eventModel.Owner);
        return 2;
    }

    private static EventOption CreateHiddenOption(EventModel eventModel, bool chooseBiologyClass)
    {
        EventOption option = chooseBiologyClass
            ? EventOption.FromRelic(
                ModelDb.Relic<BeaversBiologyClassRelic>().ToMutable(),
                eventModel,
                () => ObtainBeaversBiologyClassAndFinish(eventModel),
                HiddenOptionKey)
            : EventOption.FromRelic(
                ModelDb.Relic<ButchersMathClassRelic>().ToMutable(),
                eventModel,
                () => ObtainButchersMathClassAndFinish(eventModel),
                ButchersHiddenOptionKey);

        option = option.ThatWontSaveToChoiceHistory();

        if (option.Relic != null && eventModel.Owner != null)
        {
            option.Relic.Owner = eventModel.Owner;
        }

        return option;
    }

    private static async Task ObtainBeaversBiologyClassAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<BeaversBiologyClassRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BeaversBiologyClassRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }

    private static async Task ObtainButchersMathClassAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<ButchersMathClassRelic>() != null)
        {
            HiddenEventOptionSupport.FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<ButchersMathClassRelic>(eventModel.Owner);
        HiddenEventOptionSupport.FinishEvent(eventModel);
    }
}

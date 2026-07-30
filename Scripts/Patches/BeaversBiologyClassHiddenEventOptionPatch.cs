using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds one hidden lesson option to the History Course event.</summary>
[HarmonyPatch]
public static class BeaversBiologyClassHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BEVERS_BIOLOGY_CLASS_OPTION";
    private const string ButchersHiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BUTCHERS_MATH_CLASS_OPTION";
    private const string HistoryCourseOptionKey = "WAR_HISTORIAN_REPY.pages.INITIAL.options.UNLOCK_CAGE";
    private const float HiddenAlpha = 0f;
    private const float HoverAlpha = 0.08f;
    private const int AppearanceChancePercent = 20;

    private static readonly MethodInfo SetEventFinishedMethod =
        AccessTools.Method(typeof(EventModel), "SetEventFinished", [typeof(LocString)]);

    [HarmonyPatch(typeof(EventModel), "SetEventState")]
    [HarmonyPrefix]
    public static void SetEventStatePrefix(EventModel __instance, ref IEnumerable<EventOption> eventOptions)
    {
        if (__instance.Owner == null || __instance.IsFinished)
        {
            return;
        }

        List<EventOption> options = eventOptions.ToList();
        if (options.Count == 0
            || options.Any(option => option.TextKey is HiddenOptionKey or ButchersHiddenOptionKey)
            || !options.Any(option => option.TextKey == HistoryCourseOptionKey)
            || SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BeaversBiologyClassRelic>(__instance.Owner)
            || SnarkPageRelicTrackerModifier.HasAppearedOrOwned<ButchersMathClassRelic>(__instance.Owner)
            || __instance.Owner.RunState.Rng.Niche.NextInt(100) >= AppearanceChancePercent)
        {
            eventOptions = options;
            return;
        }

        bool chooseBiologyClass = __instance.Owner.RunState.Rng.Niche.NextInt(2) == 0;
        if (chooseBiologyClass)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<BeaversBiologyClassRelic>(__instance.Owner);
        }
        else
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<ButchersMathClassRelic>(__instance.Owner);
        }

        options.Add(CreateHiddenOption(__instance, chooseBiologyClass));
        eventOptions = options;
    }

    [HarmonyPatch(typeof(NEventOptionButton), nameof(NEventOptionButton._Ready))]
    [HarmonyPostfix]
    public static void EventOptionButtonReadyPostfix(NEventOptionButton __instance)
    {
        ApplyHiddenVisuals(__instance, HiddenAlpha);
    }

    [HarmonyPatch(typeof(NEventOptionButton), nameof(NEventOptionButton.EnableButton))]
    [HarmonyPostfix]
    public static void EventOptionButtonEnablePostfix(NEventOptionButton __instance)
    {
        ApplyHiddenVisuals(__instance, HiddenAlpha);
    }

    [HarmonyPatch(typeof(NEventOptionButton), "OnFocus")]
    [HarmonyPostfix]
    public static void EventOptionButtonFocusPostfix(NEventOptionButton __instance)
    {
        ApplyHiddenVisuals(__instance, HoverAlpha);
    }

    [HarmonyPatch(typeof(NEventOptionButton), "OnUnfocus")]
    [HarmonyPostfix]
    public static void EventOptionButtonUnfocusPostfix(NEventOptionButton __instance)
    {
        ApplyHiddenVisuals(__instance, HiddenAlpha);
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
            FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BeaversBiologyClassRelic>(eventModel.Owner);
        FinishEvent(eventModel);
    }

    private static async Task ObtainButchersMathClassAndFinish(EventModel eventModel)
    {
        if (eventModel.Owner == null || eventModel.Owner.GetRelic<ButchersMathClassRelic>() != null)
        {
            FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<ButchersMathClassRelic>(eventModel.Owner);
        FinishEvent(eventModel);
    }

    private static void FinishEvent(EventModel eventModel)
    {
        LocString description = eventModel.Description ?? eventModel.InitialDescription;
        SetEventFinishedMethod.Invoke(eventModel, [description]);
    }

    private static void ApplyHiddenVisuals(NEventOptionButton button, float alpha)
    {
        if (button.Option.TextKey is HiddenOptionKey or ButchersHiddenOptionKey)
        {
            button.Modulate = new Color(1f, 1f, 1f, alpha);
        }
    }
}

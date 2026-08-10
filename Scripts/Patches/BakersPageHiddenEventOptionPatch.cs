using System.Reflection;
using BlackSouls.Scripts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds the hidden Baker's Page option to events about forgotten things and discarded memories.</summary>
[HarmonyPatch]
public static class BakersPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BAKERS_PAGE_OPTION";
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
            FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BakersPageRelic>(eventModel.Owner);
        FinishEvent(eventModel);
    }

    private static void FinishEvent(EventModel eventModel)
    {
        LocString description = eventModel.Description ?? eventModel.InitialDescription;
        SetEventFinishedMethod.Invoke(eventModel, [description]);
    }

    private static void ApplyHiddenVisuals(NEventOptionButton button, float alpha)
    {
        if (button.Option.TextKey == HiddenOptionKey)
        {
            button.Modulate = new Color(1f, 1f, 1f, alpha);
        }
    }

    private static bool IsBakerEventOption(EventOption option)
    {
        return option.TextKey is
            "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.ACCEPT"
            or "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.CONFRONT"
            or "TRASH_HEAP.pages.INITIAL.options.DIVE_IN";
    }
}

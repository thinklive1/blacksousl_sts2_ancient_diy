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

[HarmonyPatch]
public static class BarristersPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BARRISTERS_PAGE_OPTION";
    private const float HiddenAlpha = 0f;
    private const float HoverAlpha = 0.08f;
    private const double AppearanceChance = 0.5;

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
            || !options.Any(IsDeathOption)
            || __instance.Owner.GetRelic<BarristersPageRelic>() != null
            || Random.Shared.NextDouble() >= AppearanceChance)
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
            FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BarristersPageRelic>(eventModel.Owner);
        FinishEvent(eventModel);
    }

    private static void FinishEvent(EventModel eventModel)
    {
        LocString description = eventModel.Description ?? eventModel.InitialDescription;
        SetEventFinishedMethod.Invoke(eventModel, [description]);
    }

    private static void ApplyHiddenVisuals(NEventOptionButton button, float alpha)
    {
        if (button.Option.TextKey != HiddenOptionKey)
        {
            return;
        }

        button.Modulate = new Color(1f, 1f, 1f, alpha);
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

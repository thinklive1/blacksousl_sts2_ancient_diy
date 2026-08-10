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

/// <summary>Adds the hidden Bellman's Page option to symmetry and card-crafting events.</summary>
[HarmonyPatch]
public static class BellmansPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_BELLMANS_PAGE_OPTION";
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
            FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<BellmansPageRelic>(eventModel.Owner);
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

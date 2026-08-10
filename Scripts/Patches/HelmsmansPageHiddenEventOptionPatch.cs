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

/// <summary>Adds the hidden Helmsman's Page option to voice and water themed events.</summary>
[HarmonyPatch]
public static class HelmsmansPageHiddenEventOptionPatch
{
    private const string HiddenOptionKey = "BS_ANCIENT_EASTER_EGG_HELMSMANS_PAGE_OPTION";
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
            FinishEvent(eventModel);
            return;
        }

        await RelicCmd.Obtain<HelmsmansPageRelic>(eventModel.Owner);
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

using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Runs all hidden event option providers through one guarded EventModel patch.</summary>
public sealed class HiddenEventOptionInjectionPatch : IPatchMethod
{
    public static string PatchId => "hidden_event_option_injection";
    public static string Description => "Inject BS Ancient easter-egg options into eligible events.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(EventModel),
            "SetEventState",
            [typeof(LocString), typeof(IEnumerable<EventOption>)],
            ignoreIfMissing: true)];

    public static void Prefix(EventModel __instance, ref IEnumerable<EventOption> eventOptions)
    {
        BakersPageHiddenEventOptionPatch.SetEventStatePrefix(__instance, ref eventOptions);
        BankersPageHiddenEventOptionPatch.SetEventStatePrefix(__instance, ref eventOptions);
        BarristersPageHiddenEventOptionPatch.SetEventStatePrefix(__instance, ref eventOptions);
        BeaversBiologyClassHiddenEventOptionPatch.SetEventStatePrefix(__instance, ref eventOptions);
        BellmansPageHiddenEventOptionPatch.SetEventStatePrefix(__instance, ref eventOptions);
        HelmsmansPageHiddenEventOptionPatch.SetEventStatePrefix(__instance, ref eventOptions);
    }
}

/// <summary>Provides one guarded finish path and one visual patch set for hidden event options.</summary>
internal static class HiddenEventOptionSupport
{
    private const string HiddenOptionPrefix = "BS_ANCIENT_EASTER_EGG_";
    private static readonly MethodInfo? SetEventFinishedMethod =
        AccessTools.Method(typeof(EventModel), "SetEventFinished", [typeof(LocString)]);
    private static readonly MethodInfo? SetEventStateMethod =
        AccessTools.Method(
            typeof(EventModel),
            "SetEventState",
            [typeof(LocString), typeof(IEnumerable<EventOption>)]);

    internal static bool CanFinishEvents => SetEventFinishedMethod != null || SetEventStateMethod != null;

    internal static void FinishEvent(EventModel eventModel)
    {
        LocString description = eventModel.Description ?? eventModel.InitialDescription;
        Exception? firstFailure = null;
        if (SetEventFinishedMethod != null)
        {
            try
            {
                SetEventFinishedMethod.Invoke(eventModel, [description]);
                return;
            }
            catch (Exception exception)
            {
                firstFailure = Unwrap(exception);
                Entry.Logger.Warn($"Hidden event option could not use SetEventFinished: {firstFailure.Message}");
            }
        }

        if (SetEventStateMethod != null)
        {
            try
            {
                SetEventStateMethod.Invoke(eventModel, [description, Array.Empty<EventOption>()]);
                return;
            }
            catch (Exception exception)
            {
                Exception failure = Unwrap(exception);
                Entry.Logger.Warn($"Hidden event option could not use SetEventState: {failure.Message}");
                throw failure;
            }
        }

        throw new MissingMethodException(
            firstFailure?.Message ?? "EventModel has no compatible event-finish method.");
    }

    internal static bool IsHiddenOption(EventOption? option)
    {
        return option?.TextKey.StartsWith(HiddenOptionPrefix, StringComparison.Ordinal) == true;
    }

    private static Exception Unwrap(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: { } inner }
            ? inner
            : exception;
    }
}

/// <summary>Applies the nearly invisible easter-egg style to every hidden event option.</summary>
public sealed class HiddenEventOptionVisualPatch : IPatchMethod
{
    private const float HiddenAlpha = 0f;
    private const float HoverAlpha = 0.08f;

    public static string PatchId => "hidden_event_option_visuals";
    public static string Description => "Render BS Ancient easter-egg event options as nearly invisible buttons.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [
            new(typeof(NEventOptionButton), nameof(NEventOptionButton._Ready), ignoreIfMissing: true),
            new(typeof(NEventOptionButton), nameof(NEventOptionButton.EnableButton), ignoreIfMissing: true),
            new(typeof(NEventOptionButton), "OnFocus", ignoreIfMissing: true),
            new(typeof(NEventOptionButton), "OnUnfocus", ignoreIfMissing: true),
        ];

    public static void Postfix(NEventOptionButton __instance, MethodBase __originalMethod)
    {
        float alpha = __originalMethod.Name == "OnFocus" ? HoverAlpha : HiddenAlpha;
        ApplyHiddenVisuals(__instance, alpha);
    }

    private static void ApplyHiddenVisuals(NEventOptionButton button, float alpha)
    {
        if (HiddenEventOptionSupport.IsHiddenOption(button.Option))
        {
            button.Modulate = new Color(1f, 1f, 1f, alpha);
        }
    }
}

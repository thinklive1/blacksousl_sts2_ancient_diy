using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using BlackSouls.Scripts;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Guards tea-party option localization without replacing other events' initialization.</summary>
public sealed class TeaPartyEventOptionLocVarsPatch : IPatchMethod
{
    public static string PatchId => "tea_party_event_option_loc_vars";
    public static string Description => "Tolerate incomplete third-party character data in the tea party event.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(EventOption), "AddLocVars", [typeof(EventModel)], ignoreIfMissing: true)];

    public static bool Prefix(EventOption __instance, EventModel eventModel)
    {
        if (eventModel is not EndlessTeaPartyEvent)
        {
            return true;
        }

        Player? owner = eventModel.Owner;
        try
        {
            owner?.Character?.AddDetailsTo(__instance.Description);
        }
        catch (NullReferenceException)
        {
            // A malformed external character must not prevent the event from being displayed.
        }

        __instance.Description.Add("IsMultiplayer", owner?.RunState?.Players.Count > 1);
        return false;
    }
}

/// <summary>Guards custom events against missing character data and legacy localization keys.</summary>
[HarmonyPatch]
public static class ModEventCompatibilityPatch
{
    private const string EventTable = "events";
    private const string ModEventPrefix = "BS_ANCIENT_EVENT_";
    private static readonly HashSet<string> LegacyEventRoots = new(StringComparer.Ordinal)
    {
        "BALATRO_TRAINING_DUMMY_EVENT",
        "BIRD_SINGER_EVENT",
        "BOOJUM_EVENT",
        "CLOWN_EVENT",
        "ENDLESS_TEA_PARTY_EVENT",
        "FRIENDLY_SLIME_EVENT",
        "GENTLE_GIFT_EVENT",
        "GIRL_IN_MAZE_EVENT",
        "HEART_JACK_EVENT",
        "HORRIFYING_GLUTTON_EVENT",
        "LAST_WHITE_KNIGHT_EVENT",
        "PLAYING_CARD_GARDENERS_EVENT",
        "QUEEN_OF_HEARTS_EVENT",
        "QUEEN_TART_EVENT",
        "WAX_STATUE_EVENT",
    };

    /// <summary>Uses the existing prefixed entries while older localization files are being migrated.</summary>
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool GetRawTextPrefix(LocString __instance, ref string __result)
    {
        if (__instance.LocTable != EventTable
            || __instance.LocEntryKey.StartsWith(ModEventPrefix, StringComparison.Ordinal)
            || !IsKnownLegacyEventKey(__instance.LocEntryKey))
        {
            return true;
        }

        string fallbackKey = ModEventPrefix + __instance.LocEntryKey;
        if (!LocString.Exists(EventTable, fallbackKey))
        {
            return true;
        }

        __result = new LocString(EventTable, fallbackKey).GetRawText();
        return false;
    }

    internal static bool IsKnownLegacyEventKey(string key)
    {
        int separatorIndex = key.IndexOf('.', StringComparison.Ordinal);
        string eventRoot = separatorIndex < 0 ? key : key[..separatorIndex];
        return LegacyEventRoots.Contains(eventRoot);
    }

    /// <summary>Returns the tea party portrait directly when RitsuLib leaves the vanilla path behind.</summary>
    [HarmonyPatch(typeof(EventModel), nameof(EventModel.CreateInitialPortrait))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool CreateInitialPortraitPrefix(EventModel __instance, ref Texture2D __result)
    {
        if (__instance is not EndlessTeaPartyEvent)
        {
            return true;
        }

        __result = PreloadManager.Cache.GetTexture2D(EndlessTeaPartyEvent.PortraitPath);
        return false;
    }
}

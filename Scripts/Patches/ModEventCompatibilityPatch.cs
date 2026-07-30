using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using BlackSouls.Scripts;

namespace BlackSouls.Scripts.Patches;

/// <summary>Guards custom events against missing character data and legacy localization keys.</summary>
[HarmonyPatch]
public static class ModEventCompatibilityPatch
{
    private const string EventTable = "events";
    private const string ModEventPrefix = "BS_ANCIENT_EVENT_";

    /// <summary>Skips character variables when a third-party character is not fully initialized.</summary>
    [HarmonyPatch(typeof(EventOption), "AddLocVars")]
    [HarmonyPrefix]
    private static bool AddLocVarsPrefix(EventOption __instance, EventModel eventModel)
    {
        Player? owner = eventModel.Owner;
        try
        {
            owner?.Character?.AddDetailsTo(__instance.Description);
        }
        catch (NullReferenceException)
        {
            // A malformed external character must not prevent the event from being displayed.
        }

        __instance.Description.Add("IsMultiplayer", owner != null && owner.RunState.Players.Count > 1);
        return false;
    }

    /// <summary>Uses the existing prefixed entries while older localization files are being migrated.</summary>
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool GetRawTextPrefix(LocString __instance, ref string __result)
    {
        if (__instance.LocTable != EventTable
            || __instance.LocEntryKey.StartsWith(ModEventPrefix, StringComparison.Ordinal))
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

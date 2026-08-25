using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Only Use Mod Ancients.</summary>
public class OnlyUseModAncientsPatch : IPatchMethod
{
    public static string PatchId => "mod_ancient_room_generation_rules";
    public static string Description => "Apply BS Ancient map ancient generation settings.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(ActModel),
            nameof(ActModel.GenerateRooms),
            [typeof(Rng), typeof(UnlockState), typeof(bool)],
            ignoreIfMissing: true)];

    private static readonly FieldInfo? RoomsField = AccessTools.Field(typeof(ActModel), "_rooms");
    private static readonly FieldInfo? SharedAncientSubsetField = AccessTools.Field(typeof(ActModel), "_sharedAncientSubset");
    private static bool _missingRoomsFieldLogged;
    private static bool _missingSharedAncientSubsetFieldLogged;

    internal static bool HasRoomAccess => RoomsField != null;
    internal static bool HasSharedAncientSubsetAccess => SharedAncientSubsetField != null;

    public static void Postfix(ActModel __instance, Rng rng, UnlockState unlockState, bool isMultiplayer)
    {
        RemoveGeneratedGrandGuignol(__instance, rng, unlockState);
        RemoveGeneratedDisabledModAncient(__instance, rng, unlockState);

        if (BsAncientConfig.DisableModAncients)
        {
            RemoveGeneratedModAncient(__instance, rng, unlockState);
            return;
        }

        if (!BsAncientConfig.OnlyUseModAncients)
        {
            return;
        }

        List<AncientEventModel> candidates = __instance switch
        {
            Hive => [
                .. GetEnabledHiveAncients(),
            ],
            Glory => [
                .. GetEnabledGloryAncients(),
            ],
            _ => [],
        };

        if (HasGeneratedMabelEarlier(__instance))
        {
            candidates.RemoveAll(ancient => ancient is MabelAncient);
        }

        if (candidates.Count == 0)
        {
            return;
        }

        AncientEventModel? ancient = rng.NextItem(candidates);
        if (ancient != null && TryGetRooms(__instance, out RoomSet rooms))
        {
            rooms.Ancient = ancient;
        }
    }

    private static bool HasGeneratedMabelEarlier(ActModel currentAct)
    {
        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
        {
            return false;
        }

        foreach (ActModel act in runState.Acts)
        {
            if (ReferenceEquals(act, currentAct))
            {
                return false;
            }

            if (TryGetRooms(act, out RoomSet rooms)
                && rooms.HasAncient
                && rooms.Ancient is MabelAncient)
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveGeneratedGrandGuignol(ActModel act, Rng rng, UnlockState unlockState)
    {
        if (!TryGetRooms(act, out RoomSet rooms)
            || !rooms.HasAncient
            || rooms.Ancient is not GrandGuignolAncient)
        {
            return;
        }

        List<AncientEventModel> candidates = act.GetUnlockedAncients(unlockState)
            .Concat(GetSharedAncientSubset(act))
            .Where(ancient => ancient is not GrandGuignolAncient)
            .ToList();

        AncientEventModel? replacement = rng.NextItem(candidates);
        if (replacement != null)
        {
            rooms.Ancient = replacement;
        }
    }

    private static void RemoveGeneratedModAncient(ActModel act, Rng rng, UnlockState unlockState)
    {
        if (!TryGetRooms(act, out RoomSet rooms)
            || !rooms.HasAncient
            || !IsMapModAncient(rooms.Ancient))
        {
            return;
        }

        List<AncientEventModel> candidates = act.GetUnlockedAncients(unlockState)
            .Concat(GetSharedAncientSubset(act))
            .Where(ancient => !IsMapModAncient(ancient))
            .ToList();

        AncientEventModel? replacement = rng.NextItem(candidates);
        if (replacement != null)
        {
            rooms.Ancient = replacement;
        }
    }

    private static bool IsMapModAncient(AncientEventModel ancient)
    {
        return ancient is NodeAncient or PrickettAncient or MabelAncient or LorinaAncient;
    }

    private static void RemoveGeneratedDisabledModAncient(ActModel act, Rng rng, UnlockState unlockState)
    {
        if (!TryGetRooms(act, out RoomSet rooms)
            || !rooms.HasAncient
            || !IsMapModAncient(rooms.Ancient)
            || IsEnabledMapModAncient(rooms.Ancient))
        {
            return;
        }

        List<AncientEventModel> candidates = act.GetUnlockedAncients(unlockState)
            .Concat(GetSharedAncientSubset(act))
            .Where(ancient => !IsMapModAncient(ancient) || IsEnabledMapModAncient(ancient))
            .ToList();

        AncientEventModel? replacement = rng.NextItem(candidates);
        if (replacement != null)
        {
            rooms.Ancient = replacement;
        }
    }

    private static bool IsEnabledMapModAncient(AncientEventModel ancient)
    {
        return ancient switch
        {
            NodeAncient => BsAncientConfig.EnableNodeAncient,
            PrickettAncient => BsAncientConfig.EnablePrickettAncient,
            MabelAncient => BsAncientConfig.EnableMabelAncient,
            LorinaAncient => BsAncientConfig.EnableLorinaAncient,
            _ => true,
        };
    }

    private static IEnumerable<AncientEventModel> GetEnabledHiveAncients()
    {
        if (BsAncientConfig.EnableNodeAncient)
        {
            yield return ModelDb.AncientEvent<NodeAncient>();
        }

        if (BsAncientConfig.EnableMabelAncient)
        {
            yield return ModelDb.AncientEvent<MabelAncient>();
        }

        if (BsAncientConfig.EnableLorinaAncient)
        {
            yield return ModelDb.AncientEvent<LorinaAncient>();
        }
    }

    private static IEnumerable<AncientEventModel> GetEnabledGloryAncients()
    {
        if (BsAncientConfig.EnablePrickettAncient)
        {
            yield return ModelDb.AncientEvent<PrickettAncient>();
        }

        if (BsAncientConfig.EnableMabelAncient)
        {
            yield return ModelDb.AncientEvent<MabelAncient>();
        }
    }

    private static bool TryGetRooms(ActModel act, out RoomSet rooms)
    {
        try
        {
            if (RoomsField?.GetValue(act) is RoomSet resolvedRooms)
            {
                rooms = resolvedRooms;
                return true;
            }
        }
        catch (Exception exception)
        {
            LogMissingRoomsField(exception.Message);
            rooms = null!;
            return false;
        }

        LogMissingRoomsField("field unavailable");
        rooms = null!;
        return false;
    }

    private static IEnumerable<AncientEventModel> GetSharedAncientSubset(ActModel act)
    {
        try
        {
            if (SharedAncientSubsetField?.GetValue(act) is List<AncientEventModel> subset)
            {
                return subset;
            }
        }
        catch (Exception exception)
        {
            LogMissingSharedAncientSubset(exception.Message);
            return [];
        }

        if (SharedAncientSubsetField == null)
        {
            LogMissingSharedAncientSubset("field unavailable");
        }

        return [];
    }

    private static void LogMissingRoomsField(string reason)
    {
        if (_missingRoomsFieldLogged)
        {
            return;
        }

        _missingRoomsFieldLogged = true;
        Entry.Logger.Warn($"Ancient room filtering was disabled because ActModel._rooms is unavailable: {reason}.");
    }

    private static void LogMissingSharedAncientSubset(string reason)
    {
        if (_missingSharedAncientSubsetFieldLogged)
        {
            return;
        }

        _missingSharedAncientSubsetFieldLogged = true;
        Entry.Logger.Warn($"Shared Ancient replacements will use act-local candidates because ActModel._sharedAncientSubset is unavailable: {reason}.");
    }
}

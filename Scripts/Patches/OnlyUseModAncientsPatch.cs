using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

public class OnlyUseModAncientsPatch : IPatchMethod
{
    public static string PatchId => "mod_ancient_room_generation_rules";
    public static string Description => "Apply BS Ancient map ancient generation settings.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ActModel), nameof(ActModel.GenerateRooms))];

    private static readonly AccessTools.FieldRef<ActModel, RoomSet> RoomsRef =
        AccessTools.FieldRefAccess<ActModel, RoomSet>("_rooms");

    private static readonly AccessTools.FieldRef<ActModel, List<AncientEventModel>?> SharedAncientSubsetRef =
        AccessTools.FieldRefAccess<ActModel, List<AncientEventModel>?>("_sharedAncientSubset");

    public static void Postfix(ActModel __instance, Rng rng, UnlockState unlockState, bool isMultiplayer)
    {
        RemoveGeneratedGrandGuignol(__instance, rng, unlockState);

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
                ModelDb.AncientEvent<NodeAncient>(),
                ModelDb.AncientEvent<MabelAncient>(),
            ],
            Glory => [
                ModelDb.AncientEvent<PrickettAncient>(),
                ModelDb.AncientEvent<MabelAncient>(),
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
        if (ancient != null)
        {
            RoomsRef(__instance).Ancient = ancient;
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

            if (RoomsRef(act).HasAncient && RoomsRef(act).Ancient is MabelAncient)
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveGeneratedGrandGuignol(ActModel act, Rng rng, UnlockState unlockState)
    {
        RoomSet rooms = RoomsRef(act);
        if (rooms.Ancient is not GrandGuignolAncient)
        {
            return;
        }

        List<AncientEventModel> candidates = act.GetUnlockedAncients(unlockState)
            .Concat(SharedAncientSubsetRef(act) ?? [])
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
        RoomSet rooms = RoomsRef(act);
        if (!IsMapModAncient(rooms.Ancient))
        {
            return;
        }

        List<AncientEventModel> candidates = act.GetUnlockedAncients(unlockState)
            .Concat(SharedAncientSubsetRef(act) ?? [])
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
        return ancient is NodeAncient or PrickettAncient or MabelAncient;
    }
}

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies behavior patches for Helmsmans Page Relic.</summary>
public sealed class HelmsmansPageRelicPatch : IPatchMethod
{
    private const string StunnedMoveId = "STUNNED";

    private static readonly FieldInfo? OnPerformField =
        AccessTools.Field(typeof(MoveState), "_onPerform");

    private static readonly Dictionary<MethodBase, bool> TalksToPlayerCache = [];

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = BuildOpCodeLookup();
    private static bool _missingOnPerformFieldLogged;
    private static bool _moveInspectionFailureLogged;
    private static bool _nextMoveFailureLogged;

    internal static bool CanInspectMoves => OnPerformField != null;

    public static string PatchId => "helmsmans_page_relic_enemy_take_turn";
    public static string Description => "Stun enemies whose current move attempts to talk while Helmsman's Page is owned.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [
            new(
                typeof(Creature),
                nameof(Creature.TakeTurn),
                [],
                ignoreIfMissing: true)
        ];

    [HarmonyPrefix]
    public static bool Prefix(Creature __instance, ref Task __result)
    {
        if (!TryGetTriggeringRelic(__instance, out HelmsmansPageRelic? relic)
            || __instance.Monster?.NextMove is not { } currentMove
            || currentMove.Id == StunnedMoveId
            || !MoveAttemptsToTalk(currentMove))
        {
            return true;
        }

        __result = StunAndSkipCurrentMove(__instance, currentMove, relic!);
        return false;
    }

    private static async Task StunAndSkipCurrentMove(Creature enemy, MoveState skippedMove, HelmsmansPageRelic relic)
    {
        if (enemy.Monster == null)
        {
            return;
        }

        string? nextMoveId = TryGetNextMoveId(enemy, skippedMove);
        relic.Flash();
        await CreatureCmd.Stun(enemy, nextMoveId);
        await enemy.Monster.PerformMove();
    }

    private static bool TryGetTriggeringRelic(Creature enemy, out HelmsmansPageRelic? relic)
    {
        relic = null;
        ICombatState? combatState = enemy.CombatState;
        if (!enemy.IsMonster
            || enemy.Side != CombatSide.Enemy
            || enemy.IsDead
            || enemy.Monster == null
            || enemy.Monster.SpawnedThisTurn
            || combatState == null)
        {
            return false;
        }

        foreach (Player player in combatState.Players)
        {
            relic = player.GetRelic<HelmsmansPageRelic>();
            if (relic != null)
            {
                return true;
            }
        }

        return false;
    }

    private static string? TryGetNextMoveId(Creature enemy, MoveState skippedMove)
    {
        MonsterModel? monster = enemy.Monster;
        if (monster == null)
        {
            return skippedMove.Id;
        }

        try
        {
            return skippedMove.GetNextState(enemy, monster.RunRng.MonsterAi);
        }
        catch (Exception exception)
        {
            if (!_nextMoveFailureLogged)
            {
                _nextMoveFailureLogged = true;
                Entry.Logger.Warn($"Helmsman's Page could not predict the move after a stun and will keep the current move id: {exception.Message}");
            }

            return skippedMove.Id;
        }
    }

    private static bool MoveAttemptsToTalk(MoveState move)
    {
        if (OnPerformField == null)
        {
            if (!_missingOnPerformFieldLogged)
            {
                _missingOnPerformFieldLogged = true;
                Entry.Logger.Warn("Helmsman's Page was disabled because MoveState._onPerform is unavailable.");
            }

            return false;
        }

        try
        {
            if (OnPerformField.GetValue(move) is not Delegate onPerform)
            {
                return false;
            }

            MethodInfo method = onPerform.Method;
            if (TalksToPlayerCache.TryGetValue(method, out bool cached))
            {
                return cached;
            }

            bool result = MethodCallsTalkCmdPlay(method) || AsyncMoveNextCallsTalkCmdPlay(method);
            TalksToPlayerCache[method] = result;
            return result;
        }
        catch (Exception exception)
        {
            if (!_moveInspectionFailureLogged)
            {
                _moveInspectionFailureLogged = true;
                Entry.Logger.Warn($"Helmsman's Page could not inspect an enemy move and will leave it unchanged: {exception.Message}");
            }

            return false;
        }
    }

    private static bool AsyncMoveNextCallsTalkCmdPlay(MethodInfo method)
    {
        AsyncStateMachineAttribute? asyncAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
        MethodInfo? moveNext = asyncAttribute?.StateMachineType.GetMethod(
            "MoveNext",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return moveNext != null && MethodCallsTalkCmdPlay(moveNext);
    }

    private static bool MethodCallsTalkCmdPlay(MethodBase method)
    {
        MethodBody? body = method.GetMethodBody();
        byte[]? il = body?.GetILAsByteArray();
        if (il == null || il.Length == 0)
        {
            return false;
        }

        Module module = method.Module;
        Type[]? declaringGenericArgs = method.DeclaringType?.GetGenericArguments();
        Type[]? methodGenericArgs = method is MethodInfo methodInfo ? methodInfo.GetGenericArguments() : null;

        for (int offset = 0; offset < il.Length;)
        {
            if (!TryReadOpCode(il, ref offset, out OpCode opCode))
            {
                return false;
            }

            int operandOffset = offset;
            int operandSize = GetOperandSize(opCode.OperandType, il, offset);
            if (operandSize < 0 || offset + operandSize > il.Length)
            {
                return false;
            }

            offset += operandSize;

            if (opCode != OpCodes.Call && opCode != OpCodes.Callvirt)
            {
                continue;
            }

            if (operandOffset + sizeof(int) > il.Length)
            {
                return false;
            }

            int metadataToken = BitConverter.ToInt32(il, operandOffset);
            MemberInfo? calledMember;
            try
            {
                calledMember = module.ResolveMethod(metadataToken, declaringGenericArgs, methodGenericArgs);
            }
            catch
            {
                continue;
            }

            if (calledMember is MethodBase calledMethod
                && calledMethod.Name == nameof(TalkCmd.Play)
                && calledMethod.DeclaringType == typeof(TalkCmd))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadOpCode(byte[] il, ref int offset, out OpCode opCode)
    {
        opCode = default;
        if (offset >= il.Length)
        {
            return false;
        }

        byte first = il[offset++];
        ushort value = first;
        if (first == 0xFE)
        {
            if (offset >= il.Length)
            {
                return false;
            }

            value = (ushort)(0xFE00 | il[offset++]);
        }

        return OpCodesByValue.TryGetValue(value, out opCode);
    }

    private static int GetOperandSize(OperandType operandType, byte[] il, int offset)
    {
        if (operandType == OperandType.InlineSwitch)
        {
            if (offset + sizeof(int) > il.Length)
            {
                return -1;
            }

            int branchCount = BitConverter.ToInt32(il, offset);
            return branchCount < 0 ? -1 : sizeof(int) + branchCount * sizeof(int);
        }

        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI
                or OperandType.InlineBrTarget
                or OperandType.InlineField
                or OperandType.InlineMethod
                or OperandType.InlineSig
                or OperandType.InlineString
                or OperandType.InlineSwitch
                or OperandType.InlineTok
                or OperandType.InlineType
                or OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            _ => 0
        };
    }

    private static Dictionary<ushort, OpCode> BuildOpCodeLookup()
    {
        Dictionary<ushort, OpCode> opCodes = [];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                opCodes[(ushort)opCode.Value] = opCode;
            }
        }

        return opCodes;
    }
}

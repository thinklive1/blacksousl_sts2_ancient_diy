using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class BarristersPageRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/scripts.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override Task BeforeCombatStart()
    {
        if (Owner.Creature.CombatState == null)
        {
            return Task.CompletedTask;
        }

        bool advancedAnyMove = false;
        foreach (Creature enemy in Owner.Creature.CombatState.Enemies.Where(enemy => enemy.IsAlive))
        {
            advancedAnyMove |= TryAdvancePastFirstMove(enemy);
        }

        if (advancedAnyMove)
        {
            Flash();
        }

        return Task.CompletedTask;
    }

    private static bool TryAdvancePastFirstMove(Creature enemy)
    {
        MonsterModel? monster = enemy.Monster;
        if (monster == null)
        {
            return false;
        }

        MonsterMoveStateMachine? stateMachine = monster.MoveStateMachine;
        if (stateMachine == null)
        {
            return false;
        }

        MonsterState state = monster.NextMove;
        MonsterState? stateToLog = null;

        for (int transitionsRemaining = stateMachine.States.Count + 1; transitionsRemaining > 0; transitionsRemaining--)
        {
            string nextStateId = state.GetNextState(enemy, monster.RunRng.MonsterAi);
            if (string.IsNullOrEmpty(nextStateId)
                || !stateMachine.States.TryGetValue(nextStateId, out MonsterState? nextState)
                || nextState == null)
            {
                return false;
            }

            if (stateToLog == null && nextState.ShouldAppearInLogs)
            {
                stateToLog = nextState;
            }

            if (nextState is MoveState nextMove)
            {
                monster.SetMoveImmediate(nextMove, forceTransition: true);
                if (stateToLog != null)
                {
                    stateMachine.StateLog.Add(stateToLog);
                }

                return true;
            }

            state = nextState;
        }

        return false;
    }
}

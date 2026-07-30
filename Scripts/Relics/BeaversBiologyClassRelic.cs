using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Beaver's Biology Class relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class BeaversBiologyClassRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/scripts.png";

    private decimal _recordedTurnDamage;
    private decimal _recordedTurnBlock;
    private decimal _copiedTurnDamage;
    private decimal _copiedTurnBlock;
    private int _completedPlayerTurns;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return !SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BeaversBiologyClassRelic>(runState);
    }

    public override Task AfterObtained()
    {
        if (Owner != null)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<BeaversBiologyClassRelic>(Owner);
        }

        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        _recordedTurnDamage = 0m;
        _recordedTurnBlock = 0m;
        _copiedTurnDamage = 0m;
        _copiedTurnBlock = 0m;
        _completedPlayerTurns = 0;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            // At the start of turn two and later, show the enemy moves copied from the prior player turn.
            if (_completedPlayerTurns >= 1 && ReplaceEnemyNextMoves(_copiedTurnDamage, _copiedTurnBlock))
            {
                Flash();
            }

            // Start recording the player turn that enemies will copy next.
            _recordedTurnDamage = 0m;
            _recordedTurnBlock = 0m;
        }

        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer?.Player == Owner && target.Side == CombatSide.Enemy)
        {
            _recordedTurnDamage += Math.Max(0m, result.TotalDamage);
        }

        return Task.CompletedTask;
    }

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature == Owner?.Creature)
        {
            _recordedTurnBlock += Math.Max(0m, amount);
        }

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner?.Creature == null || side != Owner.Creature.Side)
        {
            return Task.CompletedTask;
        }

        // Freeze the completed player turn so enemy actions never read a partially updated value.
        _copiedTurnDamage = _recordedTurnDamage;
        _copiedTurnBlock = _recordedTurnBlock;
        _completedPlayerTurns++;

        return Task.CompletedTask;
    }

    private bool ReplaceEnemyNextMoves(decimal copiedDamage, decimal copiedBlock)
    {
        bool changedAnyMove = false;
        foreach (Creature enemy in Owner?.Creature.CombatState?.Enemies.Where(enemy => enemy.IsAlive) ?? [])
        {
            changedAnyMove |= TryReplaceNextMove(enemy, copiedDamage, copiedBlock);
        }

        return changedAnyMove;
    }

    private static bool TryReplaceNextMove(Creature enemy, decimal copiedDamage, decimal copiedBlock)
    {
        MonsterModel? monster = enemy.Monster;
        MoveState? originalMove = monster?.NextMove;
        if (monster == null || originalMove == null)
        {
            return false;
        }

        List<AbstractIntent> intents = [];
        if (copiedDamage > 0m)
        {
            intents.Add(new SingleAttackIntent(() => copiedDamage));
        }

        if (copiedBlock > 0m)
        {
            intents.Add(new DefendIntent());
        }

        MoveState copiedMove = new(
            "BS_ANCIENT_BEAVERS_BIOLOGY_CLASS_COPY_MOVE",
            targets => PerformCopiedMove(monster, targets, copiedDamage, copiedBlock),
            [.. intents])
        {
            FollowUpState = originalMove
        };

        monster.SetMoveImmediate(copiedMove, forceTransition: true);
        ShowReplacementIntentImmediately(enemy);
        return true;
    }

    private static async Task PerformCopiedMove(
        MonsterModel monster,
        IReadOnlyList<Creature> targets,
        decimal copiedDamage,
        decimal copiedBlock)
    {
        Creature creature = monster.Creature;
        if (copiedBlock > 0m)
        {
            await CreatureCmd.GainBlock(creature, copiedBlock, ValueProp.Move, null, fast: true);
        }

        if (copiedDamage > 0m)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                targets.Where(target => target.IsAlive),
                copiedDamage,
                ValueProp.Move,
                creature,
                null);
        }
    }

    private static void ShowReplacementIntentImmediately(Creature enemy)
    {
        NCreature? creatureNode = enemy.GetCreatureNode();
        if (creatureNode != null)
        {
            creatureNode.IntentContainer.Modulate = Colors.White;
        }
    }
}

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts;

/// <summary>Represents the unseen Boojum and its history-backed memory pool.</summary>
[RegisterMonster]
public sealed class Boojum : MonsterModel
{
    private const int Health = 999;
    private const int BaseAttackDamage = 10;
    private const int InitialStrength = 10;
    private List<BoojumMemoryRecord> _historyRecords = [];
    private bool _memoryCardsShuffled;

    protected override string VisualsPath => "res://bs_ancient/assets/scenes/boojum.tscn";

    public override int MinInitialHp => Health;

    public override int MaxInitialHp => Health;

    public override bool HasDeathSfx => false;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            InitialStrength,
            Creature,
            null,
            false);

        if (Creature.CombatState?.Players is [var player]
            && BoojumHistoryMemory.TryLoadForSinglePlayer(Creature.CombatState.Players, out List<BoojumMemoryRecord> records))
        {
            _historyRecords = records;
            int totalMemory = records.Sum(record => record.RemainingCards);
            if (totalMemory > 0)
            {
                BoojumHistoryPurge.Reset(Creature.CombatState);
                await PowerCmd.Apply<BoojumMemoryPower>(
                    new ThrowingPlayerChoiceContext(),
                    player.Creature,
                    totalMemory,
                    player.Creature,
                    null,
                    false);
                player.Creature.GetPower<BoojumMemoryPower>()?.Configure(records);
                BoojumVictoryRewardModifier.Arm(player, records);
            }
        }
    }

    public override async Task AfterAutoPrePlayPhaseEntered(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (_memoryCardsShuffled
            || Creature.CombatState?.Players is not [var combatPlayer]
            || player != combatPlayer
            || _historyRecords.Count == 0)
        {
            return;
        }

        _memoryCardsShuffled = true;
        await ShuffleMemoryCards(_historyRecords);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Creature.CombatState?.RunState.IsGameOver == true)
        {
            BoojumHistoryPurge.ArmCurrentRunHistoryErasure();
        }

        return Task.CompletedTask;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attack = new(
            "BOOJUM_ERASE_MOVE",
            Attack,
            new SingleAttackIntent(BaseAttackDamage));
        MoveState strengthen = new(
            "BOOJUM_AMPLIFY_MOVE",
            DoubleStrength,
            new BuffIntent());

        attack.FollowUpState = strengthen;
        strengthen.FollowUpState = attack;
        return new MonsterMoveStateMachine([attack, strengthen], attack);
    }

    private Task Attack(IReadOnlyList<Creature> targets)
    {
        return CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            targets,
            BaseAttackDamage,
            ValueProp.Move,
            Creature,
            null);
    }

    private Task DoubleStrength(IReadOnlyList<Creature> _)
    {
        decimal currentStrength = Creature.GetPower<StrengthPower>()?.Amount ?? 0m;
        return currentStrength <= 0m
            ? Task.CompletedTask
            : PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(),
                Creature,
                currentStrength,
                Creature,
                null,
                false);
    }

    private async Task ShuffleMemoryCards(IEnumerable<BoojumMemoryRecord> records)
    {
        ICombatState? combatState = Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        int addedCount = 0;
        foreach (BoojumMemoryRecord record in records)
        {
            BoojumMemoryCard memory = combatState.CreateCard<BoojumMemoryCard>(Creature.CombatState!.Players[0]);
            memory.Configure(record);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
                memory,
                PileType.Draw,
                memory.Owner!,
                CardPilePosition.Random);
            if (result.success)
            {
                addedCount++;
            }
        }

        if (addedCount <= 0)
        {
            return;
        }

        CardPile drawPile = PileType.Draw.GetPile(combatState.Players[0]);
        drawPile.InvokeContentsChanged();
        for (int i = 0; i < addedCount; i++)
        {
            drawPile.InvokeCardAddFinished();
        }
    }

}

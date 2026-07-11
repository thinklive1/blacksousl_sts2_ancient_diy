using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

/// <summary>Implements the Jack Ketch News relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class JackKetchNewsRelic : ModRelicTemplate
{
    private const int TriggerTurn = 10;
    private const int SlamDamage = 54;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/news.png";
    private int _turnCounter;
    private bool _hasTriggered;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => !_hasTriggered;

    public override int DisplayAmount => Math.Max(TriggerTurn - _turnCounter, 0);

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("TriggerTurn", TriggerTurn),
        new DynamicVar("SlamDamage", SlamDamage)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _turnCounter = 0;
        _hasTriggered = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || _hasTriggered)
        {
            return Task.CompletedTask;
        }

        _turnCounter++;
        InvokeDisplayAmountChanged();

        if (_turnCounter < DynamicVars["TriggerTurn"].BaseValue)
        {
            return Task.CompletedTask;
        }

        _hasTriggered = true;
        InvokeDisplayAmountChanged();

        bool changedAnyMove = false;
        foreach (Creature enemy in Owner.Creature.CombatState?.Enemies.Where(enemy => enemy.IsAlive) ?? [])
        {
            changedAnyMove |= TryReplaceNextMove(enemy);
        }

        if (changedAnyMove)
        {
            Flash();
        }

        return Task.CompletedTask;
    }

    private bool TryReplaceNextMove(Creature enemy)
    {
        MonsterModel? monster = enemy.Monster;
        MoveState? originalMove = monster?.NextMove;
        if (monster == null || originalMove == null)
        {
            return false;
        }

        MoveState slamMove = new(
            "BS_ANCIENT_JACK_KETCH_NEWS_SLAM_MOVE",
            targets => PerformSlam(monster, targets),
            new SingleAttackIntent(SlamDamage))
        {
            FollowUpState = originalMove
        };

        monster.SetMoveImmediate(slamMove, forceTransition: true);
        ShowReplacementIntentImmediately(enemy);
        return true;
    }

    private static void ShowReplacementIntentImmediately(Creature enemy)
    {
        NCreature? creatureNode = enemy.GetCreatureNode();
        if (creatureNode == null)
        {
            return;
        }

        // SetMoveImmediate refreshes intents with a fade-in; make this forced replacement visible at once.
        creatureNode.IntentContainer.Modulate = Colors.White;
    }

    private async Task PerformSlam(MonsterModel monster, IReadOnlyList<Creature> targets)
    {
        Creature? target = targets.FirstOrDefault(target => target.IsAlive);
        if (target == null)
        {
            return;
        }

        await CreatureCmd.TriggerAnim(monster.Creature, "Attack", 0.6f);
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            target,
            DynamicVars["SlamDamage"].BaseValue,
            ValueProp.Move,
            monster.Creature,
            null);
    }
}

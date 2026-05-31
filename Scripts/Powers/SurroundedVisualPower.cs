using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public sealed class SurroundedVisualPower : ModPowerTemplate
{
    private SurroundedPower.Direction _facing = SurroundedPower.Direction.Right;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    public void SetFacing(SurroundedPower.Direction facing)
    {
        AssertMutable();
        _facing = facing;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Target != null && cardPlay.Card.Owner == Owner.Player)
        {
            await UpdateDirection(cardPlay.Target);
        }
    }

    public override async Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        if (CombatManager.Instance.IsInProgress && target != null && potion.Owner == Owner.Player)
        {
            await UpdateDirection(target);
        }
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && creature.Side != Owner.Side)
        {
            IReadOnlyList<Creature> hittableEnemies = Owner.CombatState?.HittableEnemies ?? [];
            if (hittableEnemies.Count != 0
                && (hittableEnemies.All(enemy => enemy.HasPower<BackAttackLeftPower>())
                    || hittableEnemies.All(enemy => enemy.HasPower<BackAttackRightPower>())))
            {
                await UpdateDirection(hittableEnemies[0]);
            }
        }
    }

    private async Task UpdateDirection(Creature target)
    {
        switch (_facing)
        {
            case SurroundedPower.Direction.Right:
                if (target.HasPower<BackAttackLeftPower>())
                {
                    await FaceDirection(SurroundedPower.Direction.Left);
                }

                break;
            case SurroundedPower.Direction.Left:
                if (target.HasPower<BackAttackRightPower>())
                {
                    await FaceDirection(SurroundedPower.Direction.Right);
                }

                break;
        }
    }

    private async Task FaceDirection(SurroundedPower.Direction direction)
    {
        _facing = direction;

        List<Creature> creatures = [Owner, .. Owner.Pets];
        IEnumerable<Node2D?> bodies = creatures.Select(creature => NCombatRoom.Instance?.GetCreatureNode(creature)?.Body);
        foreach (Node2D? body in bodies)
        {
            await FlipScale(body);
        }
    }

    private Task FlipScale(Node2D? body)
    {
        if (body == null)
        {
            return Task.CompletedTask;
        }

        float x = body.Scale.X;
        if ((_facing == SurroundedPower.Direction.Right && x < 0f)
            || (_facing == SurroundedPower.Direction.Left && x > 0f))
        {
            body.Scale *= new Vector2(-1f, 1f);
        }

        return Task.CompletedTask;
    }
}

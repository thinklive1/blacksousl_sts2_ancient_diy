using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace BlackSouls.Scripts;

[RegisterMonster]
public class CheshireCatPet : MonsterModel
{
    protected override string VisualsPath => "res://bs_ancient/assets/scenes/cheshire_cat_pet.tscn";

    public override int MinInitialHp => 9999;

    public override int MaxInitialHp => 9999;

    public override bool IsHealthBarVisible => false;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("animation", isLooping: true);
        AnimState bite = new("animation")
        {
            NextState = idle
        };
        AnimState smile = new("animation")
        {
            NextState = idle
        };

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("BiteTrigger", bite);
        animator.AddAnyState("SmileTrigger", smile);
        return animator;
    }
}

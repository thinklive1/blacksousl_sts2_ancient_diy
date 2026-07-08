using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides shared logic for Guignol execution powers.</summary>
public abstract class GuignolsExecutionPowerBase : ModPowerTemplate
{
    internal const int ExecutionThresholdPercent = 30;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected abstract string PowerIconPath { get; }

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath
    );

    protected bool IsBelowExecutionThreshold(Creature target)
    {
        return target.IsAlive
            && target.MaxHp > 0
            && target.CurrentHp * 100m < target.MaxHp * Amount;
    }

    protected static decimal DamageNeededToKill(Creature target, decimal amount)
    {
        decimal requiredDamage = target.CurrentHp + target.Block;
        return Math.Max(0m, requiredDamage - amount);
    }
}

/// <summary>Implements the Guignols Applause Execution power.</summary>
[RegisterPower]
public sealed class GuignolsApplauseExecutionPower : GuignolsExecutionPowerBase
{
    protected override string PowerIconPath => "res://bs_ancient/assets/images/powers/GuignolsApplauseExecutionPower.png";

    public override PowerType Type => PowerType.Buff;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target == null
            || amount <= 0m
            || !props.IsPoweredAttack()
            || !IsOwnerAttack(dealer)
            || !IsEnemy(target)
            || !IsBelowExecutionThreshold(target))
        {
            return 0m;
        }

        return DamageNeededToKill(target, amount);
    }

    private bool IsOwnerAttack(Creature? dealer)
    {
        return dealer == Owner || dealer?.PetOwner == Owner.Player;
    }

    private bool IsEnemy(Creature target)
    {
        return target.Side != Owner.Side;
    }
}

/// <summary>Implements the Guignols Boo Execution power.</summary>
[RegisterPower]
public sealed class GuignolsBooExecutionPower : GuignolsExecutionPowerBase
{
    protected override string PowerIconPath => "res://bs_ancient/assets/images/powers/GuignolsBooExecutionPower.png";

    public override PowerType Type => PowerType.Debuff;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner
            || amount <= 0m
            || !props.IsPoweredAttack()
            || !IsEnemyAttack(dealer)
            || !IsBelowExecutionThreshold(target))
        {
            return 0m;
        }

        return DamageNeededToKill(target, amount);
    }

    private bool IsEnemyAttack(Creature? dealer)
    {
        return dealer != null && dealer.Side != Owner.Side;
    }
}

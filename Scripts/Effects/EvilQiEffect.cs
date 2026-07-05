using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlackSouls.Scripts;

internal static class EvilQiEffect
{
    public static Task Apply(PlayerChoiceContext choiceContext, CardModel card)
    {
        return PowerCmd.Apply<EvilQiPendingPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), card.Owner.Creature, 1m, card.Owner.Creature, card, false);
    }

    public static async Task Resolve(Creature owner, int amount)
    {
        if (owner.IsDead || amount <= 0)
        {
            return;
        }

        PlayerChoiceContext effectContext = new ThrowingPlayerChoiceContext();

        await CreatureCmd.Damage(
            effectContext,
            owner,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            owner,
            null);

        ICombatState? combatState = owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = combatState.Enemies
            .Where(enemy => enemy.IsAlive)
            .ToList();

        if (enemies.Count == 0)
        {
            return;
        }

        IReadOnlyList<Creature> powerTargets = enemies
            .Where(HasDrainableStrengthOrPlating)
            .ToList();

        Creature? target = combatState.RunState.Rng.CombatTargets.NextItem(
            powerTargets.Count > 0 ? powerTargets : enemies);
        if (target == null)
        {
            return;
        }

        await DrainPower<StrengthPower>(effectContext, target, owner, amount);
        await DrainPower<PlatingPower>(effectContext, target, owner, amount);

        if (target.IsAlive && target.CurrentHp > 0)
        {
            int hpDrain = Math.Min(amount, target.CurrentHp);
            await CreatureCmd.Damage(
                effectContext,
                target,
                hpDrain,
                ValueProp.Unblockable | ValueProp.Unpowered,
                owner,
                null);
            await CreatureCmd.Heal(owner, hpDrain);
        }
    }

    private static bool HasDrainableStrengthOrPlating(Creature target)
    {
        StrengthPower? strength = target.GetPower<StrengthPower>();
        if (strength is { Amount: > 0 })
        {
            return true;
        }

        PlatingPower? plating = target.GetPower<PlatingPower>();
        return plating is { Amount: > 0 };
    }

    private static async Task DrainPower<T>(PlayerChoiceContext choiceContext, Creature target, Creature owner, int amount)
        where T : MegaCrit.Sts2.Core.Models.PowerModel
    {
        T? power = target.GetPower<T>();
        if (power == null || power.Amount <= 0)
        {
            return;
        }

        int drain = Math.Min(amount, power.Amount);
        await PowerCmd.ModifyAmount(choiceContext, power, -drain, owner, null, false);
        await PowerCmd.Apply<T>(choiceContext, owner, drain, owner, null, false);
    }
}

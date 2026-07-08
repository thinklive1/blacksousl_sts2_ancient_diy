using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

/// <summary>Provides helpers for applying and removing powers.</summary>
public static class BsPowerCmd
{
    public static async Task<T> SetAmount<T>(
        Creature target,
        decimal amount,
        Creature source,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel
    {
        T? existing = target.GetPower<T>();
        if (existing != null)
        {
            await PowerCmd.ModifyAmount(
                new ThrowingPlayerChoiceContext(),
                existing,
                amount - existing.Amount,
                source,
                cardSource,
                silent);
            return existing;
        }

        T? applied = await PowerCmd.Apply<T>(
            new ThrowingPlayerChoiceContext(),
            target,
            amount,
            source,
            cardSource,
            silent);
        return applied ?? throw new InvalidOperationException($"Failed to apply power {typeof(T).Name}.");
    }
}

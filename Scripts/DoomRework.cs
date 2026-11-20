using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Sts2Diy.Scripts;

/// <summary>Scales vanilla Doom without feeding the self-application back into the rework.</summary>
internal static class DoomRework
{
    internal static decimal Scale(decimal original) => Math.Floor(Math.Max(0m, original) * 1.5m);
    internal static decimal Backlash(decimal original) => Math.Floor(Math.Max(0m, original) / 3m);

    internal static bool DefersBacklash(CardModel? card) => card is Deathbringer or EndOfDays;

    public static async Task<DoomPower?> ApplySingle(PlayerChoiceContext context, Creature target,
        decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
    {
        DoomPower? result = await PowerCmd.Apply<DoomPower>(context, target, Scale(amount), applier, cardSource, silent);
        if (!DefersBacklash(cardSource))
        {
            await ApplyBacklash(context, applier, amount, cardSource, silent);
        }
        return result;
    }

    public static async Task<IReadOnlyList<DoomPower>> ApplyMultiple(PlayerChoiceContext context,
        IEnumerable<Creature> targets, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
    {
        IReadOnlyList<DoomPower> result = await PowerCmd.Apply<DoomPower>(context, targets, Scale(amount), applier, cardSource, silent);
        if (!DefersBacklash(cardSource))
        {
            await ApplyBacklash(context, applier, amount, cardSource, silent);
        }
        return result;
    }

    internal static Task ApplyBacklash(PlayerChoiceContext context, Creature? applier, decimal original,
        CardModel? cardSource, bool silent = false)
    {
        decimal amount = Backlash(original);
        return amount > 0m && applier is { IsAlive: true, Player: not null }
            ? PowerCmd.Apply<DoomPower>(context, applier, amount, applier, cardSource, silent)
            : Task.CompletedTask;
    }
}

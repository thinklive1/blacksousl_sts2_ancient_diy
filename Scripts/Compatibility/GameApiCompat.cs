using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlackSouls.Scripts.Compatibility;

/// <summary>Bridges game command signatures shared by the stable and beta builds.</summary>
internal static class GameApiCompat
{
    internal static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay = null)
    {
#if BS_BETA_1110
        return CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource, cardPlay);
#else
        return CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource);
#endif
    }

    internal static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay = null)
    {
#if BS_BETA_1110
        return CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource, cardPlay);
#else
        return CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource);
#endif
    }

    internal static Task LoseBlock(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? remover)
    {
#if BS_BETA_1110
        return CreatureCmd.LoseBlock(choiceContext, target, amount, remover);
#else
        return CreatureCmd.LoseBlock(target, amount);
#endif
    }

    internal static Task CheckForEmptyHand(PlayerChoiceContext choiceContext, Player player)
    {
#if BS_BETA_1110
        return CombatManager.Instance.CheckForEmptyHand(null, choiceContext, player);
#else
        return CombatManager.Instance.CheckForEmptyHand(choiceContext, player);
#endif
    }

    internal static AttackCommand FromCardCompat(
        this AttackCommand command,
        CardModel card,
        CardPlay? cardPlay)
    {
#if BS_BETA_1110
        return command.FromCard(card, cardPlay);
#else
        return command.FromCard(card);
#endif
    }

    internal static bool HasCustomCardPool(CardCreationOptions options)
    {
#if BS_BETA_1110
        return false;
#else
        return options.CustomCardPool != null;
#endif
    }

    internal static CardCreationOptions WithCardPoolsAndFilter(
        CardCreationOptions options,
        IEnumerable<CardPoolModel> pools,
        Func<CardModel, bool> filter)
    {
#if BS_BETA_1110
        return options.WithCardPools(pools).WithFilter(filter);
#else
        return options.WithCardPools(pools, filter);
#endif
    }
}

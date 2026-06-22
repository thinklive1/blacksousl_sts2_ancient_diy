using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public sealed class ArmamentEnchantment : ModEnchantmentTemplate
{
    private const decimal SelfDamageRate = 0.2m;
    private const string ArmamentIconPath = "res://bs_ancient/assets/images/enchantment/ArmamentEnchantment.png";

    private bool _isAutoPlaying;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(IconPath: ArmamentIconPath);

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
            && card.Type == CardType.Attack
            && !card.EnergyCost.CostsX
            && !card.CanonicalKeywords.Contains(CardKeyword.Unplayable);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != Card || _isAutoPlaying || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        Creature? target = GetAutoPlayTarget(card);
        if (card.TargetType.IsSingleTarget() && target == null)
        {
            return;
        }

        _isAutoPlaying = true;
        try
        {
            await CardCmd.AutoPlay(choiceContext, card, target);
        }
        finally
        {
            _isAutoPlaying = false;
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card || Card.Owner?.Creature is not { IsDead: false } owner)
        {
            return;
        }

        decimal selfDamage = Math.Ceiling(GetCardDamage(Card) * SelfDamageRate);
        if (selfDamage <= 0m)
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            owner,
            selfDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            owner,
            Card);
    }

    private Creature? GetAutoPlayTarget(CardModel card)
    {
        ICombatState? combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.RandomEnemy => combatState == null
                ? null
                : card.Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies),
            TargetType.AnyAlly => combatState == null
                ? null
                : card.Owner.RunState.Rng.CombatTargets.NextItem(combatState.Allies),
            _ => null
        };
    }

    private static decimal GetCardDamage(CardModel card)
    {
        return card.DynamicVars.Values
            .Where(var => var.Name.Contains("Damage", StringComparison.Ordinal))
            .Select(var => (decimal)var.IntValue)
            .DefaultIfEmpty(0m)
            .First();
    }
}

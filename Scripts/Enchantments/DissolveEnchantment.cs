using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public class DissolveEnchantment : ModEnchantmentTemplate
{
    private int _damageReduction;
    private int _blockReduction;

    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    [SavedProperty]
    public int BlackSouls_DamageReduction
    {
        get => _damageReduction;
        set
        {
            AssertMutable();
            _damageReduction = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_BlockReduction
    {
        get => _blockReduction;
        set
        {
            AssertMutable();
            _blockReduction = value;
        }
    }

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/FriendlySlimeRelic.png"
    );

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card) && GetReducibleVars(card).Any(var => var.BaseValue > 0);
    }

    protected override void OnEnchant()
    {
        if (!Card.Keywords.Contains(CardKeyword.Exhaust))
        {
            CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
        }
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        return -Math.Min(originalDamage, BlackSouls_DamageReduction);
    }

    public override decimal ModifyBlockAdditive(
        Creature target,
        decimal originalBlock,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return -Math.Min(originalBlock, BlackSouls_BlockReduction);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card)
        {
            return;
        }

        CardModel deckCard = Card.DeckVersion ?? Card;
        if (deckCard.HasBeenRemovedFromState)
        {
            return;
        }

        IncrementSavedReductions(deckCard);
        IncrementSavedReductions(Card);

        if (ShouldRemoveFromDeck(deckCard) && deckCard.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(deckCard);
        }
    }

    private static void IncrementSavedReductions(CardModel card)
    {
        if (card.Enchantment is not DissolveEnchantment dissolve)
        {
            return;
        }

        if (HasRemainingDamage(card, dissolve.BlackSouls_DamageReduction))
        {
            dissolve.BlackSouls_DamageReduction++;
        }

        if (HasRemainingBlock(card, dissolve.BlackSouls_BlockReduction))
        {
            dissolve.BlackSouls_BlockReduction++;
        }

        card.DynamicVars.RecalculateForUpgradeOrEnchant();
    }

    private static bool ShouldRemoveFromDeck(CardModel card)
    {
        if (card.Enchantment is not DissolveEnchantment dissolve)
        {
            return false;
        }

        List<DynamicVar> reducibleVars = GetReducibleVars(card).ToList();
        return reducibleVars.Count > 0 && reducibleVars.All(var => GetRemainingValue(var, dissolve) <= 0);
    }

    private static IEnumerable<DynamicVar> GetReducibleVars(CardModel card)
    {
        return card.DynamicVars.Values.Where(IsReducibleVar);
    }

    private static bool IsReducibleVar(DynamicVar dynamicVar)
    {
        return dynamicVar is BlockVar
            || dynamicVar.Name.Contains("Damage", StringComparison.Ordinal);
    }

    private static bool IsDamageVar(DynamicVar dynamicVar)
    {
        return dynamicVar.Name.Contains("Damage", StringComparison.Ordinal);
    }

    private static bool HasRemainingDamage(CardModel card, int currentReduction)
    {
        return GetReducibleVars(card)
            .Where(IsDamageVar)
            .Any(var => var.BaseValue - currentReduction > 0);
    }

    private static bool HasRemainingBlock(CardModel card, int currentReduction)
    {
        return GetReducibleVars(card)
            .OfType<BlockVar>()
            .Any(var => var.BaseValue - currentReduction > 0);
    }

    private static decimal GetRemainingValue(DynamicVar dynamicVar, DissolveEnchantment dissolve)
    {
        int reduction = dynamicVar is BlockVar
            ? dissolve.BlackSouls_BlockReduction
            : dissolve.BlackSouls_DamageReduction;
        return dynamicVar.BaseValue - reduction;
    }
}

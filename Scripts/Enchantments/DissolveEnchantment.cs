using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Dissolve enchantment.</summary>
[RegisterEnchantment]
public class DissolveEnchantment : ModEnchantmentTemplate
{
    private int _damageReduction;
    private int _blockReduction;
    private int _appliedDamageReduction;
    private int _appliedBlockReduction;

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

    public override void RecalculateValues()
    {
        base.RecalculateValues();
        ApplySavedReductions(Card);
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

        ReduceCardValues(deckCard);
        if (deckCard != Card)
        {
            ReduceCardValues(Card);
        }

        Card.Owner?.PlayerCombatState?.RecalculateCardValues();

        if (ShouldRemoveFromDeck(deckCard) && deckCard.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(deckCard);
        }
    }

    private static void ReduceCardValues(CardModel card)
    {
        if (card.Enchantment is not DissolveEnchantment dissolve)
        {
            return;
        }

        bool hasRemainingDamage = GetDamageVars(card).Any(var => var.IntValue > 0);
        bool hasRemainingBlock = GetBlockVars(card).Any(var => var.IntValue > 0);
        dissolve.BlackSouls_DamageReduction += hasRemainingDamage ? 1 : 0;
        dissolve.BlackSouls_BlockReduction += hasRemainingBlock ? 1 : 0;
        dissolve.ApplySavedReductions(card);
        card.DynamicVars.RecalculateForUpgradeOrEnchant();
    }

    private void ApplySavedReductions(CardModel card)
    {
        int damageDelta = BlackSouls_DamageReduction - _appliedDamageReduction;
        if (damageDelta > 0)
        {
            ApplyReductionDelta(GetDamageVars(card), damageDelta);
            _appliedDamageReduction += damageDelta;
        }

        int blockDelta = BlackSouls_BlockReduction - _appliedBlockReduction;
        if (blockDelta > 0)
        {
            ApplyReductionDelta(GetBlockVars(card), blockDelta);
            _appliedBlockReduction += blockDelta;
        }
    }

    private static void ApplyReductionDelta(IEnumerable<DynamicVar> dynamicVars, int delta)
    {
        foreach (DynamicVar dynamicVar in dynamicVars)
        {
            decimal reduction = Math.Min(dynamicVar.BaseValue, delta);
            if (reduction > 0)
            {
                dynamicVar.UpgradeValueBy(-reduction);
            }
        }
    }

    private static bool ShouldRemoveFromDeck(CardModel card)
    {
        if (card.Enchantment is not DissolveEnchantment)
        {
            return false;
        }

        List<DynamicVar> reducibleVars = GetReducibleVars(card).ToList();
        return reducibleVars.Count > 0 && reducibleVars.All(var => var.IntValue <= 0);
    }

    private static IEnumerable<DynamicVar> GetReducibleVars(CardModel card)
    {
        return card.DynamicVars.Values.Where(IsReducibleVar);
    }

    private static IEnumerable<DynamicVar> GetDamageVars(CardModel card)
    {
        return card.DynamicVars.Values.Where(var => var.Name.Contains("Damage", StringComparison.Ordinal));
    }

    private static IEnumerable<BlockVar> GetBlockVars(CardModel card)
    {
        return card.DynamicVars.Values.OfType<BlockVar>();
    }

    private static bool IsReducibleVar(DynamicVar dynamicVar)
    {
        return dynamicVar is BlockVar
            || dynamicVar.Name.Contains("Damage", StringComparison.Ordinal);
    }

}

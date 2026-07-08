using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Ashes enchantment.</summary>
[RegisterEnchantment]
public class AshesEnchantment : ModEnchantmentTemplate
{
    private const string AshesIconPath = "res://bs_ancient/assets/images/enchantment/AshesEnchantment.png";

    private bool _pendingRemoval;
    private bool _replayConsumedThisCombat;

    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    [SavedProperty]
    public bool BlackSouls_PendingRemoval
    {
        get => _pendingRemoval;
        set
        {
            AssertMutable();
            _pendingRemoval = value;
        }
    }

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: AshesIconPath
    );

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == Card ? (PileType.Exhaust, position) : (pileType, position);
    }

    public override Task BeforeCombatStart()
    {
        _replayConsumedThisCombat = false;
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card == Card && !_replayConsumedThisCombat)
        {
            _replayConsumedThisCombat = true;
            return playCount + 1;
        }

        return playCount;
    }

    public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card == Card)
        {
            BlackSouls_PendingRemoval = true;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!BlackSouls_PendingRemoval)
        {
            return;
        }

        CardModel? deckCard = Card?.DeckVersion ?? Card;
        if (deckCard != null
            && !deckCard.HasBeenRemovedFromState
            && deckCard.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(deckCard);
        }
    }
}

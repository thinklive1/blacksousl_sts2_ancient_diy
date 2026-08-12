using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Mermaid Tear card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class MermaidTearCard : ModCardTemplate
{
    private const int HpLoss = 3;
    private const string CardPortraitPath = "res://bs_ancient/assets/images/cards/MermaidTearCard.jpg";

    private readonly List<CardModel> _swallowedDeckCards = [];

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal,
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(HpLoss)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: CardPortraitPath
    );

    public MermaidTearCard() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (!IsThisCard(card) || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        await SwallowRandomDrawPileCard(choiceContext);
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RemoveSwallowedDeckCards();
    }

    private async Task SwallowRandomDrawPileCard(PlayerChoiceContext choiceContext)
    {
        CardModel? swallowed = Owner.RunState.Rng.CombatCardSelection.NextItem(GetSwallowCandidates());

        CardModel? deckVersion = swallowed?.DeckVersion;
        if (swallowed == null)
        {
            return;
        }

        PileType oldPileType = swallowed.Pile?.Type ?? PileType.None;
        MermaidTearCard deckCard = DeckVersion as MermaidTearCard ?? this;
        if (deckVersion?.Pile?.Type == PileType.Deck)
        {
            deckCard.QueueSwallowedDeckCard(deckVersion);
        }

        await CardCmd.Exhaust(choiceContext, swallowed, causedByEthereal: false, skipVisuals: true);
        RefreshPileCounter(oldPileType, cardAdded: false);
        RefreshPileCounter(PileType.Exhaust, cardAdded: true);

        MermaidTearSwallowedCardsPower? power = Owner.Creature.GetPower<MermaidTearSwallowedCardsPower>();
        if (power == null)
        {
            await PowerCmd.Apply<MermaidTearSwallowedCardsPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this,
                false);
            power = Owner.Creature.GetPower<MermaidTearSwallowedCardsPower>();
        }

        power?.SetSwallowedCount(deckCard._swallowedDeckCards.Count);
    }

    private List<CardModel> GetSwallowCandidates()
    {
        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards.ToList();
        if (candidates.Count > 0)
        {
            return candidates;
        }

        candidates = PileType.Discard.GetPile(Owner).Cards.ToList();
        if (candidates.Count > 0)
        {
            return candidates;
        }

        return PileType.Hand.GetPile(Owner).Cards
            .Where(card => !IsThisCard(card))
            .ToList();
    }

    private bool IsThisCard(CardModel card)
    {
        return card == this
            || card.DeckVersion == this
            || DeckVersion == card
            || (DeckVersion != null && card.DeckVersion == DeckVersion);
    }

    public async Task RemoveSwallowedDeckCards()
    {
        if (_swallowedDeckCards.Count == 0)
        {
            return;
        }

        List<CardModel> cards = _swallowedDeckCards
            .Where(card => card.Pile?.Type == PileType.Deck)
            .Distinct()
            .ToList();
        _swallowedDeckCards.Clear();

        if (cards.Count > 0)
        {
            await CardPileCmd.RemoveFromDeck(cards);
        }
    }

    private void QueueSwallowedDeckCard(CardModel deckCard)
    {
        if (deckCard.Pile?.Type != PileType.Deck || _swallowedDeckCards.Contains(deckCard))
        {
            return;
        }

        _swallowedDeckCards.Add(deckCard);
    }

    private void RefreshPileCounter(PileType pileType, bool cardAdded)
    {
        if (!pileType.IsCombatPile())
        {
            return;
        }

        CardPile pile = pileType.GetPile(Owner);
        pile.InvokeContentsChanged();
        if (cardAdded)
        {
            pile.InvokeCardAddFinished();
        }
        else
        {
            pile.InvokeCardRemoveFinished();
        }
    }
}

/// <summary>Implements the Mermaid Tear Swallowed Cards power.</summary>
[RegisterPower]
public sealed class MermaidTearSwallowedCardsPower : ModPowerTemplate
{
    private const string PowerIconPath = "res://bs_ancient/assets/images/powers/MermaidTearSwallowedCardsPower.png";

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath
    );

    public void SetSwallowedCount(int count)
    {
        SetAmount(count, silent: true);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.GetPower<MermaidTearSwallowedCardsPower>() == this)
        {
            await PowerCmd.Remove(this);
        }
    }
}

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Represents the wildcard ace in Lorina's card set.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class LastAceCard : ModCardTemplate
{
    private const string CardPortraitPath =
        "res://bs_ancient/assets/images/cards/LastAceCard.jpg";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(MyKeywords.TexasHoldemRules)
    ];

    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(PortraitPath: CardPortraitPath);

    public LastAceCard() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayingCardPokerHand<CardModel>? pokerHand =
            PlayingCardSuitEnchantment.FindPokerHandIncludingWildcard(Owner, this);

        // A lone wildcard is not a poker hand. Only use the hand branch when
        // the ace is part of a real multi-card hand.
        bool isPartOfPokerHand = pokerHand is { Cards.Count: >= 2 }
            && pokerHand.Cards.Any(card => ReferenceEquals(card.Value, this));

        if (!isPartOfPokerHand)
        {
            await PlayingCardSuitEnchantment.TriggerAllSuitEffects(
                choiceContext,
                Owner.Creature,
                cardPlay,
                triggerCount: 2);
        }
        else if (pokerHand is not null)
        {
            await PlayingCardSuitEnchantment.TriggerPokerHand(
                choiceContext,
                Owner.Creature,
                cardPlay,
                pokerHand);
        }

        await CardPileCmd.Draw(choiceContext, 1, Owner, fromHandDraw: true);
    }
}

/// <summary>Offers a final end-of-combat suit enchantment if it remains in hand.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class ReversedAceCard : ModCardTemplate
{
    private const string CardPortraitPath =
        "res://bs_ancient/assets/images/cards/ReversedAceCard.jpg";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(PortraitPath: CardPortraitPath);

    public ReversedAceCard() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

    public override async Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        if (Owner == null || Pile?.Type != PileType.Hand)
        {
            return;
        }

        List<CardModel> selected = (await CardSelectCmd.FromDeckGeneric(
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 2, 2),
                card => card != this
                    && PlayingCardSuitEnchantment.CanReceiveOrRerollSuit(card)))
            .ToList();

        foreach (CardModel card in selected)
        {
            PlayingCardSuitEnchantment.TryEnchantRandomSuit(card, 1, 13);
        }
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Tracks the per-turn replay limit and centered hand placement for the Croquet Mallet.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class QueenOfHeartsCroquetMalletCard : ModCardTemplate
{
    private const int MaxTriggersPerTurn = 3;
    private const string CardPortraitPath =
        "res://bs_ancient/assets/images/cards/QueenOfHeartsCroquetMalletCard.jpg";

    private int _triggersThisTurn;
    private bool _isRepositioning;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(PortraitPath: CardPortraitPath);

    internal bool CanTrigger => _triggersThisTurn < MaxTriggersPerTurn;

    public QueenOfHeartsCroquetMalletCard()
        : base(0, CardType.Attack, CardRarity.Ancient, TargetType.None)
    {
    }

    internal void ResetTriggersForTurn() => _triggersThisTurn = 0;

    internal void MarkTriggered() => _triggersThisTurn++;

    internal void CenterInHand()
    {
        if (_isRepositioning
            || Owner?.PlayerCombatState is not { } combatState
            || Pile?.Type != PileType.Hand)
        {
            return;
        }

        CardPile hand = combatState.Hand;
        List<CardModel> cards = hand.Cards.ToList();
        int currentIndex = cards.IndexOf(this);
        if (currentIndex < 0)
        {
            return;
        }

        int centerIndex = cards.Count / 2;
        if (currentIndex == centerIndex)
        {
            return;
        }

        cards.RemoveAt(currentIndex);
        cards.Insert(Math.Min(centerIndex, cards.Count), this);

        _isRepositioning = true;
        try
        {
            foreach (CardModel card in cards.AsEnumerable().Reverse())
            {
                hand.MoveToTopInternal(card);
            }

            hand.InvokeContentsChanged();
            RefreshHandVisuals(cards);
        }
        finally
        {
            _isRepositioning = false;
        }
    }

    private static void RefreshHandVisuals(IReadOnlyList<CardModel> orderedCards)
    {
        NPlayerHand? visualHand = NPlayerHand.Instance;
        if (visualHand == null)
        {
            return;
        }

        for (int index = 0; index < orderedCards.Count; index++)
        {
            if (visualHand.GetCardHolder(orderedCards[index]) is { } holder
                && holder.GetParent() == visualHand.CardHolderContainer)
            {
                visualHand.CardHolderContainer.MoveChild(holder, index);
            }
        }

        visualHand.ForceRefreshCardIndices();
    }
}

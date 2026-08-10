using BlackSouls.Scripts;

namespace BlackSouls.Tests;

public sealed class PlayingCardPokerHandEvaluatorTests
{
    [Fact]
    public void FindsPairFromMatchingRanks()
    {
        PlayingCardPokerHand<int>? hand = PlayingCardPokerHandEvaluator.FindBestHand([
            Card(1, 7, PlayingCardSuit.Heart),
            Card(2, 7, PlayingCardSuit.Spade)
        ]);

        Assert.NotNull(hand);
        Assert.Equal(PlayingCardPokerHandRank.Pair, hand.Rank);
        Assert.Equal([1, 2], hand.Cards.Select(card => card.Value));
    }

    [Fact]
    public void FindsStraightWithAceAsLowCard()
    {
        PlayingCardPokerHand<int>? hand = PlayingCardPokerHandEvaluator.FindBestHand([
            Card(1, 1, PlayingCardSuit.Heart),
            Card(2, 2, PlayingCardSuit.Diamond),
            Card(3, 3, PlayingCardSuit.Club),
            Card(4, 4, PlayingCardSuit.Spade),
            Card(5, 5, PlayingCardSuit.Heart)
        ]);

        Assert.NotNull(hand);
        Assert.Equal(PlayingCardPokerHandRank.Straight, hand.Rank);
    }

    [Fact]
    public void DoesNotTreatAGappedSequenceAsStraight()
    {
        PlayingCardPokerHand<int>? hand = PlayingCardPokerHandEvaluator.FindBestHand([
            Card(1, 1, PlayingCardSuit.Heart),
            Card(2, 2, PlayingCardSuit.Diamond),
            Card(3, 3, PlayingCardSuit.Club),
            Card(4, 4, PlayingCardSuit.Spade),
            Card(5, 6, PlayingCardSuit.Heart)
        ]);

        Assert.Null(hand);
    }

    [Fact]
    public void FindsStraightFlushAndGrantsOneExtraPlay()
    {
        PlayingCardPokerHand<int>? hand = PlayingCardPokerHandEvaluator.FindBestHand([
            Card(1, 4, PlayingCardSuit.Club),
            Card(2, 5, PlayingCardSuit.Club),
            Card(3, 6, PlayingCardSuit.Club),
            Card(4, 7, PlayingCardSuit.Club),
            Card(5, 8, PlayingCardSuit.Club)
        ]);

        Assert.NotNull(hand);
        Assert.Equal(PlayingCardPokerHandRank.StraightFlush, hand.Rank);
        Assert.Equal(1, hand.ExtraPlayCount);
    }

    [Fact]
    public void FindsRoyalFlushAndGrantsTwoExtraPlays()
    {
        PlayingCardPokerHand<int>? hand = PlayingCardPokerHandEvaluator.FindBestHand([
            Card(1, 10, PlayingCardSuit.Spade),
            Card(2, 11, PlayingCardSuit.Spade),
            Card(3, 12, PlayingCardSuit.Spade),
            Card(4, 13, PlayingCardSuit.Spade),
            Card(5, 1, PlayingCardSuit.Spade)
        ]);

        Assert.NotNull(hand);
        Assert.Equal(PlayingCardPokerHandRank.RoyalFlush, hand.Rank);
        Assert.Equal(2, hand.ExtraPlayCount);
    }

    [Fact]
    public void PrefersTheHighestAvailableHand()
    {
        PlayingCardPokerHand<int>? hand = PlayingCardPokerHandEvaluator.FindBestHand([
            Card(1, 9, PlayingCardSuit.Heart),
            Card(2, 9, PlayingCardSuit.Diamond),
            Card(3, 9, PlayingCardSuit.Club),
            Card(4, 9, PlayingCardSuit.Spade),
            Card(5, 2, PlayingCardSuit.Heart)
        ]);

        Assert.NotNull(hand);
        Assert.Equal(PlayingCardPokerHandRank.FourOfAKind, hand.Rank);
    }

    private static PlayingCardPokerCard<int> Card(int value, int rank, PlayingCardSuit suit) => new(value, rank, suit);
}

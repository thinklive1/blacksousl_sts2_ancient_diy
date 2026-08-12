namespace BlackSouls.Scripts;

/// <summary>Represents the four standard playing-card suits.</summary>
internal enum PlayingCardSuit
{
    Heart,
    Diamond,
    Club,
    Spade
}

/// <summary>Stores a card's suit-enchantment rank and suit for poker evaluation.</summary>
internal readonly record struct PlayingCardPokerCard<T>(T Value, int Rank, PlayingCardSuit Suit);

/// <summary>Lists the supported Texas Hold'em hand categories in strength order.</summary>
internal enum PlayingCardPokerHandRank
{
    Pair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8,
    RoyalFlush = 9
}

/// <summary>Describes the strongest poker group currently available in a hand.</summary>
internal sealed record PlayingCardPokerHand<T>(PlayingCardPokerHandRank Rank, IReadOnlyList<PlayingCardPokerCard<T>> Cards)
{
    /// <summary>Gets how many times every member's Suit effect resolves for this hand.</summary>
    public int SuitEffectTriggerCount => Rank switch
    {
        PlayingCardPokerHandRank.StraightFlush => 2,
        PlayingCardPokerHandRank.RoyalFlush => 3,
        _ => 1
    };
}

/// <summary>Finds the strongest supported Texas Hold'em group from suit-enchanted cards.</summary>
internal static class PlayingCardPokerHandEvaluator
{
    public static PlayingCardPokerHand<T>? FindBestHand<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>> normalized = cards
            .Where(card => card.Rank is >= PlayingCardSuitEnchantment.MinTriggersPerCombat and <= PlayingCardSuitEnchantment.MaxTriggersPerCombat)
            .ToList();

        return FindRoyalFlush(normalized)
            ?? FindStraightFlush(normalized)
            ?? FindFourOfAKind(normalized)
            ?? FindFullHouse(normalized)
            ?? FindFlush(normalized)
            ?? FindStraight(normalized)
            ?? FindThreeOfAKind(normalized)
            ?? FindTwoPair(normalized)
            ?? FindPair(normalized);
    }

    private static PlayingCardPokerHand<T>? FindRoyalFlush<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        foreach (PlayingCardSuit suit in Enum.GetValues<PlayingCardSuit>())
        {
            List<PlayingCardPokerCard<T>>? royal = SelectRanks(cards.Where(card => card.Suit == suit), [10, 11, 12, 13, 1]);
            if (royal != null)
            {
                return new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.RoyalFlush, royal);
            }
        }

        return null;
    }

    private static PlayingCardPokerHand<T>? FindStraightFlush<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        foreach (PlayingCardSuit suit in Enum.GetValues<PlayingCardSuit>())
        {
            List<PlayingCardPokerCard<T>>? straight = FindHighestStraight(cards.Where(card => card.Suit == suit));
            if (straight != null)
            {
                return new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.StraightFlush, straight);
            }
        }

        return null;
    }

    private static PlayingCardPokerHand<T>? FindFourOfAKind<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>>? group = FindSameRank(cards, 4);
        return group == null ? null : new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.FourOfAKind, group);
    }

    private static PlayingCardPokerHand<T>? FindFullHouse<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<IGrouping<int, PlayingCardPokerCard<T>>> groups = GroupByRank(cards);
        IGrouping<int, PlayingCardPokerCard<T>>? triple = groups.FirstOrDefault(group => group.Count() >= 3);
        IGrouping<int, PlayingCardPokerCard<T>>? pair = groups.FirstOrDefault(group => group.Key != triple?.Key && group.Count() >= 2);
        if (triple == null || pair == null)
        {
            return null;
        }

        return new PlayingCardPokerHand<T>(
            PlayingCardPokerHandRank.FullHouse,
            triple.Take(3).Concat(pair.Take(2)).ToList());
    }

    private static PlayingCardPokerHand<T>? FindFlush<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>>? flush = cards
            .GroupBy(card => card.Suit)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault(group => group.Count() >= 5)
            ?.OrderByDescending(card => RankValue(card.Rank))
            .Take(5)
            .ToList();

        return flush is { Count: 5 } ? new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.Flush, flush) : null;
    }

    private static PlayingCardPokerHand<T>? FindStraight<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>>? straight = FindHighestStraight(cards);
        return straight == null ? null : new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.Straight, straight);
    }

    private static PlayingCardPokerHand<T>? FindThreeOfAKind<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>>? group = FindSameRank(cards, 3);
        return group == null ? null : new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.ThreeOfAKind, group);
    }

    private static PlayingCardPokerHand<T>? FindTwoPair<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<IGrouping<int, PlayingCardPokerCard<T>>> pairs = GroupByRank(cards)
            .Where(group => group.Count() >= 2)
            .Take(2)
            .ToList();
        if (pairs.Count != 2)
        {
            return null;
        }

        return new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.TwoPair, pairs.SelectMany(pair => pair.Take(2)).ToList());
    }

    private static PlayingCardPokerHand<T>? FindPair<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>>? group = FindSameRank(cards, 2);
        return group == null ? null : new PlayingCardPokerHand<T>(PlayingCardPokerHandRank.Pair, group);
    }

    private static List<PlayingCardPokerCard<T>>? FindHighestStraight<T>(IEnumerable<PlayingCardPokerCard<T>> cards)
    {
        List<PlayingCardPokerCard<T>> source = cards.ToList();
        for (int start = 10; start >= 2; start--)
        {
            int[] ranks = start == 10 ? [10, 11, 12, 13, 1] : Enumerable.Range(start, 5).ToArray();
            List<PlayingCardPokerCard<T>>? straight = SelectRanks(source, ranks);
            if (straight != null)
            {
                return straight;
            }
        }

        // Ace may also be used as the low card in A-2-3-4-5.
        return SelectRanks(source, [1, 2, 3, 4, 5]);
    }

    private static List<PlayingCardPokerCard<T>>? SelectRanks<T>(IEnumerable<PlayingCardPokerCard<T>> cards, IReadOnlyList<int> ranks)
    {
        List<PlayingCardPokerCard<T>> source = cards.ToList();
        List<PlayingCardPokerCard<T>> selected = [];
        foreach (int rank in ranks)
        {
            int index = source.FindIndex(candidate => candidate.Rank == rank);
            if (index < 0)
            {
                return null;
            }

            selected.Add(source[index]);
        }

        return selected;
    }

    private static List<PlayingCardPokerCard<T>>? FindSameRank<T>(IReadOnlyList<PlayingCardPokerCard<T>> cards, int count)
    {
        IGrouping<int, PlayingCardPokerCard<T>>? group = GroupByRank(cards).FirstOrDefault(group => group.Count() >= count);
        return group?.Take(count).ToList();
    }

    private static List<IGrouping<int, PlayingCardPokerCard<T>>> GroupByRank<T>(IEnumerable<PlayingCardPokerCard<T>> cards)
    {
        return cards
            .GroupBy(card => card.Rank)
            .OrderByDescending(group => RankValue(group.Key))
            .ToList();
    }

    private static int RankValue(int rank) => rank == 1 ? 14 : rank;
}

/// <summary>Calculates Balatro-style chips times multiplier for a selected poker hand.</summary>
internal static class BalatroPokerScoring
{
    public static int Calculate<T>(IReadOnlyList<PlayingCardPokerCard<T>> pokerCards)
    {
        if (pokerCards.Count == 0)
        {
            return 0;
        }

        PlayingCardPokerHand<T>? hand = PlayingCardPokerHandEvaluator.FindBestHand(pokerCards);
        IReadOnlyList<PlayingCardPokerCard<T>> scoringCards;
        int baseChips;
        int multiplier;
        if (hand == null)
        {
            scoringCards = [pokerCards.OrderByDescending(card => RankValue(card.Rank)).First()];
            (baseChips, multiplier) = (5, 1);
        }
        else
        {
            scoringCards = hand.Cards;
            (baseChips, multiplier) = hand.Rank switch
            {
                PlayingCardPokerHandRank.Pair => (10, 2),
                PlayingCardPokerHandRank.TwoPair => (20, 2),
                PlayingCardPokerHandRank.ThreeOfAKind => (30, 3),
                PlayingCardPokerHandRank.Straight => (30, 4),
                PlayingCardPokerHandRank.Flush => (35, 4),
                PlayingCardPokerHandRank.FullHouse => (40, 4),
                PlayingCardPokerHandRank.FourOfAKind => (60, 7),
                PlayingCardPokerHandRank.StraightFlush or PlayingCardPokerHandRank.RoyalFlush => (100, 8),
                _ => (5, 1),
            };
        }

        int cardChips = scoringCards.Sum(card => card.Rank switch
        {
            1 => 11,
            >= 11 => 10,
            _ => card.Rank,
        });
        return (baseChips + cardChips) * multiplier;
    }

    private static int RankValue(int rank) => rank == 1 ? 14 : rank;
}

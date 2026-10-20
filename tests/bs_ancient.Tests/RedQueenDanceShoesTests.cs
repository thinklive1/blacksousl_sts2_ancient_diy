using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BsAncient.Tests;

public sealed class RedQueenDanceShoesTests
{
    [Theory]
    [InlineData(nameof(RedQueenDanceShoesRelic.BlackSouls_BonusEnabled))]
    [InlineData(nameof(RedQueenDanceShoesRelic.BlackSouls_HasStartedTurn))]
    [InlineData(nameof(RedQueenDanceShoesRelic.BlackSouls_ShuffledThisTurn))]
    public void CombatProgressParticipatesInRunSerialization(string propertyName)
    {
        var property = typeof(RedQueenDanceShoesRelic).GetProperty(propertyName)!;
        Assert.True(property.CanRead && property.CanWrite);
        Assert.True(Attribute.IsDefined(property, typeof(SavedPropertyAttribute)));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(5, 6)]
    [InlineData(22, 23)]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    public void FirstTurnAddsExactlyOneCardPerPositiveDraw(decimal count, decimal expected)
    {
        Assert.Equal(expected, RedQueenDanceShoesState.NewCombat.BeginTurn().ModifyDrawCount(count));
    }

    [Fact]
    public void MissingShuffleDisablesBonusOnTheFollowingTurn()
    {
        RedQueenDanceShoesState first = RedQueenDanceShoesState.NewCombat.BeginTurn();
        Assert.True(first.BonusEnabled);
        RedQueenDanceShoesState second = first.BeginTurn();
        Assert.False(second.BonusEnabled);
        Assert.Equal(5m, second.ModifyDrawCount(5m));
        Assert.False(second.BeginTurn().BonusEnabled);
    }

    [Fact]
    public void ShufflingWhileDisabledRestoresBonusNextTurnNotImmediately()
    {
        RedQueenDanceShoesState disabled = RedQueenDanceShoesState.NewCombat.BeginTurn().BeginTurn();
        RedQueenDanceShoesState shuffled = disabled with { ShuffledThisTurn = true };
        Assert.Equal(2m, shuffled.ModifyDrawCount(2m));
        RedQueenDanceShoesState restored = shuffled.BeginTurn();
        Assert.Equal(3m, restored.ModifyDrawCount(2m));
        Assert.False(restored.ShuffledThisTurn);
        Assert.False(restored.BeginTurn().BonusEnabled);
    }

    [Fact]
    public void ShuffleKeepsBonusForNextTurnAndIsConsumedOnce()
    {
        RedQueenDanceShoesState shuffled = RedQueenDanceShoesState.NewCombat.BeginTurn()
            with { ShuffledThisTurn = true };
        RedQueenDanceShoesState next = shuffled.BeginTurn();
        Assert.True(next.BonusEnabled);
        Assert.False(next.ShuffledThisTurn);
        Assert.False(next.BeginTurn().BonusEnabled);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RestoringSavedTurnFlagsPreservesCurrentAndNextTurnBonus(bool enabled, bool shuffled)
    {
        RedQueenDanceShoesState restored = new(true, enabled, shuffled);
        Assert.Equal(enabled ? 2m : 1m, restored.ModifyDrawCount(1m));
        Assert.Equal(shuffled, restored.BeginTurn().BonusEnabled);
    }

    [Fact]
    public void CombatResetAndCopiesDoNotShareTurnProgress()
    {
        RedQueenDanceShoesState first = RedQueenDanceShoesState.NewCombat.BeginTurn();
        RedQueenDanceShoesState other = first;
        first = first with { ShuffledThisTurn = true };
        Assert.True(first.BeginTurn().BonusEnabled);
        Assert.False(other.BeginTurn().BonusEnabled);
        Assert.True(RedQueenDanceShoesState.NewCombat.BeginTurn().BonusEnabled);
    }
}

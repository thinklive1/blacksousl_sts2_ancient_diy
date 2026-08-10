using BlackSouls.Scripts;

namespace BsAncient.Tests;

public sealed class BoojumHistoryPurgeTests : IDisposable
{
    public BoojumHistoryPurgeTests()
    {
        BoojumHistoryPurge.Reset();
    }

    public void Dispose()
    {
        BoojumHistoryPurge.Reset();
    }

    [Fact]
    public void MatchingProfileAndStartTimeConsumesTarget()
    {
        BoojumHistoryPurge.ArmCurrentRunHistoryErasure(profileId: 2, runStartTime: 123456);

        bool matched = BoojumHistoryPurge.TryConsumeCurrentRunHistoryErasure(
            profileId: 2,
            runStartTime: 123456,
            out CurrentRunHistoryTarget? expected);

        Assert.True(matched);
        Assert.Equal(new CurrentRunHistoryTarget(2, 123456), expected);
        Assert.False(BoojumHistoryPurge.TryConsumeCurrentRunHistoryErasure(2, 123456, out _));
    }

    [Theory]
    [InlineData(3, 123456)]
    [InlineData(2, 654321)]
    public void MismatchCancelsTargetWithoutMatchingLater(int profileId, long runStartTime)
    {
        BoojumHistoryPurge.ArmCurrentRunHistoryErasure(profileId: 2, runStartTime: 123456);

        bool matched = BoojumHistoryPurge.TryConsumeCurrentRunHistoryErasure(
            profileId,
            runStartTime,
            out CurrentRunHistoryTarget? expected);

        Assert.False(matched);
        Assert.Equal(new CurrentRunHistoryTarget(2, 123456), expected);
        Assert.False(BoojumHistoryPurge.TryConsumeCurrentRunHistoryErasure(2, 123456, out _));
    }
}

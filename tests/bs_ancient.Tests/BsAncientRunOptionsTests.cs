using BlackSouls.Scripts;

namespace BsAncient.Tests;

public sealed class BsAncientRunOptionsTests : IDisposable
{
    public BsAncientRunOptionsTests()
    {
        BsAncientRunOptions.ResetForTests();
    }

    public void Dispose()
    {
        BsAncientRunOptions.ResetForTests();
    }

    [Fact]
    public void PendingSelectionIsCapturedByOnlyOneRun()
    {
        object firstRun = new();
        object secondRun = new();
        BsAncientRunOptions.FairyTaleModeForNextRun = true;

        Assert.True(BsAncientRunOptions.ResolveFairyTaleModeForRun(firstRun, defaultValue: false));
        Assert.False(BsAncientRunOptions.ResolveFairyTaleModeForRun(secondRun, defaultValue: false));
    }

    [Fact]
    public void CapturedRunValueStaysStableForAllReaders()
    {
        object multiplayerRun = new();
        BsAncientRunOptions.FairyTaleModeForNextRun = true;

        Assert.True(BsAncientRunOptions.ResolveFairyTaleModeForRun(multiplayerRun, defaultValue: false));
        BsAncientRunOptions.FairyTaleModeForNextRun = false;
        Assert.True(BsAncientRunOptions.ResolveFairyTaleModeForRun(multiplayerRun, defaultValue: false));
    }
}

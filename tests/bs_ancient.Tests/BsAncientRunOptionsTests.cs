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
        BsAncientRunOptions.FairyTaleModeForNextRun = true;

        Assert.True(BsAncientRunOptions.ConsumePendingSelection(defaultValue: false));
        Assert.False(BsAncientRunOptions.ConsumePendingSelection(defaultValue: false));
    }

    [Fact]
    public void CapturedRunValueStaysStableAfterAnotherSelection()
    {
        BsAncientRunOptions.FairyTaleModeForNextRun = true;
        var captured = BsAncientRunConfig.CaptureNewRun();
        BsAncientRunOptions.FairyTaleModeForNextRun = false;
        Assert.True(captured.EnableFairyTaleMode);
        Assert.False(BsAncientRunOptions.ConsumePendingSelection(defaultValue: true));
    }
}

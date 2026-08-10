using BlackSouls.Scripts;

namespace BsAncient.Tests;

public sealed class HiddenOptionRollLedgerTests
{
    [Fact]
    public void SameEventAndOptionReusesFirstOutcome()
    {
        Dictionary<string, int> outcomes = [];
        int factoryCalls = 0;

        int first = HiddenOptionRollLedger.GetOrCreate(outcomes, "EVENT_A", "PAGE", () =>
        {
            factoryCalls++;
            return 1;
        });
        int second = HiddenOptionRollLedger.GetOrCreate(outcomes, "EVENT_A", "PAGE", () =>
        {
            factoryCalls++;
            return 0;
        });

        Assert.Equal(1, first);
        Assert.Equal(first, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void FailedRollIsCachedButAnotherEventRollsIndependently()
    {
        Dictionary<string, int> outcomes = [];
        int failedCalls = 0;

        Assert.Equal(0, HiddenOptionRollLedger.GetOrCreate(outcomes, "EVENT_A", "PAGE", () =>
        {
            failedCalls++;
            return 0;
        }));
        Assert.Equal(0, HiddenOptionRollLedger.GetOrCreate(outcomes, "EVENT_A", "PAGE", () =>
        {
            failedCalls++;
            return 1;
        }));
        Assert.Equal(2, HiddenOptionRollLedger.GetOrCreate(outcomes, "EVENT_B", "PAGE", () => 2));

        Assert.Equal(1, failedCalls);
        Assert.Equal(2, outcomes.Count);
    }
}

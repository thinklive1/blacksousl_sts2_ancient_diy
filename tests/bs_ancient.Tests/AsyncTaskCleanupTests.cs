using BlackSouls.Scripts;

namespace BsAncient.Tests;

public sealed class AsyncTaskCleanupTests
{
    [Fact]
    public async Task CleanupRunsAfterSuccessfulTask()
    {
        int cleanupCalls = 0;

        await AsyncTaskCleanup.Run(Task.CompletedTask, () => cleanupCalls++);

        Assert.Equal(1, cleanupCalls);
    }

    [Fact]
    public async Task CleanupRunsAfterFaultedTaskAndPreservesException()
    {
        int cleanupCalls = 0;
        InvalidOperationException failure = new("card failed");

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AsyncTaskCleanup.Run(Task.FromException(failure), () => cleanupCalls++));

        Assert.Same(failure, thrown);
        Assert.Equal(1, cleanupCalls);
    }
}

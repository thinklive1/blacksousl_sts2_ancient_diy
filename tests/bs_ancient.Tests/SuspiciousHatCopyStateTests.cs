using System.Text.Json;
using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Models;

namespace BsAncient.Tests;

public sealed class SuspiciousHatCopyStateTests
{
    [Fact]
    public void UiAndSaveReadsCannotSwitchTheTargetOrConsumePendingRestoreData()
    {
        var state = new SuspiciousHatCopyState<Counter>();
        state.Restore("8", _ => { });
        for (int i = 0; i < 10; i++)
        {
            Assert.Null(state.Copy);
            Assert.Null(state.Source);
            Assert.Equal("8", state.Serialize((_, _) => throw new InvalidOperationException()));
        }

        var source = new Counter("A", 2);
        Sync(state, source);
        Assert.Equal(8, state.Copy!.Value);
        Assert.Equal(2, source.Value);
    }

    [Fact]
    public void DifferentUiReadSchedulesProduceTheSameInventoryTransitions()
    {
        var sourceA = new Counter("A", 2);
        var sourceB = new Counter("B", 4);
        var host = new SuspiciousHatCopyState<Counter>();
        var client = new SuspiciousHatCopyState<Counter>();
        foreach (Counter source in new[] { sourceA, sourceB, sourceA })
        {
            Sync(host, source);
            for (int i = 0; i < 7; i++)
            {
                _ = host.Copy;
                _ = host.Serialize((copy, _) => copy.Value.ToString());
            }
            Sync(client, source);
            host.Copy!.Value++;
            client.Copy!.Value++;
            Assert.Equal(host.Copy.Value, client.Copy.Value);
            Assert.Equal(host.Identity, client.Identity);
        }
        Assert.Equal(2, sourceA.Value);
        Assert.Equal(4, sourceB.Value);
    }

    [Fact]
    public void SameTargetPreservesItsCounterAndOnlyUpdatesItsSaveLocation()
    {
        var source = new Counter("A", 2);
        var state = new SuspiciousHatCopyState<Counter>();
        Sync(state, source);
        Counter copy = state.Copy!;
        copy.Value = 8;
        bool replaced = state.Reconcile(source, Identity(source) with { Index = 4 },
            (_, _, _) => throw new InvalidOperationException("Must not recreate the same source."), _ => { });
        Assert.False(replaced);
        Assert.Same(copy, state.Copy);
        Assert.Equal(8, copy.Value);
        Assert.Equal(4, state.Identity!.Index);
    }

    [Fact]
    public void SwitchingAndRemovingTargetsReleasesExactlyTheReplacedCopies()
    {
        var state = new SuspiciousHatCopyState<Counter>();
        var released = new List<Counter>();
        var first = new Counter("A", 2);
        var second = new Counter("B", 4);
        Sync(state, first);
        Counter oldCopy = state.Copy!;
        state.Reconcile(second, Identity(second), CreateCopy, released.Add);
        Assert.Same(oldCopy, Assert.Single(released));
        Counter secondCopy = state.Copy!;
        state.Reconcile(null, null, CreateCopy, released.Add);
        Assert.Equal(2, released.Count);
        Assert.Same(secondCopy, released[1]);
        Assert.Null(state.Copy);
        Assert.Empty(state.Serialize((_, _) => throw new InvalidOperationException()));
    }

    [Fact]
    public void RestoredAndClonedStatesOwnIndependentCopies()
    {
        var source = new Counter("A", 2);
        var first = new SuspiciousHatCopyState<Counter>();
        Sync(first, source);
        first.Copy!.Value = 8;
        string saved = first.Serialize((copy, _) => JsonSerializer.Serialize(copy));
        var second = new SuspiciousHatCopyState<Counter>();
        second.Restore(saved, _ => { });
        second.Reconcile(source, Identity(source), (_, _, json) => JsonSerializer.Deserialize<Counter>(json)!, _ => { });
        second.Copy!.Value++;
        Assert.Equal(8, first.Copy.Value);
        Assert.Equal(9, second.Copy.Value);
        Assert.Equal(2, source.Value);
    }

    [Fact]
    public void FailedReplacementPreservesTheWorkingCopyAndItsPendingState()
    {
        var source = new Counter("A", 2);
        var state = new SuspiciousHatCopyState<Counter>();
        Sync(state, source);
        Counter old = state.Copy!;
        old.Value = 8;
        var replacement = new Counter("B", 4);
        Assert.Throws<InvalidOperationException>(() => state.Reconcile(replacement, Identity(replacement),
            (_, _, _) => throw new InvalidOperationException(), _ => throw new InvalidOperationException("Must not release the old copy.")));
        Assert.Same(source, state.Source);
        Assert.Same(old, state.Copy);
        Assert.Equal(8, state.Copy!.Value);
    }

    [Fact]
    public async Task AcquisitionContinuationWaitsForInitializationAndPreservesTheResult()
    {
        object player = new();
        IDisposable scope = SuspiciousHatAcquisitionScope.Begin(player);
        var original = new TaskCompletionSource<int>();
        int callbacks = 0;
        Task<int> wrapped = SuspiciousHatAcquisitionScope.Complete(original.Task, scope, value =>
        {
            Assert.Equal(60, value);
            Assert.False(SuspiciousHatAcquisitionScope.IsPending(player));
            callbacks++;
        });
        Assert.False(wrapped.IsCompleted);
        Assert.True(SuspiciousHatAcquisitionScope.IsPending(player));
        Assert.Equal(0, callbacks);
        original.SetResult(60);
        Assert.Equal(60, await wrapped);
        Assert.Equal(1, callbacks);
    }

    [Fact]
    public async Task FailedAcquisitionDoesNotCommitACopyTransition()
    {
        object player = new();
        IDisposable scope = SuspiciousHatAcquisitionScope.Begin(player);
        var error = new InvalidOperationException("Pickup failed");
        int callbacks = 0;
        Task<int> wrapped = SuspiciousHatAcquisitionScope.Complete(Task.FromException<int>(error), scope, _ => callbacks++);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => wrapped));
        Assert.Equal(0, callbacks);
        Assert.False(SuspiciousHatAcquisitionScope.IsPending(player));
    }

    [Fact]
    public void NestedAcquisitionsAreScopedPerPlayerAndReleaseOnlyOnce()
    {
        object first = new();
        object second = new();
        IDisposable outer = SuspiciousHatAcquisitionScope.Begin(first);
        IDisposable inner = SuspiciousHatAcquisitionScope.Begin(first);
        Assert.True(SuspiciousHatAcquisitionScope.IsPending(first));
        Assert.False(SuspiciousHatAcquisitionScope.IsPending(second));
        inner.Dispose();
        inner.Dispose();
        Assert.True(SuspiciousHatAcquisitionScope.IsPending(first));
        outer.Dispose();
        Assert.False(SuspiciousHatAcquisitionScope.IsPending(first));
    }

    [Fact]
    public void SynchronousAcquisitionFailureReleasesItsScopeAndPreservesTheException()
    {
        object player = new();
        IDisposable scope = SuspiciousHatAcquisitionScope.Begin(player);
        var error = new InvalidOperationException();
        Assert.Same(error, SuspiciousHatRelicObtainedPatch.Finalizer(error, scope));
        Assert.False(SuspiciousHatAcquisitionScope.IsPending(player));
    }

    private static void Sync(SuspiciousHatCopyState<Counter> state, Counter source) =>
        state.Reconcile(source, Identity(source), CreateCopy, _ => { });
    private static SuspiciousHatCopyIdentity Identity(Counter source) => new(new ModelId("RELIC", source.Id), 5, 2, 42);
    private static Counter CreateCopy(Counter source, SuspiciousHatCopyIdentity _, string pending) =>
        new(source.Id, pending.Length == 0 ? source.Value : int.Parse(pending));

    public sealed class Counter(string id, int value)
    {
        public string Id { get; set; } = id;
        public int Value { get; set; } = value;
    }
}

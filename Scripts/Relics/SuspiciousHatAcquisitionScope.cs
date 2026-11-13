using System.Runtime.CompilerServices;

namespace BlackSouls.Scripts;

/// <summary>Defers copy initialization until all nested acquisitions for one player have finished.</summary>
internal sealed class SuspiciousHatAcquisitionScope : IDisposable
{
    private sealed class PendingCount
    {
        internal int Value;
    }

    private static readonly ConditionalWeakTable<object, PendingCount> Pending = new();
    private PendingCount? _count;

    private SuspiciousHatAcquisitionScope(PendingCount count)
    {
        _count = count;
        Interlocked.Increment(ref count.Value);
    }

    internal static IDisposable Begin(object player) => new SuspiciousHatAcquisitionScope(Pending.GetOrCreateValue(player));

    internal static bool IsPending(object player) =>
        Pending.TryGetValue(player, out PendingCount? count) && Volatile.Read(ref count.Value) > 0;

    public void Dispose()
    {
        PendingCount? count = Interlocked.Exchange(ref _count, null);
        if (count != null)
        {
            Interlocked.Decrement(ref count.Value);
        }
    }

    internal static async Task<T> Complete<T>(Task<T> original, IDisposable scope, Action<T> afterSuccess)
    {
        T result;
        try
        {
            result = await original;
        }
        finally
        {
            scope.Dispose();
        }

        afterSuccess(result);
        return result;
    }
}

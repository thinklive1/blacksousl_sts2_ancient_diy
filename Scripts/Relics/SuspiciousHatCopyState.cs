using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

internal sealed record SuspiciousHatCopyIdentity(ModelId Id, int Floor, int Index, ulong OwnerNetId = 0);

/// <summary>Separates deterministic copy transitions from read-only UI and save access.</summary>
internal sealed class SuspiciousHatCopyState<T> where T : class
{
    private string _pendingSave = string.Empty;

    internal T? Source { get; private set; }
    internal T? Copy { get; private set; }
    internal SuspiciousHatCopyIdentity? Identity { get; private set; }

    internal bool Reconcile(
        T? source,
        SuspiciousHatCopyIdentity? identity,
        Func<T, SuspiciousHatCopyIdentity, string, T> createCopy,
        Action<T> releaseCopy)
    {
        if (source == null)
        {
            bool changed = Copy != null || _pendingSave.Length != 0;
            Clear(releaseCopy);
            return changed;
        }

        ArgumentNullException.ThrowIfNull(identity);
        if (ReferenceEquals(source, Source))
        {
            Identity = identity;
            return false;
        }

        // Keep the previous state if constructing the replacement fails.
        T copy = createCopy(source, identity, _pendingSave);
        if (Copy != null)
        {
            releaseCopy(Copy);
        }

        Source = source;
        Copy = copy;
        Identity = identity;
        _pendingSave = string.Empty;
        return true;
    }

    internal string Serialize(Func<T, SuspiciousHatCopyIdentity, string> serializeCopy) =>
        Copy != null && Identity != null ? serializeCopy(Copy, Identity) : _pendingSave;

    internal void Restore(string saved, Action<T> releaseCopy)
    {
        Clear(releaseCopy);
        _pendingSave = saved;
    }

    internal void Clear(Action<T> releaseCopy)
    {
        if (Copy != null)
        {
            releaseCopy(Copy);
        }

        Source = null;
        Copy = null;
        Identity = null;
        _pendingSave = string.Empty;
    }
}

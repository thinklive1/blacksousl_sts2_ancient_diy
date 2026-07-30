using System.IO;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlackSouls.Scripts;

/// <summary>Loads only safe, completed single-player histories for Boojum.</summary>
public static class BoojumHistoryMemory
{
    public static bool TryLoadForSinglePlayer(IReadOnlyList<Player>? players, out List<BoojumMemoryRecord> records)
    {
        records = [];
        if (players is not [Player player])
        {
            return false;
        }

        try
        {
            foreach (string fileName in SaveManager.Instance.GetAllRunHistoryNames())
            {
                if (!IsSafeHistoryFileName(fileName))
                {
                    continue;
                }

                var result = SaveManager.Instance.LoadRunHistory(fileName);
                RunHistory? history = result.Success ? result.SaveData : null;
                if (history is { WasAbandoned: false, Players.Count: 1 }
                    && history.Players[0].Character == player.Character.Id)
                {
                    int deckSize = history.Players[0].Deck.Count();
                    if (deckSize > 0)
                    {
                        records.Add(new BoojumMemoryRecord(
                            fileName,
                            deckSize,
                            history.StartTime,
                            history.Players[0].Deck.ToList()));
                    }
                }
            }

            records = records
                .OrderBy(record => record.RemainingCards)
                .ThenBy(record => record.FileName, StringComparer.Ordinal)
                .ToList();
            return true;
        }
        catch (Exception exception)
        {
            Entry.Logger.Warn($"Boojum could not load run history: {exception.Message}");
            records = [];
            return false;
        }
    }

    public static bool IsSafeHistoryFileName(string? fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal)
            && fileName.EndsWith(".run", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Identifies the remaining memory contributed by one completed run.</summary>
public sealed class BoojumMemoryRecord
{
    public BoojumMemoryRecord()
    {
    }

    public BoojumMemoryRecord(string fileName, int remainingCards)
    {
        FileName = fileName;
        RemainingCards = remainingCards;
    }

    public BoojumMemoryRecord(
        string fileName,
        int remainingCards,
        long startTime,
        List<SerializableCard> deck)
    {
        FileName = fileName;
        RemainingCards = remainingCards;
        StartTime = startTime;
        Deck = deck;
    }

    public string FileName { get; set; } = string.Empty;

    public int RemainingCards { get; set; }

    /// <summary>Stores the original run start time used by the generated memory card title.</summary>
    public long StartTime { get; set; }

    /// <summary>Stores the historical deck snapshot replayed by the generated memory card.</summary>
    public List<SerializableCard> Deck { get; set; } = [];
}

/// <summary>Safely backs up and erases histories consumed by Boojum's memory.</summary>
public static class BoojumHistoryPurge
{
    private const string BackupSuffix = ".boojum.bak";
    private static bool _eraseCurrentRunHistoryAfterSave;

    /// <summary>Retained for callers that begin a Boojum combat; memory erasure is immediate.</summary>
    public static void Reset(ICombatState? combatState = null)
    {
        _ = combatState;
    }

    /// <summary>Backs up and erases a history as soon as its entire memory segment is consumed.</summary>
    public static void EraseConsumedMemory(string fileName)
    {
        if (BoojumHistoryMemory.IsSafeHistoryFileName(fileName))
        {
            EraseHistoryFile(fileName);
        }
    }

    /// <summary>Marks the current run's final history entry for erasure after a lost Boojum battle.</summary>
    public static void ArmCurrentRunHistoryErasure()
    {
        _eraseCurrentRunHistoryAfterSave = true;
    }

    /// <summary>Erases the just-saved current run history only when a Boojum loss armed the marker.</summary>
    public static void PurgeCurrentRunHistoryAfterSave(RunHistory history)
    {
        if (!_eraseCurrentRunHistoryAfterSave)
        {
            return;
        }

        _eraseCurrentRunHistoryAfterSave = false;
        EraseHistoryFile($"{history.StartTime}.run");
    }

    private static void EraseHistoryFile(string fileName)
    {
        if (!BoojumHistoryMemory.IsSafeHistoryFileName(fileName))
        {
            Entry.Logger.Warn($"Boojum refused to erase an unsafe history name: '{fileName}'.");
            return;
        }

        if (!SaveManager.Instance.IsProfileInitialized)
        {
            Entry.Logger.Warn($"Boojum could not erase run history '{fileName}' because no profile is initialized.");
            return;
        }

        ISaveStore? saveStore = GetSaveStore();
        if (saveStore == null)
        {
            Entry.Logger.Warn($"Boojum could not erase run history '{fileName}' because the game save store is unavailable.");
            return;
        }

        // RunHistorySaveManager paths are save-store-relative, not operating-system paths.
        // Going through the game's store keeps the local and Steam Cloud copies in sync.
        string path = Path.Combine(
            RunHistorySaveManager.GetHistoryPath(SaveManager.Instance.CurrentProfileId),
            fileName);
        string backupPath = path + BackupSuffix;
        try
        {
            if (!saveStore.FileExists(path))
            {
                Entry.Logger.Warn($"Boojum could not find run history '{fileName}' at '{path}'.");
                return;
            }

            string? content = saveStore.ReadFile(path);
            if (content == null)
            {
                Entry.Logger.Warn($"Boojum could not read run history '{fileName}' before erasing it.");
                return;
            }

            // Preserve a recoverable copy beside the history before erasing the original run.
            saveStore.WriteFile(backupPath, content);
            saveStore.DeleteFile(path);
            if (saveStore.FileExists(path) || !saveStore.FileExists(backupPath))
            {
                Entry.Logger.Warn($"Boojum could not confirm erasure of run history '{fileName}'.");
                return;
            }

            Entry.Logger.Info($"Boojum archived and erased run history: {fileName}");
        }
        catch (Exception exception)
        {
            Entry.Logger.Warn($"Boojum could not erase run history '{fileName}': {exception.Message}");
        }
    }

    private static ISaveStore? GetSaveStore()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo? field = typeof(SaveManager).GetField("_saveStore", flags);
        return field?.GetValue(SaveManager.Instance) as ISaveStore;
    }
}

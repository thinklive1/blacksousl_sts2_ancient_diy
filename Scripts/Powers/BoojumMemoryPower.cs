using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts;

/// <summary>Consumes memory after Block but before Osty absorbs Boojum's damage.</summary>
[RegisterPower]
public sealed class BoojumMemoryPower : ModPowerTemplate
{
    private const string MemoryIconPath = "res://bs_ancient/assets/images/powers/BoojumMemoryPower.png";
    private static readonly SavedAttachedState<BoojumMemoryPower, string> MemoryRecords = new(
        "BlackSouls_BoojumMemoryRecords",
        static () => string.Empty);

    private List<BoojumMemoryRecord>? _records;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: MemoryIconPath,
        BigIconPath: MemoryIconPath);

    public void Configure(IEnumerable<BoojumMemoryRecord> records)
    {
        List<BoojumMemoryRecord> normalized = records
            .Where(record => record.RemainingCards > 0 && BoojumHistoryMemory.IsSafeHistoryFileName(record.FileName))
            .OrderBy(record => record.RemainingCards)
            .ThenBy(record => record.FileName, StringComparer.Ordinal)
            .ToList();
        PersistRecords(normalized);
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner
            || dealer?.Monster is not Boojum
            || amount <= 0m
            || Amount <= 0m)
        {
            return amount;
        }

        int absorbed = (int)Math.Min(decimal.Truncate(amount), Amount);
        if (absorbed <= 0)
        {
            return amount;
        }

        Entry.Logger.Info($"Boojum memory absorbed {absorbed} damage across {Records.Count} history segment(s).");
        ConsumeMemory(absorbed);
        SetAmount(Amount - absorbed, silent: true);
        Flash();
        return amount - absorbed;
    }

    private void ConsumeMemory(int amount)
    {
        List<BoojumMemoryRecord> records = Records;
        int remainingDamage = amount;
        foreach (BoojumMemoryRecord record in records)
        {
            if (remainingDamage <= 0)
            {
                break;
            }

            int consumed = Math.Min(record.RemainingCards, remainingDamage);
            record.RemainingCards -= consumed;
            remainingDamage -= consumed;
            if (record.RemainingCards == 0)
            {
                Entry.Logger.Info($"Boojum memory exhausted history segment: {record.FileName}");
                BoojumHistoryPurge.EraseConsumedMemory(record.FileName);
            }
        }

        records.RemoveAll(record => record.RemainingCards <= 0);
        PersistRecords(records);
    }

    private void PersistRecords(List<BoojumMemoryRecord> records)
    {
        AssertMutable();
        _records = records;
        MemoryRecords.Set(this, JsonSerializer.Serialize(records));
    }

    private List<BoojumMemoryRecord> Records
    {
        get
        {
            if (_records != null)
            {
                return _records;
            }

            try
            {
                _records = JsonSerializer.Deserialize<List<BoojumMemoryRecord>>(
                    MemoryRecords.GetValueOrDefault(this, string.Empty)) ?? [];
            }
            catch (JsonException)
            {
                _records = [];
            }

            _records = _records
                .Where(record => record.RemainingCards > 0 && BoojumHistoryMemory.IsSafeHistoryFileName(record.FileName))
                .OrderBy(record => record.RemainingCards)
                .ThenBy(record => record.FileName, StringComparer.Ordinal)
                .ToList();
            return _records;
        }
    }

}

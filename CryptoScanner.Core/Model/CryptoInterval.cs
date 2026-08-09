using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Interval")]
public class CryptoInterval
{
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// Interval name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Interval enumeration
    /// </summary>
    public CryptoIntervalPeriod IntervalPeriod { get; set; }

    /// <summary>
    /// Number of seconds for this interval
    /// </summary>
    public uint Duration { get; set; }

    /// <summary>
    /// Verwijzing naar een ander interval waar deze uit op te bouwen is
    /// </summary>
    public int? ConstructFromId { get; set; }
    [Computed]
    public virtual CryptoInterval? ConstructFrom { get; set; }


    public static CryptoInterval CreateInterval(CryptoIntervalPeriod intervalPeriod, string name, uint duration, CryptoInterval? constructFrom)
    {
        CryptoInterval cryptoInterval = new()
        {
            IntervalPeriod = intervalPeriod,
            Name = name,
            Duration = duration,
            ConstructFrom = constructFrom
        };
        return cryptoInterval;
    }

    /// <summary>
    /// Builds the canonical list of all supported intervals with their ConstructFrom chain.
    /// Single source of truth shared by the DB seed (CreateTableInterval) and test helpers.
    ///
    /// WARNING — the ORDER of this list is an identity, not a presentation choice. It determines
    /// both the autoincrement Interval.Id in the database and the index into SymbolIntervalList
    /// (see CryptoSymbol.GetSymbolInterval, which does SymbolIntervalList[(int)IntervalPeriod]).
    /// Inserting an entry in the MIDDLE shifts every id after it, which silently re-labels all
    /// stored candles, signals, positions and zones — a 15m candle then reads back as 30m, with
    /// no error anywhere. Append at the end, or migrate the existing data deliberately.
    /// </summary>
    public static List<CryptoInterval> CreateStandardIntervalList()
    {
        List<CryptoInterval> list = [];
        list.Add(CreateInterval(CryptoIntervalPeriod.interval1m, "1m", 1, null));    // 0
        list.Add(CreateInterval(CryptoIntervalPeriod.interval2m, "2m", 2, list[0])); // 1
        list.Add(CreateInterval(CryptoIntervalPeriod.interval3m, "3m", 3, list[0])); // 2
        list.Add(CreateInterval(CryptoIntervalPeriod.interval5m, "5m", 5, list[0])); // 3
        list.Add(CreateInterval(CryptoIntervalPeriod.interval10m, "10m", 10, list[3])); // 4
        list.Add(CreateInterval(CryptoIntervalPeriod.interval15m, "15m", 15, list[3])); // 5
        list.Add(CreateInterval(CryptoIntervalPeriod.interval30m, "30m", 30, list[5])); // 6
        list.Add(CreateInterval(CryptoIntervalPeriod.interval1h, "1h", 01 * 60, list[6])); // 7
        list.Add(CreateInterval(CryptoIntervalPeriod.interval2h, "2h", 02 * 60, list[7])); // 8
        list.Add(CreateInterval(CryptoIntervalPeriod.interval3h, "3h", 03 * 60, list[7])); // 9
        list.Add(CreateInterval(CryptoIntervalPeriod.interval4h, "4h", 04 * 60, list[8])); // 10
        list.Add(CreateInterval(CryptoIntervalPeriod.interval6h, "6h", 06 * 60, list[9])); // 11
        list.Add(CreateInterval(CryptoIntervalPeriod.interval8h, "8h", 08 * 60, list[10])); // 12
        list.Add(CreateInterval(CryptoIntervalPeriod.interval12h, "12h", 12 * 60, list[11])); // 13
        list.Add(CreateInterval(CryptoIntervalPeriod.interval1d, "1d", 24 * 60, list[13])); // 14
        list.Add(CreateInterval(CryptoIntervalPeriod.interval1w, "1w", 7 * 24 * 60, list[14])); // 15
        return list;
    }
}
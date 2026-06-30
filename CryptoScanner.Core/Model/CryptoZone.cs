using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Zone")]
public class CryptoZone
{
    [Key]
    public int Id { get; set; }

    public required int ExchangeId { get; set; }
    [Computed]
    public required virtual CryptoExchange Exchange { get; set; }

    public required int SymbolId { get; set; }
    [Computed]
    public required virtual CryptoSymbol Symbol { get; set; }

    public required int IntervalId { get; set; }
    [Computed]
    public required virtual CryptoInterval Interval { get; set; }

    public required CryptoZoneKind Kind { get; set; }
    public required CryptoTradeSide Side { get; set; }
    public required CryptoZoneStrength Strength { get; set; }

    // NULL for live zones; the EmulatorRun this zone belongs to during a backtest. Set at insert time
    // (ThreadSaveObjects) from GlobalData.CurrentEmulatorRunId so each run's zones stay isolated and a
    // finished run's zones can be reloaded for the chart. See ZoneDlz.LoadZonesForSymbol.
    public int? EmulatorRunId { get; set; }

    public required CandleTime OpenTime { get; set; } // Zone starts on this date, for limited types of zones
    public required decimal Top { get; set; }
    public required decimal Bottom { get; set; }
    public CandleTime? CloseTime { get; set; } // Zone ends on this date

    // Create a signal when this price triggers (once)
    public CandleTime? AlarmDate { get; set; }

    // Percentage of the zone or other text
    public string Description { get; set; } = "";

    public bool IsValid { get; set; }

    // Number of times a candle has wicked into this zone without breaking the body through it.
    // Recomputed from scratch on every CalculateZonesAsync cycle (no DB persistence needed) and
    // incremented incrementally by realtime invalidation in ZoneFvg.ScanForNew. Used together
    // with MaxTouches to disqualify a zone once its liquidity is considered depleted
    // (supply/demand-school: 0=fresh, 1=tested, 2=weakening, 3+=avoid).
    [Computed]
    public int TouchCount { get; set; }

    // True once price has reached the 50% midpoint of the zone (ICT Consequent Encroachment).
    // Combined signals can optionally disqualify mitigated zones via DisqualifyOnMitigation.
    [Computed]
    public bool IsMitigated { get; set; }

    // SMC-only, in-memory bookkeeping for ZoneSmc's incremental mitigation pass: true while price is
    // currently within a CE excursion that has already been counted as a touch, so the next candle
    // doesn't double-count it. Not used by DLZ/FVG zones. Never persisted.
    [Computed]
    public bool InsideExcursion { get; set; }

    // SMC-only, in-memory bookkeeping: the impulse candle's OpenTime, i.e. the point after which
    // mitigation/touch counting starts for this zone (the base candles and the impulse's own wick
    // must not count). Lets ZoneSmc's incremental scan resume a zone's bookkeeping across calls
    // instead of replaying its whole history. Never persisted.
    [Computed]
    public CandleTime? MitigationStartTime { get; set; }

    public string ZoneText(string action)
    {
        return $"{Symbol.Name} {action} zone #{Id} {Kind} {Side} " +
            $"({OpenTime.ToLocalTime():yyyy-MM-dd HH:mm}, {Top:N8}, " +
            $"{CloseTime?.ToLocalTime():yyyy-MM-dd HH:mm}, {Bottom:N8}) " +
            $"{Description}";
    }
}

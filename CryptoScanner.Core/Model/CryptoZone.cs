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
    // Persisted to DB so the count survives scanner restarts; on restart the scanner replays
    // only candles after LastZoneCheckTime to catch up. Used together with MaxTouches to
    // disqualify a zone once its liquidity is considered depleted
    // (supply/demand-school: 0=fresh, 1=tested, 2=weakening, 3+=avoid).
    public int TouchCount { get; set; }

    // True once price has been at or past the middle of the zone. Renamed from IsMitigated on
    // 24-08-2026: "mitigated" is a term from the order-block school - the story being that the
    // unfilled orders that made this level are getting filled as price comes back, so the level has
    // less left to push with. That story is not what the code checks, and the name told nobody what
    // it measures. This one does: price reached the midpoint. Signals can leave such a zone alone
    // through CloseZonesPastMidpoint. Persisted to DB alongside TouchCount.
    public bool ReachedMidpoint { get; set; }

    // In-memory bookkeeping for the visit counting in ZoneInvalidation. Together they answer "is
    // this the same visit as the previous candle, and has it already been counted?".
    //
    // Two fields and not one flag, because the callers do not feed every candle: the broken-check
    // loops break out as soon as a candle cannot reach any zone, so the candle on which price LEFT
    // the zone is often never applied to it. A flag that has to be cleared by that candle therefore
    // stays set forever and every later test counts as the same visit. The candle time does not need
    // to be told: a visit is over as soon as the last candle seen inside is more than one candle ago.
    //
    // Renamed from InsideExcursion on 24-08-2026 - "excursion" said nothing about price being inside
    // a zone. Never persisted: after a restart the first candle inside a zone counts as a fresh
    // visit, which over-counts by at most one per zone.
    [Computed]
    public CandleTime? LastInsideCandle { get; set; }

    /// <inheritdoc cref="LastInsideCandle"/>
    [Computed]
    public bool VisitCounted { get; set; }

    // In-memory bookkeeping: the candle from which this zone starts counting visits. The order
    // blocks set it to the impulse candle so the base candles and the impulse's own wick do not
    // count as a test of the level they created. Null means "count from the zone's own OpenTime".
    // Renamed from MitigationStartTime on 24-08-2026, same reason as ReachedMidpoint. Never persisted.
    [Computed]
    public CandleTime? TouchCountingFrom { get; set; }

    public string ZoneText(string action)
    {
        return $"{Symbol.Name} {action} zone #{Id} {Kind} {Side} " +
            $"({OpenTime.ToLocalTime():yyyy-MM-dd HH:mm}, {Top:N8}, " +
            $"{CloseTime?.ToLocalTime():yyyy-MM-dd HH:mm}, {Bottom:N8}) " +
            $"{Description}";
    }
}

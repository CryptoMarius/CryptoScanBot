using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Model;


public class CryptoSymbolData
{
    /// <summary>
    /// Trend
    /// </summary>
    // Lock for trend data
    public SemaphoreSlim TrendLock { get; set; } = new(1, 1);

    // Primary and Secondary trend data (Dow Theory interpretation)
    public CryptoTrendData TrendPrimary = new();
    public CryptoTrendData TrendSecondary = new();
    // BOS/CHoCH trend data (Break of Structure / Change of Character) — separate slot,
    // see CryptoSymbolInterval for the rationale.
    public CryptoTrendData TrendBosPrimary = new();
    public CryptoTrendData TrendBosSecondary = new();


    /// <summary>
    /// Candles
    /// </summary>
    // Lock for manipulates candles
    public SemaphoreSlim CandleLock { get; set; } = new(1, 1);
    // Interval related data like candles, last candle fetched, zones
    public List<CryptoSymbolInterval> SymbolIntervalList { get; set; } = [];


    /// <summary>
    /// DlzAdmin
    /// </summary>
    // Guards zone list writes: CalculateZones holds it exclusively (WaitAsync),
    // ScanForNew tries non-blocking (Wait(0)) and skips if unavailable,
    // preventing concurrent OrderedList corruption.
    public SemaphoreSlim ZoneLock { get; } = new(1, 1);

    // For display in the symbol grid
    // These are the closest DLZ zones (calculated from all the zones)
    // The closest dlz zones (calculated from all the active interval zones)
    // Display only (an initial hidden column in the symbol grid)
    public CryptoZoneDistance DlzZoneDistance { get; } = new();


    public CryptoSymbolData()
    {
        SymbolIntervalList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = new()
            {
                Interval = interval,
                IntervalPeriod = interval.IntervalPeriod,
            };
            SymbolIntervalList.Add(symbolInterval);
        }
    }


    public CryptoSymbolInterval Get(CryptoIntervalPeriod intervalPeriod)
    {
        return SymbolIntervalList[(int)intervalPeriod];
    }

    public CryptoSymbolInterval Get(CryptoInterval interval)
    {
        return SymbolIntervalList[(int)interval.IntervalPeriod];
    }

    public void ResetFvgData()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.FvgZones.Reset();
        }
    }

    public void ResetDlzData()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.DlzZones.Reset();
        }
    }

    public void ResetSmcData()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.SmcZones.Clear();
        }
    }

    public void ResetTrendData()
    {
        TrendPrimary.Reset();
        TrendSecondary.Reset();
        TrendBosPrimary.Reset();
        TrendBosSecondary.Reset();
    }

}

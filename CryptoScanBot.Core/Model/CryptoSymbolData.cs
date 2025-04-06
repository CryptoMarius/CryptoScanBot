using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Model;


public class CryptoSymbolData
{
    /// <summary>
    /// Trend
    /// </summary>
    // Lock for trend data
    public SemaphoreSlim TrendLock { get; set; } = new(1, 1);
    // Primary and Secondary trend data
    public CryptoTrendData TrendPrimary = new();
    public CryptoTrendData TrendSecondary = new();


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
    // Avoid the accidental removal of candles
    public bool CalculatingZones { get; set; } = false;

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

    public void ResetTrendData()
    {
        TrendPrimary.Reset();
        TrendSecondary.Reset();
    }

}

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Account;

// Interval Trend
// Last calculated trend & date for a symbol/interval
// (only need for calculation when a candle is finished on a interval)
public class IntervalTrend
{
    public long? Time { get; set; }
    public DateTime? Date { get; set; }
    public CryptoTrendIndicator Trend { get; set; }


    public void Reset()
    {
        Time = null;
        Date = null;
        Trend = CryptoTrendIndicator.Sideways;
    }
}

// Symbol Trend - MarketTrend
// Last calculated trend & date for a symbol
// (only need to calculation when a candle is finished on a interval)
public class SymbolTrend
{
    public long? Date { get; set; }
    public float? Percentage { get; set; }

    // Trend data for each interval
    public List<IntervalTrend> IntervalList { get; set; } = [];

    public SymbolTrend()
    {
        IntervalList.Clear();
        foreach (var _ in GlobalData.IntervalList)
            IntervalList.Add(new());
    }

    public void Reset()
    {
        Date = null;
        Percentage = null;

        foreach (var symbolIntervalTrend in IntervalList)
            symbolIntervalTrend.Reset();
    }


    public IntervalTrend Get(CryptoIntervalPeriod intervalPeriod)
    {
        return IntervalList[(int)intervalPeriod];
    }
}

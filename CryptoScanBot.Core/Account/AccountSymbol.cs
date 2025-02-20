using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Account;


public class AccountSymbol
{
    public required string SymbolName { get; set; }

    public SymbolTrend TrendPrimary = new();
    public SymbolTrend TrendSecondary = new();
    // Lock the candlelist to manipulates candles
    public SemaphoreSlim TrendLock { get; set; } = new(1, 1);


    // Zone data for each interval
    public List<AccountSymbolInterval> SymbolIntervalDataList { get; set; } = [];


    // The closest dlz zones (calculated from all the active interval zones)
    // Display only (an initial hidden column in the symbol grid)
    public decimal? BestLongZone { get; internal set; } = 100m; // distance%
    public decimal? BestShortZone { get; internal set; } = 100m; // distance%


    public AccountSymbol()
    {
        SymbolIntervalDataList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            AccountSymbolInterval accountSymbolTrendData = new()
            {
                Interval = interval,
                IntervalPeriod = interval.IntervalPeriod,
            };
            SymbolIntervalDataList.Add(accountSymbolTrendData);
        }
    }


    public AccountSymbolInterval Get(CryptoIntervalPeriod intervalPeriod)
    {
        return SymbolIntervalDataList[(int)intervalPeriod];
    }

    public void ResetFvgData()
    {
        foreach (AccountSymbolInterval accountSymbolInterval in SymbolIntervalDataList)
            accountSymbolInterval.FvgZones.ResetZones();
    }

    public void ResetDlzData()
    {
        foreach (AccountSymbolInterval accountSymbolInterval in SymbolIntervalDataList)
            accountSymbolInterval.DlzZones.ResetZones();
    }

    public void ResetTrendData()
    {
        TrendPrimary.Reset();
        TrendSecondary.Reset();
    }

}

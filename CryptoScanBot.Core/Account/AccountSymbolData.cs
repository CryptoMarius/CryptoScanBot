using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Account;

public class AccountSymbolData
{
    public required string SymbolName { get; set; }

    // Markettrend cache (only need to recalcutate when a candle is finished on a interval)
    public long? MarketTrendDate { get; set; }
    public float? MarketTrendPercentage { get; set; }
    [Computed]
    // Lock the candlelist to manipulates candles
    public SemaphoreSlim TrendLock { get; set; } = new(1, 1);

    // Trend data for each interval
    public List<AccountSymbolIntervalData> SymbolIntervalDataList { get; set; } = [];

    // Display only (an initial hidden column in the symbol grid)
    // These are the closest zones (calculated from all the AccountInterval zones)
    public decimal? BestLongZone { get; internal set; } = 100m; // distance%
    public decimal? BestShortZone { get; internal set; } = 100m; // distance%


    public AccountSymbolData()
    {
        SymbolIntervalDataList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            AccountSymbolIntervalData accountSymbolTrendData = new()
            {
                Interval = interval,
                IntervalPeriod = interval.IntervalPeriod,
            };
            SymbolIntervalDataList.Add(accountSymbolTrendData);
        }
    }


    public AccountSymbolIntervalData GetAccountSymbolInterval(CryptoIntervalPeriod intervalPeriod)
    {
        return SymbolIntervalDataList[(int)intervalPeriod];
    }

    public void ResetFvgData()
    {
        foreach (AccountSymbolIntervalData accountSymbolInterval in SymbolIntervalDataList)
            accountSymbolInterval.FvgZones.ResetZones();
    }

    public void ResetDlzData()
    {
        foreach (AccountSymbolIntervalData accountSymbolInterval in SymbolIntervalDataList)
            accountSymbolInterval.DlzZones.ResetZones();
        //accountSymbolInterval.Zones.ResetSwingPointData(); Why?
    }

    public void ResetTrendData()
    {
        MarketTrendDate = null;
        MarketTrendPercentage = null;
        foreach (AccountSymbolIntervalData accountSymbolInterval in SymbolIntervalDataList)
            accountSymbolInterval.Trend.ResetTrendData();
    }

}

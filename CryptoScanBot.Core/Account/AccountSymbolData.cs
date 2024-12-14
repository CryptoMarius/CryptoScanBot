using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Account;

public class AccountSymbolData
{
    public required string SymbolName { get; set; }

    // The generated signals for each symbol
    //public List<CryptoSignal> SymbolSignalList { get; set; } = [];


    // Markettrend 
    public long? MarketTrendDate { get; set; }
    public float? MarketTrendPercentage { get; set; }


    [Computed]
    // Lock the candlelist to manipulates candles
    public SemaphoreSlim TrendLock { get; set; } = new(1, 1);

    // Trend data for each interval
    public List<AccountSymbolIntervalData> SymbolTrendDataList { get; set; } = [];

    // Active zones
    public SortedList<decimal, CryptoZone> ZoneListLong { get; set; } = new(new ListHelper.DuplicateKeyComparer<decimal>());
    public SortedList<decimal, CryptoZone> ZoneListShort { get; set; } = new(new ListHelper.DuplicateKeyComparer<decimal>());

    public AccountSymbolData()
    {
        SymbolTrendDataList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            AccountSymbolIntervalData accountSymbolTrendData = new()
            {
                Interval = interval,
                IntervalPeriod = interval.IntervalPeriod,
            };
            SymbolTrendDataList.Add(accountSymbolTrendData);
        }
    }


    public AccountSymbolIntervalData GetAccountSymbolIntervalData(CryptoIntervalPeriod intervalPeriod)
    {
        return SymbolTrendDataList[(int)intervalPeriod];
    }

    public void ResetZoneData()
    {
        // Does this work (for the reset every hour)
        ZoneListLong.Clear();
        ZoneListShort.Clear();

        foreach (AccountSymbolIntervalData accountSymbolIntervalData in SymbolTrendDataList)
        {
            accountSymbolIntervalData.TimeLastSwingPoint = null;
        }
    }

    public void ResetTrendData()
    {
        // Does this work (for the reset every hour)
        MarketTrendDate = null;
        MarketTrendPercentage = null;
    }

    public void Reset()
    {
        ResetZoneData();

        ResetTrendData();

        foreach (AccountSymbolIntervalData accountSymbolIntervalData in SymbolTrendDataList)
        {
            accountSymbolIntervalData.Reset();
        }
    }
}

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

    // Active zones (CloseTime = null)
    public List<CryptoZone> ZoneListLong { get; set; } = [];
    public List<CryptoZone> ZoneListShort { get; set; } = [];

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


    public AccountSymbolIntervalData GetAccountSymbolInterval(CryptoIntervalPeriod intervalPeriod)
    {
        return SymbolTrendDataList[(int)intervalPeriod];
    }

    public void ResetZoneData()
    {
        // Does this work (for the reset every hour)
        ZoneListLong.Clear();
        ZoneListShort.Clear();

        foreach (AccountSymbolIntervalData accountSymbolInterval in SymbolTrendDataList)
            accountSymbolInterval.ResetZoneData();
    }

    public void ResetTrendData()
    {
        // Does this work (for the reset every hour)
        MarketTrendDate = null;
        MarketTrendPercentage = null;

        foreach (AccountSymbolIntervalData accountSymbolInterval in SymbolTrendDataList)
            accountSymbolInterval.ResetTrendData();
    }

    public void Reset()
    {
        ResetZoneData();
        ResetTrendData();
    }

    public void SortZones()
    {
        ZoneListLong.Sort((zoneA, zoneB) => zoneB.Top.CompareTo(zoneA.Top)); // desc via Top
        ZoneListShort.Sort((zoneA, zoneB) => zoneA.Bottom.CompareTo(zoneB.Bottom)); // asc via Bottom
    }
}

using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Intern;

public class AccountSymbolData
{
    public required string SymbolName { get; set; }

    // The generated signals for each symbol
    //public List<CryptoSignal> SymbolSignalList { get; set; } = [];


    // Markettrend 
    public long? MarketTrendDate { get; set; }
    public float? MarketTrendPercentage { get; set; }


    // Trend data for each interval
    public List<AccountSymbolIntervalData> SymbolTrendDataList { get; set; } = [];


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


    public void Reset()
    {
        MarketTrendDate = null;
        MarketTrendPercentage = null;

        foreach (AccountSymbolIntervalData accountSymbolIntervalData in SymbolTrendDataList)
        {
            accountSymbolIntervalData.Reset();
        }
    }
}

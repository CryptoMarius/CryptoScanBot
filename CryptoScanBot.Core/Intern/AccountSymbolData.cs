using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Intern;

public class AccountSymbolData
{
    public required string SymbolName { get; set; }

    // The generated signals for each symbol
    public List<CryptoSignal> SymbolSignalList { get; set; } = [];


    // The markettrend percentage
    public long? MarketTrendDate { get; set; }
    public float? MarketTrendPercentage { get; set; }

    ///// <summary>
    ///// Datum dat we de laatste keer hebben gekocht
    ///// </summary>
    //public DateTime? LastTradeDate { get; set; }

    // The trend data for each interval
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
        => SymbolTrendDataList[(int)intervalPeriod];


    public void Reset()
    {
        foreach (AccountSymbolIntervalData accountSymbolIntervalData in SymbolTrendDataList)
            accountSymbolIntervalData.Reset();
    }
}

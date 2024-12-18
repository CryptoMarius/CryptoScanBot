using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Account;

public class AccountData
{
    // Pausing because of trading rules
    public PauseTradingRule PauseTrading { get; } = new();

    // Account data per quote
    // Key = QuoteName
    public Dictionary<string, AccountQuoteData> QuoteDataList { get; set; } = [];

    // Assets + locking
    // Key = assetName
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    public DateTime? LastRefreshAssets { get; set; } = null;
    public SortedList<string, CryptoAsset> AssetList { get; } = [];

    // The data for each symbol (trend)
    // Key = symbolName
    public Dictionary<string, AccountSymbolData> SymbolDataList { get; set; } = [];

    // Key = symbolName
    public SortedList<string, CryptoPosition> PositionList { get; } = [];



    /// <summary>
    /// Clear cached information (after change of exchange), assets, orders, trades and positions
    /// </summary>
    public void Clear()
    {
        PauseTrading.Clear();

        AssetList.Clear();
        LastRefreshAssets = null;

        QuoteDataList.Clear();

        PositionList.Clear();
        SymbolDataList.Clear();
    }


    private AccountQuoteData GetQuoteData(string quoteName)
    {
        if (!QuoteDataList.TryGetValue(quoteName, out AccountQuoteData? quoteData))
        {
            quoteData = new() { QuoteName = quoteName };
            QuoteDataList.TryAdd(quoteName, quoteData);
        }
        return quoteData;
    }


    public BarometerData GetBarometer(string quoteName, CryptoIntervalPeriod intervalPeriod)
    {
        AccountQuoteData quoteData = GetQuoteData(quoteName);
        return quoteData.BarometerDataList[intervalPeriod];
    }


    public PauseBarometer GetPauseRule(string quoteName, CryptoTradeSide side)
    {
        AccountQuoteData quoteData = GetQuoteData(quoteName);
        return quoteData.PauseBarometerList[side];
    }


    public AccountSymbolData GetSymbolData(string symbolName)
    {
        if (!SymbolDataList.TryGetValue(symbolName, out AccountSymbolData? symbolData))
        {
            symbolData = new() { SymbolName = symbolName };
            SymbolDataList.TryAdd(symbolName, symbolData);
        }
        return symbolData;
    }


    public AccountSymbolIntervalData GetSymbolTrendData(string symbolName, CryptoIntervalPeriod intervalPeriod)
    {
        AccountSymbolData symbolData = GetSymbolData(symbolName);
        return symbolData.GetAccountSymbolInterval(intervalPeriod);
    }


    //public static void GetAssetsFromDatabase(CryptoDatabase database, CryptoPosition position)
    //{
    //    // De parts
    //    string sql = string.Format("select * from asset where TradeAccountId={0}", accountId);
    //    foreach (CryptoPositionPart part in database.Connection.Query<CryptoPositionPart>(sql))
    //    {
    //        if (part.IntervalId.HasValue && GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
    //            part.Interval = interval!;
    //        AddPositionPart(position, part);
    //    }

    //}

}

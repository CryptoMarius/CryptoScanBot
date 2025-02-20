using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Account;

public class AccountData
{
    // Pausing because of trading rules
    public PauseTradingRule PauseTrading { get; } = new();

    // Account data per quote for the barometer and pauzing rules
    // Key = QuoteName
    public Dictionary<string, AccountQuote> QuoteDataList { get; set; } = [];


    // Assets + locking (unused as we are aiming for Altrady as platform)
    // Key = assetName
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    public DateTime? LastRefreshAssets { get; set; } = null;
    public SortedList<string, CryptoAsset> AssetList { get; } = [];

    // Symbol data like trend and zones
    // Key = symbolName
    public Dictionary<string, AccountSymbol> SymbolDataList { get; set; } = [];

    // Open positions Key = symbolName
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


    private AccountQuote GetQuoteData(string quoteName)
    {
        if (!QuoteDataList.TryGetValue(quoteName, out AccountQuote? quoteData))
        {
            quoteData = new() { QuoteName = quoteName };
            QuoteDataList.TryAdd(quoteName, quoteData);
        }
        return quoteData;
    }


    public BarometerData GetBarometer(string quoteName, CryptoIntervalPeriod intervalPeriod)
    {
        AccountQuote quoteData = GetQuoteData(quoteName);
        return quoteData.BarometerDataList[intervalPeriod];
    }


    public PauseBarometer GetPauseRule(string quoteName, CryptoTradeSide side)
    {
        AccountQuote quoteData = GetQuoteData(quoteName);
        return quoteData.PauseBarometerList[side];
    }


    public AccountSymbol GetSymbolData(string symbolName)
    {
        if (!SymbolDataList.TryGetValue(symbolName, out AccountSymbol? symbolData))
        {
            symbolData = new() { SymbolName = symbolName };
            SymbolDataList.TryAdd(symbolName, symbolData);
        }
        return symbolData;
    }


    //public AccountSymbolInterval GetSymbolTrendData(string symbolName, CryptoIntervalPeriod intervalPeriod)
    //{
    //    AccountSymbol symbolData = GetSymbolData(symbolName);
    //    return symbolData.Get(intervalPeriod);
    //}


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

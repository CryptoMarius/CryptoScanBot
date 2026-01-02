using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Model;

public class CryptoExchangeData
{
    // Trade rulez
    // Pausing because of trading rules
    public PauseTradingRule PauseTrading { get; } = new();


    // Quotes
    // Account data per quote for the barometer and pauzing rules
    // Key = QuoteName
    public Dictionary<string, CryptoQuoteData> QuoteDataList { get; set; } = [];


    // Assets
    // Assets + locking (unused as we are aiming for AltradyStandard as platform)
    // Key = assetName
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    public DateTime? LastRefreshAssets { get; set; } = null;
    public SortedList<string, CryptoAsset> AssetList { get; } = [];


    // Open positions
    // Open positions Key = symbolName
    // (for speed we have removed this from the symbol data)
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
    }


    private CryptoQuoteData GetQuoteData(string quoteName)
    {
        if (!QuoteDataList.TryGetValue(quoteName, out CryptoQuoteData? quoteData))
        {
            quoteData = new() { Name = quoteName };
            QuoteDataList.TryAdd(quoteName, quoteData);
        }
        return quoteData;
    }


    public CryptoBarometerData GetBarometer(string quoteName, CryptoIntervalPeriod intervalPeriod)
    {
        CryptoQuoteData quoteData = GetQuoteData(quoteName);
        return quoteData.BarometerDataList[intervalPeriod];
    }


    public CryptoPauseBarometer GetPauseRule(string quoteName, CryptoTradeSide side)
    {
        CryptoQuoteData quoteData = GetQuoteData(quoteName);
        return quoteData.PauseBarometerList[side];
    }


    //public CryptoSymbolData GetSymbolData(string symbolName)
    //{
    //    if (!SymbolDataList.TryGetValue(symbolName, out CryptoSymbolData? symbolData))
    //    {
    //        symbolData = new() { SymbolName = symbolName };
    //        SymbolDataList.TryAdd(symbolName, symbolData);
    //    }
    //    return symbolData;
    //}


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

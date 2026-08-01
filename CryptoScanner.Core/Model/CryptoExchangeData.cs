using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using System.Collections.Concurrent;

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

    // Guards the lazy creation of QuoteDataList entries in GetQuoteData. The emulator processes the
    // symbols of one minute in parallel, and several share the same quote (e.g. USDT), so they would
    // otherwise concurrently insert into this non-thread-safe Dictionary and corrupt it.
    private readonly object quoteDataLock = new();


    // Assets
    // Assets + locking (unused as we are aiming for Altrady as platform)
    // Key = assetName
    // ConcurrentDictionary (was SortedList): asset updates arrive from several threads at once
    // (exchange balance callbacks, PaperAssets.Change on order fills, PositionMonitor reads), and
    // SortedList is not thread-safe - a concurrent Add/Remove could resize its internal arrays while
    // another thread's TryGetValue was mid-read, occasionally handing back a matched key with a still-
    // null value slot (NullReferenceException in PositionMonitor.HandleEntryPart). AssetListSemaphore
    // is kept for the compound multi-asset transactions in PaperAssets.Change/CreateAsset - that's
    // about business-logic atomicity, not collection safety.
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    public DateTime? LastRefreshAssets { get; set; } = null;
    public ConcurrentDictionary<string, CryptoAsset> AssetList { get; } = new();


    // Open positions
    // Open positions Key = symbolName
    // (for speed we have removed this from the symbol data)
    // ConcurrentDictionary (was SortedList): the emulator processes symbols in parallel, so positions
    // are added/looked up/removed from several threads at once. Keys are per symbol, so different
    // threads never touch the same entry; the concurrent collection just keeps the structural
    // operations (TryAdd/TryRemove/enumerate) safe. No code relied on the sorted order (only the
    // Telegram ShowPositions display, which is cosmetic).
    public ConcurrentDictionary<string, CryptoPosition> PositionList { get; } = new();



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
        // Lock the whole read+lazy-add: a Dictionary is unsafe for a concurrent read while another
        // thread is inserting, so reads must be inside the lock too. Few quotes + trivial body, so
        // contention is negligible. Without this the emulator's parallel symbol processing corrupts
        // QuoteDataList (InvalidOperationException in Dictionary.TryInsert).
        lock (quoteDataLock)
        {
            if (!QuoteDataList.TryGetValue(quoteName, out CryptoQuoteData? quoteData))
            {
                quoteData = new() { Name = quoteName };
                QuoteDataList[quoteName] = quoteData;
            }
            return quoteData;
        }
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

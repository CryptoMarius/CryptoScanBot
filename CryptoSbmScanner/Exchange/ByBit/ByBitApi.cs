using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.Spot;
using Bybit.Net.Objects.Models.V5;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Bybit;


public class BybitApi
{
    // Binance stuff
    static private BybitStreamUserData? TaskBybitStreamUserData { get; set; }
    static private BybitStreamPriceTicker? TaskBybitStreamPriceTicker { get; set; }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    // Default opties voor deze exchange
    public static void SetExchangeDefaults()
    {
        //BinanceClient.SetDefaultOptions(new BinanceClientOptions() { });
        //BinanceSocketClientOptions options = new();
        //options.SpotStreamsOptions.AutoReconnect = true;
        //options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
        //BinanceSocketClient.SetDefaultOptions(options);
    }


    static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BybitUserAssetInfo> balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            foreach (var assetInfo in balances)
            {
                // TODO, verder uitzoeken (lijkt de verkeerde info te zijn)
                //if (assetInfo.Total > 0)
                //{
                //    if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                //    {
                //        asset = new CryptoAsset();
                //        asset.Quote = assetInfo.Asset;
                //        tradeAccount.AssetList.Add(asset.Quote, asset);
                //    }
                //    asset.Free = assetInfo.Available;
                //    asset.Total = assetInfo.Total;
                //    asset.Locked = assetInfo.Locked;

                //    if (asset.Total == 0)
                //        tradeAccount.AssetList.Remove(asset.Quote);
                //}
            }
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
    }

    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitSpotUserTradeV3 item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.Id;
        trade.OrderId = item.OrderId;
        //trade.OrderListId = (long)item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#1trade QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;

        trade.TradeTime = item.TradeTime;

        if (item.IsBuyer)
            trade.Side = CryptoOrderSide.Buy;
        else
            trade.Side = CryptoOrderSide.Sell;
    }


    //static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceStreamOrderUpdate item)
    //{
    //    trade.TradeAccount = tradeAccount;
    //    trade.TradeAccountId = tradeAccount.Id;
    //    trade.Exchange = symbol.Exchange;
    //    trade.ExchangeId = symbol.ExchangeId;
    //    trade.Symbol = symbol;
    //    trade.SymbolId = symbol.Id;

    //    trade.TradeId = item.TradeId;
    //    trade.OrderId = item.Id;
    //    //trade.OrderListId = item.OrderListId;

    //    trade.Price = item.Price;
    //    trade.Quantity = item.Quantity;
    //    trade.QuoteQuantity = item.Price * item.Quantity;
    //    // enig debug werk, soms wordt het niet ingevuld!
    //    //if (item.QuoteQuantity == 0)
    //    //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#2stream QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

    //    trade.Commission = item.Fee;
    //    trade.CommissionAsset = item.FeeAsset;

    //    trade.TradeTime = item.EventTime;

    //    if (item.Side == OrderSide.Buy)
    //        trade.Side = CryptoOrderSide.Buy;
    //    else
    //        trade.Side = CryptoOrderSide.Sell;
    //}


    public static async Task FetchAssets(CryptoTradeAccount tradeAccount)
    {
        // We onderteunen momenteel enkel de exchange "binance"
        //if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab("Reading asset information from Bybit");

                BybitWeights.WaitForFairWeight(1);

                using (var client = new BybitClient())
                {
                   var accountInfo = await client.V5Api.Account.GetAssetInfoAsync();

                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo == null | accountInfo.Data == null)
                        throw new ExchangeException("Geen account data ontvangen");

                    try
                    {
                        PickupAssets(tradeAccount, accountInfo.Data.Assets);
                        GlobalData.AssetsHaveChanged("");
                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
    }

    public static void StartUserDataStream()
    {
        TaskBybitStreamUserData = new BybitStreamUserData();
        var _ = Task.Run(async () => { await TaskBybitStreamUserData.ExecuteAsync(); });
    }
    public static async Task StopUserDataStream()
    {
        if (TaskBybitStreamUserData != null)
            await TaskBybitStreamUserData?.StopAsync();
        TaskBybitStreamUserData = null;
    }
    public static void ResetUserDataStream()
    {
        // niets, hmm
    }


    public static void StartPriceTicker()
    {
        // Deze methode werkt alleen op Binance
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<string> symbolNames = new();
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    // We krijgen soms timeouts van Binance (eigenlijk de library) omdat we teveel 
                    // symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                    //int splitCount = 200;
                    //if (symbols.Count > splitCount)
                    //    splitCount = 1 + (symbols.Count / 2);

                    //raar..
                    while (symbols.Count > 0)
                    {
                        //BybitStream1mCandles bybitStream1mCandles = new(quoteData);

                        // Bybit heeft een limiet van 10 symbols (blijkbaar, wat onhandig)
                        symbolNames.Clear();
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            symbols.Remove(symbol);
                            symbolNames.Add(symbol.Name);

                            if (symbolNames.Count >= 10)
                                break;
                        }

                        // opvullen tot circa 150 coins?
                        //ExchangeStream1mCandles.Add(bybitStream1mCandles);
                        //await bybitStream1mCandles.StartAsync(); // bewust geen await

                        TaskBybitStreamPriceTicker = new BybitStreamPriceTicker();
                        _ = Task.Run(async () => { await TaskBybitStreamPriceTicker.ExecuteAsync(symbolNames); });
                    }
                }
            }
        }
    }
    public static async Task StopPriceTicker()
    {
        if (TaskBybitStreamPriceTicker != null)
            await TaskBybitStreamPriceTicker?.StopAsync();
        TaskBybitStreamPriceTicker = null;
    }
    public static void ResetPriceTickerStream()
    {
        if (TaskBybitStreamPriceTicker != null)
            TaskBybitStreamPriceTicker.tickerCount = 0;
    }
    public static int CountPriceTickerStream()
    {
        if (TaskBybitStreamPriceTicker == null)
            return 0;
        else
            return TaskBybitStreamPriceTicker.tickerCount;
    }


    public static List<BybitStream1mCandles> ExchangeStream1mCandles { get; set; } = new();

    public static async Task Start1mCandleStream()
    {
        // Deze methode werkt alleen op Binance
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    // We krijgen soms timeouts van Binance (eigenlijk de library) omdat we teveel 
                    // symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                    //int splitCount = 200;
                    //if (symbols.Count > splitCount)
                    //    splitCount = 1 + (symbols.Count / 2);

                    while (symbols.Count > 0)
                    {
                        BybitStream1mCandles bybitStream1mCandles = new(quoteData);

                        // Bybit heeft een limiet van 10 symbols (blijkbaar, wat onhandig)
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            symbols.Remove(symbol);

                            bybitStream1mCandles.symbols.Add(symbol.Name);

                            // Ergens een lijn trekken? 
                            if (bybitStream1mCandles.symbols.Count >= 10)
                                break;
                        }

                        // opvullen tot circa 150 coins?
                        ExchangeStream1mCandles.Add(bybitStream1mCandles);
                        await bybitStream1mCandles.StartAsync(); // bewust geen await
                    }
                }
            }
        }
    }
    public static async Task Stop1mCandleStream()
    {
        foreach (var exchangeStream1mCandles in ExchangeStream1mCandles)
            await exchangeStream1mCandles?.StopAsync();
        ExchangeStream1mCandles.Clear();
    }
    public static void Reset1mCandleStream()
    {
        foreach (var exchangeStream1mCandles in ExchangeStream1mCandles)
            exchangeStream1mCandles.CandlesKLinesCount = 0;
    }
    public static int Count1mCandleStream()
    {
        // Tel het aantal ontvangen 1m candles (via alle uitstaande streams)
        // Elke minuut komt er van elke munt een candle (indien er gehandeld is).
        int candlesKLineCount = 0;
        foreach (var exchangeStream1mCandles in ExchangeStream1mCandles)
            candlesKLineCount += exchangeStream1mCandles.CandlesKLinesCount;
        return candlesKLineCount;
    }
}

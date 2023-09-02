using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models;

namespace CryptoSbmScanner.Exchange.Kraken;

public class Api : ExchangeBase
{
    public static readonly string ExchangeName = "Kraken";
#if TRADEBOT
    static private UserDataStream TaskBybitStreamUserData { get; set; }
#endif
    public static List<KLineTickerItem> TickerList { get; set; } = new();


    public Api() : base() //, CryptoSymbol symbol, DateTime currentDate
    {
    }

    public override void ExchangeDefaults()
    {
        GlobalData.AddTextToLogTab($"{ExchangeName} defaults");

        // Default opties voor deze exchange
        KrakenRestClient.SetDefaultOptions(options =>
        {
            if (GlobalData.Settings.ApiKey != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        KrakenSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.Settings.ApiKey != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        ExchangeHelper.PriceTicker = new PriceTicker();
        ExchangeHelper.KLineTicker = new KLineTicker();
#if TRADEBOT
        //ExchangeHelper.UserData = new UserData();
#endif
    }

    public async override Task FetchSymbolsAsync()
    {
        await FetchSymbols.ExecuteAsync();
    }
    public async override Task FetchCandlesAsync()
    {
        await FetchCandles.ExecuteAsync();
    }

#if TRADEBOT
    //// Converteer de orderstatus van Exchange naar "intern"
    //public static CryptoOrderType LocalOrderType(SpotOrderType orderType)
    //{
    //    CryptoOrderType localOrderType = orderType switch
    //    {
    //        SpotOrderType.Market => CryptoOrderType.Market,
    //        SpotOrderType.Limit => CryptoOrderType.Limit,
    //        SpotOrderType.StopLoss => CryptoOrderType.StopLimit,
    //        SpotOrderType.StopLossLimit => CryptoOrderType.Oco,
    //        _ => throw new Exception("Niet ondersteunde ordertype"),
    //    };

    //    return localOrderType;
    //}

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Niet ondersteunde orderside"),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.Pending => CryptoOrderStatus.New,
            OrderStatus.Open => CryptoOrderStatus.Filled,
            //OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public async Task<(bool result, TradeParams tradeParams)> BuyOrSell(CryptoDatabase database,
        CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit)
    {
        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, text, price, quantity));
            return (false, null);
        }

        TradeParams tradeParams = new()
        {
            CreateTime = currentDate,
            OrderSide = orderSide,
            OrderType = orderType,
            Price = price,
            StopPrice = stop, // OCO - the price at which the limit order to sell is activated
            LimitPrice = limit, // OCO - the lowest price that the trader is willing to accept
            Quantity = quantity,
            QuoteQuantity = price * quantity,
            //OrderId = 0,
        };
        if (orderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = (decimal)tradeParams.StopPrice * tradeParams.Quantity;
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return (true, tradeParams);
        }


        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        using KrakenRestClient client = new();
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)

        WebCallResult<KrakenPlacedOrder> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                        OrderType.Market, quantity);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = 12345; // long.Parse(result.Data.OrderIds); dat wordt een conversie!
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                    OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GTC);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = 12345; // long.Parse(result.Data.OrderIds); dat wordt een conversie!
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    // wordt het nu wel of niet ondersteund? Het zou ook een extra optie van de limit kunnen (zie wel een tp)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category.Linear, symbol.Name, side, NewOrderType.Market,
                    //    quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
                    throw new Exception("${orderType} not supported");
                }
            case CryptoOrderType.Oco:
                {
                    // Een OCO is afwijkend ten opzichte van een standaard buy or sell
                    //    Bij Binance was een OCO totaal afwijkend ten opzichte van een standaard buy or sell
                    //    het had ook andere parameters en results
                    //WebCallResult<BybitOrderOcoList> result;?????
                    //    throw new Exception("${orderType} not supported");
                    throw new Exception("${orderType} not supported");
                }
            default:
                throw new Exception("${orderType} not supported");
        }

        /*
        exchange.create_order(
    symbol='BTC/USDT:USDT',
    type='limit',
    side='sell',
    amount=0.01,
    price=21100,
    params={
        'leverage': 1,
        'stopLossPrice': 20950,
        'takeProfitPrice': 21950,
    },
)
         */
    }


    public static async Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoPositionStep step)
    {
        // Order gegevens overnemen (enkel voor een eventuele error dump)
        TradeParams tradeParams = new()
        {
            CreateTime = step.CreateTime,
            OrderSide = step.Side,
            OrderType = step.OrderType,
            Price = step.Price, // the sell part (can also be a buy)
            StopPrice = step.StopPrice, // OCO - the price at which the limit order to sell is activated
            LimitPrice = step.StopLimitPrice, // OCO - the lowest price that the trader is willing to accept
            Quantity = step.Quantity,
            QuoteQuantity = step.Price * step.Quantity,
            OrderId = step.OrderId,
            Order2Id = step.Order2Id,
        };
        // Eigenlijk niet nodig
        if (step.OrderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = (decimal)tradeParams.StopPrice * tradeParams.Quantity;

        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId.HasValue)
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
            using var client = new KrakenRestClient();
            var result = await client.SpotApi.Trading.CancelOrderAsync(symbol.Name, step.OrderId.ToString());
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            }
            return (true, tradeParams);
        }

        return (false, tradeParams);
    }

        static public void PickupAssets(CryptoTradeAccount tradeAccount, Dictionary<string, KrakenBalanceAvailable> balances)
        {
            tradeAccount.AssetListSemaphore.Wait();
            try
            {
                foreach (var assetInfo in balances.Values)
                {
                    // TODO, verder uitzoeken (lijkt de verkeerde info te zijn)
                    if (assetInfo.Available > 0)
                    {
                        //if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                        //{
                        //    asset = new CryptoAsset();
                        //    asset.Quote = assetInfo.Asset;
                        //    tradeAccount.AssetList.Add(asset.Quote, asset);
                        //}
                        //asset.Free = assetInfo.Available;
                        //asset.Total = assetInfo.Total;
                        //asset.Locked = assetInfo.Locked;

                        //if (asset.Total == 0)
                        //    tradeAccount.AssetList.Remove(asset.Quote);
                    }
                }
            }
            finally
            {
                tradeAccount.AssetListSemaphore.Release();
        }
    }

    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, KrakenUserTrade item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        //trade.TradeId = long.Parse(item.TradeId); // todo, dat is waarschijnlijk conversie voor nodig
        trade.OrderId = long.Parse(item.OrderId);
        //trade.OrderListId = (long)item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#1trade QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = item.Fee;
        trade.CommissionAsset = symbol.Quote; // item.FeeAsset;?

        trade.TradeTime = item.Timestamp;

        if (item.Side == OrderSide.Buy)
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


    public override async Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        await FetchTrades.FetchTradesForSymbol(tradeAccount, symbol);
    }

    public async override Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {Api.ExchangeName}");

                LimitRates.WaitForFairWeight(1);

                using var client = new KrakenRestClient();
                {
                    var accountInfo = await client.SpotApi.Account.GetAvailableBalancesAsync();

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
                        PickupAssets(tradeAccount, accountInfo.Data);
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
        TaskBybitStreamUserData = new UserDataStream();
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
#endif

}

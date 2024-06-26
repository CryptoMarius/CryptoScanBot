﻿using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.Socket;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Exchange.BybitFutures;

public class Api : ExchangeBase
{
    private static readonly Category Category = Category.Linear;


    public override void ExchangeDefaults()
    {
        ExchangeOptions.ExchangeName = "Bybit Futures";
        ExchangeOptions.LimitAmountOfSymbols = false;
        ExchangeOptions.SubscriptionLimitSymbols = 10;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default opties voor deze exchange
        BybitRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BybitSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;

            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
            options.V5Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        ExchangeHelper.PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        ExchangeHelper.KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
#if TRADEBOT
        ExchangeHelper.UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);
#endif
    }

    public async override Task GetSymbolsAsync()
    {
        await GetSymbols.ExecuteAsync();
    }

    public async override Task GetCandlesAsync()
    {
        await GetCandles.ExecuteAsync();
    }

#if TRADEBOT
    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.LimitMaker => CryptoOrderType.StopLimit, /// ????????????????????????????????????????????????
            _ => throw new Exception("Niet ondersteunde ordertype"),
        };

        return localOrderType;
    }

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
    public static CryptoOrderStatus LocalOrderStatus(Bybit.Net.Enums.V5.OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            Bybit.Net.Enums.V5.OrderStatus.New => CryptoOrderStatus.New,
            Bybit.Net.Enums.V5.OrderStatus.Filled => CryptoOrderStatus.Filled,
            Bybit.Net.Enums.V5.OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //Bybit.Net.Enums.V5.OrderStatus.Expired => CryptoOrderStatus.Expired,
            Bybit.Net.Enums.V5.OrderStatus.Cancelled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public static async Task<bool> DoSwitchCrossIsolatedMarginAsync(BybitRestClient client, CryptoSymbol symbol)
    {
        //await client.V5Api.Account.SetLeverageAsync(Category, symbol.Name, 1, 1);
        //await client.V5Api.Account.SetMarginModeAsync(Category, symbol.Name, TradeMode.Isolated);
        Bybit.Net.Enums.TradeMode tradeMode = (Bybit.Net.Enums.TradeMode)GlobalData.Settings.Trading.CrossOrIsolated; // toevallig dezelfde volgorde
        GlobalData.AddTextToLogTab($"{symbol.Name} Setting CrossOrIsolated={tradeMode} leverage={GlobalData.Settings.Trading.Leverage}");

        var result = await client.V5Api.Account.SwitchCrossIsolatedMarginAsync(Category, symbol.Name,
            tradeMode: tradeMode, 
            buyLeverage: GlobalData.Settings.Trading.Leverage, 
            sellLeverage: GlobalData.Settings.Trading.Leverage);
        if (!result.Success)
        {
            // Aanpassing zonder dat er daadwerkelijk iets aangepast is
            // 110026: Cross / isolated margin mode is not modified
            if (result.Error.Code == 110026)
                return true; // {110026: Cross/isolated margin mode is not modified }
            if (result.Error.Code == 110027)
                return true; // {110027	Margin is not modified }

            GlobalData.AddTextToLogTab($"{symbol.Name} ERROR setting CrossOrIsolated={tradeMode} en leverage={GlobalData.Settings.Trading.Leverage} {result.Error}");
        }

        // Vanwege een onverwachte liquidatie gaan we deze ook uitgebreid loggen
        string text = JsonSerializer.Serialize(result, ExchangeHelper.JsonSerializerNotIndented);
        GlobalData.AddTextToLogTab("SwitchCrossIsolatedMarginAsync :" + text);


        // Niet te controleren????
        //if (result.Success)
        //{
        //    await client.V5Api.Account.GetSpotMarginStatusAndLeverageAsync();

        //        //BybitSpotMarginLeverageStatus
        //}


        return result.Success;
    }


    public override async Task<(bool result, TradeParams tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, CryptoTradeSide tradeSide, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit)
    {
        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!position.Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", position.Symbol.Name, text, price, quantity));
            return (false, null);
        }

        TradeParams tradeParams = new()
        {
            Purpose = part.Purpose,
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
        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return (true, tradeParams);
        }


        // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)

        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        using BybitRestClient client = new();
        if (!await DoSwitchCrossIsolatedMarginAsync(client, position.Symbol))
        {
            // Herhaal
            if (!await DoSwitchCrossIsolatedMarginAsync(client, position.Symbol))
            {
                // Herhaal
                if (!await DoSwitchCrossIsolatedMarginAsync(client, position.Symbol))
                    return (false, null); // raise? throw?
            }
        }


        WebCallResult<BybitOrderId> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    // JA, price * quantity omdat dat blijkbaar zo moet, zie voorbeeld (onderin)
                    // https://bybit-exchange.github.io/docs/v5/order/create-order
                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side,
                        NewOrderType.Market, quantity: quantity, price: null, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.OrderId;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    // Even tijdelijk (kan ook alleen maar op deze plek als je weet dat we ALLEEN long gaan!)
                    bool reduce = false;
                    if (tradeSide == CryptoTradeSide.Long && orderSide == CryptoOrderSide.Sell)
                        reduce = true;
                    if (tradeSide == CryptoTradeSide.Short && orderSide == CryptoOrderSide.Buy)
                        reduce = true;

                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side, 
                        NewOrderType.Limit, quantity: quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false, reduceOnly: reduce);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.OrderId;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    // wordt het nu wel of niet ondersteund? Het zou ook een extra optie van de limit kunnen (zie wel een tp)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category, symbol.Name, side, NewOrderType.Market,
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


    public override async Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Order gegevens overnemen (enkel voor een eventuele error dump)
        TradeParams tradeParams = new()
        {
            Purpose = part.Purpose,
            CreateTime = step.CreateTime,
            OrderSide = step.Side,
            OrderType = step.OrderType,
            Price = step.Price, // the buy or sell price
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

        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
            using var client = new BybitRestClient();
            var result = await client.V5Api.Trading.CancelOrderAsync(Category, position.Symbol.Name, step.OrderId.ToString());
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;

                // If its already gone ignore the error
                if (result.Error.Code == 110001) // 110001: Order does not exist
                    return (true, tradeParams);
            }
            return (result.Success, tradeParams);
        }

        return (false, tradeParams);
    }

    static public void PickupAssets(CryptoTradeAccount tradeAccount, BybitAllAssetBalances balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                // remove assets with total=0
                foreach (var asset in tradeAccount.AssetList.Values.ToList())
                {
                    asset.Total = 0;
                }

                foreach (var assetInfo in balances.Balances)
                {
                    if (assetInfo.WalletBalance > 0)
                    {
                        if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                        {
                            asset = new()
                            {
                                Name = assetInfo.Asset,
                                TradeAccountId = tradeAccount.Id
                            };
                            tradeAccount.AssetList.Add(asset.Name, asset);
                        }
                        asset.Total = (decimal)assetInfo.WalletBalance;
                        asset.Locked = (decimal)assetInfo.WalletBalance - (decimal)assetInfo.TransferBalance;
                        asset.Free = asset.Total - asset.Locked;

                        if (asset.Id == 0)
                            databaseThread.Connection.Insert(asset, transaction);
                        else
                            databaseThread.Connection.Update(asset, transaction);
                    }
                }

                // remove assets with total=0
                foreach (var asset in tradeAccount.AssetList.Values.ToList())
                {
                    if (asset.Total == 0)
                    {
                        databaseThread.Connection.Delete(asset, transaction);
                        tradeAccount.AssetList.Remove(asset.Name);
                    }
                }

                transaction.Commit();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                // Als er ooit een rolback plaatsvindt is de database en objects in het geheugen niet meer in sync..
                transaction.Rollback();
                throw;
            }
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
    }


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitUserTrade item)
    {
        trade.TradeTime = item.Timestamp;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId;
        trade.OrderId = item.OrderId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = item.Fee.Value;
        trade.CommissionAsset = symbol.Quote; // item.FeeAsset;?
    }


    public override async Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        return await GetTrades.FetchTradesForSymbolAsync(database, position);
    }

    static public void PickupOrder(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, Bybit.Net.Objects.Models.V5.BybitOrderUpdate item)
    {
        order.CreateTime = item.CreateTime;
        order.UpdateTime = item.UpdateTime;

        order.TradeAccount = tradeAccount;
        order.TradeAccountId = tradeAccount.Id;
        order.Exchange = symbol.Exchange;
        order.ExchangeId = symbol.ExchangeId;
        order.Symbol = symbol;
        order.SymbolId = symbol.Id;

        order.OrderId = item.OrderId;
        order.Type = LocalOrderType(item.OrderType);
        order.Side = LocalOrderSide(item.Side);
        order.Status = LocalOrderStatus(item.Status);

        order.Price = item.Price.Value;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price.Value * item.Quantity;

        order.AveragePrice = item.AveragePrice;
        order.QuantityFilled = item.QuantityFilled;
        order.QuoteQuantityFilled = item.AveragePrice * item.QuantityFilled;

        order.Commission = item.ExecutedFee.Value;
        order.CommissionAsset = item.FeeAsset;
    }


    public override async Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.GetOrdersForPositionAsync: Positie {position.Symbol.Name}");
        // Behoorlijk weinig error control ...... 

        int count = 0;
        DateTime? from; // = position.Symbol.LastOrderFetched;
        //if (from == null)
        // altijd alles ophalen, geeft wat veel logging, maar ach..
        from = position.CreateTime.AddMinutes(-1);

        ScannerLog.Logger.Trace($"GetOrdersForPositionAsync {position.Symbol.Name} fetching orders from exchange {from}");

        using var client = new BybitRestClient();
        var info = await client.V5Api.Trading.GetOrderHistoryAsync(
            Category.Spot, symbol: position.Symbol.Name, 
            startTime: from);

        
        if (info.Success && info.Data != null)
        {
            string text;
            foreach (var item in info.Data.List)
            {
                if (position.Symbol.OrderList.TryGetValue(item.OrderId, out CryptoOrder order))
                {
                    var oldStatus = order.Status;
                    var oldQuoteQuantityFilled = order.QuoteQuantityFilled;
                    PickupOrder(position.TradeAccount, position.Symbol, order, (BybitOrderUpdate)item);
                    database.Connection.Update<CryptoOrder>(order);

                    if (oldStatus != order.Status || oldQuoteQuantityFilled != order.QuoteQuantityFilled)
                    {
                        ScannerLog.Logger.Trace($"GetOrdersForPositionAsync {position.Symbol.Name} updated order {item.OrderId}");
                        text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                        ScannerLog.Logger.Trace($"{item.Symbol} order updated json={text}");
                        count++;
                    }
                }
                else
                {
                    text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                    ScannerLog.Logger.Trace($"{item.Symbol} order added json={text}");

                    order = new();
                    PickupOrder(position.TradeAccount, position.Symbol, order, (BybitOrderUpdate)item);
                    database.Connection.Insert<CryptoOrder>(order);
                    GlobalData.AddOrder(order);
                    count++;
                }

                if (position.Symbol.LastOrderFetched == null || order.CreateTime > position.Symbol.LastOrderFetched)
                    position.Symbol.LastOrderFetched = order.CreateTime;
            }
            database.Connection.Update<CryptoSymbol>(position.Symbol);
        }
        else
            GlobalData.AddTextToLogTab($"Error reading orders from {ExchangeOptions.ExchangeName} for {position.Symbol.Name} {info.Error}");

        return count;
    }

    public async override Task GetAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeOptions.ExchangeName}");

                LimitRates.WaitForFairWeight(1);

                using var client = new BybitRestClient();
                {
                    var accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Contract);
                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo == null | accountInfo.Data == null)
                        throw new ExchangeException("No account data received");

                    try
                    {
                        PickupAssets(tradeAccount, accountInfo.Data);
                        GlobalData.AssetsHaveChanged("");
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
    }

#endif

}
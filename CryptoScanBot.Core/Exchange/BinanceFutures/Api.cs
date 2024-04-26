using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
//using Binance.Net.Objects.Models.Spot;
//using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;


namespace CryptoScanBot.Core.Exchange.BinanceFutures;




//Mogelijke foutmeldingen bij het kopen (of verkopen?):

//Te laag bedrag
//Filter failure: MIN_NOTIONAL
//price* quantity is too low to be a valid order for the symbol.

//Problemen met het aantal decimalen (zowel price als amount)
//buyResult.Error = {-1111: Precision is over the maximum defined for this asset. }

//Er is te weinig geld om de order te plaatsen
//buyResult.Error = {-1013: Filter failure: PRICE_FILTER }

// buyResult.Error = { -1013: Filter failure: LOT_SIZE }

//The relationship of the prices for the orders is not correct". The prices set in the OCO is breaking the Price rules.
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price < Last Price < Stop Price
// mooit overzicht: https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md


// Vanwege "The relationship of the prices for the orders is not correct." The prices set in the OCO 
// is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)

//"The relationship of the prices for the orders is not correct." The prices set in the OCO is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price<Last Price<Stop Price

//De prijs is dan ondertussen al onder de StopPrice beland?


//The relationship of the prices for the orders is not correct."	The prices set in the OCO is breaking the Price rules.
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price < Last Price < Stop Price
// https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md

/*
 * 
https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-servertime
https://api-testnet.bybit.com/v2/public/time
{"ret_code":0,"ret_msg":"OK","result":{},"ext_code":"","ext_info":"","time_now":"1688116858.760925"}

https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-announcement
https://api-testnet.bybit.com/v2/public/announcement
{"ret_code":0,"ret_msg":"OK","result":[],"ext_code":"","ext_info":"","time_now":"1688116961.886013"}
(dat lijkt nogal op die eerste..)


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querykline
https://api-testnet.bybit.com/v2/public/kline/list
{"retCode":10001,"retMsg":"The requested symbol is invalid.","result":{},"retExtInfo":{},"time":1688117090806}
https://api-testnet.bybit.com/v2/public/kline/list?symbol=BTCUSDT&interval=1


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querysymbol
https://api-testnet.bybit.com/spot/v3/public/symbols
(denk om de versie verschillen)

 */

public class Api : ExchangeBase
{
    public override void ExchangeDefaults()
    {
        ExchangeOptions.ExchangeName = "Binance Futures";
        ExchangeOptions.LimitAmountOfSymbols = false;
        ExchangeOptions.SubscriptionLimitSymbols = 200;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default opties voor deze exchange
        BinanceRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BinanceSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;

            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds

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
    public static CryptoOrderType LocalOrderType(FuturesOrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            FuturesOrderType.Market => CryptoOrderType.Market,
            FuturesOrderType.Limit => CryptoOrderType.Limit,
            //FuturesOrderType.StopLoss => CryptoOrderType.StopLimit,
            //FuturesOrderType.StopLossLimit => CryptoOrderType.Oco, // negatief gevuld (denk ik)
            //FuturesOrderType.LimitMaker => CryptoOrderType.Oco, // postief gevuld
            _ => throw new Exception("Niet ondersteunde ordertype " + orderType.ToString()),
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
            _ => throw new Exception("Niet ondersteunde orderside " + orderSide.ToString()),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus " + orderStatus.ToString()),
        };

        return localOrderStatus;
    }


    //public override async Task<(bool succes, TradeParams tradeParams)> BuyOrSell(CryptoDatabase database,
    //    CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate,
    //    CryptoOrderType orderType, CryptoOrderSide orderSide,
    //    decimal quantity, decimal price, decimal? stop, decimal? limit)
    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, CryptoTradeSide tradeSide, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.PlaceOrder {symbol.Name}");
        // debug
        //GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, "not at this moment", price, quantity));
        //return (false, null);


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
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;
        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
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
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)
        using BinanceRestClient client = new();

        switch (orderType)
        {
            case CryptoOrderType.Market:
                { 
                    WebCallResult<BinanceUsdFuturesOrder> result;
                    result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        FuturesOrderType.Market, quantity);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = result.Data.CreateTime;
                        tradeParams.OrderId = result.Data.Id.ToString();
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    WebCallResult<BinanceUsdFuturesOrder> result;
                    result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        FuturesOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = result.Data.CreateTime;
                        tradeParams.OrderId = result.Data.Id.ToString();
                    }
                    return (result.Success, tradeParams);
                }
            //case CryptoOrderType.StopLimit:
            //    {
            //        WebCallResult<BinanceUsdFuturesOrder> result;
            //        result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol.Name, side,
            //            FuturesOrderType.StopLossLimit, quantity, price: price, stopPrice: stop, timeInForce: TimeInForce.GoodTillCanceled);
            //        if (!result.Success)
            //        {
            //            tradeParams.Error = result.Error;
            //            tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            //        }
            //        if (result.Success && result.Data != null)
            //        {
            //            tradeParams.CreateTime = result.Data.CreateTime;
            //            tradeParams.OrderId = result.Data.Id.ToString();
            //        }
            //        return (result.Success, tradeParams);
            //    }
            //case CryptoOrderType.Oco:
            //    {
            //        WebCallResult<BinanceUsdFuturesOrder> result;
            //        result = await client.UsdFuturesApi.Trading.PlaceOcoOrderAsync(symbol.Name, side,
            //            quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);

            //        if (!result.Success)
            //        {
            //            tradeParams.Error = result.Error;
            //            tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            //        }
            //        if (result.Success && result.Data != null)
            //        {
            //            // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
            //            // De 1e order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
            //            // De 2e order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
            //            // De ene order heeft een price/stopprice, de andere enkel een price (combi)
            //            BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
            //            BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();
            //            tradeParams.CreateTime = result.Data.TransactionTime; // order1.CreateTime;
            //            tradeParams.OrderId = order1.Id.ToString();
            //            tradeParams.Order2Id = order2.Id.ToString(); // Een 2e ordernummer (welke eigenlijk?)
            //        }
            //        return (result.Success, tradeParams);
            //    }
            default:
                throw new Exception("${orderType} not supported");
        }
    }


    public override async Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Order gegevens overnemen (voor een eventuele error dump)
        TradeParams tradeParams = new()
        {
            Purpose = part.Purpose,
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

        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1);
            using var client = new BinanceRestClient();
            var result = await client.UsdFuturesApi.Trading.CancelOrderAsync(position.Symbol.Name, long.Parse(step.OrderId));
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            }
            return (result.Success, tradeParams);
        }

        return (false, tradeParams);
    }

    static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BinanceFuturesAccountAsset> balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                foreach (var assetInfo in balances)
                {
                    if (assetInfo.WalletBalance > 0)
                    {
                        if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                        {
                            asset = new CryptoAsset()
                            {
                                Name = assetInfo.Asset,
                                TradeAccountId = tradeAccount.Id,
                            };
                            tradeAccount.AssetList.Add(asset.Name, asset);
                        }
                        asset.Free = assetInfo.AvailableBalance;
                        asset.Total = assetInfo.WalletBalance;
                        asset.Locked = asset.Total - asset.Free;

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

    //static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BinanceStreamBalance> balances)
    //{
    //    tradeAccount.AssetListSemaphore.Wait();
    //    {
    //        try
    //        {
    //            foreach (var assetInfo in balances)
    //            {
    //                if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
    //                {
    //                    asset = new CryptoAsset()
    //                    {
    //                        Name = assetInfo.Asset,
    //                    };
    //                    tradeAccount.AssetList.Add(asset.Name, asset);
    //                }
    //                asset.Free = assetInfo.Available;
    //                asset.Total = assetInfo.Total;
    //                asset.Locked = assetInfo.Locked;
    //            }

    //            // remove assets with total=0
    //            foreach (var asset in tradeAccount.AssetList.Values.ToList())
    //            {
    //                if (asset.Total == 0)
    //                {
    //                    //TODO: Save in database?

    //                    tradeAccount.AssetList.Remove(asset.Name);
    //                }
    //            }

    //        }
    //        finally
    //        {
    //            tradeAccount.AssetListSemaphore.Release();
    //        }
    //    }
    //}


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceFuturesUsdtTrade item)
    {
        trade.TradeTime = item.Timestamp;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.Id.ToString();
        trade.OrderId = item.OrderId.ToString();
    
        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;
    }


    public override async Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        return await BinanceFetchTrades.FetchTradesForSymbolAsync(database, position);
    }
	
	
    static public void PickupOrder(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, BinanceFuturesStreamOrderUpdateData item)
    {
        order.CreateTime = item.UpdateTime; //  CreateTime; ????????????????
        order.UpdateTime = item.UpdateTime;

        order.TradeAccount = tradeAccount;
        order.TradeAccountId = tradeAccount.Id;
        order.Exchange = symbol.Exchange;
        order.ExchangeId = symbol.ExchangeId;
        order.Symbol = symbol;
        order.SymbolId = symbol.Id;

        order.OrderId = item.OrderId.ToString();
        order.Type = LocalOrderType(item.Type);
        order.Side = LocalOrderSide(item.Side);
        order.Status = LocalOrderStatus(item.Status);

        order.Price = item.Price;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price * item.Quantity;

        order.AveragePrice = item.Price;
        order.QuantityFilled = item.AccumulatedQuantityOfFilledTrades;
        order.QuoteQuantityFilled = item.Price * item.AccumulatedQuantityOfFilledTrades;

        order.Commission = item.Fee;
        order.CommissionAsset = item.FeeAsset;
    }


    public override Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult(0);
    }


    public async override Task GetAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        //ScannerLog.Logger.Trace($"Exchange.Binance.GetAssetsForAccountAsync: Positie {tradeAccount.Name}");
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeOptions.ExchangeName}");

                LimitRates.WaitForFairWeight(1);

                using var client = new BinanceRestClient();
                {
                    WebCallResult<BinanceFuturesAccountInfo> accountInfo = await client.UsdFuturesApi.Account.GetAccountInfoAsync();
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
                        Api.PickupAssets(tradeAccount, accountInfo.Data.Assets);
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

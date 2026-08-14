using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OKX.Net;
using OKX.Net.Enums;
using OKX.Net.Clients;

namespace CryptoScanner.Core.Exchange.Okx.Futures;

public class Api : ExchangeBase
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        //Asset = new Asset();
        Candle = new Candle(this);
        Symbol = new Symbol();
        //Order = new Order();
        //Trade = new Trade();
    }


    public override IDisposable GetClient()
    {
        return new OKXRestClient();
    }

    public override void ExchangeDefaults()
    {
        // OKX allows subscribing to multiple instruments in one message (the combined channel list may be
        // up to 64 KB), so there is no need for a websocket connection per symbol.
        ExchangeOptions.SetDefaultOptions("Okx Futures", "USDT", 300, false, 50);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        OKXEnvironment environment = OKXEnvironment.Live;

        // Default opties voor deze exchange
        OKXRestClient.SetDefaultOptions(options =>
        {
            // Endpoints global
            options.Environment = environment;
            //UnifiedRestAddress = "https://www.okx.com",
            //UnifiedSocketAddress = "wss://ws.okx.com:8443",

            //options.Environment = OKXEnvironment.Europe;
            // Endpoints Europe (and not working properly)
            //UnifiedRestAddress = "https://eea.okx.com",
            //UnifiedSocketAddress = "wss://wseea.okx.com:8443",

            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new OKXCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        OKXSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.Environment = environment;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
            //options.Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            //options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new OKXCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        OKXExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
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
        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return (true, tradeParams);
        }

        throw new Exception("PlaceOrder not implemented");
        //OrderSide side;
        //if (orderSide == CryptoOrderSide.Buy)
        //    side = OrderSide.Buy;
        //else
        //    side = OrderSide.Sell;


        //// Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        ////BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)
        //using OKXRestClient client = new();

        //switch (orderType)
        //{
        //    //case CryptoOrderType.Market:
        //    //    {
        //    //        HttpResult<BinanceUsdFuturesOrder> result;
        //    //        result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
        //    //            FuturesOrderType.Market, quantity);
        //    //        if (!result.Success)
        //    //        {
        //    //            tradeParams.Error = result.Error;
        //    //            tradeParams.ResponseStatusCode = result.ResponseStatusCode;
        //    //        }
        //    //        if (result.Success && result.Data != null)
        //    //        {
        //    //            tradeParams.CreateTime = result.Data.CreateTime;
        //    //            tradeParams.OrderId = result.Data.Id.ToString();
        //    //        }
        //    //        return (result.Success, tradeParams);
        //    //    }
        //    //case CryptoOrderType.Limit:
        //    //    {
        //    //        HttpResult<BinanceUsdFuturesOrder> result;
        //    //        result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
        //    //            FuturesOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
        //    //        if (!result.Success)
        //    //        {
        //    //            tradeParams.Error = result.Error;
        //    //            tradeParams.ResponseStatusCode = result.ResponseStatusCode;
        //    //        }
        //    //        if (result.Success && result.Data != null)
        //    //        {
        //    //            tradeParams.CreateTime = result.Data.CreateTime;
        //    //            tradeParams.OrderId = result.Data.Id.ToString();
        //    //        }
        //    //        return (result.Success, tradeParams);
        //    //    }
        //    ////case CryptoOrderType.StopLimit:
        //    ////    {
        //    ////        HttpResult<BinanceUsdFuturesOrder> result;
        //    ////        result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol.Name, side,
        //    ////            FuturesOrderType.StopLossLimit, quantity, price: price, stopPrice: stop, timeInForce: TimeInForce.GoodTillCanceled);
        //    ////        if (!result.Success)
        //    ////        {
        //    ////            tradeParams.Error = result.Error;
        //    ////            tradeParams.ResponseStatusCode = result.ResponseStatusCode;
        //    ////        }
        //    ////        if (result.Success && result.Data != null)
        //    ////        {
        //    ////            tradeParams.CreateTime = result.Data.CreateTime;
        //    ////            tradeParams.OrderId = result.Data.Id.ToString();
        //    ////        }
        //    ////        return (result.Success, tradeParams);
        //    ////    }
        //    ////case CryptoOrderType.Oco:
        //    ////    {
        //    ////        HttpResult<BinanceUsdFuturesOrder> result;
        //    ////        result = await client.UsdFuturesApi.Trading.PlaceOcoOrderAsync(symbol.Name, side,
        //    ////            quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);

        //    ////        if (!result.Success)
        //    ////        {
        //    ////            tradeParams.Error = result.Error;
        //    ////            tradeParams.ResponseStatusCode = result.ResponseStatusCode;
        //    ////        }
        //    ////        if (result.Success && result.Data != null)
        //    ////        {
        //    ////            // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
        //    ////            // De 1e order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
        //    ////            // De 2e order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
        //    ////            // De ene order heeft een price/stopprice, de andere enkel een price (combi)
        //    ////            BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
        //    ////            BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();
        //    ////            tradeParams.CreateTime = result.Data.TransactionTime; // order1.CreateTime;
        //    ////            tradeParams.OrderId = order1.Id.ToString();
        //    ////            tradeParams.Order2Id = order2.Id.ToString(); // Een 2e ordernummer (welke eigenlijk?)
        //    ////        }
        //    ////        return (result.Success, tradeParams);
        //    ////    }
        //    default:
        //        throw new Exception("${orderType} not supported");
        //}
    }


    public override async Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
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
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            return (true, tradeParams);

        throw new Exception("Cancel not implemented");

        //// Annuleer de order
        //if (step.OrderId != null && step.OrderId != "")
        //{
        //    // BinanceWeights.WaitForFairBinanceWeight(1);
        //    using var client = new OKXRestClient();
        //    var result = await client.UsdFuturesApi.Trading.CancelOrderAsync(position.Symbol.Name, long.Parse(step.OrderId));
        //    if (!result.Success)
        //    {
        //        tradeParams.Error = result.Error;
        //        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
        //    }
        //    return (result.Success, tradeParams);
        //}

        //return (false, tradeParams);
    }

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "OKEX",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/OKEX_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = null,
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=OKEX:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://my.okx.com/trade-spot/{BASE}-{QUOTE}",
            }
        };
    }
}
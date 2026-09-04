using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OKX.Net;
using OKX.Net.Clients;

namespace CryptoScanner.Core.Exchange.Okx.Perpetual;

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
        // 18 billion USDT over 431 pairs a day (14-08-2026), 100 symbols stay above the boundary
        ExchangeOptions.SetDefaultOptions("Okx Perpetual", "USDT", 300, false, 50, minimalVolume: 6_100_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        OKXEnvironment environment = OKXEnvironment.Live;

        // Default options for this exchange
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
            // Switched OFF on 18-08-2026 (zero disables the check: SocketConnection only starts its timeout
            // task when Parameters.Timeout > 0). This watchdog measured the wrong thing on a kline feed -
            // it closes a socket that received no MARKET DATA for a while, and an illiquid coin simply is
            // not traded for minutes on end. On Coinbase the twelve quietest coins went 31 to 81 minutes
            // without a trade. Liveness is already covered, and better: the library pings the socket every
            // 10 seconds and aborts it when the pong does not arrive within 10 seconds
            // (SocketApiClient.KeepAliveInterval / KeepAliveTimeout, both 10s by default), which works
            // whether or not there is any trading. SubscriptionManager.MaximumTickerInactivity is the outer
            // net for a socket that stays up but stops delivering.
            options.SocketNoDataTimeout = TimeSpan.Zero;
            //options.Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            //options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new OKXCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

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


        // Check the maximum and minimum amount limits and the quantity
        if (!position.Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {text} (debug={price} {quantity})");
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


        //// Place an order on the exchange (they look alike, but it is slightly different every time)
        ////BinanceWeights.WaitForFairBinanceWeight(1); nonsense for that single tick (no repetition, right?)
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
        //    ////            // The 1st order is the stop loss (recognisable by "type": "STOP_LOSS")
        //    ////            // The 2nd order is the normal sell (recognisable by "type": "LIMIT_MAKER")
        //    ////            // One order has a price/stop price, the other one only a price (combined)
        //    ////            BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
        //    ////            BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();
        //    ////            tradeParams.CreateTime = result.Data.TransactionTime; // order1.CreateTime;
        //    ////            tradeParams.OrderId = order1.Id.ToString();
        //    ////            tradeParams.Order2Id = order2.Id.ToString(); // A 2nd order number (which one exactly?)
        //    ////        }
        //    ////        return (result.Success, tradeParams);
        //    ////    }
        //    default:
        //        throw new Exception("${orderType} not supported");
        //}
    }


    public override async Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Order details carried over for a possible error dump
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
        // Not really needed
        if (step.OrderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            return (true, tradeParams);

        throw new Exception("Cancel not implemented");

        //// Cancel the order
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
                // OKEXF, not OKEX: that is the spot exchange, which is where these links used to end up
                Code = "OKEXF",
                Execute = CryptoExternalUrlType.Internal,
                // The _SWAP suffix is part of the symbol name at Altrady, without it the market does not
                // open. Note the order: Altrady puts the quote first, so it cannot be built from
                // {EXCHANGENAME} ("SHIB-USDT-SWAP") either.
                Url = "https://app.altrady.com/d/OKEXF_{QUOTE}_{BASE}_SWAP:{interval}",
                //https://app.altrady.com/dashboard#/d/OKEXF_USDT_SHIB_SWAP?resolution=5
            },
            HyperTrader = null,
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                // OKX, not OKEX: TradingView renamed the exchange and moved the symbols with it
                Url = "https://www.tradingview.com/chart/?symbol=OKX:{BASE}{QUOTE}.P&interval={interval}",
            },
            ExchangeUrl = new()
            {
                // The instrument is named BTC-USDT-SWAP here, so base + quote is not enough.
                // Not verified from the Netherlands: my.okx.com is the European entity and it
                // bounces every perpetual page back to its home page.
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.okx.com/trade-swap/{exchangename}",
            },

            // The X-Perps live in this market too, and the outside world does not name them the way
            // it names the swaps. Only what differs is stated here.
            //
            // Which symbols count as an X-Perp for the links is read from the instrument name, not
            // from the product: the TradFi product covers a share in both contract forms, and
            // AAPL-USD_UM_XPERP-310613 is as much an X-Perp as AAVE-USD_UM_XPERP-310704. Until
            // 04-09-2026 the 49 TradFi X-Perps were sent down the swap templates and opened nothing.
            LinkProductOf = symbol => symbol.ExchangeName.Contains("_UM_XPERP", StringComparison.OrdinalIgnoreCase)
                ? CryptoProduct.XPerp
                : null,
            PerProduct =
            {
                [CryptoProduct.XPerp] = new()
                {
                    Altrady = new()
                    {
                        Code = "OKEXF",
                        Execute = CryptoExternalUrlType.Internal,
                        // Altrady rebuilds the instrument name of Okx: "APR-USD_UM_XPERP-310815"
                        // becomes APR_UM-XPERP-310815, and the quote it files it under is USDC.
                        // Confirmed against their own code for APRUSDC on 28-08-2026, and the same
                        // shape holds for every one of the 155 contracts.
                        Url = "https://app.altrady.com/d/OKEXF_{QUOTE}_{BASE}_UM-XPERP-{expiry}:{interval}",
                    },
                    TradingView = new()
                    {
                        // TradingView follows the USD_UM family of Okx: AAPLUSD.UM, XAUUSD.UM,
                        // BTCUSD.UM. Not USDC and not .P, checked on 28-08-2026 through their symbol
                        // search.
                        //
                        // The search was not the whole story: BTCUSD.UM is the family, and a family
                        // opens an empty chart. The chart wants the contract, which is the family
                        // plus the futures month code and the year of the expiry - ACTUSD.UMN2031 for
                        // ACT-USD_UM_XPERP-310704 - verified on the chart on 04-09-2026 after every
                        // X-Perp link had come up empty (see SettingsLinks.ExpiryCodeOf).
                        Execute = CryptoExternalUrlType.External,
                        Url = "https://www.tradingview.com/chart/?symbol=OKX:{BASE}USD.UM{EXPIRYCODE}&interval={interval}",
                    },
                    ExchangeUrl = new()
                    {
                        // Okx serves these from trade-futures, not trade-swap: they are filed under
                        // InstrumentType.Futures whatever their behaviour says.
                        Execute = CryptoExternalUrlType.External,
                        Url = "https://www.okx.com/trade-futures/{exchangename}",
                    },
                },
            },
        };
    }
}
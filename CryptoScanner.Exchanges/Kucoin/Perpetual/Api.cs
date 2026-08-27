using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Kucoin.Net;
using Kucoin.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kucoin.Perpetual;


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
        return new KucoinRestClient();
    }

    public override void ExchangeDefaults()
    {
        // 1083 million USDT over 660 contracts a day (14-08-2026), 274 contracts stay above the boundary.
        // USDT instead of USDC: only 5 of the 665 contracts are quoted in USDC and together they trade
        // 1.6 million a day, so that side of the exchange is nearly empty (and its boundary was 530).
        // The candle limit is 200: that is what the futures kline endpoint returns per call, whatever
        // larger window we ask for (the spot side really does hand over 1500).
        // Same story as HyperLiquid Spot but far milder, so a real boundary is enough here instead of
        // switching the check off. Measured over the night of 21/22-08-2026, on runs of 1m candles with
        // volume zero: 24 of the 102 symbols that went quiet went over five minutes, 4 went over fifteen
        // and the worst was 28 minutes. The default of five minutes woke the watchdog 54 times that
        // night and cost 46 partial restart rounds of 6 subscriptions each. Forty-five minutes clears
        // the measured worst case with room to spare.
        ExchangeOptions.SetDefaultOptions("Kucoin Perpetual", "USDT", 200, true, 1, 20, KlineDelivery.TimerFlush, minimalVolume: 370_000, pauseSymbol: "XBTUSDT",
            maximumTickerInactivity: TimeSpan.FromMinutes(45));
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        KucoinRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new KucoinCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        KucoinSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.RequestTimeout = TimeSpan.FromSeconds(60); // standard=20 seconds
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
            //options.V5Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            //options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            options.SocketSubscriptionsCombineTarget = 20;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new KucoinCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price)
        //{
        //    Enabled = false // many many errors
        //};
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);


        KucoinExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }

    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // Check the maximum and minimum amount limits and the quantity
        if (!position.Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {text} (debug={price} {quantity})");
            return Task.FromResult<(bool result, TradeParams? tradeParams)>((false, null));
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
            return Task.FromResult<(bool result, TradeParams? tradeParams)>((true, tradeParams));
        }

        // Real trading is not supported here, the same as in Kucoin Spot. What stood here placed the
        // order through client.SpotApi.Trading with the scanner name of the symbol, so it addressed the
        // spot exchange with a contract name it does not know. There is no user ticker, order or trade
        // implementation for Kucoin either, so an order that did arrive would never be followed up.
        throw new Exception("PlaceOrder not implemented");
    }

    public override Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Order details carried over (only for a possible error dump)
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
            return Task.FromResult<(bool succes, TradeParams? tradeParams)>((true, tradeParams));

        // Cancelling went through client.SpotApi.Trading as well, which cannot know an order that was
        // never placed on the spot exchange. See the remark in PlaceOrder.
        throw new Exception("Cancel not implemented");
    }

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "KUCNF",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/KUCNF_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = null,
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=KUCOIN:{BASE}{QUOTE}.P&interval={interval}",
            },
            ExchangeUrl = new()
            {
                // The instrument is named XBTUSDTM here, so base + quote is not enough
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.kucoin.com/futures/trade/{EXCHANGENAME}",
            }
        };
    }
}

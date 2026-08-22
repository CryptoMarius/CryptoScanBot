using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Mexc.Net;
using Mexc.Net.Clients;

namespace CryptoScanner.Core.Exchange.Mexc.Futures;

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
        return new MexcRestClient();
    }

    public override void ExchangeDefaults()
    {
        // 16551 million USDT over 986 contracts a day (14-08-2026), 73 contracts stay above the boundary.
        // The candle limit is 2000: that is what the futures kline endpoint returns per call, whatever
        // larger window we ask for (the spot side stops at 500).
        // One symbol per subscription because the library only offers a single-symbol overload for the
        // futures kline stream; 20 of those share one socket client.
        ExchangeOptions.SetDefaultOptions("Mexc Futures", "USDT", 2000, true, 1, 20, KlineDelivery.TimerFlush, minimalVolume: 5_600_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default options for this exchange
        MexcRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new MexcCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        MexcSocketClient.SetDefaultOptions(options =>
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

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new MexcCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        // UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        MexcExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // Check the limits of the maximum and minimum amount and the quantity
        if (!position.Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", position.Symbol.Name, text, price, quantity));
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
        };
        if (orderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return Task.FromResult<(bool result, TradeParams? tradeParams)>((true, tradeParams));
        }

        // Real trading cannot be built here at all: Mexc has had every futures order endpoint marked
        // "(Under maintenance)" since 25-07-2022 - placing, cancelling and the trigger orders. Only
        // the query endpoints answer, which is exactly enough for a scanner and nothing more. That is
        // also why trading platforms state that Mexc has no futures.
        throw new Exception("PlaceOrder not implemented");
    }


    public override Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
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
        if (step.OrderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            return Task.FromResult<(bool succes, TradeParams? tradeParams)>((true, tradeParams));

        // See the remark in PlaceOrder
        throw new Exception("Cancel not implemented");
    }

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            // Altrady does not offer Mexc futures, and cannot: the order endpoints of the exchange
            // are closed, so there is nothing for a trading platform to connect to.
            Altrady = null,
            HyperTrader = null,
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=MEXC:{BASE}{QUOTE}.P&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://futures.mexc.com/exchange/{BASE}_{QUOTE}",
            }
        };
    }
}

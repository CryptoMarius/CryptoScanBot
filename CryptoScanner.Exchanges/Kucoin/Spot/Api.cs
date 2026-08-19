using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Kucoin.Net;
using Kucoin.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kucoin.Spot;


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
        // 30 million USDC over 57 pairs a day (14-08-2026), 22 symbols stay above the boundary
        ExchangeOptions.SetDefaultOptions("Kucoin Spot", "USDC", 1500, true, 1, 20, KlineDelivery.TimerFlush, minimalVolume: 10_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");


        // Default options for this exchange
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
            // whether or not there is any trading. SubscriptionManager.MaximumTickerSilence is the outer
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
        CryptoOrderType orderType, CryptoOrderSide orderSide, decimal quantity,
        decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // Same default as the exchanges that do implement this. Paper trading needs no exchange
        // call at all, only a filled TradeParams so the caller can create the position step.
        // Returning (false, null) meant no step was ever created and the position stayed Waiting
        // for good - silently, because a null tradeParams also skips the error dump.
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
        };
        if (orderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return Task.FromResult<(bool result, TradeParams? tradeParams)>((true, tradeParams));
        }

        throw new Exception("PlaceOrder not implemented");
    }


    public override Task<(bool succes, TradeParams? tradeParams)> Cancel(
        CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
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

        throw new Exception("Cancel not implemented");
    }


    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "KUCN",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/KUCN_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "hypertrader://kucoin/{BASE}-{QUOTE}/{interval}",
                Telegram = "http://www.ccscanner.nl/hypertrader/?e=kucoin&a={BASE}&b={QUOTE}&i={interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=KUCOIN:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.kucoin.com/trade/{QUOTE}/{BASE}&interval={interval}",
            }
        };
    }
}
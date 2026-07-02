using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Kraken.Net;
using Kraken.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kraken.Futures;

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
        return new KrakenRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.SetDefaultOptions("Kraken Futures", "USD", 720, false, 5, klineDelivery: KlineDelivery.TimerFlush);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        KrakenRestClient.SetDefaultOptions(options =>
        {
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new KrakenCredentials().WithFutures(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        KrakenSocketClient.SetDefaultOptions(options =>
        {
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new KrakenCredentials().WithFutures(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        KrakenExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // not implemented
        return Task.FromResult<(bool succes, TradeParams? tradeParams)>((false, null));
    }


    public override Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // not implemented
        return Task.FromResult<(bool succes, TradeParams? tradeParams)>((false, null));
    }

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "KRKNF",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/KRKNF_{QUOTE}_{BASE}:{interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=KRAKEN.P:{BASE}{QUOTE}&interval={interval}",
            },
        };
    }
}
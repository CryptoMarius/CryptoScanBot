using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OKX.Net;
using OKX.Net.Clients;

namespace CryptoScanner.Core.Exchange.Okx.Spot;

public class Api : ExchangeBase
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        Candle = new Candle(this);
        Symbol = new Symbol();
        //Asset = new Asset();
        //Order = new Order();
        //Trade = new Trade();
    }


    public override IDisposable GetClient()
    {
        return new OKXRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.SetDefaultOptions("Okx Spot", "USDT", 300, false, 1);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default opties voor deze exchange
        OKXRestClient.SetDefaultOptions(options =>
        {
            //options.Environment = environment; OKXEnvironment.Live;

            //options.OutputOriginalData = true;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        OKXSocketClient.SetDefaultOptions(options =>
        {
            //options.Environment = environment;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        OKXExchange.RateLimiter.RateLimitTriggered += (x) =>
        {
            GlobalData.AddTextToLogTab($"RateLimitTriggered {x.Limit} {x.ApiLimit} {x.LimitDescription} {x.Current} {x.Behaviour}");
            //if (x.Behaviour == RateLimitingBehaviour.Wait && x.DelayTime.HasValue)
            //    Thread.Sleep(x.DelayTime.Value);
            //Thread.Sleep(1000);
        };
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part,
        DateTime currentDate, CryptoOrderType orderType, CryptoOrderSide orderSide,
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
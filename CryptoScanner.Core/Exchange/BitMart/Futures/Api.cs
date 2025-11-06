using BitMart.Net;
using BitMart.Net.Clients;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BitMart.Futures;

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
        return new BitMartRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.SetDefaultOptions("BitMart Futures", "USDT", 500, false, 1);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        BitMartRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        BitMartSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        BitMartExchange.RateLimiter.RateLimitTriggered += (x) =>
        {
            GlobalData.AddTextToLogTab($"RateLimitTriggered {x.Limit} {x.ApiLimit} {x.LimitDescription} {x.Current} {x.Behaviour}");
            if (x.Behaviour == RateLimitingBehaviour.Wait && x.DelayTime.HasValue)
                Thread.Sleep(x.DelayTime.Value * 1000);
            Thread.Sleep(1000);
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
                Code = "BITMF",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/BITMF_{QUOTE}_{BASE}:{interval}",
            },
        };
    }
}
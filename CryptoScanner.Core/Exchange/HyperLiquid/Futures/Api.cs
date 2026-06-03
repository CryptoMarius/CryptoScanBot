using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using HyperLiquid.Net;
using HyperLiquid.Net.Clients;


namespace CryptoScanner.Core.Exchange.HyperLiquid.Futures;

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
        return new HyperLiquidRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.SetDefaultOptions("HyperLiquid Futures", "USDC", 300, false, 1);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        HyperLiquidRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new HyperLiquidCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        HyperLiquidSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new HyperLiquidCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        HyperLiquidExchange.RateLimiter.RateLimitTriggered += (x) =>
        {
            GlobalData.AddTextToLogTab($"RateLimitTriggered {x.Limit} {x.ApiLimit} {x.LimitDescription} {x.Current} {x.Behaviour} ");
            //{x.DelayTime.Value.TotalSeconds}
            //if (x.Behaviour == RateLimitingBehaviour.Wait && x.DelayTime.HasValue)
            //{
                //x.DelayTime = 0.1 * x.DelayTime;
                //int delay = (int)Math.Round(x.DelayTime.Value.TotalSeconds * 10);
                //Thread.Sleep(delay);
                //await Task.Delay((int)Math.Round(x.DelayTime.Value.TotalSeconds * 1000));
                //x.Behaviour = RateLimitingBehaviour.
            //}
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
                Code = "HYPERLIQUIDF",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/HYPERLIQUIDF_{QUOTE}_{BASE}:{interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=HYPERLIQUIDF:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.hyperliquid.com/trade/{BASE}/{QUOTE}",
            }
        };
    }
}
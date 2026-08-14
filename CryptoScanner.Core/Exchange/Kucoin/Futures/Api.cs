using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Kucoin.Net;
using Kucoin.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kucoin.Futures;


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
        ExchangeOptions.SetDefaultOptions("Kucoin Futures", "USDT", 200, true, 1, 20, KlineDelivery.TimerFlush, minimalVolume: 370_000, pauseSymbol: "XBTUSDT");
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
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
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
        // Controleer de limiten van de maximum en minimum bedrag en de quantity
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
        // Order gegevens overnemen (enkel voor een eventuele error dump)
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
        };
    }
}

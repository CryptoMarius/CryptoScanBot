using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using HyperLiquid.Net;
using HyperLiquid.Net.Clients;


namespace CryptoScanner.Core.Exchange.HyperLiquid.Spot;

public class Api : ExchangeBase
{
    //private static readonly Category Category = Category.Spot;


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
        // 61 million USDC over 74 listed pairs a day (14-08-2026) - the whole spot side is smaller than
        // a single mid range coin on Binance. The old flat boundary left 4 symbols, this one keeps 18.
        // The thinnest market in the whole list, and thin enough that inactivity says nothing about the
        // health of a subscription. Measured over the night of 21/22-08-2026, on runs of 1m candles with
        // volume zero: of the 51 symbols that went quiet at all, 28 were quiet for over an hour, 16 for
        // over two hours and the worst for 598 minutes of a 605 minute window - a coin that traded once.
        // With the default of five minutes the scanner tore down and rebuilt all six of its bundles 57
        // times that night, once every seven minutes from the first check to the last, and paid for it
        // with the worst gap picture of all nineteen markets (49 missing minutes over 19 symbols, where
        // the runner-up had 12). Twelve hours is deliberately longer than any run: on this market the
        // liveness check is the library ping every 10 seconds, and this is only the outer net for a
        // socket that stays up and stops delivering for half a day.
        //
        // SubscriptionsPerBundle is raised for the same reason as on HyperLiquid Perpetual, and the two
        // markets share the budget: HyperLiquid allows ten websocket connections PER IP ADDRESS and a
        // thousand subscriptions, and one symbol per subscription is forced by the exchange. On the
        // default of 10 per bundle these 54 symbols became 6 socket clients, which together with the
        // 14 of the Perpetual scanner on this machine was 20 connections on an allowance of 10. At 30
        // they become 2, and the two scanners together sit at 7. See the fuller note in Perpetual/Api.cs.
        //
        // Correction, measured 24-08-2026 on the Perpetual side: bundles are socket clients, and a
        // client opens another connection for every SocketSubscriptionsCombineTarget subscriptions.
        // With that target on the library default of ten, these 54 symbols kept costing 6 connections
        // however few bundles they were packed into. The target is set to thirty in the socket options
        // below, which is what actually turns them into 2. Same reasoning as Perpetual/Api.cs.
        // The candle window per request. Measured against the live API on 28-08-2026: candleSnapshot
        // returns at most 5000 candles, and both ends of the window are inclusive (a window of 400
        // minutes gives 401 one-minute candles). The library has no limit parameter at all, only
        // startTime/endTime, so this number IS the window and nothing else caps it.
        //
        // It must never be raised above 5000. Asked for more, the exchange answers with the NEWEST
        // 5000 of the window instead of the oldest - a request for 20000 minutes came back with the
        // last 4902. In the loop of CandleBase.FetchFrom that would leave the front of the window
        // unfetched while fetchedUpTo jumps past it, so the hole is never filled.
        //
        // It stood at 300 until 28-08-2026, which is what made a cold start unusable: 1860 candles of
        // 1m plus 500 of each of the eleven higher intervals is 29 requests per symbol, and at the 25
        // requests a minute LimitRate hands out that is 3 hours 21 minutes for 173 symbols. On 5000
        // every interval fits in one request, so the same start is 12 requests per symbol.
        ExchangeOptions.SetDefaultOptions("HyperLiquid Spot", "USDC", 5000, false, 1,
            subscriptionsPerBundle: 30,
            klineDelivery: KlineDelivery.TimerFlush, minimalVolume: 21_000, pauseSymbol: "UBTCUSDC",
            maximumTickerInactivity: TimeSpan.FromHours(12));
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // The share of HyperLiquid's budget this process may spend, handed to the rate limiter the
        // package already carries instead of to a limiter of our own.
        //
        // HyperLiquid allows 1200 request weight per minute PER IP ADDRESS and an ordinary info
        // request - candleSnapshot, the symbol and ticker refresh - weighs 20, so the whole machine
        // gets 60 requests a minute and not one per scanner. 450 is 75% of that budget divided over
        // the two markets this scanner can run, so Perpetual and Spot together stay at 900 of the
        // 1200 whether one of them runs or both. That works out at 22 requests a minute per market.
        //
        // Chosen on 28-08-2026 over the two alternatives: 900 (45 requests a minute, but 150% of the
        // limit once the second market starts) and a live count of the running scanners (which does
        // give a lone scanner the full 45, at the price of machinery of our own). What it costs is
        // the cold start - 173 symbols take some 94 minutes at 22 a minute against 46 at 45 a minute.
        //
        // Everything this market sends goes through the package, so the ceiling above covers all of it.
        // (Perpetual has one call of its own, see the note in its Api.cs.)
        //
        // Correction, 28-08-2026: the 450 above rested on a weight of 20 per candle request, and that
        // is not what the exchange charges - candleSnapshot carries an extra weight per 60 candles in
        // the answer. Measured on the Perpetual market that afternoon the package counted 440 weight
        // per minute where the exchange counted some 700, so the "75% of the budget, divided over two
        // markets" this paragraph describes was in reality 117% of it per market. The surcharge is now
        // booked in Candle.cs, which makes the number below mean what it says, and the number itself
        // moved to HyperLiquidLimits - one place for both markets, with the measurements next to it.
        LibraryRateLimit.Lower(HyperLiquidExchange.RateLimiter, HyperLiquidLimits.GateName,
            HyperLiquidLimits.WeightPerMinute, ExchangeOptions.ExchangeName);
        // Spelled out because the ceiling is the budget of the whole ADDRESS, not a share per market:
        // start HyperLiquid Perpetual next to this one and both believe they may spend it.
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} takes {HyperLiquidLimits.WeightPerMinute} " +
            $"of the {HyperLiquidLimits.AddressWeightPerMinute} weight per minute this address is allowed, " +
            $"which assumes no second HyperLiquid market runs alongside it");


        HyperLiquidRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            // 90 and not 40 since 28-08-2026: the timeout has to outlast the rate limiter, not just
            // the network. The guard in LibraryRateLimit is a SLIDING window of one minute, so a
            // request that arrives when the budget is spent is held until the oldest weight falls out
            // of that window - up to a full 60 seconds. At 40 the request died INSIDE the limiter and
            // came back as a TaskCanceledException out of RateLimitGate.ProcessAsync, which is not a
            // network problem and reads like one. Costs a slower verdict on a genuinely dead
            // connection; that is the cheaper of the two.
            options.RequestTimeout = TimeSpan.FromSeconds(90); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new HyperLiquidCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        HyperLiquidSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
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
            // Kept equal to SubscriptionsPerBundle so a bundle really is one websocket connection;
            // without it the library falls back to ten per connection and the bundle setting above
            // buys nothing. See the fuller note in Perpetual/Api.cs.
            options.SocketSubscriptionsCombineTarget = 30;
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new HyperLiquidCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);


        HyperLiquidExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part,
        DateTime currentDate, CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
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

        throw new Exception("Cancel not implemented");
    }

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "HYPERLIQUID",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/HYPERLIQUID_{QUOTE}_{BASE}:{interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=HYPERLIQUID:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.hyperliquid.com/trade/{BASE}/{QUOTE}",
            }
        };
    }
}
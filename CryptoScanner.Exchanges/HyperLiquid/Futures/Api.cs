using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
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
        // 3 billion USDC over 177 listed perpetuals a day (14-08-2026), about 1/15 of Binance Futures.
        // 49 symbols stay above the boundary.
        //
        // One symbol per subscription is forced by the exchange, not a choice: the candle subscription
        // is {"type":"candle","coin":"<coin>","interval":"1m"} with a single coin field, and none of
        // the three subscriptions that do cover every coin at once (allMids, allDexsAssetCtxs,
        // fastAssetCtxs) carries candles. So the symbol count IS the subscription count here.
        //
        // Which makes SubscriptionsPerBundle the number that matters, because HyperLiquid counts
        // websocket connections PER IP ADDRESS and allows only ten of them - against a thousand
        // subscriptions. On the default of 10 per bundle, 131 symbols became 14 socket clients, and
        // with HyperLiquid Spot alongside it on the same machine that was 20 connections on an
        // allowance of 10 (measured 23-08-2026, the night both markets together lost their connection
        // 98 times, by far the worst of nineteen markets). Thirty per bundle turns those 131 into 5
        // and the Spot side into 2, so both scanners together sit at 7 of the 10 with room to grow,
        // while the subscriptions stay at 185 of the 1000 allowed.
        //
        // Same trap as the request weight in LimitRate - see the comment there. Raising this further
        // costs nothing at the exchange, but every drop then takes more subscriptions down with it.
        //
        // Correction, measured 24-08-2026: SubscriptionsPerBundle on its own does NOT decide how many
        // websocket connections there are. A bundle is one socket client, but the library hands that
        // client another connection for every SocketSubscriptionsCombineTarget subscriptions, and that
        // target was still on the library default of ten. Over the night of 23/24-08 the 42 lost
        // connections arrived in fixed groups of exactly ten consecutive subscription names while a
        // bundle held thirty, so 102 symbols ran over eleven connections and not the four that four
        // bundles suggest. The run before it, on 10 per bundle, showed fourteen such groups of ten for
        // 137 symbols. The connection count follows subscriptions / combine target, which is why that
        // target is now set to the same thirty in the socket options below.
        ExchangeOptions.SetDefaultOptions("HyperLiquid Futures", "USDC", 300, false, 1,
            subscriptionsPerBundle: 30,
            klineDelivery: KlineDelivery.TimerFlush, minimalVolume: 1_000_000);
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
            // The library, not SubscriptionsPerBundle, decides how many subscriptions share one
            // websocket connection: a socket client gets another connection for every
            // SocketSubscriptionsCombineTarget of them. Left on the library default of ten it undid
            // the bundle setting above - see the correction in that comment. Matching the two makes a
            // bundle of thirty one connection again, which takes this market from eleven connections
            // to four of the ten HyperLiquid allows per IP address. The price of matching them is
            // blast radius: a drop now marks thirty subscriptions instead of ten. Measured over
            // 23/24-08-2026 every drop recovered within one to three seconds without a single missing
            // minute, so that trade is cheap here - but it is the number to watch in the next check.
            options.SocketSubscriptionsCombineTarget = 30;
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new HyperLiquidCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        // Earlier experiment to scale down the HyperLiquid delay time; kept for reference:
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
                Code = "HYPERLIQUIDF",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/HYPERLIQUIDF_{QUOTE}_{BASE}:{interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=HYPERLIQUID:{BASE}{QUOTE}.P&interval={interval}",
            },
            ExchangeUrl = new()//HYPERLIQUID:0GUSDC.P
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.hyperliquid.com/trade/{BASE}/{QUOTE}",
            }
        };
    }
}
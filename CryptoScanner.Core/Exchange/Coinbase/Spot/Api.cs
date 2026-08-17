using Coinbase.Net;
using Coinbase.Net.Clients;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Coinbase.Spot;

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
        return new CoinbaseRestClient();
    }

    public override void ExchangeDefaults()
    {
        // Coinbase is a USD exchange: 397 online USD pairs against 21 USDT ones, and the USD side is
        // 715 million of the 24 hour volume against 3 million for USDT (measured 14-08-2026). Picking
        // USDT here would leave the scanner with a handful of near-dead symbols.
        // Volume: 715.000.000 USD over 24 hours, so the boundary is 240.000.
        // The kline stream of this exchange is fixed at 5 minutes, so the 1m candles are built from
        // the trade feed instead (see SubscriptionKLineTicker) - which needs the cache and the timer.
        //
        // Rate limits of this exchange (checked 14-08-2026, and the same values the package puts in
        // CoinbaseExchange.RateLimiter, which enforces them itself and waits when it has to):
        //   REST public      10 requests per second per IP address   (see LimitRate)
        //   REST private     30 requests per second per api key      (not used, everything is public)
        //   Socket connect  750 per second per IP address
        //   Socket messages   8 per second per IP address, unauthenticated
        // That last one is what bounds the layout below: every subscription costs one subscribe
        // message and SubscriptionManager fires them all at once, so 25 symbols per subscription turns
        // ~110 symbols into 5 messages that the package sends well inside one second. Raising the
        // number makes the startup shorter, not slower - it is not a per-message symbol limit of the
        // exchange, the candles and market_trades channels take a whole list.
        //
        // The number used to be 4, and that was the reason this exchange reconnected all night: the
        // package drops a socket that received nothing for SocketNoDataTimeout, and this feed only
        // carries trades. Individual Coinbase coins are quiet for a long time - measured over the
        // night of 16-08-2026 the twelve quietest went 31 to 81 minutes without a single trade - so a
        // subscription of 4 symbols regularly had nothing to deliver for over a minute. Replaying that
        // night against the stored 1m candles: with 4 symbols per subscription the groups were
        // completely silent for 1548 minutes (longest stretch 7 minutes), with 20 or 25 symbols not for
        // a single minute. Hence 25, which leaves room for the symbol list to shrink.
        ExchangeOptions.SetDefaultOptions("Coinbase Spot", "USD", 300, false, 25,
            klineDelivery: KlineDelivery.TimerFlush, minimalVolume: 240_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default options for this exchange
        CoinbaseRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            //options.Environment = CoinbaseEnvironment.Live;
            options.RequestTimeout = TimeSpan.FromSeconds(80); // standard=20 seconds
            //options.Environment = BybitEnvironment.Testnet;
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new CoinbaseCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        CoinbaseSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            //options.Environment = CoinbaseEnvironment.Live;
            options.RequestTimeout = TimeSpan.FromSeconds(80); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            // This is what closes a socket that stopped delivering, and on this exchange the feed is
            // trades - so it doubles as a silence detector for the coins on that connection. Two
            // minutes gives the 25 symbols of a subscription room to be quiet together on a slow
            // night, and still leaves the socket reconnected well before the four minutes of
            // SubscriptionManager.MaximumTickerSilence, which is the outer net.
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(2); // standard=30 seconds
            //options.Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            //options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new CoinbaseCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        CoinbaseExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
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
                // GDAX is what Altrady still calls Coinbase, confirmed 14-08-2026 in their own list of
                // valid exchange values ("Coinbase or GDAX")
                Code = "GDAX",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/GDAX_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = null,
            TradingView = new()
            {
                // TradingView moved on from the GDAX name, the market is COINBASE:BTCUSD there
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=COINBASE:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                // The advanced trade interface, which is the one this api talks to. The url that stood
                // here was a copy of the Okx one (my.okx.com/trade-spot/...) and answered with a 404.
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.coinbase.com/advanced-trade/spot/{BASE}-{QUOTE}",
            }
        };
    }
}
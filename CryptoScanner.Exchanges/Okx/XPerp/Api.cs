using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OKX.Net;
using OKX.Net.Clients;

namespace CryptoScanner.Core.Exchange.Okx.XPerp;

/// <summary>
/// The X-Perps of Okx: the USD_UM contracts, which the exchange publishes under
/// InstrumentType.Futures with ruleType xperp. They are perpetuals in everything that matters - the
/// expiry date in their name lies in 2031 and they pay funding - but they settle in USD VALUE rather
/// than in one fixed stablecoin, to be paid in USDC (or another accepted currency), and they take
/// USDC as margin. That is what makes them worth a market of their own: every one of the 442 linear
/// swaps under "Okx Perpetual" is quoted and settled in USDT, and Okx has no USDC swap at all.
/// This market is completely separate from "Okx Perpetual", which keeps its own symbols, candles and
/// settings.
/// </summary>
public class Api : ExchangeBase
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        Candle = new Candle(this);
        Symbol = new Symbol();
    }


    public override IDisposable GetClient()
    {
        return new OKXRestClient();
    }

    public override void ExchangeDefaults()
    {
        // OKX allows subscribing to multiple instruments in one message (the combined channel list may be
        // up to 64 KB), so there is no need for a websocket connection per symbol.
        // 361 million USD over 151 contracts a day (27-08-2026), 86 symbols stay above the boundary
        ExchangeOptions.SetDefaultOptions("Okx XPerp", "USD", 300, false, 50, minimalVolume: 120_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        OKXEnvironment environment = OKXEnvironment.Live;

        // Default options for this exchange
        OKXRestClient.SetDefaultOptions(options =>
        {
            options.Environment = environment;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new OKXCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        OKXSocketClient.SetDefaultOptions(options =>
        {
            options.Environment = environment;
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            // Switched off for the same reason as in the Okx Perpetual market: this watchdog closes a
            // socket that received no MARKET DATA for a while, and an illiquid contract simply is not
            // traded for minutes on end. The library already pings the socket every 10 seconds, and
            // SubscriptionManager.MaximumTickerInactivity is the outer net.
            options.SocketNoDataTimeout = TimeSpan.Zero;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new OKXCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);

        OKXExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // Check the maximum and minimum amount limits and the quantity
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

        // Not built, exactly as in the Okx Perpetual market. Real trading on these contracts also asks
        // for a decision the scanner cannot make on its own: they draw their margin from the unified
        // account, so which currency an order is actually financed with is a setting on the account.
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
            // No Altrady and no TradingView: both know the Okx perpetual swaps (OKEXF_USDT_BTC_SWAP,
            // OKX:BTCUSDT.P) but neither lists an X-Perp. A link built the way the Okx Perpetual market
            // builds it would be worse than none here, because OKX:BTCUSD.P does exist on TradingView -
            // it is the INVERSE swap, a different instrument that happens to chart the same price.
            // https://support.altrady.com/en/article/valid-values-for-exchange-and-symbol-1xrzfap/
            Altrady = null,
            HyperTrader = null,
            TradingView = null,
            ExchangeUrl = new()
            {
                // The instrument is named BTC-USD_UM_XPERP-310404 here, so base + quote is not enough.
                // Same caveat as the Okx Perpetual market: not verified from the Netherlands, my.okx.com
                // is the European entity and it bounces these pages back to its home page.
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.okx.com/trade-futures/{exchangename}",
            }
        };
    }
}

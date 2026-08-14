using Alpaca.Markets;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

/// <summary>
/// Alpaca is not a crypto exchange but a US stock broker (US equities, fractional shares).
/// It is plugged into the scanner as a regular exchange so the same analyzers can run on
/// stocks; the quote currency is always USD and the symbol name is the plain ticker (AAPL).
///
/// Everything runs against the paper trading environment (Environments.Paper), so no real
/// money is involved. An API key and secret are mandatory, even for reading market data.
///
/// Websites
///   https://alpaca.markets                  main site
///   https://app.alpaca.markets              dashboard, this is where the API key/secret live
///   https://app.alpaca.markets/trade/{BASE} per symbol trade page (see GetExchangeLinks below)
///
/// Documentation
///   https://docs.alpaca.markets             general documentation
///   https://docs.alpaca.markets/reference   REST API reference (trading and market data)
///   https://github.com/alpacahq/alpaca-trade-api-csharp   the Alpaca.Markets .NET SDK we use
///
/// Endpoints (handled by the SDK, listed here for reference only)
///   https://paper-api.alpaca.markets        paper trading (what this implementation uses)
///   https://api.alpaca.markets              live trading
///   https://data.alpaca.markets             market data
///
/// Unlike the other exchanges this implementation does not use CryptoExchange.Net but the
/// official Alpaca.Markets SDK, which is why the streaming ticker overrides StartAsync
/// instead of following the Subscribe() pattern.
/// </summary>
public class Api : ExchangeBase
{
    /// <summary>
    /// Number of stocks the scanner follows. Alpaca offers roughly 11.000 tradable US equities, while
    /// the free (Basic) data plan allows one streaming connection carrying at most 30 symbols. So the
    /// choice of which stocks to follow is made in <see cref="Symbol.GetSymbolsAsync"/> (the most
    /// active ones of the day) instead of by a volume boundary further down the line.
    /// Raise it together with the data plan; the subscription groups follow this number as well.
    /// </summary>
    public const int MaxSymbols = 30;

    /// <summary>
    /// The market data feed to read from. The free plan only serves IEX, which is a few percent of
    /// everything the market trades - the volume in the candles is the IEX volume, not the volume of
    /// the whole market. Stated once here so the REST requests and the WebSocket stream can never end
    /// up on a different feed than each other. With a paid plan this becomes MarketDataFeed.Sip.
    /// </summary>
    public const MarketDataFeed DataFeed = MarketDataFeed.Iex;

    /// <summary>
    /// Endpoint of the data stream, the streaming counterpart of <see cref="DataFeed"/>. The SDK
    /// derives this one from the environment instead of from the plan, and the SIP endpoint answers a
    /// free plan with "subscription does not permit", so it is stated explicitly.
    /// </summary>
    public static readonly Uri DataStreamEndpoint = new("wss://stream.data.alpaca.markets/v2/iex");


    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        Candle = new Candle(this);
        Symbol = new Symbol();
    }


    public override IDisposable GetClient()
    {
        if (GlobalData.TradingApi.Key != "")
            return Environments.Paper.GetAlpacaDataClient(
                new SecretKey(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret));

        throw new InvalidOperationException("Alpaca requires an API key. Register a free account at alpaca.markets.");
    }


    public override void ExchangeDefaults()
    {
        // 1000 bars per request, defaultQuote USD, MaxSymbols symbols per WebSocket group.
        //
        // No 24 hour volume boundary: the universe is already limited to the most active stocks of the
        // day (see Symbol.GetSymbolsAsync), and the volume that comes back is the volume of the IEX
        // feed - a few percent of what the market really trades - which makes it useless as an
        // absolute limit. A boundary on top of the selection would only switch off what was picked as
        // the most active stock of that moment.
        //
        // The pause rules watch SPY, the S&P 500 tracker. It is what bitcoin is on a crypto exchange:
        // the instrument the rest of the market follows. "BTCUSD" (the default) does not exist here.
        ExchangeOptions.SetDefaultOptions("Alpaca", "USD", 1000, false, MaxSymbols,
            minimalVolume: 0, pauseSymbol: "SPYUSD");
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
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
            Altrady = null,
            HyperTrader = null,
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol={BASE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://app.alpaca.markets/trade/{BASE}",
            }
        };
    }
}

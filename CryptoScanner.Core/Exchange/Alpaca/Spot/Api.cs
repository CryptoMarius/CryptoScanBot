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
        // 1000 bars per request, defaultQuote USD, max 100 symbols per WebSocket group
        // no volume measured (needs an account), so the boundary falls back to the default
        ExchangeOptions.SetDefaultOptions("Alpaca", "USD", 1000, false, 100);
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
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", position.Symbol.Name, text, price, quantity));
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

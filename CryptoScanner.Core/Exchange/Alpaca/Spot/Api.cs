using Alpaca.Markets;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

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
        ExchangeOptions.SetDefaultOptions("Alpaca Spot", "USD", 1000, false, 100);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide, decimal quantity,
        decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // Not implemented
        return Task.FromResult<(bool result, TradeParams? tradeParams)>((false, null));
    }


    public override Task<(bool succes, TradeParams? tradeParams)> Cancel(
        CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Not implemented
        return Task.FromResult<(bool succes, TradeParams? tradeParams)>((false, null));
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

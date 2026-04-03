using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

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
        return new BitvavoRestClient();
    }


    public override void ExchangeDefaults()
    {
        // Bitvavo is a Dutch EUR-based exchange. Max 1440 candles per REST request.
        // symbolLimitPerSubscription = 100 (Bitvavo WS accepts many markets per channel subscription)
        ExchangeOptions.SetDefaultOptions("Bitvavo Spot", "EUR", 1440, false, 10);
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
            Altrady = new()
            {
                Code = "BVVO",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/BVVO_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = null,
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=BITVAVO:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://account.bitvavo.com/markets/{BASE}-{QUOTE}",
            }
        };
    }
}

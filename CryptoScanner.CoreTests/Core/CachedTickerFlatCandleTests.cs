using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The price a cached kline ticker repeats when a minute produced no trades. It used to come from
/// CryptoSymbol.LastPrice alone, and that field is null until Process1mCandleAsync assigns it - which
/// on a market without a price ticker only happens when the websocket pushes a kline. An instrument
/// that had not traded since the scanner started therefore got no flat candle AND no ticker count, so
/// its subscription reported no activity at all and SubscriptionManager.NeedsRestart rebuilt it every
/// cycle without anything being wrong with it (HyperLiquid Perpetual, 29-08-2026: RIVNUSDC.XYZ and
/// VSTUSDC.PARA, every ten minutes from startup onwards).
/// </summary>
[TestClass]
public class CachedTickerFlatCandleTests
{
    [TestInitialize]
    public void Init() => TestBase.InitTestSession();


    private static CryptoSymbol CreateSymbol(string baseAsset)
    {
        var quoteData = GlobalData.AddQuoteData("USDT");
        return new CryptoSymbol
        {
            Status = 1,
            Exchange = GlobalData.ActiveExchange!,
            Base = baseAsset,
            Quote = "USDT",
            Name = baseAsset + "USDT",
            ExchangeName = baseAsset + "USDT",
            QuoteData = quoteData,
            PriceTickSize = 0.01m,
        };
    }


    [TestMethod]
    public void PriceComesFromLastPriceWhenTheMarketHasDelivered()
    {
        CryptoSymbol symbol = CreateSymbol("AAA");
        symbol.LastPrice = 123.45m;

        Assert.IsTrue(SubscriptionKLineCachedTicker.TryGetPriceToRepeat(symbol, out decimal price));
        Assert.AreEqual(123.45m, price);
    }


    [TestMethod]
    public void PriceFallsBackToTheLastStoredCandle()
    {
        CryptoSymbol symbol = CreateSymbol("BBB");

        // The candles a scanner holds after a restart: read from the database, or fetched over REST.
        // Neither of those routes touches LastPrice, which is what left the symbol without a price.
        DateTime openTime = new(2026, 8, 29, 10, 00, 00, DateTimeKind.Utc);
        CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], openTime, 10m, 12m, 9m, 11m, 1);
        CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], openTime.AddMinutes(1), 11m, 14m, 11m, 13m, 1);
        symbol.LastPrice = null;

        Assert.IsTrue(SubscriptionKLineCachedTicker.TryGetPriceToRepeat(symbol, out decimal price),
            "without this the subscription never marks activity and is restarted every cycle");
        Assert.AreEqual(13m, price, "the close of the newest 1m candle");
    }


    [TestMethod]
    public void NoPriceAtAllWhenNothingIsKnownYet()
    {
        CryptoSymbol symbol = CreateSymbol("CCC");
        symbol.LastPrice = null;

        Assert.AreEqual(0, symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.Count);
        Assert.IsFalse(SubscriptionKLineCachedTicker.TryGetPriceToRepeat(symbol, out decimal price),
            "a flat candle cannot be invented out of nothing");
        Assert.AreEqual(0m, price);
    }
}

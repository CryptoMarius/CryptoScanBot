using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// Since 27-09-2026 the socket callback of a cached ticker only enqueues its update and the minute
/// flush merges the queue (open point 92). These tests pin down that the merged candle is exactly
/// what the old direct merge produced: klines take the max of high and volume, the min of low and
/// the last close; trades add up their volume; and the order of arrival is kept.
/// </summary>
[TestClass]
public class CachedTickerQueueTests : TestBase
{
    private sealed class QueueTestSubscription(ExchangeOptions options) : SubscriptionKLineCachedTicker(options)
    {
        public override Task<WebSocketResult<UpdateSubscription>?> Subscribe()
            => Task.FromResult<WebSocketResult<UpdateSubscription>?>(null);

        public void Init(CryptoSymbol symbol) => InitializeCache([symbol]);
        public void Kline(DateTime time, decimal o, decimal h, decimal l, decimal c, decimal v)
            => UpdateCacheFromKline("QT-USDT", time, o, h, l, c, v);
        public void Trade(DateTime time, decimal price, decimal volume)
            => UpdateCacheFromTrade("QT-USDT", time, price, volume);
    }

    private static readonly DateTime Minute = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

    private static QueueTestSubscription Create()
    {
        InitTestSession();
        ExchangeOptions options = new() { ExchangeName = "QueueTestExchange" };
        QueueTestSubscription subscription = new(options);
        CryptoSymbol symbol = new()
        {
            Exchange = GlobalData.ActiveExchange!,
            Name = "QTUSDT",
            ExchangeName = "QT-USDT",
            Base = "QT",
            Quote = "USDT",
            QuoteData = GlobalData.AddQuoteData("USDT"),
            Status = 1,
            PriceTickSize = 0.01m,
        };
        subscription.Init(symbol);
        return subscription;
    }

    [TestMethod]
    public void KlinesMergeAsBefore()
    {
        QueueTestSubscription subscription = Create();
        subscription.Kline(Minute.AddSeconds(5), 10m, 11m, 9.5m, 10.5m, 100m);
        subscription.Kline(Minute.AddSeconds(30), 10m, 12m, 9.8m, 11.5m, 250m);
        subscription.Kline(Minute.AddSeconds(55), 10m, 11.8m, 9.0m, 11.2m, 240m);

        Assert.IsTrue(subscription.TryGetMergedCandle("QT-USDT", Minute, out CryptoCandle candle));
        Assert.AreEqual(10m, candle.Open);
        Assert.AreEqual(12m, candle.High);
        Assert.AreEqual(9.0m, candle.Low);
        Assert.AreEqual(11.2m, candle.Close, "the last update in order of arrival sets the close");
        Assert.AreEqual(250m, candle.Volume, "kline volume is cumulative, so the maximum counts");
    }

    [TestMethod]
    public void TradesMergeAsBefore()
    {
        QueueTestSubscription subscription = Create();
        subscription.Trade(Minute.AddSeconds(1), 10m, 5m);
        subscription.Trade(Minute.AddSeconds(2), 12m, 7m);
        subscription.Trade(Minute.AddSeconds(3), 9m, 1m);
        subscription.Trade(Minute.AddSeconds(4), 11m, 2m);

        Assert.IsTrue(subscription.TryGetMergedCandle("QT-USDT", Minute, out CryptoCandle candle));
        Assert.AreEqual(10m, candle.Open);
        Assert.AreEqual(12m, candle.High);
        Assert.AreEqual(9m, candle.Low);
        Assert.AreEqual(11m, candle.Close);
        Assert.AreEqual(15m, candle.Volume, "trade volume adds up");
    }

    [TestMethod]
    public void EachMinuteGetsItsOwnCandleAndInvalidKlinesAreDropped()
    {
        QueueTestSubscription subscription = Create();
        subscription.Kline(Minute.AddSeconds(10), 10m, 10m, 10m, 10m, 1m);
        subscription.Kline(Minute.AddSeconds(20), 0m, 0m, 0m, 0m, 0m);     // dropped by the guard
        subscription.Kline(Minute.AddSeconds(70), 20m, 21m, 19m, 20.5m, 3m); // the next minute

        Assert.IsTrue(subscription.TryGetMergedCandle("QT-USDT", Minute, out CryptoCandle first));
        Assert.AreEqual(10m, first.Low);
        Assert.IsTrue(subscription.TryGetMergedCandle("QT-USDT", Minute.AddMinutes(1), out CryptoCandle second));
        Assert.AreEqual(20.5m, second.Close);
    }
}

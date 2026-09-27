using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// A market with opening hours (Alpaca, ExchangeOptions.ContinuousMarket = false, open point 9): a
/// closed night gets no flat candles, and the indicators are fed the last real candles instead of a
/// time window full of stand-ins. A crypto exchange keeps the old behaviour.
/// </summary>
[DoNotParallelize]
[TestClass]
public class ContinuousMarketTests : TestBase
{
    private bool _saved;

    [TestInitialize]
    public void Save() => _saved = ExchangeBase.ExchangeOptions.ContinuousMarket;

    [TestCleanup]
    public void Restore() => ExchangeBase.ExchangeOptions.ContinuousMarket = _saved;

    private static CryptoCandle Candle(CandleTime time, decimal price) => new()
    {
        TickDecimals = 2,
        OpenTime = time,
        Open = price,
        High = price + 1,
        Low = price - 1,
        Close = price,
        Volume = 10,
    };

    /// <summary>A 1m series: a session of 300 candles, a closed stretch of 600 minutes, a session of 200.</summary>
    private static (CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, CandleTime last) Build()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        symbolInterval.CandleList.Clear();

        CandleTime time = new(10_000_000);
        for (int i = 0; i < 300; i++, time += 1u)
            symbolInterval.CandleList.TryAdd(time, Candle(time, 100 + i % 7));
        time += 600u;   // closed
        CandleTime last = time;
        for (int i = 0; i < 200; i++, time += 1u)
        {
            symbolInterval.CandleList.TryAdd(time, Candle(time, 110 + i % 5));
            last = time;
        }
        symbolInterval.LastCandleSynchronized = last + 1u;
        return (symbol, interval, symbolInterval, last);
    }


    [TestMethod]
    public void AClosedNightGetsNoFlatCandles()
    {
        var (symbol, interval, symbolInterval, _) = Build();
        ExchangeBase.ExchangeOptions.ContinuousMarket = false;
        CandleTools.BulkAddMissingCandles(symbol, interval);
        Assert.AreEqual(500, symbolInterval.CandleList.Count, "no candles added for the closed stretch");
    }


    [TestMethod]
    public void ACryptoExchangeStillFillsTheGap()
    {
        var (symbol, interval, symbolInterval, _) = Build();
        ExchangeBase.ExchangeOptions.ContinuousMarket = true;
        CandleTools.BulkAddMissingCandles(symbol, interval);
        Assert.AreEqual(1100, symbolInterval.CandleList.Count, "the 600 quiet minutes are filled flat");
    }


    [TestMethod]
    public void TheIndicatorsGetTheLastRealCandles()
    {
        var (symbol, interval, _, last) = Build();
        ExchangeBase.ExchangeOptions.ContinuousMarket = false;

        var history = IndicatorEngine.CollectCandles(symbol, interval, last, out string error);
        Assert.IsNotNull(history, error);
        Assert.AreEqual(260, history.Count);
        // 260 real candles reach back into the first session, over the closed stretch
        Assert.IsTrue(history.All(q => q.Volume > 0), "no flat stand-ins in the window");
        Assert.IsTrue(history[0].Timestamp < history[^1].Timestamp.AddMinutes(-600), "the window spans the closed night");
    }
}

#if DEBUG   // BBMA is DEBUG only, see CryptoData.Ema50 and BbmaPlugin.Strategies
using CryptoScanner.Analyzers.Bbma;
using CryptoScanner.Analyzers.Bbma.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Analyzer.Bbma;

/// <summary>
/// The TPW forward pass IsSignal keeps per symbol, interval and side (UpdateSharedTpwCache) must
/// give the same per-bar states as the full BuildTpwCache over the same candles, however it is fed:
/// one candle at a time, with candles skipped between evaluations, or after time went back.
/// <para>
/// The candles are a seeded random walk with the indicators computed from it (SMA20 and two
/// standard deviations, the WMA5/10 of the highs and lows, the EMA50), so Extremes and TPW's occur
/// the way they do on a real chart.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class BbmaOmniTpwSharedTests : TestBase
{
    private const int CandleCount = 400;
    private const int FirstEvaluated = 120;

    [TestInitialize]
    public void Setup() => InitTestSession();


    private static CryptoSymbol MakeSymbol()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        return new CryptoSymbol
        {
            Id = 1,
            Name = "TESTUSDT",
            Base = "TEST",
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDT"),
            PriceTickSize = 0.01m,
        };
    }

    private static double Wma(double[] values, int end, int length)
    {
        double sum = 0;
        double weights = 0;
        for (int i = 0; i < length; i++)
        {
            double weight = length - i;
            sum += values[end - i] * weight;
            weights += weight;
        }
        return sum / weights;
    }

    /// <summary>Fills the 5m interval of <paramref name="symbol"/> and returns the open times, oldest first.</summary>
    private static List<CandleTime> Fill(CryptoSymbol symbol, int seed)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        Random random = new(seed);

        double[] open = new double[CandleCount];
        double[] high = new double[CandleCount];
        double[] low = new double[CandleCount];
        double[] close = new double[CandleCount];
        double price = 100;
        for (int i = 0; i < CandleCount; i++)
        {
            open[i] = price;
            price *= 1 + (random.NextDouble() - 0.5) * 0.02;
            close[i] = price;
            high[i] = Math.Max(open[i], close[i]) * (1 + random.NextDouble() * 0.006);
            low[i] = Math.Min(open[i], close[i]) * (1 - random.NextDouble() * 0.006);
        }

        List<CandleTime> times = [];
        double ema = close[0];
        for (int i = 0; i < CandleCount; i++)
        {
            ema += (close[i] - ema) * 2.0 / 51.0;
            CandleTime time = new((uint)(1000 * interval.Duration + i * interval.Duration));
            times.Add(time);
            symbolInterval.CandleList.TryAdd(time, new CryptoCandle
            {
                TickDecimals = 4,
                OpenTime = time,
                Open = (decimal)Math.Round(open[i], 4),
                High = (decimal)Math.Round(high[i], 4),
                Low = (decimal)Math.Round(low[i], 4),
                Close = (decimal)Math.Round(close[i], 4),
            });

            if (i < 20)
                continue;
            double mean = 0;
            for (int j = i - 19; j <= i; j++)
                mean += close[j];
            mean /= 20;
            double variance = 0;
            for (int j = i - 19; j <= i; j++)
                variance += (close[j] - mean) * (close[j] - mean);
            double deviation = 2 * Math.Sqrt(variance / 20);

            symbolInterval.Data[time] = new CryptoData
            {
                Sma20 = mean,
                BollingerBandsDeviation = deviation,
                BollingerBandsPercentage = 100 * 2 * deviation / mean,
                Ema50 = ema,
                Wma05Low = Wma(low, i, 5),
                Wma10Low = Wma(low, i, 10),
                Wma05High = Wma(high, i, 5),
                Wma10High = Wma(high, i, 10),
                Atr14 = 1.0,
            };
        }
        return times;
    }

    private static SignalBbmaOmniBase MakeAlgorithm(CryptoSymbol symbol, CryptoTradeSide side, CandleTime time)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        symbolInterval.CandleList.TryGetValue(time, out CryptoCandle candle);
        symbolInterval.Data.TryGetValue(time, out CryptoData? data);
        MyData candleLast = new() { Candle = candle, CandleData = data! };

        if (side == CryptoTradeSide.Long)
            return new SignalBbmaOmniLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = side,
                SignalStrategy = BbmaPlugin.StrategyInternal.ToLower(),
                CandleLast = candleLast,
            };
        return new SignalBbmaOmniShort
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = BbmaPlugin.StrategyInternal.ToLower(),
            CandleLast = candleLast,
        };
    }

    /// <summary>The opposite side's Extreme, as IsSignal wires it up.</summary>
    private static Func<MyData, bool> OppositeExtreme(CryptoSymbol symbol, CryptoTradeSide side, CandleTime time)
    {
        SignalBbmaOmniBase opposite = MakeAlgorithm(symbol, side == CryptoTradeSide.Long ? CryptoTradeSide.Short : CryptoTradeSide.Long, time);
        return opposite is SignalBbmaOmniShort shortSide ? shortSide.IsExtremeSellBar : ((SignalBbmaOmniLong)opposite).IsExtremeBuyBar;
    }

    /// <summary>What a full BuildTpwCache gives over all candles, on a symbol of its own.</summary>
    private static Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> FullPass(CryptoTradeSide side, int seed, out List<CandleTime> times)
    {
        CryptoSymbol symbol = MakeSymbol();
        times = Fill(symbol, seed);
        SignalBbmaOmniBase algorithm = MakeAlgorithm(symbol, side, times[^1]);
        Func<MyData, bool> opposite = OppositeExtreme(symbol, side, times[^1]);
        if (algorithm is SignalBbmaOmniLong longSide)
            longSide.BuildTpwCache(algorithm.SymbolInterval, opposite);
        else
            ((SignalBbmaOmniShort)algorithm).BuildTpwCache(algorithm.SymbolInterval, opposite);
        return algorithm.TpwStatesForTest();
    }

    private static void AssertSame(Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> expected,
        Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> actual, CandleTime upTo, string what)
    {
        foreach (var (time, state) in expected)
        {
            if (time > upTo)
                continue;
            Assert.IsTrue(actual.TryGetValue(time, out var got), $"{what}: no state for {time}");
            Assert.AreEqual(state.TpwBuyCount, got.TpwBuyCount, $"{what}: TpwBuyCount at {time}");
            Assert.AreEqual(state.IsTpwBuyFired, got.IsTpwBuyFired, $"{what}: IsTpwBuyFired at {time}");
            Assert.AreEqual(state.TpwSellCount, got.TpwSellCount, $"{what}: TpwSellCount at {time}");
            Assert.AreEqual(state.IsTpwSellFired, got.IsTpwSellFired, $"{what}: IsTpwSellFired at {time}");
        }
    }


    [TestMethod]
    [DataRow(CryptoTradeSide.Long, 1)]
    [DataRow(CryptoTradeSide.Long, 2)]
    [DataRow(CryptoTradeSide.Short, 1)]
    [DataRow(CryptoTradeSide.Short, 2)]
    public void SharedPass_OneCandleAtATime_EqualsFullPass(CryptoTradeSide side, int seed)
    {
        var expected = FullPass(side, seed, out _);
        int fired = expected.Values.Count(s => side == CryptoTradeSide.Long ? s.IsTpwBuyFired : s.IsTpwSellFired);
        Assert.IsTrue(fired > 0, "the random walk has to contain TPW's, or this test proves nothing");

        CryptoSymbol symbol = MakeSymbol();
        List<CandleTime> times = Fill(symbol, seed);
        Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> actual = [];
        for (int i = FirstEvaluated; i < times.Count; i++)
        {
            SignalBbmaOmniBase algorithm = MakeAlgorithm(symbol, side, times[i]);
            actual = algorithm.SharedTpwStatesForTest(OppositeExtreme(symbol, side, times[i]));
        }
        AssertSame(expected, actual, times[^1], "one at a time");
    }

    [TestMethod]
    [DataRow(CryptoTradeSide.Long)]
    [DataRow(CryptoTradeSide.Short)]
    public void SharedPass_SkippedCandlesAndTimeBack_EqualsFullPass(CryptoTradeSide side)
    {
        var expected = FullPass(side, 3, out _);

        CryptoSymbol symbol = MakeSymbol();
        List<CandleTime> times = Fill(symbol, 3);

        // Every seventh candle only (most candles are not a Reentry and never reach the pass)
        Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> actual = [];
        for (int i = FirstEvaluated; i < times.Count; i += 7)
            actual = MakeAlgorithm(symbol, side, times[i]).SharedTpwStatesForTest(OppositeExtreme(symbol, side, times[i]));
        AssertSame(expected, actual, times[FirstEvaluated + 7 * ((times.Count - 1 - FirstEvaluated) / 7)], "skipped");

        // Time goes back (a new emulator run on the same symbol): starts over
        CandleTime back = times[FirstEvaluated + 30];
        actual = MakeAlgorithm(symbol, side, back).SharedTpwStatesForTest(OppositeExtreme(symbol, side, back));
        AssertSame(expected, actual, back, "after time went back");
        Assert.IsFalse(actual.Keys.Any(k => k > back), "states newer than the evaluated candle after starting over");
    }
}
#endif

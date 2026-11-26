using CryptoScanner.Analyzers.CandlePattern;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;
using PatternLong = CryptoScanner.Analyzers.CandlePattern.Signal.CandlePatternLong;
using PatternShort = CryptoScanner.Analyzers.CandlePattern.Signal.CandlePatternShort;

namespace CryptoScanner.CoreTests.Analyzer.CandlePattern;

/// <summary>
/// Three methods in the candlepattern strategy, and the checks that came with it: the size of the
/// decisive candles against the average body before them (MinSizeFactor), the close against a trend
/// EMA (TrendEmaLength), and the stop-loss beyond the pattern with the take profit at RiskReward
/// times its distance (StopBeyondPattern).
/// <para>
/// A hand-built symbol, the same way CandlePatternPrecedingMoveTests builds one: a run of filler
/// candles with small bodies, then the pattern. The filler decides what the size and the EMA see.
/// </para>
/// </summary>
[TestClass]
public class CandlePatternThreeMethodsTests
{
    private const byte TickDec = 4;
    private const CryptoIntervalPeriod Period = CryptoIntervalPeriod.interval1h;

    // Somewhere away from zero: CandlePatternBase.IndicatorsOkay refuses a candle with OpenTime 0.
    private const uint FirstOpenTime = 1000;

    // Enough filler for the trend EMA (six times its length) and the average body (20 candles).
    private const int FillerCount = 130;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        GlobalData.Settings ??= new SettingsBasic();
        SetupIntervalList();
    }

    private static void SetupIntervalList()
    {
        if (GlobalData.IntervalList.Count > 0)
            return;

        int id = 0;
        foreach (CryptoInterval interval in CryptoInterval.CreateStandardIntervalList())
        {
            interval.Id = id++;
            GlobalData.IntervalList.Add(interval);
            GlobalData.IntervalListId.Add(interval.Id, interval);
            GlobalData.IntervalListPeriodName.Add(interval.Name, interval);
            GlobalData.IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }
    }

    private static CryptoSymbol CreateSymbol()
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange" };
        var quoteData = new CryptoQuoteData { Name = "USDT" };
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "TEST",
            Quote = "USDT",
            Name = "TESTUSDT",
            Exchange = exchange,
            ExchangeName = exchange.Name,
            QuoteData = quoteData,
            PriceDecimals = TickDec,
            PriceTickSize = 0.0001m,
            PriceMinimum = 0m,
            PriceMaximum = 0m,
            QuantityTickSize = 0.01m,
            QuantityMinimum = 0.01m,
            QuantityMaximum = 100000m,
            QuoteValueMinimum = 1m,
            QuoteValueMaximum = 200000m,
        };
    }

    private static CryptoCandle MakeCandle(int index, decimal open, decimal high, decimal low, decimal close)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriod[Period];
        return new CryptoCandle
        {
            TickDecimals = TickDec,
            OpenTime = new CandleTime(FirstOpenTime + (uint)index * interval.Duration),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };
    }

    /// <summary>
    /// Filler candles around <paramref name="level"/> with a body of <paramref name="body"/>, rising
    /// by <paramref name="step"/> per candle, then a rising three methods: a decisive green candle
    /// 100 -> 105.5 (low 99.5, high 106), two small candles inside it, and a green candle closing at
    /// 107.5, above the first one's high.
    /// </summary>
    private static List<CryptoCandle> Series(decimal level, decimal body, decimal step = 0m)
    {
        List<CryptoCandle> candles = [];
        for (int i = 0; i < FillerCount; i++)
        {
            decimal open = level - (FillerCount - i) * step;
            decimal close = open + body;
            candles.Add(MakeCandle(i, open, Math.Max(open, close) + 0.05m, Math.Min(open, close) - 0.05m, close));
        }

        int n = FillerCount;
        candles.Add(MakeCandle(n, open: 100m, high: 106m, low: 99.5m, close: 105.5m));
        candles.Add(MakeCandle(n + 1, open: 105m, high: 105.5m, low: 103m, close: 103.5m));
        candles.Add(MakeCandle(n + 2, open: 103.5m, high: 104.5m, low: 102.5m, close: 104m));
        candles.Add(MakeCandle(n + 3, open: 104m, high: 108m, low: 103.8m, close: 107.5m));
        return candles;
    }

    /// <summary>The same series reflected around 200: a falling three methods for a short.</summary>
    private static List<CryptoCandle> Mirror(List<CryptoCandle> candles)
        => candles.Select((c, i) => MakeCandle(i, 200m - c.Open, 200m - c.Low, 200m - c.High, 200m - c.Close)).ToList();


    private sealed record Outcome(bool IsSignal, string ExtraText, decimal? SlPercentage, decimal? TpPercentage);

    /// <summary>
    /// Runs the strategy on the last candle with only three methods selected and the preceding-move
    /// requirement left ON at three candles, so a test also proves it does not apply to a
    /// continuation. CandlePatternPlugin.Settings is process-static, so the previous values are put back.
    /// </summary>
    private static Outcome Run(CryptoTradeSide side, List<CryptoCandle> candles,
        decimal minSizeFactor = 0m, int trendEmaLength = 0, bool stopBeyondPattern = false, decimal riskReward = 2m)
    {
        CryptoSymbol symbol = CreateSymbol();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(Period);
        symbolInterval.CandleList.Clear();
        symbolInterval.Data.Clear();
        foreach (CryptoCandle candle in candles)
        {
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            symbolInterval.Data.Add(candle.OpenTime, new CryptoData());
        }

        CryptoInterval interval = GlobalData.IntervalListPeriod[Period];
        MyData candleLast = new() { Candle = candles[^1], CandleData = new CryptoData() };

        string strategy = CandlePatternPlugin.StrategyInternal.ToLower();
        SignalCreateBase signal = side == CryptoTradeSide.Long
            ? new PatternLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = side,
                SignalStrategy = strategy,
                CandleLast = candleLast,
            }
            : new PatternShort
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = side,
                SignalStrategy = strategy,
                CandleLast = candleLast,
            };

        CandlePatternStrategySettings settings = CandlePatternPlugin.Settings;
        List<string> patterns = settings.Patterns;
        int precedingCandles = settings.PrecedingCandles;
        decimal precedingPercentage = settings.PrecedingPercentage;
        List<string> requireZone = settings.RequireZone;
        decimal oldMinSize = settings.MinSizeFactor;
        int oldEma = settings.TrendEmaLength;
        bool oldStop = settings.StopBeyondPattern;
        decimal oldRiskReward = settings.RiskReward;
        try
        {
            settings.Patterns = [nameof(CryptoCandlePattern.ThreeMethods)];
            settings.PrecedingCandles = 3;
            settings.PrecedingPercentage = 0m;
            settings.RequireZone = [];
            settings.MinSizeFactor = minSizeFactor;
            settings.TrendEmaLength = trendEmaLength;
            settings.StopBeyondPattern = stopBeyondPattern;
            settings.RiskReward = riskReward;

            bool result = signal.IsSignal();
            return new Outcome(result, signal.ExtraText, signal.OverrideSlPercentage, signal.OverrideProfitPercentage);
        }
        finally
        {
            settings.Patterns = patterns;
            settings.PrecedingCandles = precedingCandles;
            settings.PrecedingPercentage = precedingPercentage;
            settings.RequireZone = requireZone;
            settings.MinSizeFactor = oldMinSize;
            settings.TrendEmaLength = oldEma;
            settings.StopBeyondPattern = oldStop;
            settings.RiskReward = oldRiskReward;
        }
    }


    /// <summary>
    /// The filler rises into the pattern, so a reversal would be refused with "price did not run
    /// against the trade first". Three methods is a continuation and fires anyway, without a stop
    /// or take profit of its own while StopBeyondPattern is off.
    /// </summary>
    [TestMethod]
    public void Long_AfterARise_IsASignal()
    {
        Outcome outcome = Run(CryptoTradeSide.Long, Series(level: 99m, body: 0.1m, step: 0.05m));

        Assert.IsTrue(outcome.IsSignal, $"refused with '{outcome.ExtraText}'");
        Assert.AreEqual("ThreeMethods", outcome.ExtraText);
        Assert.IsNull(outcome.SlPercentage);
        Assert.IsNull(outcome.TpPercentage);
    }


    /// <summary>
    /// Entry at the close 107.5, stop under the first candle at 99.5: (107.5 - 99.5) / 107.5 =
    /// 7.4419%, rounded 7.44. Take profit twice that distance, 14.8837%, rounded 14.88.
    /// </summary>
    [TestMethod]
    public void Long_StopBeyondPattern_HandsTheStopAndTwiceItAsTakeProfit()
    {
        Outcome outcome = Run(CryptoTradeSide.Long, Series(level: 99m, body: 0.1m), stopBeyondPattern: true);

        Assert.IsTrue(outcome.IsSignal, $"refused with '{outcome.ExtraText}'");
        Assert.AreEqual(7.44m, outcome.SlPercentage);
        Assert.AreEqual(14.88m, outcome.TpPercentage);
    }


    /// <summary>A risk/reward of zero keeps the take profit of the trader settings.</summary>
    [TestMethod]
    public void Long_RiskRewardZero_LeavesTheTakeProfitToTheTrader()
    {
        Outcome outcome = Run(CryptoTradeSide.Long, Series(level: 99m, body: 0.1m), stopBeyondPattern: true, riskReward: 0m);

        Assert.IsTrue(outcome.IsSignal, $"refused with '{outcome.ExtraText}'");
        Assert.AreEqual(7.44m, outcome.SlPercentage);
        Assert.IsNull(outcome.TpPercentage);
    }


    /// <summary>
    /// The mirror image for a short: entry 92.5, stop above the first candle at 100.5:
    /// 8 / 92.5 = 8.6486%, rounded 8.65; take profit 17.2973%, rounded 17.30.
    /// </summary>
    [TestMethod]
    public void Short_StopBeyondPattern_IsTheMirrorImage()
    {
        Outcome outcome = Run(CryptoTradeSide.Short, Mirror(Series(level: 99m, body: 0.1m)), stopBeyondPattern: true);

        Assert.IsTrue(outcome.IsSignal, $"refused with '{outcome.ExtraText}'");
        Assert.AreEqual(8.65m, outcome.SlPercentage);
        Assert.AreEqual(17.30m, outcome.TpPercentage);
    }


    /// <summary>
    /// Filler bodies of 0.1 against a first body of 5.5 and a last body of 3.5: far above 1.5 times
    /// the average. Filler bodies of 3 put the first candle at about 1.8x but the last one at about
    /// 1.0x (its average now also holds the first candle), so it is refused.
    /// </summary>
    [TestMethod]
    public void MinSizeFactor_RefusesADecisiveCandleThatIsNotLarge()
    {
        Outcome small = Run(CryptoTradeSide.Long, Series(level: 99m, body: 0.1m), minSizeFactor: 1.5m);
        Assert.IsTrue(small.IsSignal, $"refused with '{small.ExtraText}'");

        Outcome large = Run(CryptoTradeSide.Long, Series(level: 99m, body: 3m), minSizeFactor: 1.5m);
        Assert.IsFalse(large.IsSignal);
        StringAssert.Contains(large.ExtraText, "candle too small");
    }


    /// <summary>
    /// Filler flat around 95: the EMA20 sits well under the close of 107.5, so a long passes. Filler
    /// flat around 120: four candles near 105 pull the EMA down only to about 115, above the close,
    /// so the long is refused.
    /// </summary>
    [TestMethod]
    public void TrendEma_RefusesALongWithTheCloseBelowIt()
    {
        Outcome above = Run(CryptoTradeSide.Long, Series(level: 95m, body: 0.1m), trendEmaLength: 20);
        Assert.IsTrue(above.IsSignal, $"refused with '{above.ExtraText}'");

        Outcome below = Run(CryptoTradeSide.Long, Series(level: 120m, body: 0.1m), trendEmaLength: 20);
        Assert.IsFalse(below.IsSignal);
        StringAssert.Contains(below.ExtraText, "close not above EMA20");
    }
}

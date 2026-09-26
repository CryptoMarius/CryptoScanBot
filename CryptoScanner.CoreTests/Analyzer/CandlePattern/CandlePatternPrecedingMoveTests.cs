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
/// Pins down what the "preceding move" requirement of the candlepattern strategy measures
/// (CandlePatternBase.PrecededByAMoveTheOtherWay, driven by PrecedingCandles / PrecedingPercentage).
/// <para>
/// The move has to be read up to the close of the candle BEFORE the reversal candle. Measuring it up
/// to the close of the reversal candle itself makes it the NET move including the reversal, so a
/// bullish engulfing that recovers a three-candle drop in one candle comes out as a RISE and is
/// rejected with "price did not run against the trade first" - the stronger the reversal, the sooner
/// it is thrown away. These tests fail on that code and pass on the corrected version.
/// </para>
/// <para>
/// No zone requirement, no indicators: the strategy reads nothing but the candles themselves, so a
/// hand-built symbol with five candles is enough and no database is needed, in the same way
/// AtrRbBandsTests and StobbSoftSbmTests build theirs.
/// </para>
/// </summary>
[TestClass]
public class CandlePatternPrecedingMoveTests
{
    private const byte TickDec = 4;
    private const CryptoIntervalPeriod Period = CryptoIntervalPeriod.interval15m;

    // Somewhere away from zero: CandlePatternBase.IndicatorsOkay refuses a candle with OpenTime 0.
    private const uint FirstOpenTime = 1000;

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

    /// <summary>One candle, as open/high/low/close; the caller decides where it sits in time.</summary>
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
    /// Puts the candles in the interval, oldest first, with an empty CryptoData per candle.
    /// The empty row is required rather than cosmetic: SignalBase.GetPrevCandle reads the previous
    /// candle through CryptoSymbolInterval.TryGetCandle, which only hands out a MyData when BOTH the
    /// candle and its Data row are there. The strategy reads no indicator from it.
    /// </summary>
    private static CryptoSymbolInterval LoadSymbolInterval(CryptoSymbol symbol, List<CryptoCandle> candles)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(Period);
        symbolInterval.CandleList.Clear();
        symbolInterval.Data.Clear();
        foreach (CryptoCandle candle in candles)
        {
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            symbolInterval.Data.Add(candle.OpenTime, new CryptoData());
        }
        return symbolInterval;
    }

    /// <summary>
    /// Runs the strategy over the given candles (the last one is the pattern candle) with the
    /// preceding-move requirement set to three candles and any move in the right direction.
    /// CandlePatternPlugin.Settings is process-static, so the previous values are put back.
    /// </summary>
    private static bool IsSignal(CryptoTradeSide side, List<CryptoCandle> candles, out string extraText)
    {
        CryptoSymbol symbol = CreateSymbol();
        CryptoSymbolInterval symbolInterval = LoadSymbolInterval(symbol, candles);
        CryptoInterval interval = GlobalData.IntervalListPeriod[Period];
        MyData candleLast = new()
        {
            Candle = candles[^1],
            CandleData = new CryptoData(),
        };

        SignalCreateBase signal = side == CryptoTradeSide.Long
            ? new PatternLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Long,
                SignalStrategy = CandlePatternPlugin.StrategyInternal.ToLower(),
                CandleLast = candleLast,
            }
            : new PatternShort
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Short,
                SignalStrategy = CandlePatternPlugin.StrategyInternal.ToLower(),
                CandleLast = candleLast,
            };

        CandlePatternStrategySettings settings = CandlePatternPlugin.Settings;
        List<string> patterns = settings.Patterns;
        int precedingCandles = settings.PrecedingCandles;
        decimal precedingPercentage = settings.PrecedingPercentage;
        List<string> requireZone = settings.RequireZone;
        try
        {
            settings.Patterns = [nameof(CryptoCandlePattern.Engulfing)];
            settings.PrecedingCandles = 3;
            settings.PrecedingPercentage = 0m;
            settings.RequireZone = [];

            bool result = signal.IsSignal();
            extraText = signal.ExtraText;
            return result;
        }
        finally
        {
            settings.Patterns = patterns;
            settings.PrecedingCandles = precedingCandles;
            settings.PrecedingPercentage = precedingPercentage;
            settings.RequireZone = requireZone;
        }
    }

    // ── Long ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Closes 100, 99, 98 and then a bearish 97 (the first candle of the pattern), followed by a
    /// bullish engulfing that closes at 100.5. The move against a long is 100 -> 97, three percent
    /// down, so the pattern has something to reverse. Reading the move up to the engulfing close
    /// instead gives (100 - 100.5) / 100 = -0.5% and rejects it.
    /// </summary>
    [TestMethod]
    public void Long_EngulfingRecoversTheWholeDrop_IsASignal()
    {
        List<CryptoCandle> candles =
        [
            MakeCandle(0, open: 100.2m, high: 100.3m, low: 99.9m, close: 100m),
            MakeCandle(1, open: 100m, high: 100.1m, low: 98.9m, close: 99m),
            MakeCandle(2, open: 99m, high: 99.1m, low: 97.9m, close: 98m),
            MakeCandle(3, open: 98m, high: 98.2m, low: 96.8m, close: 97m),
            MakeCandle(4, open: 96.9m, high: 100.6m, low: 96.8m, close: 100.5m),
        ];

        bool result = IsSignal(CryptoTradeSide.Long, candles, out string extraText);

        Assert.IsTrue(result, $"A three percent drop precedes this engulfing, but it was refused "
            + $"with '{extraText}'");
        Assert.AreEqual("Engulfing", extraText);
    }

    /// <summary>
    /// The same shape after a RISE: closes 97, 98, 99 and then a bearish 100, so a long has nothing
    /// to reverse. This is what the requirement is for - it is what keeps a hammer in a rising market
    /// from counting as a buy signal.
    /// </summary>
    [TestMethod]
    public void Long_EngulfingAfterARise_IsNoSignal()
    {
        List<CryptoCandle> candles =
        [
            MakeCandle(0, open: 96.8m, high: 97.1m, low: 96.7m, close: 97m),
            MakeCandle(1, open: 97m, high: 98.1m, low: 96.9m, close: 98m),
            MakeCandle(2, open: 98m, high: 99.1m, low: 97.9m, close: 99m),
            MakeCandle(3, open: 101m, high: 101.2m, low: 99.8m, close: 100m),
            MakeCandle(4, open: 99.9m, high: 103.6m, low: 99.8m, close: 103.5m),
        ];

        bool result = IsSignal(CryptoTradeSide.Short, candles, out _);
        Assert.IsFalse(result, "guard: these candles form no bearish engulfing either");

        result = IsSignal(CryptoTradeSide.Long, candles, out string extraText);

        Assert.IsFalse(result, "price rose before this engulfing, so a long has nothing to reverse");
        StringAssert.Contains(extraText, "price did not run against the trade first");
    }

    // ── Short ────────────────────────────────────────────────────────────

    /// <summary>
    /// The mirror image: closes 100, 101, 102 and then a bullish 103, followed by a bearish engulfing
    /// that closes at 99.5. The move against a short is 100 -> 103, three percent up. Reading the
    /// move up to the engulfing close gives (99.5 - 100) / 100 = -0.5% and rejects it.
    /// </summary>
    [TestMethod]
    public void Short_EngulfingRecoversTheWholeRise_IsASignal()
    {
        List<CryptoCandle> candles =
        [
            MakeCandle(0, open: 99.8m, high: 100.1m, low: 99.7m, close: 100m),
            MakeCandle(1, open: 100m, high: 101.1m, low: 99.9m, close: 101m),
            MakeCandle(2, open: 101m, high: 102.1m, low: 100.9m, close: 102m),
            MakeCandle(3, open: 102m, high: 103.2m, low: 101.8m, close: 103m),
            MakeCandle(4, open: 103.1m, high: 103.2m, low: 99.4m, close: 99.5m),
        ];

        bool result = IsSignal(CryptoTradeSide.Short, candles, out string extraText);

        Assert.IsTrue(result, $"A three percent rise precedes this engulfing, but it was refused "
            + $"with '{extraText}'");
        Assert.AreEqual("Engulfing", extraText);
    }

    /// <summary>The same shape after a DROP: a short has nothing to reverse.</summary>
    [TestMethod]
    public void Short_EngulfingAfterADrop_IsNoSignal()
    {
        List<CryptoCandle> candles =
        [
            MakeCandle(0, open: 103.2m, high: 103.3m, low: 102.9m, close: 103m),
            MakeCandle(1, open: 103m, high: 103.1m, low: 101.9m, close: 102m),
            MakeCandle(2, open: 102m, high: 102.1m, low: 100.9m, close: 101m),
            MakeCandle(3, open: 99m, high: 100.2m, low: 98.8m, close: 100m),
            MakeCandle(4, open: 100.1m, high: 100.2m, low: 96.4m, close: 96.5m),
        ];

        bool result = IsSignal(CryptoTradeSide.Long, candles, out _);
        Assert.IsFalse(result, "guard: these candles form no bullish engulfing either");

        result = IsSignal(CryptoTradeSide.Short, candles, out string extraText);

        Assert.IsFalse(result, "price fell before this engulfing, so a short has nothing to reverse");
        StringAssert.Contains(extraText, "price did not run against the trade first");
    }
}

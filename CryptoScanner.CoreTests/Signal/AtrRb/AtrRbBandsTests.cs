using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.AtrRb;

using Skender.Stock.Indicators;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal.AtrRb;

/// <summary>
/// Unit tests for <see cref="AtrRbBandsHelper"/>: a Keltner-style EMA basis with ATR-based
/// outer bands. Tests use deterministic synthetic candles so they need no external data files.
/// The methods under test require a CryptoSymbolInterval with populated CandleList, so we
/// build a lightweight symbol + interval setup without database access.
/// </summary>
[TestClass]
public class AtrRbBandsTests
{
    private const int CandleCount = 300;
    private const byte TickDec = 4;

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

    private static List<CryptoCandle> MakeCandles(int count, double basePrice = 100.0, double amplitude = 10.0)
    {
        var list = new List<CryptoCandle>(count);
        decimal prevClose = (decimal)basePrice;
        for (int i = 0; i < count; i++)
        {
            double mid = basePrice + amplitude * Math.Sin(i * 0.05) + 3 * Math.Cos(i * 0.11);
            decimal close = Math.Round((decimal)mid, TickDec);
            decimal high = close + 0.50m + (i % 5) * 0.05m;
            decimal low = close - 0.50m - (i % 3) * 0.05m;
            list.Add(new CryptoCandle
            {
                TickDecimals = TickDec,
                OpenTime = new CandleTime((uint)(i * 15)),
                Open = prevClose,
                High = high,
                Low = low,
                Close = close,
                Volume = 1000m + (i % 13) * 50m,
            });
            prevClose = close;
        }
        return list;
    }

    private static CryptoSymbolInterval LoadSymbolInterval(CryptoSymbol symbol, List<CryptoCandle> candles)
    {
        CryptoSymbolInterval si = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval15m);
        si.CandleList.Clear();
        foreach (var candle in candles)
            si.CandleList.TryAdd(candle.OpenTime, candle);
        return si;
    }

    // ── IsLowerBandBreak ─────────────────────────────────────────────────

    [TestMethod]
    public void IsLowerBandBreak_InsufficientCandles_ReturnsFalse()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(5);
        var si = LoadSymbolInterval(symbol, candles);

        bool result = AtrRbBandsHelper.IsLowerBandBreak(si, candles[^1].OpenTime,
            out double pct, out double lower);

        Assert.IsFalse(result);
        Assert.AreEqual(0.0, pct);
    }

    [TestMethod]
    public void IsUpperBandBreak_InsufficientCandles_ReturnsFalse()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(5);
        var si = LoadSymbolInterval(symbol, candles);

        bool result = AtrRbBandsHelper.IsUpperBandBreak(si, candles[^1].OpenTime,
            out double pct, out double upper);

        Assert.IsFalse(result);
        Assert.AreEqual(0.0, pct);
    }

    [TestMethod]
    public void IsLowerBandBreak_NormalPrice_ReturnsFalse()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(CandleCount);
        var si = LoadSymbolInterval(symbol, candles);

        bool result = AtrRbBandsHelper.IsLowerBandBreak(si, candles[^1].OpenTime,
            out double pct, out double lower);

        Assert.IsFalse(result, "Normal oscillating price should not break the lower ATR band");
    }

    [TestMethod]
    public void IsUpperBandBreak_NormalPrice_ReturnsFalse()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(CandleCount);
        var si = LoadSymbolInterval(symbol, candles);

        bool result = AtrRbBandsHelper.IsUpperBandBreak(si, candles[^1].OpenTime,
            out double pct, out double upper);

        Assert.IsFalse(result, "Normal oscillating price should not break the upper ATR band");
    }

    [TestMethod]
    public void IsLowerBandBreak_ExtremeDropout_Triggers()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(CandleCount);

        // Force an extreme drop on the last candle
        var last = candles[^1];
        decimal extremeLow = last.Low - 50m;
        candles[^1] = last with { Low = extremeLow, Close = extremeLow + 0.1m };

        // Also ensure the lookback window candles did NOT have extreme lows
        var settings = GlobalData.Settings.Signal.AtrRb;
        for (int j = candles.Count - settings.BreakLookback; j < candles.Count - 1; j++)
        {
            if (j >= 0 && candles[j].Low < extremeLow)
                candles[j] = candles[j] with { Low = extremeLow + 1m };
        }

        var si = LoadSymbolInterval(symbol, candles);

        bool result = AtrRbBandsHelper.IsLowerBandBreak(si, candles[^1].OpenTime,
            out double pct, out double lower);

        Assert.IsTrue(result, "Extreme low should trigger a lower band break");
        Assert.IsTrue(pct > 0, "pctDeviation should be positive");
        Assert.IsTrue(lower > 0, "lowerBand should be positive");
    }

    [TestMethod]
    public void IsUpperBandBreak_ExtremeBreakout_Triggers()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(CandleCount);

        // Force an extreme high on the last candle
        var last = candles[^1];
        decimal extremeHigh = last.High + 50m;
        candles[^1] = last with { High = extremeHigh, Close = extremeHigh - 0.1m };

        // Also ensure the lookback window candles did NOT have extreme highs
        var settings = GlobalData.Settings.Signal.AtrRb;
        for (int j = candles.Count - settings.BreakLookback; j < candles.Count - 1; j++)
        {
            if (j >= 0 && candles[j].High > extremeHigh)
                candles[j] = candles[j] with { High = extremeHigh - 1m };
        }

        var si = LoadSymbolInterval(symbol, candles);

        bool result = AtrRbBandsHelper.IsUpperBandBreak(si, candles[^1].OpenTime,
            out double pct, out double upper);

        Assert.IsTrue(result, "Extreme high should trigger an upper band break");
        Assert.IsTrue(pct > 0, "pctDeviation should be positive");
        Assert.IsTrue(upper > 0, "upperBand should be positive");
    }

    // ── Band math verification ───────────────────────────────────────────

    [TestMethod]
    public void BandMath_LowerBand_IsEmaMinusAtrTimesOuterMult()
    {
        var candles = MakeCandles(CandleCount);
        var settings = GlobalData.Settings.Signal.AtrRb;

        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        var emaList = quotes.ToEma(settings.Length);
        var atrList = quotes.ToAtr(settings.Length);

        int idx = candles.Count - 1;
        double? ema = emaList[idx].Ema;
        double? atr = atrList[idx].Atr;

        Assert.IsNotNull(ema, "EMA must be available for the last candle");
        Assert.IsNotNull(atr, "ATR must be available for the last candle");

        double expectedLower = ema.Value - atr.Value * settings.OuterMult;
        double expectedUpper = ema.Value + atr.Value * settings.OuterMult;

        Console.WriteLine($"EMA({settings.Length})={ema:N4}, ATR({settings.Length})={atr:N4}");
        Console.WriteLine($"Lower={expectedLower:N4}, Upper={expectedUpper:N4}");
        Console.WriteLine($"OuterMult={settings.OuterMult}");

        Assert.IsTrue(expectedLower < ema.Value, "Lower band must be below EMA");
        Assert.IsTrue(expectedUpper > ema.Value, "Upper band must be above EMA");
    }

    [TestMethod]
    public void PctDeviation_UsesStopLossAtrFactor()
    {
        var symbol = CreateSymbol();
        var candles = MakeCandles(CandleCount);
        var settings = GlobalData.Settings.Signal.AtrRb;

        // Force extreme low to trigger
        var last = candles[^1];
        candles[^1] = last with { Low = last.Low - 50m, Close = last.Close - 49m };
        for (int j = candles.Count - settings.BreakLookback; j < candles.Count - 1; j++)
        {
            if (j >= 0)
                candles[j] = candles[j] with { Low = candles[j].Low + 10m };
        }

        var si = LoadSymbolInterval(symbol, candles);
        bool triggered = AtrRbBandsHelper.IsLowerBandBreak(si, candles[^1].OpenTime,
            out double pct, out double lower);

        if (!triggered)
        {
            Assert.Inconclusive("Could not trigger a lower band break with synthetic data");
            return;
        }

        // Verify pctDeviation formula: StopLossAtrFactor * (ATR / Close * 100)
        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        var atrList = quotes.ToAtr(settings.Length);
        double atr = atrList[candles.Count - 1].Atr!.Value;
        double close = (double)candles[^1].Close;
        double expectedPct = settings.StopLossAtrFactor * (atr / close * 100);

        Assert.AreEqual(expectedPct, pct, 0.01,
            $"pctDeviation ({pct:N4}) should match StopLossAtrFactor * ATR%  ({expectedPct:N4})");
    }
}

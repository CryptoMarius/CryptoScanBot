using CryptoScanner.Analyzers.KumoSqueeze.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Skender.Stock.Indicators;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Analyzer.Ichimoku;

/// <summary>
/// Pins down which result row of <c>ToIchimoku</c> carries the cloud that belongs to the current
/// candle - the row the kumosqueeze and ichimokukumobreakout strategies must read.
/// <para>
/// Skender.Stock.Indicators already shifts the Senkou spans forward: the values on row i are
/// computed from row i - senkouOffset (Ichimoku.StaticSeries: <c>results[i - senkouOffset]</c>, and
/// the XML documentation of CalculateSenkouSpanB: "Uses historical data from range [currentIndex -
/// senkouOffset - senkouBPeriods + 1, currentIndex - senkouOffset]"). So the LAST row is the cloud
/// that sits under the current candle, and its TenkanSen/KijunSen are that candle's own lines.
/// </para>
/// <para>
/// The strategies used to read row (count - 1 - kijunPeriods) instead, which is the cloud that sat
/// under the price 26 candles ago plus a Tenkan/Kijun pair of the same age. These tests fail on that
/// code and pass on the corrected version.
/// </para>
/// <para>
/// The synthetic series is a step: flat low candles followed by flat high candles. The step makes the
/// two rows provably different, and every expected value is recomputed from the raw highs and lows so
/// nothing is taken from the library on trust. The second test drives
/// <see cref="KumoSqueezeSignalBase.GetIchimokuCloud"/> itself; it needs no database and no indicator
/// hub, only a hand-built symbol with a populated CandleList, in the same way AtrRbBandsTests builds
/// them (IndicatorEngine.CollectCandles demands at least 260 candles on the interval).
/// </para>
/// </summary>
[TestClass]
public class IchimokuCloudAlignmentTests
{
    private const int TenkanPeriods = 9;
    private const int KijunPeriods = 26;
    private const int SenkouBPeriods = 52;

    // Skender's default Senkou offset, the number of periods the spans are shifted forward.
    private const int SenkouOffset = 26;

    private const byte TickDec = 4;

    // The step: the series is flat low up to this index and flat high after it. The index differs per
    // test so that the step always falls inside the window the LAST row's cloud is built from, which
    // is what makes that row provably different from the row the old code read.
    private const decimal LowClose = 100m;
    private const decimal HighClose = 200m;

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

    // ── Synthetic data ───────────────────────────────────────────────────

    private static decimal StepClose(int index, int lowCandles) => index < lowCandles ? LowClose : HighClose;

    /// <summary>
    /// Skender quotes: flat around <see cref="LowClose"/> up to <paramref name="lowCandles"/>, flat
    /// around <see cref="HighClose"/> after it. High and low sit half a point either side of the close.
    /// </summary>
    private static List<IQuote> MakeQuotes(int count, int lowCandles)
    {
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var list = new List<IQuote>(count);
        for (int i = 0; i < count; i++)
        {
            decimal close = StepClose(i, lowCandles);
            list.Add(new Quote(start.AddMinutes(15 * i), close, close + 0.5m, close - 0.5m, close, 1000m));
        }
        return list;
    }

    /// <summary>
    /// The same step series as <see cref="MakeQuotes"/>, but as CryptoCandles on a 15m grid so
    /// IndicatorEngine.CollectCandles can find them back by OpenTime.
    /// </summary>
    private static List<CryptoCandle> MakeCandles(int count, int lowCandles)
    {
        var list = new List<CryptoCandle>(count);
        for (int i = 0; i < count; i++)
        {
            decimal close = StepClose(i, lowCandles);
            list.Add(new CryptoCandle
            {
                TickDecimals = TickDec,
                // CandleTime counts minutes, and the 15m interval has Duration 15.
                OpenTime = new CandleTime((uint)((i + 1) * 15)),
                Open = close,
                High = close + 0.5m,
                Low = close - 0.5m,
                Close = close,
                Volume = 1000m,
            });
        }
        return list;
    }

    /// <summary>
    /// (highest high + lowest low) / 2 over the inclusive index range - the midpoint every Ichimoku
    /// line is built from.
    /// </summary>
    private static decimal Midpoint(IReadOnlyList<IQuote> quotes, int fromIndex, int toIndex)
    {
        decimal highest = decimal.MinValue;
        decimal lowest = decimal.MaxValue;
        for (int i = fromIndex; i <= toIndex; i++)
        {
            if (quotes[i].High > highest)
                highest = quotes[i].High;
            if (quotes[i].Low < lowest)
                lowest = quotes[i].Low;
        }
        return (highest + lowest) / 2;
    }

    // ── The library contract ─────────────────────────────────────────────

    [TestMethod]
    public void ToIchimoku_LastRow_CarriesTheCloudOfTheCurrentCandle()
    {
        // 200 candles with the step at 150: the cloud window of the last row (candles 122..173) sits
        // right across the step.
        List<IQuote> quotes = MakeQuotes(200, lowCandles: 150);
        List<IchimokuResult> resultList = quotes.ToIchimoku(TenkanPeriods, KijunPeriods, SenkouBPeriods).ToList();

        Assert.AreEqual(quotes.Count, resultList.Count, "ToIchimoku returns one row per quote");

        int last = resultList.Count - 1;
        int shifted = last - SenkouOffset;

        // Senkou Span B on the last row comes from the window that ends senkouOffset candles back.
        decimal expectedSpanB = Midpoint(quotes, shifted - SenkouBPeriods + 1, shifted);

        // Senkou Span A on the last row is the Tenkan/Kijun midpoint of that same shifted row.
        decimal expectedTenkanShifted = Midpoint(quotes, shifted - TenkanPeriods + 1, shifted);
        decimal expectedKijunShifted = Midpoint(quotes, shifted - KijunPeriods + 1, shifted);
        decimal expectedSpanA = (expectedTenkanShifted + expectedKijunShifted) / 2;

        // Tenkan and Kijun on the last row are NOT shifted: they are this candle's own lines.
        decimal expectedTenkanLast = Midpoint(quotes, last - TenkanPeriods + 1, last);
        decimal expectedKijunLast = Midpoint(quotes, last - KijunPeriods + 1, last);

        IchimokuResult cloud = resultList[last];

        Assert.IsNotNull(cloud.SenkouSpanA);
        Assert.IsNotNull(cloud.SenkouSpanB);
        Assert.AreEqual(expectedSpanB, cloud.SenkouSpanB!.Value, 0.00000001m,
            $"Senkou Span B on the last row must follow from the data 26..78 candles back, expected {expectedSpanB} but got {cloud.SenkouSpanB}");
        Assert.AreEqual(expectedSpanA, cloud.SenkouSpanA!.Value, 0.00000001m,
            $"Senkou Span A on the last row must be the Tenkan/Kijun midpoint of the row 26 candles back, expected {expectedSpanA} but got {cloud.SenkouSpanA}");
        Assert.AreEqual(expectedTenkanLast, cloud.TenkanSen!.Value, 0.00000001m,
            $"Tenkan-Sen on the last row is the line of the current candle, not a shifted one, expected {expectedTenkanLast} but got {cloud.TenkanSen}");
        Assert.AreEqual(expectedKijunLast, cloud.KijunSen!.Value, 0.00000001m,
            $"Kijun-Sen on the last row is the line of the current candle, not a shifted one, expected {expectedKijunLast} but got {cloud.KijunSen}");

        // And the row the old code read is a different cloud altogether.
        IchimokuResult stale = resultList[shifted];
        Assert.AreNotEqual(cloud.SenkouSpanA!.Value, stale.SenkouSpanA!.Value,
            "row (count - 1 - 26) carries the cloud of 26 candles ago and must differ from the last row");
        Assert.AreNotEqual(cloud.KijunSen!.Value, stale.KijunSen!.Value,
            "row (count - 1 - 26) carries a Kijun-Sen of 26 candles ago");
    }

    // ── GetIchimokuCloud ─────────────────────────────────────────────────

    /// <summary>
    /// Exposes the protected helper under test. Nothing else of the strategy is exercised.
    /// </summary>
    private sealed class CloudProbe : KumoSqueezeSignalBase
    {
        public IchimokuResult? ReadCloud(int tenkanPeriods, int kijunPeriods, int senkouBPeriods)
            => GetIchimokuCloud(tenkanPeriods, kijunPeriods, senkouBPeriods);
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

    [TestMethod]
    public void GetIchimokuCloud_ReturnsTheLastRowOfToIchimoku()
    {
        // CollectCandles wants at least 260 candles on the interval and then builds a 260 candle
        // window ending at the requested OpenTime.
        const int candleCount = 300;
        const int collectedCandles = 260;

        // The helper collects candles 40..299; the cloud window of the last row is candles 222..273, so
        // the step goes at 250 to fall inside it.
        List<CryptoCandle> candles = MakeCandles(candleCount, lowCandles: 250);

        CryptoSymbol symbol = CreateSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        symbolInterval.CandleList.Clear();
        foreach (CryptoCandle candle in candles)
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);

        var probe = new CloudProbe
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = CryptoTradeSide.Long,
            SignalStrategy = "kumosqueeze",
            CandleLast = new MyData
            {
                Candle = candles[^1],
                CandleData = new CryptoData(),
            },
        };

        IchimokuResult? cloud = probe.ReadCloud(TenkanPeriods, KijunPeriods, SenkouBPeriods);
        Assert.IsNotNull(cloud, "300 candles on a 15m interval is enough for an Ichimoku cloud");

        // The same calculation over the same window the helper collects.
        IReadOnlyList<IQuote> window = candles.Skip(candleCount - collectedCandles).AsQuotes();
        List<IchimokuResult> expectedList = window.ToIchimoku(TenkanPeriods, KijunPeriods, SenkouBPeriods).ToList();
        IchimokuResult expected = expectedList[^1];

        Assert.AreEqual(expected.SenkouSpanA!.Value, cloud!.SenkouSpanA!.Value, 0.00000001m,
            $"GetIchimokuCloud must hand back the LAST row - the cloud under the current candle - expected Span A {expected.SenkouSpanA} but got {cloud.SenkouSpanA}");
        Assert.AreEqual(expected.SenkouSpanB!.Value, cloud.SenkouSpanB!.Value, 0.00000001m,
            $"GetIchimokuCloud must hand back the LAST row - the cloud under the current candle - expected Span B {expected.SenkouSpanB} but got {cloud.SenkouSpanB}");
        Assert.AreEqual(expected.TenkanSen!.Value, cloud.TenkanSen!.Value, 0.00000001m,
            $"The Tenkan-Sen must be the one of the current candle, expected {expected.TenkanSen} but got {cloud.TenkanSen}");
        Assert.AreEqual(expected.KijunSen!.Value, cloud.KijunSen!.Value, 0.00000001m,
            $"The Kijun-Sen must be the one of the current candle, expected {expected.KijunSen} but got {cloud.KijunSen}");

        // Guard that the assertions above are not vacuous: the row the old code read differs.
        IchimokuResult stale = expectedList[^(SenkouOffset + 1)];
        Assert.AreNotEqual(expected.SenkouSpanA!.Value, stale.SenkouSpanA!.Value,
            "the step in the series must make row (count - 1 - 26) a different cloud");
    }
}

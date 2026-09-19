using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// Filling in the market trend of minutes that nobody measured.
/// <para>
/// Every restart that lasts longer than a minute leaves such a hole: the values from before it come
/// back with the barometer candles, but the minutes the scanner was down were never measured, and a
/// zero there draws as "the market was exactly neutral". MarketTrendBackfill computes them
/// afterwards by feeding the zigzag candle by candle, the same way the live scanner arrives at the
/// value once a minute.
/// </para>
/// </summary>
// GlobalData.AddQuoteData hands out the ONE shared USDT entry and the barometer reads its symbol
// list, so this may not run alongside other tests.
[DoNotParallelize]
[TestClass]
public class MarketTrendBackfillTests : TestBase
{
    private const string QuoteName = "USDT";

    /// <summary>The trend stamped on the coins themselves, which is what the LIVE measurement reads.
    /// Deliberately a value the zigzag below cannot produce, so the two are told apart.</summary>
    private const float StampedTrend = 42.5f;

    /// <summary>Candles per interval. Four full swings of the pattern below, which gives the zigzag
    /// enough pivots to call a trend at all.</summary>
    private const int CandlesPerInterval = 40;

    private double savedMinimalVolume;
    private bool savedFetchCandles;
    private List<CryptoSymbol> savedSymbolList = [];

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        var quote = GlobalData.AddQuoteData(QuoteName);
        savedMinimalVolume = quote.MinimalVolume;
        savedFetchCandles = quote.FetchCandles;
        savedSymbolList = quote.SymbolList;

        quote.MinimalVolume = 0;
        quote.FetchCandles = true;
        quote.SymbolList = [];

        // Independent of whatever test class ran before this one - see ResetBarometerSymbols.
        ResetBarometerSymbols(QuoteName);
    }

    [TestCleanup]
    public void Restore()
    {
        var quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = savedMinimalVolume;
        quote.FetchCandles = savedFetchCandles;
        quote.SymbolList = savedSymbolList;

        ResetBarometerSymbols(QuoteName);
    }


    /// <summary>
    /// A price that walks in swings: six candles up, three back down, and on balance upward (or
    /// downward when <paramref name="rising"/> is false). A straight line gives the zigzag no pivots
    /// at all and therefore no trend; the swings are what makes it read as higher highs and higher
    /// lows.
    /// </summary>
    private static decimal PriceAt(int index, bool rising)
    {
        int cycle = index / 9;
        int within = index % 9;
        decimal value = 100m + cycle * 12m + (within < 6 ? within * 3m : (6 * 3m) - (within - 5) * 4m);
        return rising ? value : 300m - value;
    }


    /// <summary>
    /// Coins with candles in EVERY interval. That is not decoration: the trend of a coin is the
    /// weighted sum over all intervals, and both the live calculation and the backfill give up on a
    /// coin that has an interval without candles.
    /// </summary>
    private static void BuildSymbols(CandleTime lastClosedMinute, int count, bool rising)
    {
        CryptoQuoteData quote = GlobalData.AddQuoteData(QuoteName);

        for (int i = 0; i < count; i++)
        {
            string coin = $"MTB{i}";
            CryptoSymbol symbol = new()
            {
                Id = 600 + i,
                Name = coin + QuoteName,
                Base = coin,
                Quote = QuoteName,
                Exchange = GlobalData.ActiveExchange!,
                ExchangeId = GlobalData.ActiveExchange!.Id,
                ExchangeName = coin + QuoteName,
                QuoteData = quote,
                PriceTickSize = 0.01m,
                Volume = 1_000_000_000,
                Status = 1,
            };

            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                    continue;

                // The 1m series also has to cover the whole window the barometer draws, so it gets
                // as many candles as there are minutes in it.
                int candleCount = interval.IntervalPeriod == CryptoIntervalPeriod.interval1m
                    ? Constants.BarometerGraphHours * 60 + 60
                    : CandlesPerInterval;

                CryptoCandleList candleList = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                CandleTime last = IntervalTools.StartOfIntervalCandle(lastClosedMinute, interval.Duration);
                for (int index = 0; index < candleCount; index++)
                {
                    CandleTime openTime = last - (uint)((candleCount - 1 - index) * interval.Duration);
                    decimal price = PriceAt(index, rising);
                    candleList.TryAdd(openTime, new CryptoCandle
                    {
                        OpenTime = openTime,
                        TickDecimals = 2,
                        Open = price,
                        High = price + 0.5m,
                        Low = price - 0.5m,
                        Close = price,
                    });
                }
            }

            // What the LIVE measurement reads. The backfill ignores it and builds its own zigzag,
            // which is exactly how the two are told apart below.
            symbol.Data.TrendPrimary.Time = lastClosedMinute;
            symbol.Data.TrendPrimary.Percentage = StampedTrend;
            symbol.Data.TrendSecondary.Time = lastClosedMinute;
            symbol.Data.TrendSecondary.Percentage = StampedTrend;

            quote.SymbolList.Add(symbol);
        }
    }


    [TestMethod]
    public void RisingMarketGivesAPositiveTrendForEveryMinute()
    {
        CandleTime lastClosedMinute = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - 1;
        BuildSymbols(lastClosedMinute, 5, rising: true);

        CandleTime from = lastClosedMinute - 60;
        var series = MarketTrendBackfill.Measure(GlobalData.AddQuoteData(QuoteName).SymbolList,
            from, lastClosedMinute, MarketTrend.MinimumSymbols);

        Assert.AreEqual(61, series.Count, "every minute of the window should get a value");
        foreach ((CandleTime minute, (decimal? primary, decimal? secondary)) in series)
        {
            Assert.IsTrue(primary.HasValue && secondary.HasValue, $"{minute} has no value");
            Assert.IsTrue(primary!.Value > 0m, $"{minute} should read as a rising market, not {primary}");
            Assert.IsTrue(primary.Value <= 100m, $"{minute} is outside -100..+100: {primary}");
            Assert.IsTrue(secondary!.Value is > -100m and <= 100m, $"{minute} is outside -100..+100: {secondary}");
        }
    }


    [TestMethod]
    public void FallingMarketGivesANegativeTrend()
    {
        CandleTime lastClosedMinute = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - 1;
        BuildSymbols(lastClosedMinute, 5, rising: false);

        CandleTime from = lastClosedMinute - 60;
        var series = MarketTrendBackfill.Measure(GlobalData.AddQuoteData(QuoteName).SymbolList,
            from, lastClosedMinute, MarketTrend.MinimumSymbols);

        Assert.AreEqual(61, series.Count);
        foreach ((CandleTime minute, (decimal? primary, decimal? _)) in series)
            Assert.IsTrue(primary!.Value < 0m, $"{minute} should read as a falling market, not {primary}");
    }


    [TestMethod]
    public void StaysEmptyBelowTheMinimum()
    {
        // Four coins against a floor of five: the same rule the live measurement applies, because an
        // average over four coins is the trend of four coins.
        CandleTime lastClosedMinute = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - 1;
        BuildSymbols(lastClosedMinute, 4, rising: true);

        var series = MarketTrendBackfill.Measure(GlobalData.AddQuoteData(QuoteName).SymbolList,
            lastClosedMinute - 10, lastClosedMinute, MarketTrend.MinimumSymbols);

        Assert.AreEqual(0, series.Count);
    }


    [TestMethod]
    public void TheBarometerFillsTheBacklogAndLeavesMeasuredMinutesAlone()
    {
        // The whole point, end to end: after one calculation the newest minute carries the value the
        // LIVE measurement produced and every older minute was never measured by anybody. The backfill
        // has to fill those older ones and keep its hands off the one that was measured.
        CandleTime lastClosedMinute = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - 1;
        BuildSymbols(lastClosedMinute, 5, rising: true);

        BarometerTools.CalculatePriceBarometerForQuote(GlobalData.AddQuoteData(QuoteName));

        Assert.IsTrue(GlobalData.ActiveExchange!.TryGetSymbolByPair(Constants.SymbolNameBarometerExtra + QuoteName,
            out CryptoSymbol? extraSymbol));
        CryptoCandleList extraCandles = extraSymbol!.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;

        Assert.IsTrue(extraCandles.TryGetValue(lastClosedMinute, out CryptoCandle newest));
        Assert.AreEqual((decimal)StampedTrend, BarometerCandleFields.Read(newest, BarometerGraphValue.MarketTrendPrimary),
            "the minute the live measurement wrote may not be recomputed");

        Assert.IsTrue(extraCandles.TryGetValue(lastClosedMinute - 30, out CryptoCandle older),
            "the minutes before it should have barometer candles");
        decimal filled = BarometerCandleFields.Read(older, BarometerGraphValue.MarketTrendPrimary);
        Assert.AreNotEqual(0m, filled, "a minute nobody measured should have been calculated afterwards");
        Assert.AreNotEqual((decimal)StampedTrend, filled, "and calculated, not copied from the live measurement");
        Assert.IsTrue(filled is > -100m and <= 100m, $"outside -100..+100: {filled}");
    }
}

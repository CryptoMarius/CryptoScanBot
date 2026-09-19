using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The market trend: the average trend percentage over the coins of one quote coin.
/// <para>
/// The live scanner measured it nowhere, so both market trend figures were a flat zero line on the
/// dashboard graph while the emulator filled them from its own copy of this calculation. What these
/// tests guard is the rule that copy was collapsed into: which coins take part, and that a
/// measurement resting on too few of them is not reported at all.
/// </para>
/// </summary>
// AddQuoteData hands out the ONE shared USDT entry, so the minimum set here is visible to every
// other test that reads it. Hence the restore in cleanup and no parallel run alongside others.
[DoNotParallelize]
[TestClass]
public class MarketTrendTests : TestBase
{
    private const string QuoteName = "USDT";

    private double savedMinimalVolume;
    private bool savedFetchCandles;

    [TestInitialize]
    public void Setup()
    {
        // GetSymbolInterval indexes a list built from GlobalData.IntervalList, so the intervals have
        // to exist before a symbol can be asked for one.
        InitTestSession();
        var quote = GlobalData.AddQuoteData(QuoteName);
        savedMinimalVolume = quote.MinimalVolume;
        savedFetchCandles = quote.FetchCandles;
        quote.MinimalVolume = 0;   // EnoughVolume() answers true, so volume plays no part here
        quote.FetchCandles = true; // a quote that is not fetched takes no part in the measurement
    }

    [TestCleanup]
    public void Restore()
    {
        var quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = savedMinimalVolume;
        quote.FetchCandles = savedFetchCandles;
    }


    private static CandleTime LastMinute =>
        CandleTime.AlignFromDateTime(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 1);


    /// <summary>
    /// One coin per trend percentage, with that trend already calculated and marked as current.
    /// <para>
    /// The marking is what keeps this a test of the averaging rather than of the zigzag underneath
    /// it: CalculateSymbolTrendAsync recomputes only when the candles have moved past the time on
    /// the trend data, so a trend stamped at the coin's newest candle is handed back untouched.
    /// </para>
    /// </summary>
    private static List<CryptoSymbol> Build(string baseName, (float Primary, float Secondary)[] trends)
    {
        ExchangeModel exchange = new() { Id = 98, Name = "MarketTrendTestExchange" };
        CryptoQuoteData quote = GlobalData.AddQuoteData(QuoteName);

        List<CryptoSymbol> symbols = [];
        for (int i = 0; i < trends.Length; i++)
        {
            string coin = $"{baseName}{i}";
            CryptoSymbol symbol = new()
            {
                Id = 800 + i,
                Name = coin + QuoteName,
                Base = coin,
                Quote = QuoteName,
                Exchange = exchange,
                ExchangeId = exchange.Id,
                ExchangeName = coin + QuoteName,
                QuoteData = quote,
                PriceTickSize = 0.01m,
                Volume = 1_000_000_000,
            };

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            symbolInterval.CandleList.TryAdd(LastMinute, new CryptoCandle { OpenTime = LastMinute, Close = 100m });

            symbol.Data.TrendPrimary.Time = LastMinute;
            symbol.Data.TrendPrimary.Percentage = trends[i].Primary;
            symbol.Data.TrendSecondary.Time = LastMinute;
            symbol.Data.TrendSecondary.Percentage = trends[i].Secondary;

            symbols.Add(symbol);
        }

        return symbols;
    }


    [TestMethod]
    public void AveragesTheTrendOfEveryCoinThatTookPart()
    {
        // 100, 60, 20, -20, -60 averages 20; the secondary set averages -10.
        List<CryptoSymbol> symbols = Build("MTA",
            [(100f, 0f), (60f, -20f), (20f, -40f), (-20f, 10f), (-60f, 0f)]);

        (decimal? primary, decimal? secondary) = MarketTrend.Measure(symbols, MarketTrend.MinimumSymbols);

        Assert.AreEqual(20m, primary);
        Assert.AreEqual(-10m, secondary);
    }


    [TestMethod]
    public void StaysEmptyBelowTheMinimum()
    {
        // Four coins, minimum five: an average over four coins is the trend of four coins with the
        // word "market" written on it, and the caller keeps its previous value instead.
        List<CryptoSymbol> symbols = Build("MTB", [(100f, 100f), (60f, 60f), (20f, 20f), (-20f, -20f)]);

        (decimal? primary, decimal? secondary) = MarketTrend.Measure(symbols, MarketTrend.MinimumSymbols);

        Assert.IsNull(primary);
        Assert.IsNull(secondary);
    }


    [TestMethod]
    public void BothTrendsAreCountedSeparately()
    {
        // One coin has no secondary trend yet - too little history for that zigzag setting. It must
        // lower neither the count nor the sum of the secondary average, or a coin that is missing
        // would silently pull the market towards zero.
        List<CryptoSymbol> symbols = Build("MTC",
            [(30f, 30f), (30f, 30f), (30f, 30f), (30f, 30f), (30f, 30f), (30f, 30f)]);
        symbols[^1].Data.TrendSecondary.Percentage = null;

        (decimal? primary, decimal? secondary) = MarketTrend.Measure(symbols, MarketTrend.MinimumSymbols);

        Assert.AreEqual(30m, primary, "six coins at 30");
        Assert.AreEqual(30m, secondary, "five coins at 30, not six with one counted as zero");
    }


    [TestMethod]
    public void BarometerSymbolsDoNotMeasureThemselves()
    {
        // The barometer symbols live in the same symbol list of the quote coin. They hold the
        // measurement itself, so a trend of their own would be the market measuring its own
        // thermometer - the same rule the barometer applies in CryptoBarometerPrice.
        List<CryptoSymbol> symbols = Build("MTD",
            [(30f, 30f), (30f, 30f), (30f, 30f), (30f, 30f), (30f, 30f)]);

        List<CryptoSymbol> withBarometer = [.. Build("$BMP", [(-100f, -100f)]), .. symbols];
        Assert.IsTrue(withBarometer[0].IsBarometerSymbol(), "the fixture has to produce a barometer symbol");

        (decimal? primary, decimal? secondary) = MarketTrend.Measure(withBarometer, MarketTrend.MinimumSymbols);

        Assert.AreEqual(30m, primary);
        Assert.AreEqual(30m, secondary);
    }
}

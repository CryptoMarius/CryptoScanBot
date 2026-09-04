using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The barometer of a replay: the same measurement as the live scanner, but over an explicit list of
/// symbols instead of the full pool of a quote coin.
/// <para>
/// A replay has no barometer timer, so every run used to carry a barometer of zero and every
/// barometer condition passed against a value that was never calculated. What these tests guard is
/// the pair that makes the emulator path trustworthy: the figures have to match the percentages that
/// went in, and a measurement resting on too few coins may not be stored at all - a "market" of
/// three coins is the price change of three coins.
/// </para>
/// </summary>
// AddQuoteData hands out the ONE shared USDT entry, so the minimum set here is visible to every
// other test that reads it. Hence the restore in cleanup and no parallel run alongside others.
[DoNotParallelize]
[TestClass]
public class BarometerForSymbolsTests : TestBase
{
    private const string QuoteName = "USDT";
    private const decimal BasePrice = 100m;

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
        quote.FetchCandles = true; // a quote that is not fetched takes no part in the barometer
    }

    [TestCleanup]
    public void Restore()
    {
        var quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = savedMinimalVolume;
        quote.FetchCandles = savedFetchCandles;
    }


    /// <summary>
    /// One coin per percentage, each with a 1m candle at the start and at the end of the interval.
    /// The exchange is a fresh one per test, so the barometer values land on an object no other test
    /// reads.
    /// </summary>
    private static (ExchangeModel Exchange, List<CryptoSymbol> Symbols) Build(decimal[] percentages,
        CryptoInterval interval, CandleTime lastMinute)
    {
        ExchangeModel exchange = new() { Id = 99, Name = "BarometerTestExchange" };
        CryptoQuoteData quote = GlobalData.AddQuoteData(QuoteName);
        CandleTime firstMinute = lastMinute - interval.Duration;

        List<CryptoSymbol> symbols = [];
        for (int i = 0; i < percentages.Length; i++)
        {
            string coin = $"BAR{i}";
            CryptoSymbol symbol = new()
            {
                Id = 900 + i,
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
            symbolInterval.CandleList.TryAdd(firstMinute, new CryptoCandle { OpenTime = firstMinute, Close = BasePrice });
            symbolInterval.CandleList.TryAdd(lastMinute, new CryptoCandle
            {
                OpenTime = lastMinute,
                Close = BasePrice + BasePrice * percentages[i] / 100m,
            });

            symbols.Add(symbol);
        }

        return (exchange, symbols);
    }


    private static CryptoInterval Interval15m => GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];

    private static CandleTime LastMinute =>
        CandleTime.AlignFromDateTime(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 1);


    [TestMethod]
    public void EveryFigureFollowsFromThePercentagesThatWentIn()
    {
        // arrange - five coins: +1%, +2%, -1%, 0%, +3%
        var (exchange, symbols) = Build([1m, 2m, -1m, 0m, 3m], Interval15m, LastMinute);
        BarometerResult result = new();

        // act
        bool stored = BarometerTools.CalculateForSymbols(exchange, symbols[0].QuoteData!, symbols,
            Interval15m, LastMinute, 5, result);

        // assert - the average is (1+2-1+0+3)/5 = 1, the median of [-1,0,1,2,3] is 1, and three of
        // the five rose, which is 60 on the 0..100 scale the rest of the code uses.
        Assert.IsTrue(stored, "a measurement over five coins should be stored");
        CryptoBarometerData data = exchange.Data.GetBarometer(QuoteName, CryptoIntervalPeriod.interval15m);
        Assert.AreEqual(1m, data.PriceBarometer, "average");
        Assert.AreEqual(1m, data.PriceMedian, "median");
        Assert.AreEqual(60m, data.PricePercentageRising, "breadth");
        Assert.AreEqual(5, data.PriceSymbolCount, "number of coins that took part");
        Assert.AreEqual(0, data.PriceOutlierCount, "no coin is an outlier here");
        Assert.AreEqual(LastMinute, data.PriceDateTime, "the moment measured");
        // How far the typical coin moved regardless of direction: (1+2+1+0+3)/5 = 1.4
        Assert.AreEqual(1.4m, data.PriceMovement, "movement");
    }


    [TestMethod]
    public void TooFewCoinsStoreNothingAtAll()
    {
        // arrange - four coins against a minimum of five
        var (exchange, symbols) = Build([1m, 2m, -1m, 0m], Interval15m, LastMinute);
        BarometerResult result = new();

        // act
        bool stored = BarometerTools.CalculateForSymbols(exchange, symbols[0].QuoteData!, symbols,
            Interval15m, LastMinute, 5, result);

        // assert - not a number that describes no market, but nothing. A stored zero would read as a
        // flat market, and a condition would silently decide on it.
        Assert.IsFalse(stored, "four coins is under the minimum");
        CryptoBarometerData data = exchange.Data.GetBarometer(QuoteName, CryptoIntervalPeriod.interval15m);
        Assert.IsNull(data.PriceBarometer, "nothing may be stored below the minimum");
        Assert.IsNull(data.PriceDateTime, "not even the moment");
    }


    [TestMethod]
    public void AMissingCandleLeavesTheCoinOutInsteadOfCountingItAsZero()
    {
        // arrange - five coins, but one of them has no candle at the start of the interval
        var (exchange, symbols) = Build([1m, 2m, -1m, 0m, 3m], Interval15m, LastMinute);
        CandleTime firstMinute = LastMinute - Interval15m.Duration;
        symbols[4].GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.Remove(firstMinute);
        BarometerResult result = new();

        // act - the minimum is four here, so the measurement itself may still be stored
        bool stored = BarometerTools.CalculateForSymbols(exchange, symbols[0].QuoteData!, symbols,
            Interval15m, LastMinute, 4, result);

        // assert - four coins took part, and the +3% of the fifth is absent instead of a 0% that
        // would drag the average down
        Assert.IsTrue(stored);
        CryptoBarometerData data = exchange.Data.GetBarometer(QuoteName, CryptoIntervalPeriod.interval15m);
        Assert.AreEqual(4, data.PriceSymbolCount, "the coin without a candle may not take part");
        Assert.AreEqual(0.5m, data.PriceBarometer, "(1+2-1+0)/4 = 0.5");
    }


    /// <summary>
    /// The figures have to survive the trip to storage. Since 04-09-2026 that storage is the candle
    /// of the barometer symbol, not a table of its own: BarometerSnapshot held a second copy of a
    /// measurement the candle format already carries, was never cleared by the purge before a run,
    /// and grew to 5.566.145 rows over 151 runs.
    /// <para>
    /// A candle keeps its prices as integer ticks, so this also catches the failure that matters:
    /// writing a value before TickDecimals is set rounds it to whole numbers, and a barometer of
    /// -1,25 would read back as -1 without a word.
    /// </para>
    /// <para>
    /// Two decimals, because that is what the barometer symbols are created with (PriceTickSize
    /// 0.01, see BarometerTools.CheckBarometerSymbolPrecence) and what both dashboards show. The
    /// table this replaced kept full precision in TEXT columns, so a figure is rounded now where it
    /// was not before - on a percentage that moves in single percents and is read to two decimals,
    /// that is the format's own precision rather than a loss. Anything beyond ~21 ticks of headroom
    /// overflows the Int32 the candle stores, which is why eight decimals is not an option here.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryFigureSurvivesTheTripThroughTheCandleFields()
    {
        CryptoBarometerData data = new()
        {
            PriceBarometer = -1.25m,
            PriceMedian = -0.75m,
            PricePercentageRising = 42.5m,
            PriceSpread = 3.12m,
            PriceMovement = 1.88m,
            PriceBitcoinVersusMarket = -0.5m,
            PriceSymbolCount = 47,
            PriceOutlierCount = 2,
        };

        CryptoCandle primary = new() { TickDecimals = 2 };
        CryptoCandle extra = new() { TickDecimals = 2 };
        BarometerCandleFields.Store(ref primary, data);
        BarometerCandleFields.StoreExtra(ref extra, data);

        Assert.AreEqual(-1.25m, BarometerCandleFields.Read(primary, BarometerGraphValue.Average));
        Assert.AreEqual(-0.75m, BarometerCandleFields.Read(primary, BarometerGraphValue.Median));
        Assert.AreEqual(42.5m, BarometerCandleFields.Read(primary, BarometerGraphValue.Rising));
        Assert.AreEqual(3.12m, BarometerCandleFields.Read(primary, BarometerGraphValue.Spread));
        Assert.AreEqual(47m, BarometerCandleFields.Read(primary, BarometerGraphValue.SymbolCount));
        Assert.AreEqual(1.88m, BarometerCandleFields.Read(extra, BarometerGraphValue.Movement));
        Assert.AreEqual(-0.5m, BarometerCandleFields.Read(extra, BarometerGraphValue.BitcoinVersusMarket));
        Assert.AreEqual(2m, extra.High, "the outlier count sits in the free High field of $BMX");
    }


    /// <summary>
    /// A quote coin without a bitcoin pair has no bitcoin-against-the-market figure. A candle cannot
    /// hold "absent", so it stores a zero and the tooltip leaves the line out rather than showing
    /// one. What must NOT happen is that zero landing in another field.
    /// </summary>
    [TestMethod]
    public void AMeasurementWithoutBitcoinStoresAZeroAndNothingElseMoves()
    {
        CryptoBarometerData data = new()
        {
            PriceBarometer = 0.5m,
            PriceMovement = 1.5m,
            PriceBitcoinVersusMarket = null,
        };

        CryptoCandle extra = new() { TickDecimals = 2 };
        BarometerCandleFields.StoreExtra(ref extra, data);

        Assert.AreEqual(0m, BarometerCandleFields.Read(extra, BarometerGraphValue.BitcoinVersusMarket));
        Assert.AreEqual(1.5m, BarometerCandleFields.Read(extra, BarometerGraphValue.Movement),
            "movement keeps its own field");
    }
}

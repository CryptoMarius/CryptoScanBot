using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// Where the live scanner puts the market trend in the barometer candles.
/// <para>
/// The first attempt wrote it to the minute the calculation runs FOR, which sounds right and stored
/// nothing at all: the 1m candles of the minute in progress are still in the ticker cache (they are
/// flushed a few seconds after the minute closes), so the barometer measurement for that minute
/// fails and no candle is ever created for it. Both market trend figures therefore stayed a flat
/// zero line on the dashboard, exactly as before the measurement was built. What this test pins down
/// is the rule that replaced it: the trend lands on the newest minute that actually got a candle.
/// </para>
/// </summary>
// GlobalData.AddQuoteData hands out the ONE shared USDT entry and the barometer reads its symbol
// list, so this may not run alongside other tests.
[DoNotParallelize]
[TestClass]
public class BarometerMarketTrendWriteTests : TestBase
{
    private const string QuoteName = "USDT";
    private const decimal BasePrice = 100m;

    /// <summary>How much history each coin gets, in minutes. Enough for the 1d barometer to have a
    /// previous candle to compare against, so every interval in the loop produces candles.</summary>
    private const int HistoryMinutes = 24 * 60 + 10;

    private const float TrendPrimary = 42.5f;
    private const float TrendSecondary = -17.5f;

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

        quote.MinimalVolume = 0;   // EnoughVolume() answers true, so volume plays no part here
        quote.FetchCandles = true; // a quote that is not fetched takes no part in the barometer
        quote.SymbolList = [];     // the coins of this test only, put back in Restore()
    }

    [TestCleanup]
    public void Restore()
    {
        var quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = savedMinimalVolume;
        quote.FetchCandles = savedFetchCandles;
        quote.SymbolList = savedSymbolList;

        // The barometer symbols keep their candles between calculations; another test class must not
        // meet the ones written here.
        foreach (string baseName in new[] { Constants.SymbolNameBarometerPrice, Constants.SymbolNameBarometerExtra })
        {
            if (GlobalData.ActiveExchange!.TryGetSymbolByPair(baseName + QuoteName, out CryptoSymbol? symbol))
                symbol.ClearCandles();
        }
    }


    /// <summary>
    /// Five coins with a day of 1m candles and a trend that is already calculated.
    /// <para>
    /// The trend is stamped at the newest candle so CalculateSymbolTrendAsync hands it back instead
    /// of recomputing a zigzag - this is a test of WHERE the trend is written, not of what the
    /// zigzag makes of it.
    /// </para>
    /// <para>
    /// The prices move, and they may not all move alike. A coin list that stands perfectly still
    /// produces a barometer candle of four zeroes, and BarometerCandleFields.IsLegacyLayout reads
    /// four equal fields as a candle from before that layout existed - so the next calculation
    /// throws the candle away and builds it again. True for the real scanner as well; it costs
    /// nothing there because everything in such a candle is zero anyway, but a fixture that trips
    /// over it would be testing that clean-up instead of this rule.
    /// </para>
    /// </summary>
    private static void BuildSymbols(CandleTime lastClosedMinute)
    {
        CryptoQuoteData quote = GlobalData.AddQuoteData(QuoteName);

        for (int i = 0; i < 5; i++)
        {
            string coin = $"BMT{i}";
            CryptoSymbol symbol = new()
            {
                Id = 700 + i,
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

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            for (int minute = 0; minute <= HistoryMinutes; minute++)
            {
                CandleTime openTime = lastClosedMinute - minute;
                symbolInterval.CandleList.TryAdd(openTime, new CryptoCandle
                {
                    OpenTime = openTime,
                    // Without the decimals a candle keeps its prices in WHOLE ticks: every price
                    // below rounds to 100 flat, every percentage becomes zero, and the barometer
                    // candle that comes out is the all-zero one IsLegacyLayout throws away.
                    TickDecimals = 2,
                    Close = BasePrice + (minute + i) % 7 * 0.01m,
                });
            }

            symbol.Data.TrendPrimary.Time = lastClosedMinute;
            symbol.Data.TrendPrimary.Percentage = TrendPrimary;
            symbol.Data.TrendSecondary.Time = lastClosedMinute;
            symbol.Data.TrendSecondary.Percentage = TrendSecondary;

            quote.SymbolList.Add(symbol);
        }
    }


    [TestMethod]
    public void MarketTrendLandsOnTheNewestCandleThatExists()
    {
        // The minute in progress has no flushed 1m candles, so the newest complete minute is the one
        // before it - the same situation the live scanner is in on every calculation.
        CandleTime runningMinute = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
        CandleTime lastClosedMinute = runningMinute - 1;
        BuildSymbols(lastClosedMinute);

        BarometerTools.CalculatePriceBarometerForQuote(GlobalData.AddQuoteData(QuoteName));

        Assert.IsTrue(GlobalData.ActiveExchange!.TryGetSymbolByPair(Constants.SymbolNameBarometerExtra + QuoteName,
            out CryptoSymbol? extraSymbol), "the second barometer symbol should exist after a calculation");
        CryptoCandleList extraCandles = extraSymbol!.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;

        // The premise: nothing is written for the minute in progress, so a trend attached to it goes
        // nowhere. This is the assumption the first version got wrong.
        Assert.IsFalse(extraCandles.TryGetValue(runningMinute, out _),
            "the minute in progress has no 1m candles yet, so it gets no barometer candle either");

        Assert.IsTrue(extraCandles.TryGetValue(lastClosedMinute, out CryptoCandle newest),
            "the newest complete minute should have a barometer candle");
        Assert.AreEqual((decimal)TrendPrimary, BarometerCandleFields.Read(newest, BarometerGraphValue.MarketTrendPrimary));
        Assert.AreEqual((decimal)TrendSecondary, BarometerCandleFields.Read(newest, BarometerGraphValue.MarketTrendSecondary));

        // And only there: the trend can only be read as it stands now, so the minutes before it keep
        // what they had rather than carry today's market painted over their history.
        Assert.IsTrue(extraCandles.TryGetValue(lastClosedMinute - 5, out CryptoCandle earlier),
            "the minutes before it should have barometer candles as well");
        Assert.AreEqual(0m, BarometerCandleFields.Read(earlier, BarometerGraphValue.MarketTrendPrimary));
        Assert.AreEqual(0m, BarometerCandleFields.Read(earlier, BarometerGraphValue.MarketTrendSecondary));
    }


    [TestMethod]
    public void ASecondCalculationDoesNotWipeTheTrend()
    {
        // The barometer recalculates the last minutes on every run, so the candle that carries the
        // trend is written again a few seconds later. That pass may not blank it out.
        CandleTime lastClosedMinute = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - 1;
        BuildSymbols(lastClosedMinute);

        CryptoQuoteData quote = GlobalData.AddQuoteData(QuoteName);
        BarometerTools.CalculatePriceBarometerForQuote(quote);

        GlobalData.ActiveExchange!.TryGetSymbolByPair(Constants.SymbolNameBarometerExtra + QuoteName, out CryptoSymbol? extraSymbol);
        CryptoCandleList extraCandles = extraSymbol!.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
        Assert.IsTrue(extraCandles.TryGetValue(lastClosedMinute, out CryptoCandle afterFirst));
        Assert.AreEqual((decimal)TrendPrimary, BarometerCandleFields.Read(afterFirst, BarometerGraphValue.MarketTrendPrimary),
            "the first calculation should have stored the trend");

        // The second run measures nothing (the dashboard of the Avalonia shell calls it that way,
        // it runs on the UI thread). It rewrites the same minutes, and it may not blank the value
        // the run before it stored.
        BarometerTools.CalculatePriceBarometerForQuote(quote, measureMarketTrend: false);

        Assert.IsTrue(extraCandles.TryGetValue(lastClosedMinute, out CryptoCandle newest));
        Assert.AreEqual((decimal)TrendPrimary, BarometerCandleFields.Read(newest, BarometerGraphValue.MarketTrendPrimary));
        Assert.AreEqual((decimal)TrendSecondary, BarometerCandleFields.Read(newest, BarometerGraphValue.MarketTrendSecondary));
    }
}

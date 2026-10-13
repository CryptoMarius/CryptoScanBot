using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The market RSI: the 1h RSI of every coin of a quote, averaged. What these tests guard is the
/// value itself and the candles it may NOT look at - the 1h candle that closes at the signal moment
/// and the 1m candle of the minute that is still being processed - because those are the ones that
/// are not reliably there for every coin when a signal is evaluated.
/// </summary>
[DoNotParallelize]
[TestClass]
public class MarketRsiTests : TestBase
{
    private const string QuoteName = "MRSI";
    private static readonly CandleTime Moment =
        CandleTime.AlignFromDateTime(new DateTime(2026, 1, 15, 16, 0, 0, DateTimeKind.Utc), 1);

    private CryptoQuoteData quote = null!;

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = 0;
        quote.FetchCandles = true;
        MarketRsi.Reset();
    }

    [TestCleanup]
    public void Cleanup()
    {
        quote.SymbolList = [];
        MarketRsi.Reset();
    }


    /// <summary>
    /// Coins with 1h candles up to and including the hour that closes at the moment. Hour index 0 is
    /// the candle that closes at the moment itself, index 1 the one before it, and so on.
    /// </summary>
    private List<CryptoSymbol> Build(int coins, Func<int, int, decimal> hourClose)
    {
        ExchangeModel exchange = new() { Id = 97, Name = "MarketRsiExchange" };
        List<CryptoSymbol> symbols = [];
        for (int c = 0; c < coins; c++)
        {
            string coin = $"MR{c}";
            CryptoSymbol symbol = new()
            {
                Id = 800 + c,
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
            CryptoCandleList hours = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
            for (int h = 0; h < MarketRsi.HistoryHours + 2; h++)
            {
                CandleTime open = Moment - (uint)(60 * (h + 1));
                hours.TryAdd(open, new CryptoCandle { OpenTime = open, Close = hourClose(c, h) });
            }
            symbols.Add(symbol);
        }
        quote.SymbolList = symbols;
        return symbols;
    }

    private static void AddMinute(CryptoSymbol symbol, CandleTime open, decimal close)
    {
        symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList
            .TryAdd(open, new CryptoCandle { OpenTime = open, Close = close });
    }


    [TestMethod]
    public void RisingCoinsGiveHundredAndFallingCoinsZero()
    {
        // five coins that only rise and five that only fall: 100 and 0, averaged 50
        Build(10, (c, h) => c < 5 ? 1000m - h : 1000m + h);

        double? value = MarketRsi.Calculate(quote, Moment, out int count);

        Assert.AreEqual(10, count);
        Assert.IsNotNull(value);
        Assert.AreEqual(50.0, value.Value, 1e-9);
    }

    [TestMethod]
    public void TheHourThatClosesAtTheMomentIsNotUsed()
    {
        // every coin rises, except in the hour that closes at the moment itself, where it crashes
        Build(6, (c, h) => h == 0 ? 1m : 1000m - h);

        double? value = MarketRsi.Calculate(quote, Moment, out _);

        Assert.AreEqual(100.0, value!.Value, 1e-9, "the crash in the hour closing at the moment counted");
    }

    [TestMethod]
    public void ThePriceOfTheMinuteBeforeCountsAndTheCurrentMinuteDoesNot()
    {
        List<CryptoSymbol> symbols = Build(6, (c, h) => 1000m - h);

        // the minute that is still being processed (open T-1) crashes: ignored
        foreach (CryptoSymbol symbol in symbols)
            AddMinute(symbol, Moment - 1u, 1m);
        double? untouched = MarketRsi.Calculate(quote, Moment, out _);
        Assert.AreEqual(100.0, untouched!.Value, 1e-9, "the minute still being processed counted");

        // the minute before it (open T-2, closed at T-1) crashes: that is the last price, so it counts
        MarketRsi.Reset();
        foreach (CryptoSymbol symbol in symbols)
            AddMinute(symbol, Moment - 2u, 1m);
        double? crashed = MarketRsi.Calculate(quote, Moment, out _);
        Assert.IsTrue(crashed!.Value < 50.0, $"the price of the minute before did not count ({crashed.Value:N1})");
    }

    [TestMethod]
    public void ACoinWhoseNewestHourComesInLateJoinsAMinuteLater()
    {
        // six falling coins and one rising coin whose newest whole hour is not there yet
        List<CryptoSymbol> symbols = Build(7, (c, h) => c < 6 ? 1000m + h : 1000m - h);
        CryptoCandleList late = symbols[6].GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
        CandleTime newestHour = Moment - 120u;
        CryptoCandle newest = late[newestHour];
        late.Remove(newestHour);

        // 15:50: the newest whole hour is 14:00-15:00
        double? before = MarketRsi.Calculate(quote, Moment - 10u, out int countBefore);
        Assert.AreEqual(6, countBefore, "the late coin should wait, not count");
        Assert.AreEqual(0.0, before!.Value, 1e-9);

        // the candle arrives; the next minute it takes part
        late.TryAdd(newestHour, newest);
        double? after = MarketRsi.Calculate(quote, Moment - 9u, out int countAfter);
        Assert.AreEqual(7, countAfter);
        Assert.AreEqual(100.0 / 7, after!.Value, 1e-9);
    }

    [TestMethod]
    public void FewerThanFiveCoinsIsNoMarket()
    {
        Build(MarketRsi.MinimumSymbols - 1, (c, h) => 1000m - h);

        Assert.IsNull(MarketRsi.Calculate(quote, Moment, out int count));
        Assert.AreEqual(MarketRsi.MinimumSymbols - 1, count);
    }

    [TestMethod]
    public void TheFilterSkipsOnlyInsideTheBand()
    {
        List<CryptoSymbol> symbols = Build(10, (c, h) => c < 5 ? 1000m - h : 1000m + h); // market RSI 50
        SettingsCompiled settings = new() { MarketRsiActive = true, MarketRsiMinimum = 45m, MarketRsiMaximum = 55m };

        Assert.IsFalse(PositionTools.ValidMarketRsiConditions(symbols[0], Moment, settings, out string reaction));
        StringAssert.Contains(reaction, "Market RSI 50");

        settings.MarketRsiMinimum = 55m;
        settings.MarketRsiMaximum = 70m;
        Assert.IsTrue(PositionTools.ValidMarketRsiConditions(symbols[0], Moment, settings, out _));

        settings.MarketRsiActive = false;
        settings.MarketRsiMinimum = 0m;
        settings.MarketRsiMaximum = 100m;
        Assert.IsTrue(PositionTools.ValidMarketRsiConditions(symbols[0], Moment, settings, out _), "an inactive filter skipped");
    }

    [TestMethod]
    public void NoMarketRsiDoesNotStopTrading()
    {
        List<CryptoSymbol> symbols = Build(2, (c, h) => 1000m - h);
        SettingsCompiled settings = new() { MarketRsiActive = true, MarketRsiMinimum = 0m, MarketRsiMaximum = 100m };

        Assert.IsTrue(PositionTools.ValidMarketRsiConditions(symbols[0], Moment, settings, out string reaction));
        StringAssert.Contains(reaction, "not available");
    }
}

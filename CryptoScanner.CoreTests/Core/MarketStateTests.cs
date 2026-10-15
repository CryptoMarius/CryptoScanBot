using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The market state of a quote: a score from -100 to +100 out of BTC, the breadth and the market
/// over 72 hours. These tests guard how the three parts are combined, the direction of each part on
/// a market that clearly goes one way, the band of the filter, and that a missing BTC does not
/// block trading.
/// </summary>
[DoNotParallelize]
[TestClass]
public class MarketStateTests : TestBase
{
    private const string QuoteName = "MSTQ";
    private static readonly CandleTime Moment =
        CandleTime.AlignFromDateTime(new DateTime(2026, 3, 10, 16, 0, 0, DateTimeKind.Utc), 1);

    private CryptoQuoteData quote = null!;
    private CryptoSymbol? btc;

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = 0;
        quote.FetchCandles = true;
        MarketState.Reset();
    }

    [TestCleanup]
    public void Cleanup()
    {
        quote.SymbolList = [];
        if (btc != null)
            GlobalData.ActiveExchange!.SymbolListName.Remove(btc.Name);
        MarketState.Reset();
    }


    private CryptoSymbol NewSymbol(string coin, int id)
    {
        ExchangeModel exchange = new() { Id = 96, Name = "MarketStateExchange" };
        return new CryptoSymbol
        {
            Id = id,
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
    }

    /// <summary>
    /// Ten coins whose 1h close moves by <paramref name="hourlyStep"/> every hour, and a BTC whose
    /// candles of <paramref name="btcInterval"/> move by <paramref name="btcStep"/> per candle.
    /// </summary>
    private void Build(decimal hourlyStep, decimal btcStep, CryptoIntervalPeriod btcInterval = CryptoIntervalPeriod.interval4h)
    {
        List<CryptoSymbol> symbols = [];
        for (int c = 0; c < 10; c++)
        {
            CryptoSymbol symbol = NewSymbol($"MS{c}", 900 + c);
            CryptoCandleList hours = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
            for (int h = 0; h < MarketState.HistoryHours + 5; h++)
            {
                CandleTime open = Moment - (uint)(60 * (h + 1));
                hours.TryAdd(open, new CryptoCandle { OpenTime = open, Close = 1000m + (MarketState.HistoryHours - h) * hourlyStep });
            }
            symbols.Add(symbol);
        }
        quote.SymbolList = symbols;

        btc = NewSymbol("BTC", 950);
        CryptoSymbolInterval interval = btc.GetSymbolInterval(btcInterval);
        uint duration = interval.Interval.Duration;
        CandleTime last = Moment.AlignToIntervalMinutes(duration) - duration;
        for (int k = 0; k < 400; k++)
        {
            CandleTime open = last - (uint)(duration * k);
            interval.CandleList.TryAdd(open, new CryptoCandle { OpenTime = open, Close = 50000m + (400 - k) * btcStep });
        }
        GlobalData.ActiveExchange!.SymbolListName[btc.Name] = btc;
    }


    [TestMethod]
    public void Combine_AveragesTheThreeParts()
    {
        // Everything at its full value
        Assert.AreEqual(100.0, MarketState.Combine(100, 100, 12), 1e-9);
        Assert.AreEqual(-100.0, MarketState.Combine(-100, 0, -12), 1e-9);
        // Neutral: BTC 0, half the coins above the EMA50, market flat
        Assert.AreEqual(0.0, MarketState.Combine(0, 50, 0), 1e-9);
        // 75% above the EMA50 is +50, a market 3% up is +50 (6% counts as full), BTC +20
        Assert.AreEqual(40.0, MarketState.Combine(20, 75, 3), 1e-9);
        // Parts beyond their full value are clamped, so one extreme part cannot carry the score
        Assert.AreEqual((100.0 + 0 + 0) / 3, MarketState.Combine(500, 50, 0), 1e-9);
    }

    [TestMethod]
    public void Ema_IsSeededWithTheAverage()
    {
        double[] ema = MarketState.Ema([1, 2, 3, 4, 5], 3);

        Assert.AreEqual(2.0, ema[2], 1e-9);
        Assert.AreEqual(3.0, ema[3], 1e-9);
        Assert.AreEqual(4.0, ema[4], 1e-9);
    }

    [TestMethod]
    public void RisingMarket_ScoresHigh()
    {
        Build(hourlyStep: 1m, btcStep: 10m);

        MarketStateValue? value = MarketState.Calculate(quote, Moment, CryptoIntervalPeriod.interval4h);

        Assert.IsNotNull(value);
        Assert.IsTrue(value.BtcPart > 0, $"BTC part {value.BtcPart}");
        Assert.AreEqual(100.0, value.Breadth, 1e-9);
        Assert.IsTrue(value.Market72h > 0);
        Assert.AreEqual(10, value.SymbolCount);
        Assert.IsTrue(value.Score > 60, $"score {value.Score}");
    }

    [TestMethod]
    public void FallingMarket_ScoresLow()
    {
        Build(hourlyStep: -1m, btcStep: -10m);

        MarketStateValue? value = MarketState.Calculate(quote, Moment, CryptoIntervalPeriod.interval4h);

        Assert.IsNotNull(value);
        Assert.IsTrue(value.BtcPart < 0, $"BTC part {value.BtcPart}");
        Assert.AreEqual(0.0, value.Breadth, 1e-9);
        Assert.IsTrue(value.Market72h < 0);
        Assert.IsTrue(value.Score < -60, $"score {value.Score}");
    }

    [TestMethod]
    public void BtcAgainstTheCoins_PullsTheScoreDown()
    {
        Build(hourlyStep: 1m, btcStep: 10m);
        double allUp = MarketState.Calculate(quote, Moment, CryptoIntervalPeriod.interval4h)!.Score;
        Cleanup();
        Setup();

        // The coins rise, BTC falls
        Build(hourlyStep: 1m, btcStep: -10m);
        MarketStateValue value = MarketState.Calculate(quote, Moment, CryptoIntervalPeriod.interval4h)!;

        Assert.IsTrue(value.BtcPart < 0);
        Assert.IsTrue(value.Score < allUp, $"{value.Score} against {allUp}");
    }

    [TestMethod]
    public void TheIntervalOfTheBtcPartIsUsed()
    {
        // BTC only has 1h candles: on 4h there is no BTC part, on 1h there is
        Build(hourlyStep: 1m, btcStep: 10m, btcInterval: CryptoIntervalPeriod.interval1h);

        Assert.IsNull(MarketState.Calculate(quote, Moment, CryptoIntervalPeriod.interval4h));
        Assert.IsTrue(MarketState.Calculate(quote, Moment, CryptoIntervalPeriod.interval1h)!.Score > 60);
    }

    [TestMethod]
    public void WithoutBtc_TheFilterLetsEverythingThrough()
    {
        Build(hourlyStep: -1m, btcStep: -10m);
        GlobalData.ActiveExchange!.SymbolListName.Remove(btc!.Name);

        SettingsCompiled settings = new()
        {
            MarketStateActive = true,
            MarketStateMinimum = 60m,
            MarketStateMaximum = 100m,
        };
        bool valid = PositionTools.ValidMarketStateConditions(quote.SymbolList[0], Moment, settings, out string reaction);

        Assert.IsTrue(valid, "a missing measurement may not stop trading");
        Assert.AreEqual("Market state not available", reaction);
    }

    [TestMethod]
    public void TheFilterOnlyLetsTheBandThrough()
    {
        Build(hourlyStep: -1m, btcStep: -10m);

        SettingsCompiled settings = new()
        {
            MarketStateActive = true,
            MarketStateMinimum = 60m,
            MarketStateMaximum = 100m,
        };
        bool valid = PositionTools.ValidMarketStateConditions(quote.SymbolList[0], Moment, settings, out string reaction);

        Assert.IsFalse(valid);
        StringAssert.StartsWith(reaction, "Market state -");

        settings.MarketStateMinimum = -100m;
        Assert.IsTrue(PositionTools.ValidMarketStateConditions(quote.SymbolList[0], Moment, settings, out _));
    }
}

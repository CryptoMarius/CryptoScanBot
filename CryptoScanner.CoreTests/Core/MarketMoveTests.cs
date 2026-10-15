using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The market move since an entry, and the market exit built on it: a long leaves when the market
/// fell far enough, a short when it rose far enough, and only the trader side is set by the queue.
/// </summary>
[DoNotParallelize]
[TestClass]
public class MarketMoveTests : TestBase
{
    private const string QuoteName = "MMVQ";
    private static readonly CandleTime Moment =
        CandleTime.AlignFromDateTime(new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc), 1);

    private CryptoQuoteData quote = null!;

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = 0;
        quote.FetchCandles = true;
        MarketMove.Reset();
    }

    [TestCleanup]
    public void Cleanup()
    {
        quote.SymbolList = [];
        MarketMove.Reset();
    }

    /// <summary>Ten coins whose 1h close moves by <paramref name="hourlyPercent"/> percent every hour.</summary>
    private void Build(double hourlyPercent)
    {
        ExchangeModel exchange = new() { Id = 95, Name = "MarketMoveExchange" };
        List<CryptoSymbol> symbols = [];
        for (int c = 0; c < 10; c++)
        {
            string coin = $"MM{c}";
            CryptoSymbol symbol = new()
            {
                Id = 980 + c,
                Name = coin + QuoteName,
                Base = coin,
                Quote = QuoteName,
                Exchange = exchange,
                ExchangeId = exchange.Id,
                ExchangeName = coin + QuoteName,
                QuoteData = quote,
                PriceTickSize = 0.0001m,
                Volume = 1_000_000_000,
            };
            CryptoCandleList hours = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
            for (int h = 0; h < 48; h++)
            {
                CandleTime open = Moment - (uint)(60 * (h + 1));
                decimal close = (decimal)(100.0 * Math.Pow(1 + hourlyPercent / 100.0, 48 - h));
                hours.TryAdd(open, new CryptoCandle { OpenTime = open, TickDecimals = 6, Close = close });
            }
            symbols.Add(symbol);
        }
        quote.SymbolList = symbols;
    }


    [TestMethod]
    public void Since_MeasuresTheWholeHoursBetweenTheTwoMoments()
    {
        Build(hourlyPercent: -1);

        // Ten hours back: ten moves of -1%
        double? move = MarketMove.Since(quote, Moment - 600u, Moment);

        Assert.IsNotNull(move);
        Assert.AreEqual((Math.Pow(0.99, 10) - 1) * 100, move.Value, 1e-3);
    }

    [TestMethod]
    public void Since_WithinTheSameHourIsZero()
    {
        Build(hourlyPercent: -1);

        Assert.AreEqual(0.0, MarketMove.Since(quote, Moment - 20u, Moment - 5u));
    }

    [TestMethod]
    public void Since_TooFewCoinsIsNull()
    {
        Build(hourlyPercent: -1);
        quote.SymbolList = quote.SymbolList.Take(3).ToList();

        Assert.IsNull(MarketMove.Since(quote, Moment - 600u, Moment));
    }

    [TestMethod]
    public void MarketExitDue_LongOnAFallShortOnARise()
    {
        Assert.IsTrue(CryptoScanner.Core.Trader.PositionMonitor.MarketExitDue(CryptoTradeSide.Long, -3.0, 3m));
        Assert.IsFalse(CryptoScanner.Core.Trader.PositionMonitor.MarketExitDue(CryptoTradeSide.Long, -2.9, 3m));
        Assert.IsFalse(CryptoScanner.Core.Trader.PositionMonitor.MarketExitDue(CryptoTradeSide.Long, 5.0, 3m), "a rising market is not against a long");
        Assert.IsTrue(CryptoScanner.Core.Trader.PositionMonitor.MarketExitDue(CryptoTradeSide.Short, 3.5, 3m));
        Assert.IsFalse(CryptoScanner.Core.Trader.PositionMonitor.MarketExitDue(CryptoTradeSide.Short, -8.0, 3m), "a falling market is not against a short");
        Assert.IsFalse(CryptoScanner.Core.Trader.PositionMonitor.MarketExitDue(CryptoTradeSide.Long, -50.0, 0m), "zero means off");
    }

    [TestMethod]
    public void Queue_SetsTheTraderSideOnly()
    {
        SettingsTextual signal = new(), trading = new();
        SettingsTextualMarketExit original = trading.MarketExit;

        EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketExit = 5m }, signal, trading);

        Assert.IsTrue(trading.MarketExit.Active);
        Assert.AreEqual(5m, trading.MarketExit.Percentage);
        Assert.IsFalse(signal.MarketExit.Active, "the exit belongs to the trader");
        Assert.AreNotSame(original, trading.MarketExit, "a new object, so the queue can put the original back");

        EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketExit = 0m }, signal, trading);
        Assert.IsFalse(trading.MarketExit.Active, "zero switches it off");

        Assert.ThrowsExactly<NotSupportedException>(() =>
            EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketExit = -1m }, signal, trading));
    }
}

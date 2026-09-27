using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The per-symbol inactivity check (open point 34): where the silence is measured from, and how the
/// wait between two asks grows for a symbol that keeps bringing nothing back - the part that keeps
/// a thin coin from turning into a REST storm.
/// </summary>
[DoNotParallelize]
[TestClass]
public class SilentSymbolCatchUpTests : TestBase
{
    private static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);

    [TestInitialize]
    public void Setup() => InitTestSession();


    private static CryptoSymbol MakeSymbol()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        return new CryptoSymbol
        {
            Id = 1,
            Name = "TESTUSDT",
            Base = "TEST",
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDT"),
            PriceTickSize = 0.01m,
        };
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Since when a symbol is silent
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void WithoutAnyCandle_ThereIsNoSilenceToMeasure()
    {
        CryptoSymbol symbol = MakeSymbol();
        Assert.IsNull(SilentSymbolCatchUp.SilentSince(symbol));
    }

    [TestMethod]
    public void WithoutInventedCandles_TheSilenceStartsAtTheCloseOfTheNewestCandle()
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        CandleTime open = new(1000);
        symbolInterval.CandleList.TryAdd(open, new CryptoCandle { TickDecimals = 2, OpenTime = open, Open = 1m, High = 1m, Low = 1m, Close = 1m });

        Assert.AreEqual((open + 1u).ToDateTime(), SilentSymbolCatchUp.SilentSince(symbol));
    }

    [TestMethod]
    public void WithInventedCandles_TheSilenceStartsAtTheFirstInventedMinute()
    {
        // A cached ticker keeps the list growing with flat candles, so the newest candle says
        // nothing; the first invented minute does.
        CryptoSymbol symbol = MakeSymbol();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        CandleTime newest = new(1000);
        symbolInterval.CandleList.TryAdd(newest, new CryptoCandle { TickDecimals = 2, OpenTime = newest, Open = 1m, High = 1m, Low = 1m, Close = 1m });
        symbolInterval.SynthesizedFrom = new CandleTime(900);

        Assert.AreEqual(new CandleTime(900).ToDateTime(), SilentSymbolCatchUp.SilentSince(symbol));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The wait between two asks
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AnAskThatBroughtCandles_WaitsTheInactivityLimitAgain()
    {
        Assert.AreEqual(FiveMinutes, SilentSymbolCatchUp.NextWait(broughtSomething: true, previousWait: TimeSpan.FromHours(2), FiveMinutes));
    }

    [TestMethod]
    public void AnAskThatBroughtNothing_DoublesTheWait()
    {
        Assert.AreEqual(TimeSpan.FromMinutes(10), SilentSymbolCatchUp.NextWait(false, null, FiveMinutes));
        Assert.AreEqual(TimeSpan.FromMinutes(20), SilentSymbolCatchUp.NextWait(false, TimeSpan.FromMinutes(10), FiveMinutes));
        Assert.AreEqual(TimeSpan.FromMinutes(40), SilentSymbolCatchUp.NextWait(false, TimeSpan.FromMinutes(20), FiveMinutes));
    }

    [TestMethod]
    public void TheWaitNeverGrowsPastFourHours()
    {
        Assert.AreEqual(SilentSymbolCatchUp.MaxWait, SilentSymbolCatchUp.NextWait(false, TimeSpan.FromHours(3), FiveMinutes));
        Assert.AreEqual(SilentSymbolCatchUp.MaxWait, SilentSymbolCatchUp.NextWait(false, SilentSymbolCatchUp.MaxWait, FiveMinutes));
    }

    [TestMethod]
    public void ASilentCoinCostsOnlyAHandfulOfAsksPerDay()
    {
        // 5, 10, 20, 40, 80, 160, 240, 240, ... minutes: a coin that never trades again is asked
        // about nine times in the first day, not 288 times.
        TimeSpan? wait = null;
        TimeSpan elapsed = TimeSpan.Zero;
        int asks = 0;
        while (elapsed < TimeSpan.FromHours(24))
        {
            wait = SilentSymbolCatchUp.NextWait(false, wait, FiveMinutes);
            elapsed += wait.Value;
            asks++;
        }
        Assert.IsTrue(asks <= 10, $"{asks} asks in a day");
    }
}

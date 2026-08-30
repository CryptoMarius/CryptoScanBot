using CryptoScanner.Analyzers.FailedBreakout;
using CryptoScanner.Analyzers.FailedBreakout.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The failed breakout: price sets a new high or low over the lookback window and then closes back
/// inside it.
/// <para>
/// The candles are built by hand, so what the strategy is looking at is readable from the test. The
/// series is a quiet range with one candle poking out of it, which is exactly the situation the
/// strategy exists for and exactly the situation that is easy to get wrong by one candle - if the
/// level were taken from the break window itself, the break would set the level it is supposed to
/// have broken and nothing would ever fire.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class FailedBreakoutTests : TestBase
{
    private const int Lookback = 20;
    private const int BreakWindow = 3;

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        // Settings has an internal setter, so the shared instance is adjusted in place rather than
        // replaced - and put back in cleanup, because every test in the process reads that one object.
        FailedBreakoutPlugin.Settings.LookbackCandles = Lookback;
        FailedBreakoutPlugin.Settings.BreakWithinCandles = BreakWindow;
        FailedBreakoutPlugin.Settings.MinimumBreakPercentage = 0m;
    }

    [TestCleanup]
    public void Restore()
    {
        FailedBreakoutSettings fresh = new();
        FailedBreakoutPlugin.Settings.LookbackCandles = fresh.LookbackCandles;
        FailedBreakoutPlugin.Settings.BreakWithinCandles = fresh.BreakWithinCandles;
        FailedBreakoutPlugin.Settings.MinimumBreakPercentage = fresh.MinimumBreakPercentage;
    }


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

    private static CryptoInterval MakeInterval()
        => GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];


    /// <summary>
    /// Builds a series of <paramref name="count"/> candles ending at index 0 (the newest). The
    /// caller adjusts individual candles afterwards; everything not touched is a quiet candle
    /// inside the range 99..101.
    /// </summary>
    private static (SignalCreateBase Algorithm, CryptoCandle[] Candles) MakeSeries(
        CryptoTradeSide side, int count, Action<CryptoCandle[]> shape)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = MakeInterval();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        CryptoCandle[] candles = new CryptoCandle[count];
        for (int i = 0; i < count; i++)
        {
            candles[i] = new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((count - i) * interval.Duration)),
                Open = 100m,
                High = 101m,
                Low = 99m,
                Close = 100m,
            };
        }
        shape(candles);

        foreach (CryptoCandle candle in candles)
        {
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            symbolInterval.Data[candle.OpenTime] = new CryptoData();
        }

        return (new FailedBreakoutBase
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = "failedbreakout",
            CandleLast = new MyData { Candle = candles[0], CandleData = new CryptoData() },
        }, candles);
    }

    private static int Enough => Lookback + BreakWindow + 2;


    // ═══════════════════════════════════════════════════════════════════════
    //  The break that did not hold
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ABreakAboveThatClosesBackInside_IsAShortSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            // Candle 1 pokes above the 101 ceiling, the newest candle closes back under it.
            candles[1].High = 105m;
            candles[0].Close = 100m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    [TestMethod]
    public void ABreakBelowThatClosesBackInside_IsALongSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, candles =>
        {
            candles[1].Low = 95m;
            candles[0].Close = 100m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>
    /// The break is only interesting when it fails. One that is still holding - the newest candle
    /// closes above the old ceiling - is a breakout, and trading it as a reversal is backwards.
    /// </summary>
    [TestMethod]
    public void ABreakThatIsStillHolding_IsNoSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[1].High = 105m;
            candles[0].High = 106m;
            candles[0].Close = 104m;    // above the old ceiling of 101
        });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "still holding");
    }


    [TestMethod]
    public void AQuietRangeWithoutABreak_IsNoSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, _ => { });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no break");
    }


    /// <summary>
    /// A break older than the window does not count: by then price has had time to settle back and
    /// it is no longer the same event.
    /// </summary>
    [TestMethod]
    public void ABreakOlderThanTheWindow_IsNoSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[BreakWindow + 2].High = 105m;
        });

        Assert.IsFalse(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>
    /// With a minimum set, a break by a hair is not a break. Expressed as a percentage of the level,
    /// so it means the same thing on a coin at 65 000 and one at 0.01.
    /// </summary>
    [TestMethod]
    public void ABreakSmallerThanTheMinimum_IsNoSignal()
    {
        FailedBreakoutPlugin.Settings.MinimumBreakPercentage = 1m;   // 1% above 101 is 102.01

        var (tiny, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles => candles[1].High = 101.5m);
        Assert.IsFalse(tiny.IsSignal(), tiny.ExtraText);

        var (proper, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles => candles[1].High = 105m);
        Assert.IsTrue(proper.IsSignal(), proper.ExtraText);
    }


    /// <summary>
    /// At the start of a run there is not enough history yet. That has to be a quiet no, not an
    /// exception and not a signal taken against a level built from three candles.
    /// </summary>
    [TestMethod]
    public void WithoutEnoughHistory_ItSaysNoInsteadOfThrowing()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, 5, candles => candles[1].High = 105m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "not enough candles");
    }


    /// <summary>
    /// The level must come from BEFORE the break window. Taking it from the whole series would let
    /// the break set its own level, and then nothing could ever break it.
    /// </summary>
    [TestMethod]
    public void TheLevelIsTakenFromBeforeTheBreakWindow()
    {
        // Two candles inside the break window poke out. If the level were the maximum over
        // everything, it would be 105 and the close at 100 would never count as "back inside".
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[1].High = 105m;
            candles[2].High = 104m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }
}

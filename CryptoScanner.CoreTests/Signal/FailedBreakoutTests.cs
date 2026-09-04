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
        // Off for the tests above the range-position section: their quiet candles close at 100,
        // which is exactly the middle of the 99..101 range, and that is the one place the default
        // of 50 says no to both sides.
        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 100m;
        FailedBreakoutPlugin.Settings.RequireZone = [];
        FailedBreakoutPlugin.Settings.ZoneTolerancePercentage = 0m;
        _dlzIntervals = [.. GlobalData.Settings.Signal.ZonesDlz.IntervalList];
    }

    [TestCleanup]
    public void Restore()
    {
        FailedBreakoutSettings fresh = new();
        FailedBreakoutPlugin.Settings.LookbackCandles = fresh.LookbackCandles;
        FailedBreakoutPlugin.Settings.BreakWithinCandles = fresh.BreakWithinCandles;
        FailedBreakoutPlugin.Settings.MinimumBreakPercentage = fresh.MinimumBreakPercentage;
        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = fresh.CloseWithinRangePercentage;
        FailedBreakoutPlugin.Settings.RequireZone = fresh.RequireZone;
        FailedBreakoutPlugin.Settings.ZoneTolerancePercentage = fresh.ZoneTolerancePercentage;

        // The zone intervals are read from the global settings, which every test in the process
        // shares - so put back what was there rather than what the defaults say.
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = _dlzIntervals;
    }

    /// <summary>The zone intervals as they were before the test changed them.</summary>
    private List<string> _dlzIntervals = [];


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


    // ════════════════════════════════════════════════════════════════════════
    //  Where the close sits in the range
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// One wide candle breaks the ceiling AND the floor and closes low in the range. Before the
    /// range-position check that was a long and a short on the same candle (SUSHIUSDC, 04-09-2026).
    /// The close near the floor is a spring, so the long stays; the short is the move that already
    /// happened, so it goes.
    /// </summary>
    [TestMethod]
    public void ABreakOfBothLevelsThatClosesLow_IsOnlyALong()
    {
        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 50m;

        static void Whipsaw(CryptoCandle[] candles)
        {
            candles[0].High = 105m;
            candles[0].Low = 95m;
            candles[0].Close = 99.3m;    // 15% of the 99..101 range up from the floor
        }

        var (longSide, _) = MakeSeries(CryptoTradeSide.Long, Enough, Whipsaw);
        Assert.IsTrue(longSide.IsSignal(), longSide.ExtraText);

        var (shortSide, _) = MakeSeries(CryptoTradeSide.Short, Enough, Whipsaw);
        Assert.IsFalse(shortSide.IsSignal(), shortSide.ExtraText);
        StringAssert.Contains(shortSide.ExtraText, "too far from the broken level");
    }


    [TestMethod]
    public void ABreakOfBothLevelsThatClosesHigh_IsOnlyAShort()
    {
        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 50m;

        static void Whipsaw(CryptoCandle[] candles)
        {
            candles[0].High = 105m;
            candles[0].Low = 95m;
            candles[0].Close = 100.7m;   // 15% of the range down from the ceiling
        }

        var (shortSide, _) = MakeSeries(CryptoTradeSide.Short, Enough, Whipsaw);
        Assert.IsTrue(shortSide.IsSignal(), shortSide.ExtraText);

        var (longSide, _) = MakeSeries(CryptoTradeSide.Long, Enough, Whipsaw);
        Assert.IsFalse(longSide.IsSignal(), longSide.ExtraText);
    }


    /// <summary>
    /// The exact middle belongs to neither side at the default of 50: a close halfway is as far
    /// from the ceiling as from the floor, and saying yes to both is the very thing this fixes.
    /// </summary>
    [TestMethod]
    public void ACloseExactlyInTheMiddle_IsNeitherSide()
    {
        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 50m;

        static void Whipsaw(CryptoCandle[] candles)
        {
            candles[0].High = 105m;
            candles[0].Low = 95m;
            candles[0].Close = 100m;
        }

        var (shortSide, _) = MakeSeries(CryptoTradeSide.Short, Enough, Whipsaw);
        Assert.IsFalse(shortSide.IsSignal(), shortSide.ExtraText);

        var (longSide, _) = MakeSeries(CryptoTradeSide.Long, Enough, Whipsaw);
        Assert.IsFalse(longSide.IsSignal(), longSide.ExtraText);
    }


    /// <summary>
    /// A stricter percentage asks for a close right next to the broken level. The same close that
    /// passes at 50 is refused at 10.
    /// </summary>
    [TestMethod]
    public void AStricterPercentage_WantsTheCloseNextToTheLevel()
    {
        static void Upthrust(CryptoCandle[] candles)
        {
            candles[1].High = 105m;
            candles[0].Close = 100.7m;   // 15% of the range under the ceiling
        }

        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 50m;
        var (loose, _) = MakeSeries(CryptoTradeSide.Short, Enough, Upthrust);
        Assert.IsTrue(loose.IsSignal(), loose.ExtraText);

        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 10m;
        var (strict, _) = MakeSeries(CryptoTradeSide.Short, Enough, Upthrust);
        Assert.IsFalse(strict.IsSignal(), strict.ExtraText);
    }


    /// <summary>
    /// At 100 the check is off, and a whipsaw fires both sides again - the behaviour every run
    /// made before this setting existed had, kept reachable so those runs can be repeated.
    /// </summary>
    [TestMethod]
    public void AtOneHundredPercent_TheCheckIsOff()
    {
        FailedBreakoutPlugin.Settings.CloseWithinRangePercentage = 100m;

        static void Whipsaw(CryptoCandle[] candles)
        {
            candles[0].High = 105m;
            candles[0].Low = 95m;
            candles[0].Close = 99.3m;
        }

        var (longSide, _) = MakeSeries(CryptoTradeSide.Long, Enough, Whipsaw);
        Assert.IsTrue(longSide.IsSignal(), longSide.ExtraText);

        var (shortSide, _) = MakeSeries(CryptoTradeSide.Short, Enough, Whipsaw);
        Assert.IsTrue(shortSide.IsSignal(), shortSide.ExtraText);
    }


    // ════════════════════════════════════════════════════════════════════════
    //  The optional zone requirement
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ticking a zone asks for the break to have failed AT a zone. It is a second requirement on top
    /// of the level the strategy builds itself, so the very same series that fires without it has to
    /// stop firing when there is no zone to be found.
    /// </summary>
    [TestMethod]
    public void WithARequiredZoneThatIsNotThere_ItIsNoSignal()
    {
        FailedBreakoutPlugin.Settings.RequireZone = ["dlz"];
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = ["5m"];

        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[1].High = 105m;
            candles[0].Close = 100m;
        });

        Assert.IsFalse(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "not in a");
    }


    [TestMethod]
    public void WithARequiredZoneTheCandleTouches_ItIsASignal()
    {
        FailedBreakoutPlugin.Settings.RequireZone = ["dlz"];
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = ["5m"];

        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[1].High = 105m;
            candles[0].Close = 100m;
        });
        // A short reads the short zones. The newest candle runs 99..101, so a band of 100..102
        // overlaps it; the zone opened long before that candle, or it would be look-ahead.
        AddDlzZone(algorithm, CryptoTradeSide.Short, bottom: 100m, top: 102m);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "in dlz");
    }


    /// <summary>
    /// A zone on the other side is not the zone this trade is looking for - a short fails at
    /// resistance, and a support band underneath it says nothing about that.
    /// </summary>
    [TestMethod]
    public void AZoneOnTheOtherSide_DoesNotCount()
    {
        FailedBreakoutPlugin.Settings.RequireZone = ["dlz"];
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = ["5m"];

        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[1].High = 105m;
            candles[0].Close = 100m;
        });
        AddDlzZone(algorithm, CryptoTradeSide.Long, bottom: 100m, top: 102m);

        Assert.IsFalse(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>
    /// With nothing ticked the requirement is off, which is the default and what every run made
    /// before this setting existed did.
    /// </summary>
    [TestMethod]
    public void WithoutARequiredZone_TheZonesAreNeverLookedAt()
    {
        FailedBreakoutPlugin.Settings.RequireZone = [];
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = [];

        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, candles =>
        {
            candles[1].High = 105m;
            candles[0].Close = 100m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>
    /// Adds an open DLZ zone on the interval the series was built on. Only the side, the band and
    /// the open time are read; the rest is what the model demands.
    /// </summary>
    private static void AddDlzZone(SignalCreateBase algorithm, CryptoTradeSide side,
        decimal bottom, decimal top)
    {
        algorithm.SymbolInterval.Dlz.Zones.Add(new CryptoZone
        {
            Kind = CryptoZoneKind.DominantLevel,
            Strength = CryptoZoneStrength.Strong,
            ExchangeId = algorithm.Symbol.ExchangeId,
            Exchange = null!,
            SymbolId = algorithm.Symbol.Id,
            Symbol = null!,
            IntervalId = algorithm.Interval.Id,
            Interval = null!,
            Side = side,
            Bottom = bottom,
            Top = top,
            OpenTime = new CandleTime(1),
            CloseTime = null,
            IsValid = true,
        });
    }
}

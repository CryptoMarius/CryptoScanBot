using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Analyzers.Mac.Chart;
using CryptoScanner.Analyzers.Mac.Indicators;
using CryptoScanner.Analyzers.Mac.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// MAC - the four-line cloud plus the break of a pivot level. These tests fix what the strategy
/// does, one rule at a time.
/// <para>
/// The cloud and the levels are written by hand per candle, so what the strategy is looking at is
/// readable from the test. The thing that is easy to get wrong by one candle is the break itself:
/// the candle BEFORE it has to be on the other side of the level, or a market that has been above
/// its resistance for a week would fire on every candle.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class MacTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        // Settings has an internal setter, so the shared instance is adjusted in place rather than
        // replaced - and put back in cleanup, because every test in the process reads that one object.
        ApplyDefaults(MacPlugin.Settings);

        // MakeSeries puts the level at 101 and the tests close just through it, which the measured
        // buffer of 2% would swallow. The buffer is a separate rule with tests of its own, so it is
        // out of the way here; a test that is about the buffer sets the value it wants.
        MacPlugin.Settings.BreakoutBufferPercentage = 0m;
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(MacPlugin.Settings);

    private static void ApplyDefaults(MacSettings settings)
    {
        MacSettings fresh = new();
        settings.EntryOnBreakout = fresh.EntryOnBreakout;
        settings.EntryOnSecondLineCross = fresh.EntryOnSecondLineCross;
        settings.EntryOnCloudCross = fresh.EntryOnCloudCross;
        settings.EntryOnSpringboard = fresh.EntryOnSpringboard;
        settings.EntryOnLineCross = fresh.EntryOnLineCross;
        settings.Speed = fresh.Speed;
        settings.FastEmaLength = fresh.FastEmaLength;
        settings.SecondEmaLength = fresh.SecondEmaLength;
        settings.MediumSmaLength = fresh.MediumSmaLength;
        settings.SlowSmaLength = fresh.SlowSmaLength;
        settings.RequirePriceOutsideCloud = fresh.RequirePriceOutsideCloud;
        settings.MinimumCloudWidthPercentage = fresh.MinimumCloudWidthPercentage;
        settings.RequireCloudWidening = fresh.RequireCloudWidening;
        settings.MinimumSlowLineSlopePercentage = fresh.MinimumSlowLineSlopePercentage;
        settings.SlowLineLookbackCandles = fresh.SlowLineLookbackCandles;
        settings.PivotLeftCandles = fresh.PivotLeftCandles;
        settings.PivotRightCandles = fresh.PivotRightCandles;
        settings.PivotMaximumAgeCandles = fresh.PivotMaximumAgeCandles;
        settings.BreakoutBufferPercentage = fresh.BreakoutBufferPercentage;
        settings.UseRsiFilter = fresh.UseRsiFilter;
        settings.RsiLongMinimum = fresh.RsiLongMinimum;
        settings.RsiShortMaximum = fresh.RsiShortMaximum;
        settings.UseVolumeFilter = fresh.UseVolumeFilter;
        settings.VolumeMultiplier = fresh.VolumeMultiplier;
        settings.VolumeAverageCandles = fresh.VolumeAverageCandles;
        settings.ExitOnCloudFlip = fresh.ExitOnCloudFlip;
        settings.ExitConfirmationCandles = fresh.ExitConfirmationCandles;
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
    /// Builds <paramref name="count"/> candles ending at index 0 (the newest). Every candle closes
    /// at 100 with the cloud pointing the way of the trade and the price outside it, and carries a
    /// level the price has NOT broken: a resistance at 101 for a long, a support at 99 for a short.
    /// The caller then breaks it on the candles it wants a signal on.
    /// </summary>
    private static (MacBase Algorithm, MacCandleData[] Mac, CryptoCandle[] Candles) MakeSeries(
        CryptoTradeSide side, int count,
        Action<MacCandleData[]>? shape = null,
        Action<CryptoCandle[]>? shapeCandles = null,
        Action<CryptoData[]>? shapeData = null)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = MakeInterval();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        CryptoCandle[] candles = new CryptoCandle[count];
        CryptoData[] data = new CryptoData[count];
        MacCandleData[] mac = new MacCandleData[count];
        for (int i = 0; i < count; i++)
        {
            candles[i] = new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((count - i) * interval.Duration)),
                Open = 100m,
                High = 100.5m,
                Low = 99.5m,
                Close = 100m,
                // A flat volume, so the volume filter has an average to compare against and a test
                // that wants a spike only has to raise the volume of the candle in hand.
                Volume = 100m,
            };
            // A long gets a cloud under the price and pointing up, a short one above it pointing
            // down, so every test starts from a candle that only lacks the break.
            mac[i] = side == CryptoTradeSide.Long
                ? new MacCandleData { EmaFast = 99, EmaSecond = 98.5, SmaMedium = 98, SmaSlow = 97,
                    PivotHigh = 101, PivotHighAge = 5 }
                : new MacCandleData { EmaFast = 101, EmaSecond = 101.5, SmaMedium = 102, SmaSlow = 103,
                    PivotLow = 99, PivotLowAge = 5 };
            data[i] = new CryptoData { Rsi = side == CryptoTradeSide.Long ? 60.0 : 40.0 };
        }
        shape?.Invoke(mac);
        shapeCandles?.Invoke(candles);
        shapeData?.Invoke(data);

        for (int i = 0; i < count; i++)
        {
            data[i].SetPluginData(mac[i]);
            symbolInterval.CandleList.TryAdd(candles[i].OpenTime, candles[i]);
            symbolInterval.Data[candles[i].OpenTime] = data[i];
        }

        return (new MacBase
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = "mac",
            CandleLast = new MyData { Candle = candles[0], CandleData = data[0] },
        }, mac, candles);
    }

    private const int Enough = 10;


    /// <summary>
    /// The overlay marks the crossing of the two EMAs as "open long" / "open short". Every marker
    /// has to sit on a candle where the fast line really changed sides, and no such candle may be
    /// left unmarked.
    /// </summary>
    [TestMethod]
    public void EveryOpenLongMarkerSitsOnARealCrossing()
    {
        var candles = new List<CryptoCandle>();
        for (int i = 0; i < 400; i++)
        {
            double wave = Math.Sin(i / 11.0) * 900;
            decimal close = (decimal)(30000 + wave);
            candles.Add(new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((i + 1) * 1440)),
                Open = close,
                High = close + 80m,
                Low = close - 80m,
                Close = close,
                Volume = 100m,
            });
        }

        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        // Only the crossing markers: the same call also returns the confirmation dots, and those
        // have a crossing of their own to answer to.
        var labels = new MacChartOverlay().GetLabels(symbol, interval, candles)
            .Where(l => l.Text is "▲" or "▼").ToList();
        MacLineValues[] values = MacLinesHelper.Compute(candles);

        // Count the crossings the lines themselves show, and check every marker against them. The
        // crossing only needs the two EMAs, not the whole cloud: the slow SMA takes 150 candles to
        // start and their signal does not wait for it. The STRATEGY does - it wants all four lines
        // before it evaluates anything - so during that warm-up the chart can show a cross that no
        // signal was ever produced for.
        int crossings = 0;
        var marked = new HashSet<long>(labels.Select(l => l.Time));
        for (int i = 1; i < candles.Count; i++)
        {
            if (values[i].EmaFast == null || values[i].EmaSecond == null
                || values[i - 1].EmaFast == null || values[i - 1].EmaSecond == null)
                continue;
            bool above = values[i].EmaFast!.Value > values[i].EmaSecond!.Value;
            bool wasAbove = values[i - 1].EmaFast!.Value > values[i - 1].EmaSecond!.Value;
            if (above == wasAbove)
                continue;

            crossings++;
            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();
            Assert.IsTrue(marked.Contains(time), "crossing at candle " + i + " carries no marker");
            var label = labels.First(l => l.Time == time);
            Assert.AreEqual(above ? "▲" : "▼", label.Text, "wrong side at candle " + i);
        }

        Assert.IsTrue(crossings > 2, "the series should cross a few times, found " + crossings);
        Assert.AreEqual(crossings, labels.Count, "a marker was drawn without a crossing");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Entering on the close crossing the second line
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Only this trigger on, so nothing else can account for the signal.</summary>
    private static void OnlyTheSecondLineCross()
    {
        MacSettings settings = MacPlugin.Settings;
        settings.EntryOnBreakout = false;
        settings.EntryOnBreakoutRun = false;
        settings.EntryOnCloudCross = false;
        settings.EntryOnSpringboard = false;
        settings.EntryOnLineCross = false;
        settings.EntryOnSecondLineCross = true;
    }


    /// <summary>
    /// The reference draws "Close Short" when the close crosses UP through the second line. Read as
    /// an entry that is a LONG: what closes a short opens a long.
    /// </summary>
    [TestMethod]
    public void ACloseCrossingUpThroughTheSecondLine_IsALong()
    {
        OnlyTheSecondLineCross();
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shapeCandles: candles =>
        {
            candles[1].Close = 98m;     // under the second line, which sits at 98.5
            candles[0].Close = 99m;     // and through it, without reaching the level at 101
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "close crossed over the second line");
    }


    /// <summary>The mirror: the close falling through the second line opens a short.</summary>
    [TestMethod]
    public void ACloseCrossingDownThroughTheSecondLine_IsAShort()
    {
        OnlyTheSecondLineCross();
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough, shapeCandles: candles =>
        {
            candles[1].Close = 102m;    // above the second line, which sits at 101.5
            candles[0].Close = 101m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "close crossed under the second line");
    }


    /// <summary>
    /// And it fires while the CLOUD still points the other way, which is the whole point: this is
    /// the marker that closes the opposite position, so the cloud is by definition against us. Every
    /// other trigger but the line cross is blocked there.
    /// </summary>
    [TestMethod]
    public void ItFiresEvenWhileTheCloudStillPointsTheOtherWay()
    {
        OnlyTheSecondLineCross();
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (MacCandleData one in mac)
                {
                    // the fast line UNDER the second one: a cloud pointing down, against a long
                    one.EmaFast = 98.0;
                    one.EmaSecond = 98.5;
                }
            },
            shapeCandles: candles =>
            {
                candles[1].Close = 98m;
                candles[0].Close = 99m;
            });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>A close that stays on its own side of the line is nothing.</summary>
    [TestMethod]
    public void ACloseThatStaysUnderTheSecondLine_IsNoSignal()
    {
        OnlyTheSecondLineCross();
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shapeCandles: candles =>
        {
            candles[1].Close = 98m;
            candles[0].Close = 98.2m;   // still under the second line at 98.5
        });

        Assert.IsFalse(algorithm.IsSignal());
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The break
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ACloseThroughTheResistance_IsALong()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "broke the resistance");
    }


    [TestMethod]
    public void ACloseThroughTheSupport_IsAShort()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough,
            shapeCandles: candles => candles[0].Close = 98m);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "broke the support");
    }


    [TestMethod]
    public void ACloseThatStaysUnderTheResistance_IsNoSignal()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "did not clear the resistance");
    }


    /// <summary>
    /// A level that was already broken on the previous candle is not a break. Without this check a
    /// market trading above its resistance would signal on every candle.
    /// </summary>
    [TestMethod]
    public void ALevelThatWasAlreadyBroken_IsNoSignal()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shapeCandles: candles =>
        {
            candles[0].Close = 103m;
            candles[1].Close = 102m;
        });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "already broken");
    }


    [TestMethod]
    public void NoLevelYet_IsNoSignal()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                    t.PivotHigh = null;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no pivot high");
    }


    [TestMethod]
    public void ALevelPastItsMaximumAge_IsNoSignal()
    {
        MacPlugin.Settings.PivotMaximumAgeCandles = 20;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                    t.PivotHighAge = 21;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "candles old");
    }


    /// <summary>
    /// An old level is accepted by default: a level that has held for a long time is the one the
    /// market watches, so age is not a reason to ignore it unless a run says otherwise.
    /// </summary>
    [TestMethod]
    public void AnOldLevel_IsAcceptedByDefault()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                    t.PivotHighAge = 5000;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.AreEqual(0, MacPlugin.Settings.PivotMaximumAgeCandles);
        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>The buffer lifts the trigger above the level, so a break of one tick is not enough.</summary>
    [TestMethod]
    public void ABreakSmallerThanTheBuffer_IsNoSignal()
    {
        MacPlugin.Settings.BreakoutBufferPercentage = 1m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles => candles[0].Close = 101.5m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "did not clear the resistance");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The cloud
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ACloudPointingTheOtherWay_IsNoSignal()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                {
                    t.EmaFast = 98;
                    t.EmaSecond = 99;
                }
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "cloud points down");
    }


    [TestMethod]
    public void APriceInsideTheCloud_IsNoSignal()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                // The cloud still points up, but it sits AROUND the breaking candle.
                foreach (var t in mac)
                {
                    t.EmaFast = 103;
                    t.EmaSecond = 101.5;
                    t.SmaMedium = 101;
                    t.SmaSlow = 100.5;
                }
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "not above the cloud");
    }


    [TestMethod]
    public void ACloudNarrowerThanTheMinimum_IsNoSignal()
    {
        MacPlugin.Settings.MinimumCloudWidthPercentage = 2m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles => candles[0].Close = 102m);

        // The default cloud is 99 against 98, which on a price of 102 is under 1%.
        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "cloud only");
    }


    [TestMethod]
    public void ANarrowingCloud_IsNoSignalWhenWideningIsAsked()
    {
        MacPlugin.Settings.RequireCloudWidening = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                // Wider on the previous candle than on the one in hand: the cloud of the series is
                // 99 down to 97, this one runs 99 down to 96.
                mac[1].EmaFast = 99;
                mac[1].SmaSlow = 96;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "cloud narrowing");
    }


    [TestMethod]
    public void AWideningCloud_IsALong()
    {
        MacPlugin.Settings.RequireCloudWidening = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                // Narrower on the previous candle: 99 down to 98 against the 99 down to 97 of the
                // series, so the cloud is opening up on the candle in hand.
                mac[1].EmaFast = 99;
                mac[1].SmaSlow = 98;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "widening");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The cloud cross - the second trigger, "Open Long"/"Open Short"
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The cross fires on its own, without a level being broken: the price stays at 100 and there is
    /// a resistance at 101 that nobody touches.
    /// </summary>
    [TestMethod]
    public void TheCloudCrossingUp_IsALongOfItsOwn()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnCloudCross = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shape: mac =>
        {
            // The cloud pointed DOWN on the candle before, so the candle in hand is the cross.
            mac[1].EmaFast = 98;
            mac[1].EmaSecond = 99;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "cloud crossed up");
    }


    [TestMethod]
    public void TheCloudCrossingDown_IsAShortOfItsOwn()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnCloudCross = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough, shape: mac =>
        {
            mac[1].EmaFast = 102;
            mac[1].EmaSecond = 101;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "cloud crossed down");
    }


    /// <summary>
    /// A cloud that already pointed our way has not crossed. Without this the cross trigger would
    /// fire on every candle of a trend instead of at its start.
    /// </summary>
    [TestMethod]
    public void ACloudThatAlreadyPointedThisWay_IsNoCross()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnCloudCross = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no cross");
    }


    /// <summary>
    /// The price sits ON the cloud at a cross, so "price outside the cloud" - which is on by
    /// default - must not apply to that trigger, or it would never fire.
    /// </summary>
    [TestMethod]
    public void ACrossFiresEvenWithPriceOutsideTheCloudSwitchedOn()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnCloudCross = true;
        MacPlugin.Settings.RequirePriceOutsideCloud = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shape: mac =>
        {
            // A cloud AROUND the price: the cross is there, the price is not above it.
            mac[0].EmaFast = 101;
            mac[0].EmaSecond = 99.5;
            mac[1].EmaFast = 99;
            mac[1].EmaSecond = 101;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    [TestMethod]
    public void WithBothTriggersOff_NothingFires()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnCloudCross = false;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no entry trigger");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The springboard bounce - the pullback entry
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The price dips to the fast line and closes back above it, with the previous candle already
    /// above it. That is the bounce; no level has to be broken for it.
    /// </summary>
    [TestMethod]
    public void ADipToTheFastLineThatClosesBackAbove_IsALong()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnSpringboard = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles =>
            {
                // The candle reaches down to 99 (the fast EMA) and closes at 100 again.
                candles[0].Low = 98.5m;
                candles[0].Close = 100m;
            });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "bounced off the fast line");
    }


    /// <summary>A candle that never comes near the fast line is not a bounce.</summary>
    [TestMethod]
    public void ACandleThatNeverReachesTheFastLine_IsNoBounce()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnSpringboard = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough);

        // The default series has its low at 99.5, just above the fast EMA of 99.
        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "did not reach the fast line");
    }


    /// <summary>A candle that dips AND closes under the line is a break, not a bounce.</summary>
    [TestMethod]
    public void ACandleThatClosesUnderTheFastLine_IsNoBounce()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnSpringboard = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles =>
            {
                candles[0].Low = 98m;
                candles[0].Close = 98.5m;
            });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "closed under the fast line");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The line cross - the second line through the third
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The second line closing above the third is the cross. It is its own trigger, so no level has
    /// to give way and the price may sit anywhere.
    /// </summary>
    [TestMethod]
    public void TheSecondLineClosingOverTheThird_IsALong()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnLineCross = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                // The candle before still had the second line under the third.
                mac[1].EmaSecond = 97.5;
            });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "second line crossed over the third");
    }


    /// <summary>
    /// The whole point of this trigger: it fires while the cloud still points the OTHER way. The
    /// second line gives way before the fast one does, and a test for the cloud first would make
    /// the trigger unreachable - which is exactly what happened on 22-09-2026 before this test.
    /// </summary>
    [TestMethod]
    public void TheLineCrossFiresWhileTheCloudStillPointsDown()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnLineCross = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                mac[1].EmaSecond = 97.5;
                // The fast line under the second one: the cloud of a short, not of a long.
                mac[0].EmaFast = 98.2;
            });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>Two lines that stay on their own side of each other are no cross.</summary>
    [TestMethod]
    public void TwoLinesThatDoNotMeet_AreNoLineCross()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnLineCross = true;
        // The default series has the second line above the third on BOTH candles.
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "stayed on its side of the third");
    }


    /// <summary>The short side, mirrored: the second line closing under the third.</summary>
    [TestMethod]
    public void TheSecondLineClosingUnderTheThird_IsAShort()
    {
        MacPlugin.Settings.EntryOnBreakout = false;
        MacPlugin.Settings.EntryOnLineCross = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough,
            shape: mac =>
            {
                // The candle before still had the second line over the third.
                mac[1].EmaSecond = 102.5;
            });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "second line crossed under the third");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The slow line - the trend filter
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ASlowLineThatIsNotRisingEnough_IsNoLong()
    {
        MacPlugin.Settings.MinimumSlowLineSlopePercentage = 1m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                    t.SlowSlopePercentage = 0.2;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "slow line only 0.20%");
    }


    [TestMethod]
    public void ARisingSlowLine_LetsTheLongThrough()
    {
        MacPlugin.Settings.MinimumSlowLineSlopePercentage = 1m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                    t.SlowSlopePercentage = 2.5;
            },
            shapeCandles: candles => candles[0].Close = 102m);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "slow line 2.50%");
    }


    /// <summary>A short wants the slow line pointing DOWN by that same amount.</summary>
    [TestMethod]
    public void ARisingSlowLine_IsNoShort()
    {
        MacPlugin.Settings.MinimumSlowLineSlopePercentage = 1m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough,
            shape: mac =>
            {
                foreach (var t in mac)
                    t.SlowSlopePercentage = 2.5;
            },
            shapeCandles: candles => candles[0].Close = 98m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "slow line 2.50%");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The volume filter - "volume MA x factor"
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ACandleWithoutTheVolumeSpike_IsNoSignal()
    {
        MacPlugin.Settings.UseVolumeFilter = true;
        MacPlugin.Settings.VolumeMultiplier = 3m;
        MacPlugin.Settings.VolumeAverageCandles = 5;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles => candles[0].Close = 102m);

        // Every candle trades 100, so the ratio is 1.00x against the 3x asked.
        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "volume 1.00x the average");
    }


    [TestMethod]
    public void ACandleWithTheVolumeSpike_IsALong()
    {
        MacPlugin.Settings.UseVolumeFilter = true;
        MacPlugin.Settings.VolumeMultiplier = 3m;
        MacPlugin.Settings.VolumeAverageCandles = 5;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shapeCandles: candles =>
        {
            candles[0].Close = 102m;
            candles[0].Volume = 400m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "volume 4.00x");
    }


    /// <summary>
    /// The signal candle is left out of its own average: with it included, a spike of four times the
    /// volume over five candles would read as less than four.
    /// </summary>
    [TestMethod]
    public void TheSignalCandleIsNotPartOfItsOwnAverage()
    {
        MacPlugin.Settings.UseVolumeFilter = true;
        MacPlugin.Settings.VolumeMultiplier = 4m;
        MacPlugin.Settings.VolumeAverageCandles = 5;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shapeCandles: candles =>
        {
            candles[0].Close = 102m;
            candles[0].Volume = 400m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "volume 4.00x");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The RSI filter
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AnRsiUnderTheMinimum_IsNoLong()
    {
        MacPlugin.Settings.UseRsiFilter = true;
        MacPlugin.Settings.RsiLongMinimum = 55m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            shapeCandles: candles => candles[0].Close = 102m,
            shapeData: data => data[0].Rsi = 45.0);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "under the minimum");
    }


    [TestMethod]
    public void AnRsiAboveTheMaximum_IsNoShort()
    {
        MacPlugin.Settings.UseRsiFilter = true;
        MacPlugin.Settings.RsiShortMaximum = 45m;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough,
            shapeCandles: candles => candles[0].Close = 98m,
            shapeData: data => data[0].Rsi = 55.0);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "above the maximum");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TheCloudFlippingAgainstTheposition_IsAnExit()
    {
        MacPlugin.Settings.ExitOnCloudFlip = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shape: mac =>
        {
            mac[0].EmaFast = 98;
            mac[0].EmaSecond = 99;
        });

        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "flipped down");
    }


    [TestMethod]
    public void ACloudStillOnOurSide_IsNoExit()
    {
        MacPlugin.Settings.ExitOnCloudFlip = true;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough);

        Assert.IsFalse(algorithm.IsExitSignal());
        StringAssert.Contains(algorithm.ExtraText, "still points our way");
    }


    [TestMethod]
    public void AFlipThatDoesNotHold_IsNoExitWhenConfirmationIsAsked()
    {
        MacPlugin.Settings.ExitOnCloudFlip = true;
        MacPlugin.Settings.ExitConfirmationCandles = 2;
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, shape: mac =>
        {
            // Against us on the candle in hand, still our way on the one before it.
            mac[0].EmaFast = 98;
            mac[0].EmaSecond = 99;
        });

        Assert.IsFalse(algorithm.IsExitSignal());
        StringAssert.Contains(algorithm.ExtraText, "not held");
    }


    /// <summary>The exit costs the monitor nothing while it is switched off.</summary>
    [TestMethod]
    public void TheExitIsOffByDefault()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough);

        Assert.IsFalse(algorithm.HasExitSignal);
        Assert.IsFalse(algorithm.IsExitSignal());
    }
}


/// <summary>
/// The pivot levels MAC breaks through, as the indicator extension produces them. The EMAs come
/// from the shared registry and are Skender's; the pivots are ours, so they are what these tests
/// pin down: WHEN a level is confirmed, which price it carries, and how old it says it is.
/// </summary>
[DoNotParallelize]
[TestClass]
public class MacIndicatorExtensionTests : TestBase
{
    [TestInitialize]
    public void Setup() => InitTestSession();

    [TestCleanup]
    public void Restore()
    {
        MacSettings fresh = new();
        MacPlugin.Settings.PivotLeftCandles = fresh.PivotLeftCandles;
        MacPlugin.Settings.PivotRightCandles = fresh.PivotRightCandles;
    }


    /// <summary>
    /// Feeds the highs given (the lows are kept flat under them) and returns what the extension
    /// wrote into the candle data of the LAST candle.
    /// </summary>
    private static MacCandleData? Feed(decimal[] highs, int left, int right)
    {
        MacPlugin.Settings.PivotLeftCandles = left;
        MacPlugin.Settings.PivotRightCandles = right;

        MacIndicatorExtension extension = new();
        extension.Init(new IndicatorRegistry(500));

        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < highs.Length; i++)
        {
            decimal high = highs[i];
            extension.OnCandleAdded(new Quote(start.AddMinutes(5 * i), high - 1m, high, 1m, high - 1m, 10m));
        }

        CryptoData data = new();
        extension.FillData(data);
        return data.GetPluginData<MacCandleData>();
    }


    /// <summary>
    /// A pivot high is the candle that is higher than the candles on both sides of it, and it is
    /// only known once the right-hand candles are in - which is why its age can never be under
    /// PivotRightCandles.
    /// </summary>
    [TestMethod]
    public void ThePeakBecomesTheLevel_OnceItsRightHandCandlesAreIn()
    {
        // Index 2 is the peak; with left = right = 2 it is confirmed at index 4.
        decimal[] highs = [100, 101, 110, 102, 100, 99, 98];
        MacCandleData? mac = Feed(highs, left: 2, right: 2);

        Assert.IsNotNull(mac);
        Assert.AreEqual(110.0, mac!.PivotHigh);
        // The newest candle is index 6, the peak index 2.
        Assert.AreEqual(4, mac.PivotHighAge);
    }


    /// <summary>A peak without its right-hand candles yet is not a level.</summary>
    [TestMethod]
    public void APeakWithoutItsRightHandCandles_IsNotALevelYet()
    {
        decimal[] highs = [100, 101, 110, 102];
        MacCandleData? mac = Feed(highs, left: 2, right: 2);

        Assert.IsNull(mac?.PivotHigh);
    }


    /// <summary>
    /// The four lengths are a set: EMA(20), EMA(40), SMA(50) and SMA(150) on the close together are
    /// what this strategy is, and a run that wants a faster or slower cloud scales all four in
    /// proportion. Changing one default on its own makes it a different strategy, so this test is
    /// here to make that a deliberate act.
    /// </summary>
    [TestMethod]
    public void TheFourLineLengthsAreTheDefaults()
    {
        MacSettings fresh = new();

        Assert.AreEqual(20, fresh.FastEmaLength, "fast line is EMA(20)");
        Assert.AreEqual(40, fresh.SecondEmaLength, "second line is EMA(40)");
        Assert.AreEqual(50, fresh.MediumSmaLength, "third line is SMA(50)");
        Assert.AreEqual(150, fresh.SlowSmaLength, "slow line is SMA(150)");
        Assert.AreEqual(MacSpeed.Standard, fresh.Speed, "and the speed they belong to");
    }


    /// <summary>
    /// The three speeds of the reference indicator, each measured off its status line and fitted
    /// against our own candles to the cent. The first line does not move.
    /// </summary>
    [TestMethod]
    public void EachSpeedHasTheLengthsMeasuredOffTheReference()
    {
        MacSettings settings = new() { Speed = MacSpeed.Standard };
        Assert.AreEqual((20, 40, 50, 150), settings.Lines(), "Standard");

        settings.Speed = MacSpeed.Fast;
        Assert.AreEqual((20, 30, 40, 80), settings.Lines(), "Fast");

        settings.Speed = MacSpeed.Slow;
        Assert.AreEqual((20, 50, 100, 200), settings.Lines(), "Slow");
    }


    /// <summary>
    /// Choosing a speed may NOT write the four numbers: a setting that quietly changes another one
    /// is how a strategy ends up with a state nobody set. Only Custom reads them.
    /// </summary>
    [TestMethod]
    public void TheSpeedLeavesTheFourNumbersAlone()
    {
        MacSettings settings = new()
        {
            Speed = MacSpeed.Slow,
            FastEmaLength = 9,
            SecondEmaLength = 21,
            MediumSmaLength = 34,
            SlowSmaLength = 55,
        };

        Assert.AreEqual((20, 50, 100, 200), settings.Lines(), "the preset decides");
        Assert.AreEqual(9, settings.FastEmaLength, "and the numbers are untouched");
        Assert.AreEqual(55, settings.SlowSmaLength, "all four of them");

        settings.Speed = MacSpeed.Custom;
        Assert.AreEqual((9, 21, 34, 55), settings.Lines(), "until the speed hands over to them");
    }


    /// <summary>
    /// The chart overlay computes the same lines a second time, over a whole candle list instead of
    /// one candle at a time. Two paths to the same numbers drift apart sooner or later, so this
    /// holds them together: feed one series through both and compare every candle.
    /// <para>
    /// The pivots have to match exactly - it is the same rule written twice. The moving averages are
    /// allowed a hair of difference, because one side is Skender's incremental hub and the other its
    /// batch calculation.
    /// </para>
    /// </summary>
    [TestMethod]
    public void TheOverlayAndTheStrategySeeTheSameLines()
    {
        // A wave with a rising floor, so there are pivots of both kinds and the averages move.
        var candles = new List<CryptoCandle>();
        for (int i = 0; i < 400; i++)
        {
            double wave = Math.Sin(i / 7.0) * 400 + Math.Sin(i / 3.0) * 120;
            decimal close = (decimal)(30000 + i * 25 + wave);
            candles.Add(new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((i + 1) * 1440)),
                Open = close,
                High = close + 80m,
                Low = close - 80m,
                Close = close,
                Volume = 100m,
            });
        }

        MacLineValues[] overlay = MacLinesHelper.Compute(candles);

        MacIndicatorExtension extension = new();
        extension.Init(new IndicatorRegistry(500));
        for (int i = 0; i < candles.Count; i++)
        {
            extension.OnCandleAdded(candles[i]);
            CryptoData data = new();
            extension.FillData(data);
            MacCandleData? live = data.GetPluginData<MacCandleData>();

            Assert.AreEqual(overlay[i].PivotHigh, live?.PivotHigh, "pivot high at candle " + i);
            Assert.AreEqual(overlay[i].PivotLow, live?.PivotLow, "pivot low at candle " + i);

            if (overlay[i].HasCloud && live?.EmaFast != null)
            {
                Assert.AreEqual(overlay[i].EmaFast!.Value, live.EmaFast!.Value, 0.01, "fast ema at candle " + i);
                Assert.AreEqual(overlay[i].EmaSecond!.Value, live.EmaSecond!.Value, 0.01, "second ema at candle " + i);
                Assert.AreEqual(overlay[i].SmaMedium!.Value, live.SmaMedium!.Value, 0.01, "medium sma at candle " + i);
                Assert.AreEqual(overlay[i].SmaSlow!.Value, live.SmaSlow!.Value, 0.01, "slow sma at candle " + i);
            }
        }
    }


    /// <summary>A later, higher peak replaces the level; the older one is no longer the one to break.</summary>
    [TestMethod]
    public void ALaterPeakReplacesTheLevel()
    {
        decimal[] highs = [100, 101, 110, 102, 100, 105, 120, 106, 104];
        MacCandleData? mac = Feed(highs, left: 2, right: 2);

        Assert.AreEqual(120.0, mac?.PivotHigh);
    }
}

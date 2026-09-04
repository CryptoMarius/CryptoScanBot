using CryptoScanner.Analyzers.MacdCross;
using CryptoScanner.Analyzers.MacdCross.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The MACD crossover: in when the MACD line crosses its signal line, out when the two cross back.
/// <para>
/// The MACD values are written by hand per candle, so what the strategy is looking at is readable
/// from the test. The series is "lines against us" everywhere, and the test moves the MACD line to
/// the other side on the candles it wants to have crossed. The one thing that is easy to get wrong
/// by one candle is the cross itself: the candle BEFORE the confirmation window has to be on the
/// other side, or a market where the lines have been apart for weeks would fire on every candle.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class MacdCrossTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        // Settings has an internal setter, so the shared instance is adjusted in place rather than
        // replaced - and put back in cleanup, because every test in the process reads that one object.
        ApplyDefaults(MacdCrossPlugin.Settings);
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(MacdCrossPlugin.Settings);

    private static void ApplyDefaults(MacdCrossSettings settings)
    {
        MacdCrossSettings fresh = new();
        settings.ConfirmationCandles = fresh.ConfirmationCandles;
        settings.MinimumDistancePercentage = fresh.MinimumDistancePercentage;
        settings.RequireCrossBeyondZeroLine = fresh.RequireCrossBeyondZeroLine;
        settings.AdxMinimum = fresh.AdxMinimum;
        settings.AdxRecentlyBelow = fresh.AdxRecentlyBelow;
        settings.AdxRecentlyWithinCandles = fresh.AdxRecentlyWithinCandles;
        settings.RelativeVolumeMinimum = fresh.RelativeVolumeMinimum;
        settings.RelativeVolumeCandles = fresh.RelativeVolumeCandles;
        settings.RelativeVolumeAverageCandles = fresh.RelativeVolumeAverageCandles;
        settings.ExitOnCrossBack = fresh.ExitOnCrossBack;
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
    /// Builds <paramref name="count"/> candles ending at index 0 (the newest), every one closing at
    /// 100 with the MACD line AGAINST the given side: under the signal line for a long, above it for
    /// a short. The caller then puts the MACD on the trade's side on the candles it wants crossed.
    /// </summary>
    private static (MacdCrossBase Algorithm, CryptoData[] Data) MakeSeries(
        CryptoTradeSide side, int count, Action<CryptoData[]> shape,
        Action<CryptoCandle[]>? shapeCandles = null)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = MakeInterval();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        CryptoCandle[] candles = new CryptoCandle[count];
        CryptoData[] data = new CryptoData[count];
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
            data[i] = new CryptoData
            {
                MacdValue = side == CryptoTradeSide.Long ? -1.0 : 1.0,
                MacdSignal = 0.0,
            };
        }
        shape(data);
        shapeCandles?.Invoke(candles);

        for (int i = 0; i < count; i++)
        {
            symbolInterval.CandleList.TryAdd(candles[i].OpenTime, candles[i]);
            symbolInterval.Data[candles[i].OpenTime] = data[i];
        }

        return (new MacdCrossBase
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = "macdcross",
            CandleLast = new MyData { Candle = candles[0], CandleData = data[0] },
        }, data);
    }

    /// <summary>Puts the MACD line on the trade's side of the signal line on candle <paramref name="index"/>.</summary>
    private static void OnOurSide(CryptoData[] data, CryptoTradeSide side, int index, double distance = 1.0)
        => data[index].MacdValue = side == CryptoTradeSide.Long ? distance : -distance;

    private const int Enough = 10;


    // ═══════════════════════════════════════════════════════════════════════
    //  The cross
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TheMacdLineCrossingAboveTheSignalLine_IsALong()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "crossed above");
    }


    [TestMethod]
    public void TheMacdLineCrossingUnderTheSignalLine_IsAShort()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Short, 0));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "crossed under");
    }


    /// <summary>
    /// Lines that have been on the trade's side all along have not crossed. Without the check on
    /// the candle before the window, every candle of a long uptrend would be a long signal.
    /// </summary>
    [TestMethod]
    public void LinesThatWereAlreadyOnThisSide_AreNoSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            for (int i = 0; i < data.Length; i++)
                OnOurSide(data, CryptoTradeSide.Long, i);
        });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no cross");
    }


    [TestMethod]
    public void LinesAgainstTheTrade_AreNoSignal()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "not on our side");
    }


    /// <summary>
    /// The MACD line sitting exactly on the signal line is on nobody's side: not a cross for the
    /// long, and the candle before a cross may sit there too.
    /// </summary>
    [TestMethod]
    public void ExactlyOnTheSignalLine_IsNeitherSide()
    {
        var (touching, _) = MakeSeries(CryptoTradeSide.Long, Enough, data => data[0].MacdValue = 0.0);
        Assert.IsFalse(touching.IsSignal(), touching.ExtraText);

        var (fromTouching, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            data[1].MacdValue = 0.0;
            OnOurSide(data, CryptoTradeSide.Long, 0);
        });
        Assert.IsTrue(fromTouching.IsSignal(), fromTouching.ExtraText);
    }


    /// <summary>
    /// A hub that is still warming up has no MACD yet. That has to be a quiet no, not an exception.
    /// </summary>
    [TestMethod]
    public void WithoutMacdValues_ItSaysNoInsteadOfThrowing()
    {
        var (noMacd, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            data[0].MacdValue = null;
        });
        Assert.IsFalse(noMacd.IndicatorsOkay(noMacd.CandleLast));

        // The previous candle without a MACD: the walk back fails, not the test.
        var (noPrevious, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            data[1].MacdSignal = null;
        });
        Assert.IsFalse(noPrevious.IsSignal());
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Confirmation candles
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// With confirmation the cross has to be older: the lines on our side for the window AND the
    /// candle before the window on the other side.
    /// </summary>
    [TestMethod]
    public void WithConfirmation_TheCrossHasToBeThatManyCandlesOld()
    {
        MacdCrossPlugin.Settings.ConfirmationCandles = 2;

        // Crossed three candles ago: candles 0, 1 and 2 on our side, candle 3 against.
        var (held, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            OnOurSide(data, CryptoTradeSide.Long, 1);
            OnOurSide(data, CryptoTradeSide.Long, 2);
        });
        Assert.IsTrue(held.IsSignal(), held.ExtraText);
        StringAssert.Contains(held.ExtraText, "2 candle(s) ago");

        // Crossed on this very candle: too fresh.
        var (fresh, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));
        Assert.IsFalse(fresh.IsSignal());
        StringAssert.Contains(fresh.ExtraText, "not held");

        // Crossed four candles ago: that signal was two candles back, this candle is not it.
        var (stale, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            for (int i = 0; i <= 3; i++)
                OnOurSide(data, CryptoTradeSide.Long, i);
        });
        Assert.IsFalse(stale.IsSignal());
        StringAssert.Contains(stale.ExtraText, "no cross");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The zero line
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A long wants the cross under the zero line. Read at the cross candle, not at the signal
    /// candle: with confirmation the two differ, and the question is where the cross happened.
    /// </summary>
    [TestMethod]
    public void BeyondZeroLine_ALongWantsTheCrossUnderZero()
    {
        MacdCrossPlugin.Settings.RequireCrossBeyondZeroLine = true;

        // MACD -0.5 above a signal line of -0.7: on our side, and still under zero.
        var (under, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            data[0].MacdValue = -0.5;
            data[0].MacdSignal = -0.7;
        });
        Assert.IsTrue(under.IsSignal(), under.ExtraText);

        // MACD 0.5 above a signal line of 0.3: a cross, but above zero.
        var (above, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            data[0].MacdValue = 0.5;
            data[0].MacdSignal = 0.3;
        });
        Assert.IsFalse(above.IsSignal());
        StringAssert.Contains(above.ExtraText, "zero line");
    }


    [TestMethod]
    public void BeyondZeroLine_AShortWantsTheCrossAboveZero()
    {
        MacdCrossPlugin.Settings.RequireCrossBeyondZeroLine = true;

        var (above, _) = MakeSeries(CryptoTradeSide.Short, Enough, data =>
        {
            data[0].MacdValue = 0.5;
            data[0].MacdSignal = 0.7;
        });
        Assert.IsTrue(above.IsSignal(), above.ExtraText);

        var (under, _) = MakeSeries(CryptoTradeSide.Short, Enough, data =>
        {
            data[0].MacdValue = -0.5;
            data[0].MacdSignal = -0.3;
        });
        Assert.IsFalse(under.IsSignal());
        StringAssert.Contains(under.ExtraText, "zero line");
    }


    [TestMethod]
    public void BeyondZeroLine_ReadsTheCrossCandleNotTheSignalCandle()
    {
        MacdCrossPlugin.Settings.RequireCrossBeyondZeroLine = true;
        MacdCrossPlugin.Settings.ConfirmationCandles = 1;

        // Crossed under zero on candle 1, and by candle 0 the MACD has climbed above zero. The cross
        // was where it should be, so this counts.
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            data[1].MacdValue = -0.5;
            data[1].MacdSignal = -0.7;
            data[0].MacdValue = 0.5;
            data[0].MacdSignal = 0.3;
        });
        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Minimum distance
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The lines one price unit apart on a coin at 100 is 1%. A percentage of the price, so the
    /// same number means the same thing whatever the coin costs.
    /// </summary>
    [TestMethod]
    public void MinimumDistance_IsAPercentageOfThePrice()
    {
        MacdCrossPlugin.Settings.MinimumDistancePercentage = 2m;
        var (narrow, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0, distance: 1.0));
        Assert.IsFalse(narrow.IsSignal());
        StringAssert.Contains(narrow.ExtraText, "1.000% apart");

        MacdCrossPlugin.Settings.MinimumDistancePercentage = 0.5m;
        var (wide, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0, distance: 1.0));
        Assert.IsTrue(wide.IsSignal(), wide.ExtraText);
    }


    [TestMethod]
    public void MinimumDistance_ReadsTheSameForAShort()
    {
        MacdCrossPlugin.Settings.MinimumDistancePercentage = 0.5m;
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Short, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Short, 0, distance: 1.0));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "1.000% apart");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Trend strength (ADX)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Puts the same ADX on every candle of the series.</summary>
    private static void AdxEverywhere(CryptoData[] data, double adx)
    {
        foreach (CryptoData d in data)
            d.Adx14 = adx;
    }


    [TestMethod]
    public void MinimumAdx_RefusesARangingMarket()
    {
        MacdCrossPlugin.Settings.AdxMinimum = 25m;

        var (trending, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 30.0);
        });
        Assert.IsTrue(trending.IsSignal(), trending.ExtraText);
        StringAssert.Contains(trending.ExtraText, "adx 30.0");

        var (ranging, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 18.0);
        });
        Assert.IsFalse(ranging.IsSignal());
        StringAssert.Contains(ranging.ExtraText, "under the minimum");
    }


    /// <summary>
    /// The ADX is declared by the plugin, but a warming-up hub has none yet. With the filter on
    /// that has to be a no said out loud; with the filter off (the default) the ADX is never read,
    /// which is what every other test in this file relies on.
    /// </summary>
    [TestMethod]
    public void MinimumAdx_WithoutAnAdx_SaysSo()
    {
        MacdCrossPlugin.Settings.AdxMinimum = 25m;
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "adx not available");
    }


    /// <summary>
    /// The young trend: the ADX has to have been under the threshold somewhere in the window. An
    /// ADX that has sat at 30 for the whole window is the tail of a move.
    /// </summary>
    [TestMethod]
    public void AdxRecentlyBelow_WantsTheAdxToHaveComeFromTheRangingZone()
    {
        MacdCrossPlugin.Settings.AdxRecentlyBelow = 20m;
        MacdCrossPlugin.Settings.AdxRecentlyWithinCandles = 5;

        var (young, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 30.0);
            data[3].Adx14 = 15.0;     // inside the window of five
        });
        Assert.IsTrue(young.IsSignal(), young.ExtraText);

        var (old, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 30.0);
            data[6].Adx14 = 15.0;     // just outside it
        });
        Assert.IsFalse(old.IsSignal());
        StringAssert.Contains(old.ExtraText, "did not come from under");

        var (never, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 30.0);
        });
        Assert.IsFalse(never.IsSignal());
    }


    /// <summary>
    /// Both together is the intended use: out of the ranging zone (recently under 20) and into a
    /// trend (now at least 25). The signal candle counts for both.
    /// </summary>
    [TestMethod]
    public void MinimumAndRecentlyBelow_TogetherAskForAnAdxClimbingOutOfTheRange()
    {
        MacdCrossPlugin.Settings.AdxMinimum = 25m;
        MacdCrossPlugin.Settings.AdxRecentlyBelow = 20m;
        MacdCrossPlugin.Settings.AdxRecentlyWithinCandles = 5;

        var (climbing, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 15.0);
            data[1].Adx14 = 22.0;
            data[0].Adx14 = 27.0;
        });
        Assert.IsTrue(climbing.IsSignal(), climbing.ExtraText);

        // Still climbing but not there yet: the minimum says no before the window is looked at.
        var (notYet, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            AdxEverywhere(data, 15.0);
            data[0].Adx14 = 22.0;
        });
        Assert.IsFalse(notYet.IsSignal());
        StringAssert.Contains(notYet.ExtraText, "under the minimum");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Relative volume
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Two recent candles against five before them. The baseline is the candles BEFORE the recent
    /// window, so the spike does not dilute itself: 250 against 100 is 2.5x, not 250 against the
    /// average of all seven.
    /// </summary>
    [TestMethod]
    public void RelativeVolume_ComparesTheRecentCandlesWithTheOnesBeforeThem()
    {
        MacdCrossPlugin.Settings.RelativeVolumeMinimum = 2m;
        MacdCrossPlugin.Settings.RelativeVolumeCandles = 2;
        MacdCrossPlugin.Settings.RelativeVolumeAverageCandles = 5;

        static void Volumes(CryptoCandle[] candles, decimal recent)
        {
            for (int i = 0; i < candles.Length; i++)
                candles[i].Volume = 100m;
            candles[0].Volume = recent;
            candles[1].Volume = recent;
        }

        var (busy, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            data => OnOurSide(data, CryptoTradeSide.Long, 0),
            candles => Volumes(candles, 250m));
        Assert.IsTrue(busy.IsSignal(), busy.ExtraText);
        StringAssert.Contains(busy.ExtraText, "volume 2.50x");

        var (quiet, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            data => OnOurSide(data, CryptoTradeSide.Long, 0),
            candles => Volumes(candles, 150m));
        Assert.IsFalse(quiet.IsSignal());
        StringAssert.Contains(quiet.ExtraText, "1.50x the average");
    }


    /// <summary>
    /// No volume in the baseline is not "infinitely busy", it is unknown - a no.
    /// </summary>
    [TestMethod]
    public void RelativeVolume_WithoutABaseline_SaysNo()
    {
        MacdCrossPlugin.Settings.RelativeVolumeMinimum = 2m;
        MacdCrossPlugin.Settings.RelativeVolumeCandles = 2;
        MacdCrossPlugin.Settings.RelativeVolumeAverageCandles = 5;

        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            data => OnOurSide(data, CryptoTradeSide.Long, 0),
            candles => { candles[0].Volume = 250m; candles[1].Volume = 250m; });
        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no volume to compare");
    }


    /// <summary>
    /// The baseline reaches further back than the series is long: not enough candles, said so, no
    /// exception. The start of every run looks like this.
    /// </summary>
    [TestMethod]
    public void RelativeVolume_WithoutEnoughHistory_SaysNoInsteadOfThrowing()
    {
        MacdCrossPlugin.Settings.RelativeVolumeMinimum = 2m;
        MacdCrossPlugin.Settings.RelativeVolumeCandles = 2;
        MacdCrossPlugin.Settings.RelativeVolumeAverageCandles = 50;

        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            data => OnOurSide(data, CryptoTradeSide.Long, 0));
        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "not enough candles");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The exit reads the position's side: a long leaves when the MACD line is under the signal
    /// line. The default series has it there, so a long wants out and a short does not.
    /// </summary>
    [TestMethod]
    public void LinesAgainstThePosition_AskForTheExit()
    {
        var (longSide, _) = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });
        Assert.IsTrue(longSide.IsExitSignal(), longSide.ExtraText);
        StringAssert.Contains(longSide.ExtraText, "crossed back under");

        var (shortSide, _) = MakeSeries(CryptoTradeSide.Short, Enough, _ => { });
        Assert.IsTrue(shortSide.IsExitSignal(), shortSide.ExtraText);
        StringAssert.Contains(shortSide.ExtraText, "crossed back above");
    }


    [TestMethod]
    public void LinesStillOnOurSide_DoNotAskForTheExit()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));

        Assert.IsFalse(algorithm.IsExitSignal());
        StringAssert.Contains(algorithm.ExtraText, "still on our side");
    }


    /// <summary>
    /// The exit is a state, not an event: lines that have been against the position for many
    /// candles still ask for it. That is what makes a restart, or a candle the monitor did not get
    /// to see, harmless.
    /// </summary>
    [TestMethod]
    public void TheExitIsAState_NotACrossOnThisCandle()
    {
        // Against us for the whole series - no candle on which the cross back "happened".
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });
        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
    }


    [TestMethod]
    public void ExitConfirmation_WantsTheLinesAgainstUsThatLong()
    {
        MacdCrossPlugin.Settings.ExitConfirmationCandles = 1;

        // Crossed back on this candle only: candle 1 was still on our side.
        var (fresh, _) = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 1));
        Assert.IsFalse(fresh.IsExitSignal());
        StringAssert.Contains(fresh.ExtraText, "not held");

        var (held, _) = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });
        Assert.IsTrue(held.IsExitSignal(), held.ExtraText);
    }


    /// <summary>
    /// With the exit switched off the strategy neither declares an exit rule nor answers yes to it,
    /// so the position monitor never asks and the trader's normal exits are all there is.
    /// </summary>
    [TestMethod]
    public void WithExitOnCrossBackOff_ThereIsNoExitRule()
    {
        MacdCrossPlugin.Settings.ExitOnCrossBack = false;
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });

        Assert.IsFalse(algorithm.HasExitSignal);
        Assert.IsFalse(algorithm.IsExitSignal());
    }


    [TestMethod]
    public void WithExitOnCrossBackOn_TheStrategyDeclaresAnExitRule()
    {
        var (algorithm, _) = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });
        Assert.IsTrue(algorithm.HasExitSignal);
    }
}

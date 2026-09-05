using CryptoScanner.Analyzers.Bbma;
using CryptoScanner.Analyzers.Bbma.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Analyzer.Bbma;

/// <summary>
/// The BBMA Omni strategy: the pieces of the 3-TF signal that were fixed on 2026-09-05 and the
/// strategy's own exit.
/// <para>
/// Two things went wrong before that date. The LTF lookback letter for a CSD or a CSM candle was
/// '-', which the code-match rejects, so exactly the two setups the BBMA rules call the strongest
/// ("Reentry after CSD" and "Reentry after CSM") never fired. And the HTF trend filter demanded
/// the WMA zone on the wrong side of the mid-band — the opposite of the OmniView Green/Red Zone
/// the indicator itself draws. The tests below pin the corrected behaviour.
/// </para>
/// <para>
/// Standard band used throughout: mid 100, deviation 5 → upper 105, lower 95.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class BbmaOmniTests : TestBase
{
    private const double Mid = 100.0;
    private const double Dev = 5.0;     // → upper 105, lower 95

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        ApplyDefaults(BbmaPlugin.Settings);
        // The exit tests read the band of the position's own interval unless they say otherwise;
        // the HTF band (the default) needs HTF data, which only the HTF tests set up.
        BbmaPlugin.Settings.TakeProfitOnHtfBand = false;
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(BbmaPlugin.Settings);

    private static void ApplyDefaults(BbmaSettings settings)
    {
        BbmaSettings fresh = new();
        settings.ReentryStrict = fresh.ReentryStrict;
        settings.ReentryMinCandlesAfterTrigger = fresh.ReentryMinCandlesAfterTrigger;
        settings.HtfSetupLookback = fresh.HtfSetupLookback;
        settings.HtfSetupExtremeInvalidates = fresh.HtfSetupExtremeInvalidates;
        settings.TakeProfitAtOuterBand = fresh.TakeProfitAtOuterBand;
        settings.TakeProfitOnHtfBand = fresh.TakeProfitOnHtfBand;
        settings.StopBeyondReentryCandle = fresh.StopBeyondReentryCandle;
        settings.StopMarginPercentage = fresh.StopMarginPercentage;
    }


    /// <summary>
    /// A candle with its indicator values. The WMA's default to a neutral configuration (all
    /// four above the mid) and the EMA50 to under the mid, so a test only names what it changes.
    /// </summary>
    private static MyData MakeData(
        decimal open, decimal high, decimal low, decimal close,
        double ema50 = 95.0,
        double wma5Low = 101.0, double wma10Low = 101.5,
        double wma5High = 102.0, double wma10High = 102.5)
    {
        var candle = new CryptoCandle
        {
            TickDecimals = 2,
            Open = open,
            High = high,
            Low = low,
            Close = close,
        };

        var data = new CryptoData
        {
            Sma20 = Mid,
            BollingerBandsDeviation = Dev,
            BollingerBandsPercentage = 1.0,
            Ema50 = ema50,
            Wma05Low = wma5Low,
            Wma10Low = wma10Low,
            Wma05High = wma5High,
            Wma10High = wma10High,
        };

        return new MyData { Candle = candle, CandleData = data };
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


    /// <summary>
    /// An algorithm of the given side with <paramref name="candleLast"/> as the candle that just
    /// closed — all the exit rule looks at.
    /// </summary>
    private static SignalBbmaOmniBase MakeAlgorithm(CryptoTradeSide side, MyData candleLast)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        if (side == CryptoTradeSide.Long)
        {
            return new SignalBbmaOmniLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = side,
                SignalStrategy = BbmaPlugin.StrategyInternal.ToLower(),
                CandleLast = candleLast,
            };
        }

        return new SignalBbmaOmniShort
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = BbmaPlugin.StrategyInternal.ToLower(),
            CandleLast = candleLast,
        };
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The code-match: CSD and CSM are triggers, not a dash
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CodeLetters_CsdAndCsm_HaveALetterOfTheirOwn()
    {
        Assert.AreEqual("D", SignalBbmaOmniBase.OmniStateCode(SignalBbmaOmniBase.OmniState.Csd));
        Assert.AreEqual("M", SignalBbmaOmniBase.OmniStateCode(SignalBbmaOmniBase.OmniState.Csm));
        Assert.AreEqual("-", SignalBbmaOmniBase.OmniStateCode(SignalBbmaOmniBase.OmniState.None));
        Assert.AreEqual("R", SignalBbmaOmniBase.OmniStateCode(SignalBbmaOmniBase.OmniState.Reentry));
    }


    /// <summary>
    /// "Reentry after CSD" and "Reentry after CSM" are the two strongest setups of the method; the
    /// code-match has to let them through like it lets an Extreme or an MHV through.
    /// </summary>
    [TestMethod]
    public void CodeMatch_AcceptsAReentryAfterCsdOrCsm()
    {
        Assert.IsTrue(SignalBbmaOmniBase.IsCodeMatch("RRD"), "HTF reentry, LTF after a CSD");
        Assert.IsTrue(SignalBbmaOmniBase.IsCodeMatch("R-M"), "HTF reentry, LTF after a CSM");
        Assert.IsTrue(SignalBbmaOmniBase.IsCodeMatch("RRE"), "the classic RRE code still passes");
        Assert.IsTrue(SignalBbmaOmniBase.IsCodeMatch("REH"), "the classic REM code (H for MHV) still passes");
    }


    [TestMethod]
    public void CodeMatch_StillWantsAnHtfReentryAndARealLtfTrigger()
    {
        Assert.IsFalse(SignalBbmaOmniBase.IsCodeMatch("RR-"), "no LTF trigger found");
        Assert.IsFalse(SignalBbmaOmniBase.IsCodeMatch("RRR"), "another reentry is not a trigger");
        Assert.IsFalse(SignalBbmaOmniBase.IsCodeMatch("ERE"), "the HTF has to be in reentry");
        Assert.IsFalse(SignalBbmaOmniBase.IsCodeMatch("-RE"), "the HTF has to be in reentry");
        Assert.IsFalse(SignalBbmaOmniBase.IsCodeMatch(""), "a malformed code never matches");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The HTF trend zone (OmniView Green Zone / Red Zone)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The uptrend picture of BBMA: EMA50 under the mid-band, the MA5/10 zone above it.
    /// </summary>
    [TestMethod]
    public void HtfZone_Bullish_WhenEma50BelowMidAndWmaZoneAboveIt()
    {
        var data = MakeData(101, 103, 100.5m, 102, ema50: 95, wma5Low: 101, wma10Low: 101.5, wma5High: 102, wma10High: 102.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsHtfTrendBullish(data));
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBearish(data));
    }


    /// <summary>
    /// The configuration the OLD long filter demanded (Wma05Low under the mid) is not an uptrend
    /// zone: the reentry zone sits below the mid-band there.
    /// </summary>
    [TestMethod]
    public void HtfZone_NotBullish_WhenTheWmaZoneIsUnderTheMid()
    {
        var data = MakeData(101, 103, 98, 102, ema50: 95, wma5Low: 99, wma10Low: 101.5, wma5High: 102, wma10High: 102.5);
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBullish(data));
    }


    [TestMethod]
    public void HtfZone_NotBullish_WhenEma50IsAboveTheMid()
    {
        var data = MakeData(101, 103, 100.5m, 102, ema50: 101, wma5Low: 101, wma10Low: 101.5, wma5High: 102, wma10High: 102.5);
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBullish(data));
    }


    /// <summary>OmniView uses non-strict comparisons: a WMA exactly on the mid is still inside the zone.</summary>
    [TestMethod]
    public void HtfZone_Bullish_WhenAWmaSitsExactlyOnTheMid()
    {
        var data = MakeData(101, 103, 100, 102, ema50: 100, wma5Low: 100, wma10Low: 100, wma5High: 101, wma10High: 101);
        Assert.IsTrue(SignalBbmaOmniBase.IsHtfTrendBullish(data));
    }


    [TestMethod]
    public void HtfZone_Bearish_WhenEma50AboveMidAndWmaZoneBelowIt()
    {
        var data = MakeData(99, 99.5m, 97, 98, ema50: 105, wma5Low: 97.5, wma10Low: 97, wma5High: 99, wma10High: 98.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsHtfTrendBearish(data));
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBullish(data));
    }


    [TestMethod]
    public void HtfZone_NotBearish_WhenTheWmaZoneIsAboveTheMid()
    {
        var data = MakeData(99, 102, 97, 98, ema50: 105, wma5Low: 97.5, wma10Low: 97, wma5High: 101, wma10High: 98.5);
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBearish(data));
    }


    [TestMethod]
    public void HtfZone_Neither_WhenTheIndicatorsAreMissing()
    {
        var data = MakeData(101, 103, 100.5m, 102);
        data.CandleData!.Ema50 = null;
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBullish(data));
        Assert.IsFalse(SignalBbmaOmniBase.IsHtfTrendBearish(data));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit: take profit at the outer band
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Exit_Long_WhenAClosedCandleReachedTheUpperBand()
    {
        // High 105.5 ≥ upper 105: target reached, the close does not matter.
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, MakeData(103, 105.5m, 102.5m, 104));
        Assert.IsTrue(algorithm.HasExitSignal);
        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "reached the upper band");
    }


    [TestMethod]
    public void Exit_Long_NotYet_WhileTheUpperBandIsOutOfReach()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, MakeData(103, 104.9m, 102.5m, 104));
        Assert.IsFalse(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "not reached");
    }


    [TestMethod]
    public void Exit_Short_WhenAClosedCandleReachedTheLowerBand()
    {
        // Low 94.5 ≤ lower 95.
        var algorithm = MakeAlgorithm(CryptoTradeSide.Short, MakeData(97, 97.5m, 94.5m, 96));
        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "reached the lower band");
    }


    [TestMethod]
    public void Exit_Short_NotYet_WhileTheLowerBandIsOutOfReach()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Short, MakeData(97, 97.5m, 95.1m, 96));
        Assert.IsFalse(algorithm.IsExitSignal(), algorithm.ExtraText);
    }


    /// <summary>
    /// The exit is the LONG's upper band: a long that touches the LOWER band has nothing to take
    /// profit on, and a short that touches the upper band neither.
    /// </summary>
    [TestMethod]
    public void Exit_ReadsTheBandOfThePositionsSide()
    {
        var longSide = MakeAlgorithm(CryptoTradeSide.Long, MakeData(97, 97.5m, 94.5m, 96));
        Assert.IsFalse(longSide.IsExitSignal(), longSide.ExtraText);

        var shortSide = MakeAlgorithm(CryptoTradeSide.Short, MakeData(103, 105.5m, 102.5m, 104));
        Assert.IsFalse(shortSide.IsExitSignal(), shortSide.ExtraText);
    }


    /// <summary>
    /// With the band exit switched off the strategy declares no exit rule, so the position
    /// monitor never asks and the trader's stop loss and take profit are all there is.
    /// </summary>
    [TestMethod]
    public void Exit_Off_ThereIsNoExitRule()
    {
        BbmaPlugin.Settings.TakeProfitAtOuterBand = false;
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, MakeData(103, 105.5m, 102.5m, 104));
        Assert.IsFalse(algorithm.HasExitSignal);
        Assert.IsFalse(algorithm.IsExitSignal());
    }


    [TestMethod]
    public void Exit_NoBandData_SaysNoInsteadOfThrowing()
    {
        var data = MakeData(103, 105.5m, 102.5m, 104);
        data.CandleData!.BollingerBandsDeviation = null;
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, data);
        Assert.IsFalse(algorithm.IsExitSignal());
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The stop: just beyond the reentry candle
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Long: close 100, low 99 → 1% to the low, plus 0.1% margin = 1.1%.
    /// </summary>
    [TestMethod]
    public void Stop_Long_DistanceToTheLowPlusMargin()
    {
        var candle = MakeData(99.5m, 100.5m, 99, 100);
        decimal? sl = SignalBbmaOmniBase.StopPercentageBeyondCandle(candle, CryptoTradeSide.Long, 0.1m);
        Assert.AreEqual(1.1m, sl);
    }


    /// <summary>
    /// Short: close 100, high 102 → 2% to the high, no margin = 2%.
    /// </summary>
    [TestMethod]
    public void Stop_Short_DistanceToTheHighPlusMargin()
    {
        var candle = MakeData(101, 102, 99.5m, 100);
        decimal? sl = SignalBbmaOmniBase.StopPercentageBeyondCandle(candle, CryptoTradeSide.Short, 0m);
        Assert.AreEqual(2.0m, sl);
    }


    /// <summary>
    /// A candle that closed on its own low leaves no room without margin: no stop of the
    /// strategy's own, the trader keeps its global percentage.
    /// </summary>
    [TestMethod]
    public void Stop_Null_WhenThereIsNoRoomBeyondTheCandle()
    {
        var candle = MakeData(101, 101, 100, 100);
        Assert.IsNull(SignalBbmaOmniBase.StopPercentageBeyondCandle(candle, CryptoTradeSide.Long, 0m));

        // The margin alone makes it a stop again.
        Assert.AreEqual(0.1m, SignalBbmaOmniBase.StopPercentageBeyondCandle(candle, CryptoTradeSide.Long, 0.1m));
    }


    /// <summary>Before IsSignal ran there is nothing to hand to the trader.</summary>
    [TestMethod]
    public void Stop_NotSet_BeforeASignalFired()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, MakeData(99.5m, 100.5m, 99, 100));
        Assert.IsNull(algorithm.OverrideSlPercentage);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  The reentry candle: loose (OmniView) against strict (the rules)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Long, zone above the mid: MA10 low 101.5, MA5 low 102. The low dipped to 101 (touched both),
    /// the close came back to 101.8 — above MA10 but still under MA5, so inside the zone. OmniView
    /// takes that as a reentry, the rules do not: the candle must not close beyond the MA5/10.
    /// </summary>
    [TestMethod]
    public void Reentry_Long_ACloseInsideTheZone_IsLooseOnly()
    {
        var data = MakeData(102.5m, 102.6m, 101, 101.8m, wma5Low: 102, wma10Low: 101.5, wma5High: 103, wma10High: 102.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsReentryBuy(data, strict: false));
        Assert.IsFalse(SignalBbmaOmniBase.IsReentryBuy(data, strict: true));
    }


    [TestMethod]
    public void Reentry_Long_ACloseBackAboveBothMas_IsStrictToo()
    {
        var data = MakeData(102.5m, 102.6m, 101, 102.2m, wma5Low: 102, wma10Low: 101.5, wma5High: 103, wma10High: 102.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsReentryBuy(data, strict: false));
        Assert.IsTrue(SignalBbmaOmniBase.IsReentryBuy(data, strict: true));
    }


    /// <summary>
    /// The zone under the mid-band (MA5 low 99, MA10 low 98.5) with a close above the mid: a bounce
    /// from under the mid, not a pullback in an uptrend. Loose says yes, strict says no.
    /// </summary>
    [TestMethod]
    public void Reentry_Long_AZoneUnderTheMid_IsLooseOnly()
    {
        var data = MakeData(99.5m, 100.6m, 98.8m, 100.4m, wma5Low: 99, wma10Low: 98.5, wma5High: 100, wma10High: 99.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsReentryBuy(data, strict: false));
        Assert.IsFalse(SignalBbmaOmniBase.IsReentryBuy(data, strict: true));
    }


    [TestMethod]
    public void Reentry_Long_NoTouchOfTheZone_IsNoReentryEitherWay()
    {
        var data = MakeData(102.5m, 103, 102.2m, 102.8m, wma5Low: 102, wma10Low: 101.5, wma5High: 103, wma10High: 102.5);
        Assert.IsFalse(SignalBbmaOmniBase.IsReentryBuy(data, strict: false));
        Assert.IsFalse(SignalBbmaOmniBase.IsReentryBuy(data, strict: true));
    }


    [TestMethod]
    public void Reentry_Long_ACloseUnderTheMid_IsNoReentryEitherWay()
    {
        var data = MakeData(100.5m, 100.6m, 99.5m, 99.8m, wma5Low: 100, wma10Low: 100.2, wma5High: 101, wma10High: 101.2);
        Assert.IsFalse(SignalBbmaOmniBase.IsReentryBuy(data, strict: false));
        Assert.IsFalse(SignalBbmaOmniBase.IsReentryBuy(data, strict: true));
    }


    /// <summary>Short mirror: MA5 high 98, MA10 high 98.5; the high touched 99, the close 98.2 is inside the zone.</summary>
    [TestMethod]
    public void Reentry_Short_ACloseInsideTheZone_IsLooseOnly()
    {
        var data = MakeData(97.5m, 99, 97.4m, 98.2m, ema50: 105, wma5Low: 97, wma10Low: 97.5, wma5High: 98, wma10High: 98.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsReentrySell(data, strict: false));
        Assert.IsFalse(SignalBbmaOmniBase.IsReentrySell(data, strict: true));
    }


    [TestMethod]
    public void Reentry_Short_ACloseBackUnderBothMas_IsStrictToo()
    {
        var data = MakeData(97.5m, 99, 97.4m, 97.8m, ema50: 105, wma5Low: 97, wma10Low: 97.5, wma5High: 98, wma10High: 98.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsReentrySell(data, strict: true));
    }


    [TestMethod]
    public void Reentry_Short_AZoneAboveTheMid_IsLooseOnly()
    {
        var data = MakeData(100.5m, 101.2m, 99.4m, 99.6m, ema50: 105, wma5Low: 100, wma10Low: 100.5, wma5High: 101, wma10High: 101.5);
        Assert.IsTrue(SignalBbmaOmniBase.IsReentrySell(data, strict: false));
        Assert.IsFalse(SignalBbmaOmniBase.IsReentrySell(data, strict: true));
    }


    /// <summary>
    /// The pullback has to take its time: a trigger on the candle before the reentry is too fresh
    /// with the default minimum of three, and zero switches the check off.
    /// </summary>
    [TestMethod]
    public void Reentry_TriggerMustBeSomeCandlesBack()
    {
        Assert.IsTrue(SignalBbmaOmniBase.TriggerTooRecent(candlesSinceTrigger: 1, minimum: 3));
        Assert.IsTrue(SignalBbmaOmniBase.TriggerTooRecent(candlesSinceTrigger: 2, minimum: 3));
        Assert.IsFalse(SignalBbmaOmniBase.TriggerTooRecent(candlesSinceTrigger: 3, minimum: 3));
        Assert.IsFalse(SignalBbmaOmniBase.TriggerTooRecent(candlesSinceTrigger: 10, minimum: 3));
        Assert.IsFalse(SignalBbmaOmniBase.TriggerTooRecent(candlesSinceTrigger: 1, minimum: 0));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit on the HTF band
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// An algorithm on 5m whose HTF (1h, the fixed triplet for 5m is 15m/1h) has a closed candle
    /// with a wider band: mid 100, deviation 10 → upper 110, lower 90. The 5m candle is the last
    /// one of that hour, so the hour candle counts as closed at its close.
    /// </summary>
    private static SignalBbmaOmniBase MakeAlgorithmWithHtf(CryptoTradeSide side, MyData candleLast, double htfDeviation = 10.0)
    {
        SignalBbmaOmniBase algorithm = MakeAlgorithm(side, candleLast);
        CryptoInterval ltf = algorithm.Interval;
        CryptoInterval htf = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Hour 100 of the epoch; the LTF candle opens 55 minutes into it.
        CandleTime htfOpen = new((uint)(100 * htf.Duration));
        // CryptoCandle is a struct: take a copy, stamp the time, put it back.
        CryptoCandle ltfCandle = candleLast.Candle;
        ltfCandle.OpenTime = htfOpen + (uint)(11 * ltf.Duration);
        candleLast.Candle = ltfCandle;

        CryptoSymbolInterval htfSymbolInterval = algorithm.Symbol.GetSymbolInterval(htf.IntervalPeriod);
        var htfCandle = new CryptoCandle { TickDecimals = 2, OpenTime = htfOpen, Open = 100, High = 104, Low = 96, Close = 101 };
        htfSymbolInterval.CandleList.TryAdd(htfOpen, htfCandle);
        htfSymbolInterval.Data[htfOpen] = new CryptoData
        {
            Sma20 = Mid,
            BollingerBandsDeviation = htfDeviation,
            BollingerBandsPercentage = 1.0,
        };
        return algorithm;
    }


    /// <summary>
    /// The 5m band is reached (high 106 over 105) but the 1h band (110) is not: with the HTF band
    /// as target the position stays.
    /// </summary>
    [TestMethod]
    public void Exit_HtfBand_TheOwnBandIsNotEnough()
    {
        BbmaPlugin.Settings.TakeProfitOnHtfBand = true;
        var algorithm = MakeAlgorithmWithHtf(CryptoTradeSide.Long, MakeData(103, 106, 102.5m, 104));
        Assert.IsFalse(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "1h");
        StringAssert.Contains(algorithm.ExtraText, "not reached");
    }


    [TestMethod]
    public void Exit_HtfBand_Long_WhenTheHtfUpperBandIsReached()
    {
        BbmaPlugin.Settings.TakeProfitOnHtfBand = true;
        var algorithm = MakeAlgorithmWithHtf(CryptoTradeSide.Long, MakeData(108, 110.5m, 107.5m, 109));
        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "reached the upper band 110");
        StringAssert.Contains(algorithm.ExtraText, "1h");
    }


    [TestMethod]
    public void Exit_HtfBand_Short_WhenTheHtfLowerBandIsReached()
    {
        BbmaPlugin.Settings.TakeProfitOnHtfBand = true;
        var algorithm = MakeAlgorithmWithHtf(CryptoTradeSide.Short, MakeData(92, 92.5m, 89.5m, 91));
        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "reached the lower band 90");
    }


    /// <summary>Without HTF data there is no band to aim at: no exit, no exception.</summary>
    [TestMethod]
    public void Exit_HtfBand_NoHtfData_SaysNoInsteadOfThrowing()
    {
        BbmaPlugin.Settings.TakeProfitOnHtfBand = true;
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, MakeData(108, 110.5m, 107.5m, 109));
        Assert.IsFalse(algorithm.IsExitSignal());
        StringAssert.Contains(algorithm.ExtraText, "no HTF data");
    }


    /// <summary>With the HTF band off the same candle leaves on its own 5m band.</summary>
    [TestMethod]
    public void Exit_OwnBand_WhenTheHtfBandIsSwitchedOff()
    {
        BbmaPlugin.Settings.TakeProfitOnHtfBand = false;
        var algorithm = MakeAlgorithmWithHtf(CryptoTradeSide.Long, MakeData(103, 106, 102.5m, 104));
        Assert.IsTrue(algorithm.IsExitSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "5m");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  The HTF setup: a CSD or CSM behind the HTF reentry, nothing against it since
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A series of 1h candles, index 0 = newest (the HTF reentry candle the check runs on), every
    /// one a quiet candle inside the band on the trade's side of the mid. The shape callback
    /// turns the candles it wants into a CSD, a CSM, or something against the trade. Returns the
    /// HTF classifier (made through CreateForInterval, so its opposite-side checkers are wired)
    /// and the newest candle.
    /// </summary>
    private static (SignalBbmaOmniBase Htf, MyData Current) MakeHtfSeries(
        CryptoTradeSide side, int count, Action<CryptoCandle[], CryptoData[]> shape)
    {
        CryptoInterval htf = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        MyData ltfCandle = side == CryptoTradeSide.Long
            ? MakeData(101, 102, 100.5m, 101.5m)
            : MakeData(99, 99.5m, 98, 98.5m, ema50: 105, wma5Low: 97.5, wma10Low: 97, wma5High: 98, wma10High: 98.5);
        SignalBbmaOmniBase ltf = MakeAlgorithm(side, ltfCandle);
        CryptoSymbolInterval htfSymbolInterval = ltf.Symbol.GetSymbolInterval(htf.IntervalPeriod);

        CryptoCandle[] candles = new CryptoCandle[count];
        CryptoData[] data = new CryptoData[count];
        for (int i = 0; i < count; i++)
        {
            MyData quiet = side == CryptoTradeSide.Long
                ? MakeData(101, 102, 100.5m, 101.5m)
                : MakeData(99, 99.5m, 98, 98.5m, ema50: 105, wma5Low: 97.5, wma10Low: 97, wma5High: 98, wma10High: 98.5);
            CryptoCandle candle = quiet.Candle;
            candle.OpenTime = new CandleTime((uint)((300 - i) * htf.Duration));
            candles[i] = candle;
            data[i] = quiet.CandleData!;
        }
        shape(candles, data);

        for (int i = 0; i < count; i++)
        {
            htfSymbolInterval.CandleList.TryAdd(candles[i].OpenTime, candles[i]);
            htfSymbolInterval.Data[candles[i].OpenTime] = data[i];
        }

        SignalBbmaOmniBase htfClassifier = ltf.CreateForInterval(htf);
        return (htfClassifier, new MyData { Candle = candles[0], CandleData = data[0] });
    }

    /// <summary>A long CSM: the close at or beyond the upper band (105).</summary>
    private static void CsmBuy(CryptoCandle[] c, int i) { c[i].Open = 103; c[i].High = 106.5m; c[i].Low = 102.5m; c[i].Close = 106; }
    /// <summary>A long CSD: opened under the mid, closed above it and above both MA5/10 high (102 / 102.5).</summary>
    private static void CsdBuy(CryptoCandle[] c, int i) { c[i].Open = 99; c[i].High = 103.5m; c[i].Low = 98.5m; c[i].Close = 103; }
    /// <summary>A short CSM: the close at or beyond the lower band (95).</summary>
    private static void CsmSell(CryptoCandle[] c, int i) { c[i].Open = 96; c[i].High = 96.5m; c[i].Low = 93.5m; c[i].Close = 94; }
    /// <summary>
    /// A sell-side Extreme: MA5 high poked above the upper band, a bearish candle whose high
    /// reached the band and whose close fell back under it.
    /// </summary>
    private static void ExtremeSell(CryptoCandle[] c, CryptoData[] d, int i)
    {
        c[i].Open = 104.5m; c[i].High = 105.5m; c[i].Low = 103; c[i].Close = 103;
        d[i].Wma05High = 106;
    }


    [TestMethod]
    public void HtfSetup_Long_ACsmSomeCandlesBack_IsTheSetup()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) => CsmBuy(c, 3));
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSM", setup);
    }


    [TestMethod]
    public void HtfSetup_Long_ACsdSomeCandlesBack_IsTheSetup()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) => CsdBuy(c, 2));
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSD", setup);
    }


    /// <summary>The nearer setup names the reentry: a CSM after a CSD makes it a "reentry after CSM".</summary>
    [TestMethod]
    public void HtfSetup_Long_TheMostRecentSetupCounts()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) => { CsdBuy(c, 5); CsmBuy(c, 2); });
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSM", setup);
    }


    [TestMethod]
    public void HtfSetup_Long_NothingWithinTheLookback_IsNoSetup()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (_, _) => { });
        Assert.IsFalse(htf.CheckHtf(current, out string setup));
        StringAssert.Contains(setup, "no CSD/CSM");
    }


    [TestMethod]
    public void HtfSetup_Long_ASetupBeyondTheLookback_DoesNotCount()
    {
        BbmaPlugin.Settings.HtfSetupLookback = 3;
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) => CsmBuy(c, 5));
        Assert.IsFalse(htf.CheckHtf(current, out string setup));
        StringAssert.Contains(setup, "within 3");
    }


    /// <summary>A CSM buy five candles back, but a close under the lower band since: the market said the other way.</summary>
    [TestMethod]
    public void HtfSetup_Long_AnOppositeCsmSinceTheSetup_VoidsIt()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) => { CsmBuy(c, 5); CsmSell(c, 2); });
        Assert.IsFalse(htf.CheckHtf(current, out string setup));
        StringAssert.Contains(setup, "opposite CSM");
    }


    [TestMethod]
    public void HtfSetup_Long_AnOppositeExtremeSinceTheSetup_VoidsIt()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, d) => { CsmBuy(c, 5); ExtremeSell(c, d, 2); });
        Assert.IsFalse(htf.CheckHtf(current, out string setup));
        StringAssert.Contains(setup, "opposite Extreme");
    }


    [TestMethod]
    public void HtfSetup_Long_AnOppositeExtreme_IsIgnoredWhenSwitchedOff()
    {
        BbmaPlugin.Settings.HtfSetupExtremeInvalidates = false;
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, d) => { CsmBuy(c, 5); ExtremeSell(c, d, 2); });
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSM", setup);
    }


    /// <summary>An opposite CSM OLDER than the setup is history: the setup came after it.</summary>
    [TestMethod]
    public void HtfSetup_Long_AnOppositeCsmBeforeTheSetup_DoesNotMatter()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) => { CsmSell(c, 6); CsmBuy(c, 3); });
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSM", setup);
    }


    [TestMethod]
    public void HtfSetup_LookbackZero_AcceptsEveryReentry()
    {
        BbmaPlugin.Settings.HtfSetupLookback = 0;
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (_, _) => { });
        Assert.IsTrue(htf.CheckHtf(current, out string setup));
        Assert.AreEqual("any", setup);
    }


    [TestMethod]
    public void HtfSetup_Short_ACsmSell_IsTheSetup()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Short, 8, (c, _) => CsmSell(c, 3));
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSM", setup);
    }


    [TestMethod]
    public void HtfSetup_Short_AnOppositeCsmBuySinceTheSetup_VoidsIt()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Short, 8, (c, _) => { CsmSell(c, 5); CsmBuy(c, 2); });
        Assert.IsFalse(htf.CheckHtf(current, out string setup));
        StringAssert.Contains(setup, "opposite CSM");
    }


    /// <summary>
    /// The classifier made for the HTF reads ITS interval: the two-bar CSD looks at the 1h candle
    /// before, not at a 5m candle. A CSD in its two-bar form (previous 1h candle fully under the
    /// mid, this one opened and closed above it and above the MA zone) is only visible that way.
    /// </summary>
    [TestMethod]
    public void HtfClassifier_ReadsThePreviousCandleOfItsOwnInterval()
    {
        var (htf, current) = MakeHtfSeries(CryptoTradeSide.Long, 8, (c, _) =>
        {
            c[3].Open = 98; c[3].High = 99; c[3].Low = 97.5m; c[3].Close = 98.5m;   // under the mid
            c[2].Open = 101; c[2].High = 103.5m; c[2].Low = 100.8m; c[2].Close = 103; // two-bar CSD
        });
        Assert.IsTrue(htf.CheckHtf(current, out string setup), setup);
        Assert.AreEqual("CSD", setup);
    }
}

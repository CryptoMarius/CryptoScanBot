using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The order in which the signals of one minute are judged, which is what decides WHICH signal wins.
/// <para>
/// Only one position can be open per coin, so the first signal that gets through opens it and the
/// rest of that minute is never looked at. Until 16-09-2026 that order came out of the loop by
/// accident: the interval list is in enum order, so the shortest interval whose candle had just
/// closed always went first. Nothing says that is a good rule, so it is a setting now - and these
/// tests pin down that the default still hands back exactly the old order.
/// </para>
/// <para>
/// Inside ONE interval there is nothing to choose: SignalCreate empties the list of that interval and
/// of every lower one before it adds a signal, so only the newest survives.
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class SignalPriorityOrderTests : TestBase
{
    private CryptoSignalPriority _saved;

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        _saved = GlobalData.Settings.Trading.SignalPriority;
    }

    [TestCleanup]
    public void Restore()
    {
        GlobalData.Settings.Trading.SignalPriority = _saved;
    }


    /// <summary>A signal on this interval, at this price. Only the price and the interval matter here.</summary>
    private static CryptoSignal Signal(CryptoInterval interval, decimal price)
    {
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        return new CryptoSignal
        {
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = interval,
            IntervalId = interval.Id,
            Candle = null,
            Side = CryptoTradeSide.Long,
            Strategy = "stobb",
            SignalPrice = price,
        };
    }

    private static CryptoSymbolInterval Interval(CryptoIntervalPeriod period, params decimal[] signalPrices)
    {
        CryptoInterval interval = GlobalData.IntervalList.First(x => x.IntervalPeriod == period);
        CryptoSymbolInterval symbolInterval = new() { Interval = interval, IntervalPeriod = period };
        foreach (decimal price in signalPrices)
            symbolInterval.SignalList.Add(Signal(interval, price));
        return symbolInterval;
    }

    private static List<string> Names(List<CryptoSymbolInterval> intervals)
        => [.. intervals.Select(x => x.Interval!.Name)];


    [TestMethod]
    public void TheDefault_LeavesTheOrderExactlyAsItWas()
    {
        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.ShortestIntervalFirst;
        List<CryptoSymbolInterval> intervals =
        [
            Interval(CryptoIntervalPeriod.interval1m, 100m),
            Interval(CryptoIntervalPeriod.interval15m, 100m),
            Interval(CryptoIntervalPeriod.interval1h, 100m),
        ];

        CollectionAssert.AreEqual(new List<string> { "1m", "15m", "1h" },
            Names(PositionMonitor.OrderIntervalsForSignals(intervals, 100m)),
            "de standaard is de volgorde van de lijst zelf, en die staat op interval-volgorde");
    }


    [TestMethod]
    public void LongestIntervalFirst_TurnsTheOrderAround()
    {
        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.LongestIntervalFirst;
        List<CryptoSymbolInterval> intervals =
        [
            Interval(CryptoIntervalPeriod.interval1m, 100m),
            Interval(CryptoIntervalPeriod.interval15m, 100m),
            Interval(CryptoIntervalPeriod.interval1h, 100m),
        ];

        CollectionAssert.AreEqual(new List<string> { "1h", "15m", "1m" },
            Names(PositionMonitor.OrderIntervalsForSignals(intervals, 100m)));
    }


    /// <summary>
    /// The interval holding the nearest entry goes first, whatever its duration - so a 1h signal at
    /// 101 beats a 1m signal at 130 when the price is 100.
    /// </summary>
    [TestMethod]
    public void NearestEntryPrice_PutsTheClosestSignalInFront()
    {
        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.NearestEntryPrice;
        List<CryptoSymbolInterval> intervals =
        [
            Interval(CryptoIntervalPeriod.interval1m, 130m),
            Interval(CryptoIntervalPeriod.interval15m, 110m),
            Interval(CryptoIntervalPeriod.interval1h, 101m),
        ];

        CollectionAssert.AreEqual(new List<string> { "1h", "15m", "1m" },
            Names(PositionMonitor.OrderIntervalsForSignals(intervals, 100m)));
    }


    /// <summary>An interval without signals has nothing to offer and must not push the others back.</summary>
    [TestMethod]
    public void NearestEntryPrice_PutsAnIntervalWithoutSignalsLast()
    {
        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.NearestEntryPrice;
        List<CryptoSymbolInterval> intervals =
        [
            Interval(CryptoIntervalPeriod.interval1m),
            Interval(CryptoIntervalPeriod.interval15m, 110m),
        ];

        CollectionAssert.AreEqual(new List<string> { "15m", "1m" },
            Names(PositionMonitor.OrderIntervalsForSignals(intervals, 100m)));
    }


    [TestMethod]
    public void InsideOneInterval_OnlyTheNearestRuleReorders()
    {
        CryptoInterval interval = GlobalData.IntervalList.First(x => x.IntervalPeriod == CryptoIntervalPeriod.interval5m);
        List<CryptoSignal> signals = [Signal(interval, 130m), Signal(interval, 101m), Signal(interval, 110m)];

        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.ShortestIntervalFirst;
        CollectionAssert.AreEqual(new List<decimal> { 130m, 101m, 110m },
            PositionMonitor.OrderSignals(signals, 100m).Select(s => s.SignalPrice).ToList(),
            "de standaard laat de volgorde staan waarin de signalen zijn toegevoegd");

        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.NearestEntryPrice;
        CollectionAssert.AreEqual(new List<decimal> { 101m, 110m, 130m },
            PositionMonitor.OrderSignals(signals, 100m).Select(s => s.SignalPrice).ToList());
    }


    /// <summary>
    /// A long signal above the price and a short signal below it are the same distance away; the rule
    /// measures the distance, not the direction, and equal distances keep the order they had.
    /// </summary>
    [TestMethod]
    public void NearestEntryPrice_MeasuresDistanceAndNotDirection()
    {
        GlobalData.Settings.Trading.SignalPriority = CryptoSignalPriority.NearestEntryPrice;
        CryptoInterval interval = GlobalData.IntervalList.First(x => x.IntervalPeriod == CryptoIntervalPeriod.interval5m);
        List<CryptoSignal> signals = [Signal(interval, 95m), Signal(interval, 105m), Signal(interval, 99m)];

        CollectionAssert.AreEqual(new List<decimal> { 99m, 95m, 105m },
            PositionMonitor.OrderSignals(signals, 100m).Select(s => s.SignalPrice).ToList());
    }
}

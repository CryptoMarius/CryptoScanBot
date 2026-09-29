using CryptoScanner.Analyzers.SrFlip;
using CryptoScanner.Analyzers.SrFlip.Signal;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.CoreTests.Analyzer.SrFlip;

/// <summary>
/// The srflip strategy (open point 46) signals on the candle that closes a flip, and on no other.
/// Checked on the SOLUSDT 1h candles of the emulator against the events of the Core scan over the
/// same candles.
/// </summary>
[DoNotParallelize]
[TestClass]
public class SrFlipSignalTests : TestBase
{
    private SrFlipSettings _saved = new();

    [TestInitialize]
    public void Save() => _saved = SrFlipPlugin.Settings;

    [TestCleanup]
    public void Restore() => new SrFlipPlugin().SettingsBase = _saved;

    private static (CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, List<CryptoCandle> candles) Load()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Zones", "Data", "SOLUSDT-1h.json"));
        return (symbol, interval, symbolInterval, [.. symbolInterval.CandleList.Values]);
    }

    private static SrFlipBase Algorithm(CryptoTradeSide side, CryptoSymbol symbol, CryptoInterval interval,
        CryptoSymbolInterval symbolInterval, CryptoCandle candle)
    {
        MyData data = new() { Candle = candle, CandleData = new CryptoData() };
        if (side == CryptoTradeSide.Long)
            return new SrFlipLong { Symbol = symbol, Interval = interval, SymbolInterval = symbolInterval, SignalSide = side, SignalStrategy = "srflip", CandleLast = data };
        return new SrFlipShort { Symbol = symbol, Interval = interval, SymbolInterval = symbolInterval, SignalSide = side, SignalStrategy = "srflip", CandleLast = data };
    }

    private static bool FlipOn(SupportResistanceResult scan, int index, CryptoTradeSide side, Func<SupportResistanceEvent, bool> which)
        => scan.Events.Any(e => e.Index == index && e.Side == side && e.Type == SupportResistanceEventType.Flip && which(e));


    [TestMethod]
    public void ItSignalsOnTheFlipsOfTheScanAndNowhereElse()
    {
        var (symbol, interval, symbolInterval, candles) = Load();
        // The whole history in the window, so the strategy sees exactly what one scan over the
        // candles sees (the scan is causal, so its events up to candle k do not depend on later ones).
        new SrFlipPlugin().SettingsBase = new SrFlipSettings { WaitForConfirmation = false, VolumeFactor = 0, HistoryCandles = candles.Count };
        SupportResistanceResult scan = SupportResistance.Scan(candles);

        // From candle 60 on: the strategy wants at least that much history before it scans at all
        var flips = scan.Events.Where(e => e.Type == SupportResistanceEventType.Flip && e.Index >= 60).ToList();
        Assert.IsTrue(flips.Count > 10, "the test data has flips");

        // A sample of the flips, and the candle right after each of them
        foreach (var flip in flips.Where((_, n) => n % 10 == 0))
        {
            var algorithm = Algorithm(flip.Side, symbol, interval, symbolInterval, candles[flip.Index]);
            Assert.IsTrue(algorithm.IsSignal(), $"flip at candle {flip.Index}: {algorithm.ExtraText}");
            Assert.IsTrue(algorithm.ExtraText.StartsWith("flip on"), algorithm.ExtraText);
            Assert.IsNotNull(algorithm.OverrideSlPercentage, "the stop beyond the level");
            Assert.AreEqual(algorithm.OverrideSlPercentage!.Value * 2m, algorithm.OverrideProfitPercentage!.Value, 0.02m,
                "take profit at twice the stop");

            int after = flip.Index + 1;
            if (after < candles.Count && !FlipOn(scan, after, flip.Side, _ => true))
            {
                var next = Algorithm(flip.Side, symbol, interval, symbolInterval, candles[after]);
                Assert.IsFalse(next.IsSignal(), $"no flip on candle {after}");
            }
        }
    }


    [TestMethod]
    public void TheSettingsSwitchKindsAndWeakLevelsOff()
    {
        var (symbol, interval, symbolInterval, candles) = Load();
        SupportResistanceResult scan = SupportResistance.Scan(candles);

        // A horizontal flip on a candle that has no sloped flip on the same side
        var horizontal = scan.Events.First(e => e.Type == SupportResistanceEventType.Flip && e.Kind == SupportResistanceKind.Horizontal && e.Index >= 60
            && !FlipOn(scan, e.Index, e.Side, x => x.Kind == SupportResistanceKind.Sloped));
        new SrFlipPlugin().SettingsBase = new SrFlipSettings { WaitForConfirmation = false, VolumeFactor = 0, HistoryCandles = candles.Count, UseHorizontal = false };
        Assert.IsFalse(Algorithm(horizontal.Side, symbol, interval, symbolInterval, candles[horizontal.Index]).IsSignal(),
            "horizontal switched off");

        new SrFlipPlugin().SettingsBase = new SrFlipSettings { WaitForConfirmation = false, VolumeFactor = 0, HistoryCandles = candles.Count, UseSloped = false,
            MinimumTouches = scan.Events.Where(e => e.Index == horizontal.Index && e.Side == horizontal.Side).Max(e => e.Touches) + 1 };
        Assert.IsFalse(Algorithm(horizontal.Side, symbol, interval, symbolInterval, candles[horizontal.Index]).IsSignal(),
            "a level with fewer touches than asked does not count");

        var sloped = scan.Events.First(e => e.Type == SupportResistanceEventType.Flip && e.Kind == SupportResistanceKind.Sloped && e.Index >= 60
            && !FlipOn(scan, e.Index, e.Side, x => x.Kind == SupportResistanceKind.Horizontal));
        new SrFlipPlugin().SettingsBase = new SrFlipSettings { WaitForConfirmation = false, VolumeFactor = 0, HistoryCandles = candles.Count, UseSloped = false };
        Assert.IsFalse(Algorithm(sloped.Side, symbol, interval, symbolInterval, candles[sloped.Index]).IsSignal(),
            "sloped switched off");
    }


    /// <summary>
    /// With WaitForConfirmation (29-09-2026) the signal comes on the first candle after the retest that
    /// closes in the trade direction and beyond the retest candle's high (long) or low (short) - never on
    /// the retest itself, and once per flip. Worked out here from the candles, independent of the strategy.
    /// </summary>
    [TestMethod]
    public void WithConfirmationItSignalsOnTheFirstCandleThatTurns()
    {
        var (symbol, interval, symbolInterval, candles) = Load();
        const int window = 3;
        new SrFlipPlugin().SettingsBase = new SrFlipSettings { HistoryCandles = candles.Count, WaitForConfirmation = true,
            ConfirmationCandles = window, VolumeFactor = 0 };
        SupportResistanceResult scan = SupportResistance.Scan(candles);

        int confirmed = 0, unconfirmed = 0;
        foreach (var flip in scan.Events.Where(e => e.Type == SupportResistanceEventType.Flip && e.Index >= 60 && e.Index + window < candles.Count))
        {
            int side = flip.Side == CryptoTradeSide.Long ? 1 : -1;
            double atr = Math.Abs(flip.Level - flip.Stop) / SupportResistance.Default.StopAtr;
            CryptoCandle retest = candles[flip.Index];
            int? expected = null;
            for (int j = flip.Index + 1; j <= flip.Index + window; j++)
            {
                CryptoCandle c = candles[j];
                if ((flip.Level - (double)c.Close) * side > SupportResistance.Default.RetestAtr * atr)
                    break;
                bool turns = side == 1 ? c.Close > c.Open : c.Close < c.Open;
                bool beyond = side == 1 ? c.Close > retest.High : c.Close < retest.Low;
                if (turns && beyond)
                {
                    expected = j;
                    break;
                }
            }

            // Another flip of the same side close by can confirm on the same candle; skip those
            if (scan.Events.Any(e => e.Type == SupportResistanceEventType.Flip && e.Side == flip.Side && e != flip
                && Math.Abs(e.Index - flip.Index) <= window))
                continue;

            Assert.IsFalse(Algorithm(flip.Side, symbol, interval, symbolInterval, retest).IsSignal(),
                $"no signal on the retest candle {flip.Index} itself");
            for (int j = flip.Index + 1; j <= flip.Index + window; j++)
            {
                var algorithm = Algorithm(flip.Side, symbol, interval, symbolInterval, candles[j]);
                Assert.AreEqual(j == expected, algorithm.IsSignal(), $"flip at {flip.Index}, candle {j}: {algorithm.ExtraText}");
            }
            if (expected != null)
                confirmed++;
            else
                unconfirmed++;
        }
        Assert.IsTrue(confirmed > 3 && unconfirmed > 3, $"both cases in the data ({confirmed} confirmed, {unconfirmed} not)");
    }


    [TestMethod]
    public void WithoutStopBeyondTheLevelTheTraderKeepsItsOwnExits()
    {
        var (symbol, interval, symbolInterval, candles) = Load();
        SupportResistanceResult scan = SupportResistance.Scan(candles);
        var flip = scan.Events.First(e => e.Type == SupportResistanceEventType.Flip && e.Index >= 60);

        new SrFlipPlugin().SettingsBase = new SrFlipSettings { WaitForConfirmation = false, VolumeFactor = 0, HistoryCandles = candles.Count, StopBeyondLevel = false };
        var algorithm = Algorithm(flip.Side, symbol, interval, symbolInterval, candles[flip.Index]);
        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        Assert.IsNull(algorithm.OverrideSlPercentage);
        Assert.IsNull(algorithm.OverrideProfitPercentage);
    }
}

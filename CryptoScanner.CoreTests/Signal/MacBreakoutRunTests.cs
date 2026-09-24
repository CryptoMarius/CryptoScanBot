using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Analyzers.Mac.Indicators;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The break read as a RUN: which candles inside a stretch beyond the level carry a mark, and which
/// stretches earn marks at all.
/// <para>
/// Both layers come from measurement against the reference indicator, so both are pinned here. The
/// counting layer is exact and can be asserted on a handful of candles; the stretch layer is a
/// BAND on how far the level stands from the slow line, which is asserted as a property - the
/// verdict on every run has to follow the band, on the run's first candle and nowhere else.
/// </para>
/// </summary>
[TestClass]
public class MacBreakoutRunTests
{
    private static readonly DateTime Epoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // The plugin settings are shared by every test in the run, so what this class puts there has to
    // be handed back. Leaving them behind broke fourteen other MAC tests that happened to run after
    // it - and only when the whole suite ran, never on their own.
    private MacSettings _before = new();

    [TestInitialize]
    public void Remember() => _before = MacPlugin.Settings;

    [TestCleanup]
    public void Restore() => new MacPlugin().SettingsBase = _before;


    /// <summary>
    /// Feeds a closing price series and hands back what the indicator published per candle. The
    /// candle is built around the close with a range of <paramref name="range"/>, so the average
    /// candle range the run test measures against is that same number.
    /// </summary>
    private static List<MacCandleData?> Publish(IReadOnlyList<decimal> closes, decimal range)
    {
        MacSettings settings = new() { UseRsiLevels = true };
        new MacPlugin().SettingsBase = settings;

        IndicatorRegistry registry = new(500);
        MacIndicatorExtension extension = new();
        extension.Init(registry);

        List<MacCandleData?> published = [];
        for (int i = 0; i < closes.Count; i++)
        {
            decimal close = closes[i];
            Quote quote = new(Epoch.AddMinutes(5 * i), close, close + range / 2m,
                close - range / 2m, close, 100m);
            registry.QuoteHub.Add(quote);
            extension.OnCandleAdded(quote);
            CryptoData data = new();
            extension.FillData(data);
            published.Add(data.GetPluginData<MacCandleData>());
        }
        return published;
    }


    /// <summary>
    /// A series built to leave a RESISTANCE behind and then climb through it.
    /// <para>
    /// The level is the high of the candle on which the strength index crosses back DOWN through
    /// its upper bound, so the shape has to be: quiet, then a rise that pushes the index over that
    /// bound, then a dip that brings it back under - which sets the level - and then a climb that
    /// carries the close past it and keeps going.
    /// </para>
    /// </summary>
    private static List<decimal> ClimbThroughAResistance(int settle, int rise, int dip, int climb,
        decimal step)
    {
        List<decimal> closes = [];
        decimal price = 1000m;
        for (int i = 0; i < settle; i++)
        {
            // A saw of one step up and one down keeps the averages alive without a trend.
            price += i % 2 == 0 ? step : -step;
            closes.Add(price);
        }
        for (int i = 0; i < rise; i++)
        {
            price += step * 3m;
            closes.Add(price);
        }
        for (int i = 0; i < dip; i++)
        {
            price -= step * 4m;
            closes.Add(price);
        }
        for (int i = 0; i < climb; i++)
        {
            price += step * 3m;
            closes.Add(price);
        }
        return closes;
    }


    [TestMethod]
    public void TheRankCountsNewHighsAndNothingElse()
    {
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 120, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 2m);

        // Every candle that carries a rank at all has to better every close before it inside its
        // own stretch, and the ranks of one stretch have to run 1, 2, 3, ... without a gap.
        int expected = 0;
        decimal reach = decimal.MinValue;
        bool inside = false;
        for (int i = 0; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            if (mac?.RsiLevelHigh == null)
            {
                inside = false;
                continue;
            }
            bool beyond = (double)closes[i] > mac.RsiLevelHigh.Value;
            if (!beyond)
            {
                inside = false;
                Assert.AreEqual(0, mac.BreakoutRank,
                    $"candle {i} is not beyond the level and may not carry a rank");
                continue;
            }
            if (!inside)
            {
                inside = true;
                expected = 0;
                reach = decimal.MinValue;
            }
            if (closes[i] > reach)
            {
                reach = closes[i];
                expected++;
                Assert.AreEqual(expected, mac.BreakoutRank,
                    $"candle {i} makes a new high of its stretch and should be number {expected}");
            }
            else
            {
                Assert.AreEqual(0, mac.BreakoutRank,
                    $"candle {i} does not better its stretch and may not carry a rank");
            }
        }
        Assert.IsTrue(expected > 3, "the series should hold a stretch of at least four new highs");
    }


    /// <summary>
    /// A settle and then several climbs of growing size, each leaving a resistance behind and then
    /// taking it out. The size of the climb is what moves the level away from the slow line, so a
    /// series like this puts runs on BOTH sides of the band and on it - which is what a test of a
    /// band needs and one climb cannot give.
    /// </summary>
    private static List<decimal> ClimbsOfDifferentSize(params decimal[] steps)
    {
        List<decimal> closes = [];
        decimal price = 1000m;
        for (int i = 0; i < 260; i++)
        {
            // A saw of one up and one down keeps the averages alive without a trend.
            price += i % 2 == 0 ? 1m : -1m;
            closes.Add(price);
        }
        foreach (decimal step in steps)
        {
            for (int i = 0; i < 20; i++)
            {
                price += step * 2m;
                closes.Add(price);
            }
            for (int i = 0; i < 6; i++)
            {
                price -= step * 3m;
                closes.Add(price);
            }
            for (int i = 0; i < 30; i++)
            {
                price += step * 2m;
                closes.Add(price);
            }
            for (int i = 0; i < 50; i++)
            {
                price -= step * 1.4m;
                closes.Add(price);
            }
        }
        return closes;
    }


    /// <summary>
    /// The verdict on a run has to follow the band, and it has to be taken on the run's FIRST
    /// candle. Both bounds are pinned: a level that hugs the trend earns nothing, and so does one
    /// that has been left far behind.
    /// </summary>
    [TestMethod]
    public void TheVerdictFollowsTheBandAroundTheTrend()
    {
        // MacIndicatorExtension.BreakLevelNear and BreakLevelFar. Private there on purpose, so the
        // numbers stand here as well - if one moves without the other, this test says so.
        const double near = 1.4;
        const double far = 5.35;
        const decimal range = 8m;

        List<decimal> closes = ClimbsOfDifferentSize(0.4m, 1m, 3m);
        List<MacCandleData?> published = Publish(closes, range);

        int allowed = 0;
        int refused = 0;
        List<string> measured = [];
        bool inside = false;
        bool verdict = false;
        for (int i = 0; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            double? level = mac?.RsiLevelHigh;
            if (level == null || mac?.SmaSlow == null || (double)closes[i] <= level.Value)
            {
                inside = false;
                continue;
            }
            if (!inside)
            {
                // The first candle of the run: every candle carries the same range, so the average
                // range the indicator divides by is that range itself.
                inside = true;
                double distance = (level.Value - mac.SmaSlow.Value) / (double)range;
                verdict = distance > near && distance < far;
                measured.Add($"{i}:{distance:0.00}");
                if (verdict)
                    allowed++;
                else
                    refused++;
            }
            Assert.AreEqual(verdict, mac.BreakoutRunAllowed,
                $"candle {i} belongs to a run whose level stands "
                + $"{(level.Value - mac.SmaSlow.Value) / (double)range:0.00} candle ranges from "
                + "the slow line, and the verdict of its first candle has to hold for all of it");
        }
        Assert.IsTrue(allowed > 0 && refused > 0,
            $"this series should hold runs on both sides of the band, not {allowed} and "
            + $"{refused} - {string.Join(" ", measured)}");
    }


    /// <summary>
    /// A level that sits right on top of the trend earns nothing. The candles are so big here that
    /// the whole climb is a fraction of one of them, which is the lower bound of the band.
    /// </summary>
    [TestMethod]
    public void ALevelThatHugsTheTrendEarnsNothing()
    {
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 120, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 200m);

        // A candle range of 200 against steps of 3 makes every distance a fraction of one range,
        // so no level can stand the one and a half ranges clear of the trend that the band asks.
        foreach (MacCandleData? mac in published)
        {
            if (mac == null)
                continue;
            Assert.IsFalse(mac.BreakoutRunAllowed,
                "no level in this series stands clear enough of the trend");
            Assert.IsFalse(mac.BreakdownRunAllowed,
                "and none does on the other side either");
        }
    }
}

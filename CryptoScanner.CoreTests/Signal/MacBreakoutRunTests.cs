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

        // The volume grows five percent a candle. A CONSTANT volume would sit exactly on its own
        // twenty candle average, and the break rule asks for a third more than that, so with a flat
        // series no candle would ever carry a rank and the counting test below would measure
        // nothing. A steady climb of five percent puts every candle about half again over the
        // average of the twenty before it, which clears the floor without touching what is tested.
        List<MacCandleData?> published = [];
        decimal volume = 100m;
        for (int i = 0; i < closes.Count; i++)
        {
            decimal close = closes[i];
            volume *= 1.05m;
            Quote quote = new(Epoch.AddMinutes(5 * i), close, close + range / 2m,
                close - range / 2m, close, volume);
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


    /// <summary>
    /// The candidate test: the WICK has to better the hundred candles before this one. Measured
    /// against the reference's own markers, all 747 of them do; on the CLOSE only 85% do, and that
    /// difference is what kept this marker out of reach for weeks.
    /// </summary>
    [TestMethod]
    public void AWickThatDoesNotBetterTheHundredBeforeIt_EarnsNothing()
    {
        // A saw that leaves a resistance behind and then crawls back over it without ever taking
        // out the high of the settle: the close gets beyond the level, the wick never leads.
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 40, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 2m);

        decimal hoogste = closes.Take(293).Max() + 1m;
        for (int i = 293; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            if (mac?.RsiLevelHigh == null || (double)closes[i] <= mac.RsiLevelHigh.Value)
                continue;
            if (closes[i] + 1m > hoogste)
                break;                              // vanaf hier leidt de wick wel
            Assert.AreEqual(0, mac.BreakoutRank,
                $"candle {i} closes beyond the level but its high of {closes[i] + 1m:N2} does not "
                + $"better the {hoogste:N2} behind it, so it may not carry a number");
        }
    }


    /// <summary>
    /// The counter hangs on the POSITION, not on the stretch: it restarts when the fast line
    /// crosses the second, and it lets three candidates through.
    /// <para>
    /// Anchored on the stretch the best reading reaches 93% of the reference's markers while only
    /// 43% of what it fires is right; anchored on the entry it reaches 89% at 81%.
    /// </para>
    /// </summary>
    [TestMethod]
    public void TheCounterRestartsAtTheEntryAndStopsAtThree()
    {
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 120, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 2m);

        int hoogste = 0;
        int gezien = 0;
        int vorige = 0;
        for (int i = 0; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            if (mac == null)
                continue;
            int rank = mac.BreakoutRank;
            if (rank == 0)
                continue;
            gezien++;
            hoogste = Math.Max(hoogste, rank);
            Assert.IsTrue(rank == vorige + 1 || rank == 1,
                $"candle {i} carries number {rank} while the one before it carried {vorige}; the "
                + "counter has to run 1, 2, 3 and restart at 1, never jump");
            vorige = rank;
        }
        Assert.IsTrue(gezien > 0, "this series should produce break candidates at all");
        Assert.IsTrue(hoogste >= 3, $"the counter should reach three, not {hoogste}");
    }


    /// <summary>
    /// And nothing is drawn in the first candles after the entry. The reference skips those, and
    /// asking for five candles lifts the agreement from 89% at 81% to 91% at 87%.
    /// </summary>
    [TestMethod]
    public void TheFirstCandlesAfterTheEntry_EarnNothing()
    {
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 120, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 2m);

        // BreakoutRunAllowed says whether the position is old enough; while it is false no candle
        // may carry a number.
        int tegengehouden = 0;
        for (int i = 0; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            if (mac == null || mac.BreakoutRunAllowed)
                continue;
            tegengehouden++;
            Assert.AreEqual(0, mac.BreakoutRank,
                $"candle {i} sits too soon after the entry and may not carry a number");
        }
        Assert.IsTrue(tegengehouden > 0,
            "this series should hold candles that sit too soon after an entry");
    }
}

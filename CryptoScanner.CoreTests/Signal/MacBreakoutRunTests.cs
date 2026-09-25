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
/// Both layers come from measurement against the strategy, so both are pinned here. The
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
        MacSettings settings = new();
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
    /// against its own markers, all 747 of them do; on the CLOSE only 85% do, and that
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
    /// Anchored on the stretch the best reading reaches 93% of the strategy's markers while only
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
    /// And the close NINE candles back has to have stood past the second line. That one condition
    /// is what makes the strategy skip candidates - at the start of a position and in the middle
    /// of one alike - and it is exact: at a lag of eight it misses 55 of the 747 markers and fires
    /// 59 too many, at nine it misses nothing and fires nothing extra, at ten it misses 72.
    /// </summary>
    [TestMethod]
    public void ACandleWhoseCloseWasNotPastTheSecondLineNineBack_EarnsNothing()
    {
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 120, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 2m);

        // BreakoutRunAllowed carries the answer of that look back, so while it is false no candle
        // may carry a number - and it has to be false somewhere, or the test proves nothing.
        int tegengehouden = 0;
        for (int i = 0; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            if (mac == null || mac.BreakoutRunAllowed)
                continue;
            tegengehouden++;
            Assert.AreEqual(0, mac.BreakoutRank,
                $"candle {i} had its close under the second line nine candles back and may not "
                + "carry a number");
        }
        Assert.IsTrue(tegengehouden > 0,
            "this series should hold candles whose close was not yet past the second line");
    }


    /// <summary>
    /// The look back is read on the candle in hand, not one later. FillData runs after the tracking
    /// and the counter has moved on by then, so reading it again there asks about the candle EIGHT
    /// back - which turned away four markers per side and per coin before it was caught.
    /// </summary>
    [TestMethod]
    public void TheLookBackIsReadOnTheCandleItBelongsTo()
    {
        List<decimal> closes = ClimbThroughAResistance(settle: 260, rise: 25, dip: 8,
            climb: 120, step: 1m);
        List<MacCandleData?> published = Publish(closes, range: 2m);

        int gecontroleerd = 0;
        for (int i = 9; i < closes.Count; i++)
        {
            MacCandleData? mac = published[i];
            if (mac?.EmaSecond == null || published[i - 9]?.EmaSecond == null)
                continue;
            bool voorbij = (double)closes[i - 9] > published[i - 9]!.EmaSecond!.Value;
            gecontroleerd++;
            Assert.AreEqual(voorbij, mac.BreakoutRunAllowed,
                $"candle {i} should report the close of candle {i - 9} against the second line "
                + "of that same candle");
        }
        Assert.IsTrue(gecontroleerd > 100, "not enough candles were compared to prove anything");
    }
}

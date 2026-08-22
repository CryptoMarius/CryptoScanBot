using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;
using CryptoScanner.Core.Zones;

using System.Text;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// The DLZ zones are calculated incrementally: a cached ZigZag survives between calls, and
/// <see cref="CryptoSymbolIntervalData.Dlz.ProcessedCandleMarker"/> is the cursor that says which pivots
/// were already turned into zones. Every host relies on this - the Avalonia scanner, the Photino
/// scanner and the emulator all run the same Core path - so the property that has to hold is:
///
///     the zones you get do not depend on how often the caller happened to ask.
///
/// That is the same property <see cref="Trend.ZigZagIncrementalTests"/> pins one level lower for
/// the ZigZag itself. It holding there does not make it hold here: DLZ adds a second layer of state
/// on top (the Dominant flag on a pivot, and the cursor deciding which pivots are even looked at),
/// and a dominant pivot is only recognised when the NEXT pivot arrives - so a pivot can be confirmed
/// in a later call than the one it appeared in.
///
/// The zoom is switched off (ZoneDlzTests.ConfigureSettingsForTest), so these tests exercise the
/// pivot-to-zone logic without needing lower-interval candles or a candle database.
///
/// STATUS 2026-08-21: green. The route there, measured against 162 zones from one full calculation
/// on 3315 ETHUSDT 1h candles:
///
///     candles per call    time cursor   after skip fix   settled/provisional
///     one                           0              228                   162
///     five                          4              167                   162
///     fifteen                      40              146                   162
///     hundred                     130              156                   162
///
/// The first column is a cursor in candle time: at one candle per call - the rhythm the emulator
/// runs at - it produced literally nothing. Fixing the skip to test the CONFIRMING pivot instead of
/// the candidate removed that systematic loss but left both remainders, because a cursor in time
/// cannot express "this triple has been judged": the pivot list stays mutable at its right edge, so
/// the pivot that confirms a triple today need not be the one that confirms it tomorrow.
///
/// What closed it is splitting the list in two at ZigZagIndicator.SettledCount. Before that boundary
/// nothing can change any more, so a verdict is final and is recorded once; after it every verdict is
/// rebuilt from scratch on each call and REPLACES the previous one instead of adding to it. Their
/// union is what a full calculation sees. The boundary is not a guess - MeasureHowFarBackAChangeReaches
/// below measures the deepest a change ever reaches back (2 pivots on this data) and asserts a
/// ceiling of 25, which is the constant the design is built against.
///
/// The intro grading moved into the same walk for the same reason. It used to be a second pass with
/// a cursor of its own, and that one graded the DOMINANT pivot - always older than the confirmer that
/// made it dominant - so it skipped exactly the pivots it was meant to grade and left Strength on
/// None. ZonesIncludingStrengthDoNotDependOnHowOftenWeAsk is what holds that closed; the zone-index
/// merge cannot, because it does not key on Strength.
/// </summary>
[TestClass]
public class ZoneDlzIncrementalTests : TestBase
{
    /// <summary>
    /// One zone, reduced to the fields that decide whether two runs agree.
    /// <para>
    /// Strength is in here even though the zone-index merge does NOT key on it
    /// (ZoneTools.CreateZoneIndex uses side/openTime/top/bottom). That is the point: a zone that
    /// comes out Weak in one rhythm and Strong in another would slip through the merge unnoticed
    /// and reach the strategies as a different zone. With ZoneStartApply off it is None everywhere
    /// and this field costs nothing; ZonesIncludingStrengthDoNotDependOnHowOftenWeAsk switches the
    /// grading on so it actually has something to say.
    /// </para>
    /// </summary>
    private readonly record struct ZoneKey(CryptoTradeSide Side, long OpenTime, decimal Top,
        decimal Bottom, CryptoZoneStrength Strength)
    {
        public static ZoneKey Of(CryptoZone zone) =>
            new(zone.Side, zone.OpenTime.Minutes, zone.Top, zone.Bottom, zone.Strength);

        public override string ToString() =>
            $"{Side} {new CandleTime((uint)OpenTime).ToDateTime():yyyy-MM-dd HH:mm} {Top}..{Bottom} {Strength}";
    }


    /// <summary>
    /// Symbol, interval and candles in one go, deliberately. CreateTestSymbol calls ClearCandles,
    /// so asking for the symbol a second time wipes the candles that were just loaded.
    /// </summary>
    private static (CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candles) LoadScenario()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("Error assembly");
        LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Trend\\ETHUSDT\\ETHUSDT-1h.json"));

        return (symbol, interval, symbolInterval.CandleList);
    }


    /// <summary>
    /// Runs the replay the way the production code does: feed the candles that arrived since the
    /// previous call into the cached ZigZag, then mark the newly dominant pivots and turn them into
    /// zones, both bounded by the cursor. blockSize 0 means one single call over everything, which
    /// is the "full calculation" reference.
    /// <para>
    /// Mirrors ZoneDlz.CalculateZonesAsync's two branches (ZoneDlz.cs around line 700-780) minus the
    /// database merge - the merge reconciles the result, it does not produce it, so leaving it out
    /// is what makes a difference in the CALCULATION visible instead of hidden by the diff.
    /// </para>
    /// </summary>
    private static async Task<List<CryptoZone>> Replay(CryptoSymbol symbol, CryptoInterval interval,
        CryptoCandleList candles, int blockSize,
        SortedList<CryptoIntervalPeriod, bool>? alreadyLoaded = null)
    {
        ZigZagIndicator indicator = new(TrendType.Primary, false);
        // Marking an interval as loaded is what keeps the zoom from going to disk or to the exchange
        // for it: ZoneCandleEngine.FetchFrom skips the read for anything already in this map.
        SortedList<CryptoIntervalPeriod, bool> loaded = alreadyLoaded == null
            ? [] : new SortedList<CryptoIntervalPeriod, bool>(alreadyLoaded);

        // The reference: everything in one go, straight down the full branch of CalculateZonesAsync.
        // Kept as its own path on purpose - the moment the reference runs through the incremental
        // machinery it stops being an independent answer to compare against.
        if (blockSize == 0)
        {
            foreach (CryptoCandle candle in candles.Values)
            {
                indicator.Calculate(candle, batchProcess: true);
                indicator.LastFedCandleTime = candle.OpenTime;
            }
            indicator.FinishBatch();
            await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

            List<CryptoZone> everything = [];
            ZoneDlz.CreateZonesFromZigZag(symbol, interval, indicator.ZigZagList, everything);
            return everything;
        }

        // The incremental design, in two areas. Settled verdicts are collected once and never
        // recomputed; the mutable tail is thrown away and rebuilt on every settle, because a
        // verdict there is still allowed to change. Their union is what a full calculation sees.
        List<CryptoZone> committed = [];
        List<CryptoZone> provisional = [];
        CandleTime? cursor = null;
        int inBlock = 0;

        async Task Settle()
        {
            indicator.FinishBatch();
            List<ZigZagResult> settledPivots = [];
            List<ZigZagResult> provisionalPivots = [];
            cursor = await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded,
                cursor, settledPivots, provisionalPivots);

            ZoneDlz.CreateZonesFromZigZag(symbol, interval, settledPivots, committed);

            // REPLACED, not appended. Appending is what produced the duplicates: a tail pivot judged
            // on one call and again on the next left two copies of the same zone behind.
            provisional = [];
            ZoneDlz.CreateZonesFromZigZag(symbol, interval, provisionalPivots, provisional);
        }

        foreach (CryptoCandle candle in candles.Values)
        {
            indicator.Calculate(candle, batchProcess: true);
            indicator.LastFedCandleTime = candle.OpenTime;
            if (++inBlock >= blockSize)
            {
                await Settle();
                inBlock = 0;
            }
        }
        if (inBlock > 0)
            await Settle();

        return [.. committed, .. provisional];
    }


    /// <summary>
    /// A short, readable account of where two zone sets differ. Dumping both lists in full makes a
    /// failure unreadable at 160 zones, and the interesting part is which ones went missing.
    /// </summary>
    private static string CompareZones(List<CryptoZone> full, List<CryptoZone> chunked, int blockSize)
    {
        List<ZoneKey> expected = [.. full.Select(ZoneKey.Of).OrderBy(k => k.OpenTime)];
        List<ZoneKey> actual = [.. chunked.Select(ZoneKey.Of).OrderBy(k => k.OpenTime)];
        if (expected.SequenceEqual(actual))
            return "";

        HashSet<ZoneKey> expectedSet = [.. expected];
        HashSet<ZoneKey> actualSet = [.. actual];
        List<ZoneKey> missing = [.. expected.Where(k => !actualSet.Contains(k))];
        List<ZoneKey> extra = [.. actual.Where(k => !expectedSet.Contains(k))];

        StringBuilder builder = new();
        int duplicates = actual.Count - actualSet.Count;
        builder.AppendLine($"blockSize {blockSize}: {actual.Count} zones, full calculation gives {expected.Count}.");
        builder.AppendLine($"missing {missing.Count}, unexpected {extra.Count}, duplicates {duplicates}.");
        foreach (ZoneKey key in missing.Take(5))
            builder.AppendLine($"  missing : {key}");
        foreach (ZoneKey key in extra.Take(5))
            builder.AppendLine($"  extra   : {key}");
        return builder.ToString();
    }


    /// <summary>
    /// The property every host depends on: asking once over the whole history and asking in small
    /// steps have to give the same zones. A difference here means the scanner and the emulator
    /// disagree about the same candles purely because of their calling rhythm - and the emulator
    /// calls once per candle while the live scanner calls once an hour.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(15)]
    [DataRow(100)]
    public async Task ZonesDoNotDependOnHowOftenWeAsk(int blockSize)
    {
        var (symbol, interval, candles) = LoadScenario();

        List<CryptoZone> full = await Replay(symbol, interval, candles, 0);
        List<CryptoZone> chunked = await Replay(symbol, interval, candles, blockSize);

        string difference = CompareZones(full, chunked, blockSize);
        Console.WriteLine(difference == "" ? $"blockSize {blockSize}: identical ({full.Count} zones)" : difference);
        Assert.AreEqual("", difference);
    }


    /// <summary>
    /// The same property with the intro grading switched on, so Strength is actually decided instead
    /// of staying None for every zone.
    /// <para>
    /// Worth its own test because the grading is the one part the zone-index merge cannot repair. The
    /// merge keys on side/openTime/top/bottom (ZoneTools.CreateZoneIndex), so a zone that comes out
    /// Weak in one rhythm and Strong in another matches its old self, keeps the old Strength, and
    /// reaches the strategies as something the full calculation never produced - silently.
    /// </para>
    /// <para>
    /// This is what made the grading move into CalculateDlzAsync. It used to be a second walk with a
    /// cursor of its own, and that cursor graded the DOMINANT pivot - which is always older than the
    /// confirmer that made it dominant, so it skipped exactly the pivots it was meant to grade.
    /// </para>
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(15)]
    [DataRow(100)]
    public async Task ZonesIncludingStrengthDoNotDependOnHowOftenWeAsk(int blockSize)
    {
        var (symbol, interval, candles) = LoadScenario();
        GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply = true;
        try
        {
            List<CryptoZone> full = await Replay(symbol, interval, candles, 0);
            List<CryptoZone> chunked = await Replay(symbol, interval, candles, blockSize);

            // If nothing is graded the test proves nothing, so say so instead of passing quietly.
            int graded = full.Count(z => z.Strength != CryptoZoneStrength.None);
            Assert.IsTrue(graded > 0,
                "no zone was graded at all - ZoneStartApply did not take effect, so this test is empty");

            string difference = CompareZones(full, chunked, blockSize);
            Console.WriteLine(difference == ""
                ? $"blockSize {blockSize}: identical ({full.Count} zones, {graded} graded)"
                : difference);
            Assert.AreEqual("", difference);
        }
        finally
        {
            GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply = false;
        }
    }


    /// <summary>
    /// One pass over everything has to leave BOTH areas filled: the committed store and the tail
    /// together have to be the whole answer, with nothing counted twice.
    /// <para>
    /// This pins the hand-over between the two branches of CalculateZonesAsync, which is where the
    /// design is easiest to get wrong and where no other test looks. The incremental branch submits
    /// its committed store plus the tail to the merge and reconciles with DeleteRemainingZones - so
    /// if a full scan finishes with an empty store, the very first incremental pass afterwards
    /// submits only the tail and the reconciliation deletes every zone the full scan just made. That
    /// is exactly what happened until the full branch started seeding the store as well.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task OnePassFillsBothTheStoreAndTheTail()
    {
        var (symbol, interval, candles) = LoadScenario();

        ZigZagIndicator indicator = new(TrendType.Primary, false);
        SortedList<CryptoIntervalPeriod, bool> loaded = [];
        foreach (CryptoCandle candle in candles.Values)
        {
            indicator.Calculate(candle, batchProcess: true);
            indicator.LastFedCandleTime = candle.OpenTime;
        }
        indicator.FinishBatch();

        List<ZigZagResult> settledPivots = [];
        List<ZigZagResult> provisionalPivots = [];
        CandleTime? cursor = await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator,
            loaded, null, settledPivots, provisionalPivots);

        List<CryptoZone> committed = [];
        List<CryptoZone> provisional = [];
        ZoneDlz.CreateZonesFromZigZag(symbol, interval, settledPivots, committed);
        ZoneDlz.CreateZonesFromZigZag(symbol, interval, provisionalPivots, provisional);

        List<CryptoZone> reference = await Replay(symbol, interval, candles, 0);

        Assert.IsTrue(committed.Count > 0,
            "the committed store came out empty - an incremental pass after this would have its " +
            "zones deleted by the reconciliation");
        Assert.IsNotNull(cursor, "no cursor came back, so the next pass would rejudge everything");

        List<ZoneKey> expected = [.. reference.Select(ZoneKey.Of).OrderBy(k => k.OpenTime)];
        List<ZoneKey> together = [.. committed.Concat(provisional).Select(ZoneKey.Of)
            .OrderBy(k => k.OpenTime)];
        Assert.AreEqual("", CompareZones(reference, [.. committed, .. provisional], 0));
        CollectionAssert.AreEqual(expected, together, "store plus tail is not the whole answer");

        Console.WriteLine($"committed {committed.Count}, tail {provisional.Count}, " +
                          $"together {together.Count}, full calculation {expected.Count}");
    }


    /// <summary>
    /// ADAUSDT with every interval below the working one in memory, so the zoom has something to
    /// zoom into. Returns the map that tells ZoneCandleEngine those intervals are already available.
    /// </summary>
    private static (CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candles,
        SortedList<CryptoIntervalPeriod, bool> loaded) LoadZoomScenario()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("Error assembly");

        // The working interval plus everything the zoom can step down into. MakeDominantAndZoomInAsync
        // walks the periods downwards one at a time until the zone is narrow enough, so a gap in this
        // list would silently end the zoom early instead of failing.
        (CryptoIntervalPeriod period, string file)[] wanted =
        [
            (CryptoIntervalPeriod.interval1h, "1h"),
            (CryptoIntervalPeriod.interval30m, "30m"),
            (CryptoIntervalPeriod.interval15m, "15m"),
            (CryptoIntervalPeriod.interval10m, "10m"),
            (CryptoIntervalPeriod.interval5m, "5m"),
            (CryptoIntervalPeriod.interval3m, "3m"),
            (CryptoIntervalPeriod.interval2m, "2m"),
            (CryptoIntervalPeriod.interval1m, "1m"),
        ];

        SortedList<CryptoIntervalPeriod, bool> loaded = [];
        foreach ((CryptoIntervalPeriod period, string file) in wanted)
        {
            LoadCandleDataFromDisk(symbol.GetSymbolInterval(period).CandleList,
                Path.Combine(path, $"Analyzer\\Bbma\\ADAUSDT\\ADAUSDT-{file}.json"));
            loaded[period] = true;
        }

        return (symbol, interval, symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList, loaded);
    }


    /// <summary>
    /// The same property with the zoom switched ON, which is how the scanner actually runs
    /// (ZoomLowerTimeFrames defaults to true, 0.2 to 0.7 percent).
    /// <para>
    /// This is a different path, not a variation on the same one. MakeDominantAndZoomInAsync walks
    /// down the lower intervals and narrows Top/Bottom against their candles, so the zone geometry
    /// itself - the thing the merge keys on - now depends on data the pivot logic never touches. If
    /// the zoom were to see a different amount of history depending on when it was asked, two runs
    /// would disagree about the same zone while both looked perfectly reasonable.
    /// </para>
    /// <para>
    /// Emulator mode is switched on for the duration, and not to simulate the emulator: it is the
    /// documented way to tell ZoneCandleEngine.FetchFrom to work with what is local and never call an
    /// exchange. Without it this test would depend on the network.
    /// </para>
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(15)]
    [DataRow(100)]
    public async Task ZonesWithZoomDoNotDependOnHowOftenWeAsk(int blockSize)
    {
        var (symbol, interval, candles, loaded) = LoadZoomScenario();

        var dlz = GlobalData.Settings.Signal.ZonesDlz;
        bool emulatorMode = GlobalData.IsEmulatorMode;
        int? runId = GlobalData.CurrentEmulatorRunId;
        try
        {
            GlobalData.IsEmulatorMode = true;
            GlobalData.CurrentEmulatorRunId = 1;

            dlz.ZoomLowerTimeFrames = true;
            dlz.MinimumZoomedPercentage = 0.2;
            dlz.MaximumZoomedPercentage = 0.7;

            List<CryptoZone> full = await Replay(symbol, interval, candles, 0, loaded);
            List<CryptoZone> chunked = await Replay(symbol, interval, candles, blockSize, loaded);

            // Without this the test would pass on an empty answer and prove nothing.
            Assert.IsTrue(full.Count > 0, "the full calculation produced no zones at all");

            // And this proves the zoom actually ran. A zoomed zone is narrower than the pivot candle
            // it started from, so switching the zoom off has to give a different answer. If it does
            // not, the percentages never crossed MaximumZoomedPercentage on this data and the test
            // would be measuring the plain path twice under a different name.
            dlz.ZoomLowerTimeFrames = false;
            List<CryptoZone> withoutZoom = await Replay(symbol, interval, candles, 0, loaded);
            dlz.ZoomLowerTimeFrames = true;

            int changedByZoom = full.Select(ZoneKey.Of).Except(withoutZoom.Select(ZoneKey.Of)).Count();
            Assert.IsTrue(changedByZoom > 0,
                "the zoom changed nothing, so this test never exercised the zoom path");
            Console.WriteLine($"zoom changed {changedByZoom} of {full.Count} zones " +
                              $"(without zoom: {withoutZoom.Count})");

            string difference = CompareZones(full, chunked, blockSize);
            Console.WriteLine(difference == ""
                ? $"blockSize {blockSize}: identical ({full.Count} zones, zoom on)"
                : difference);
            Assert.AreEqual("", difference);
        }
        finally
        {
            GlobalData.IsEmulatorMode = emulatorMode;
            GlobalData.CurrentEmulatorRunId = runId;
            dlz.ZoomLowerTimeFrames = false;
            dlz.MinimumZoomedPercentage = 0;
            dlz.MaximumZoomedPercentage = 0;
        }
    }


    /// <summary>
    /// How far back does a new candle still change the answer?
    /// <para>
    /// This is the number the incremental design hangs on. If a change never reaches further back
    /// than a bounded number of pivots, everything before that boundary can be written once and
    /// never revisited - no index over every zone on every recalculation, no re-zooming a pivot that
    /// was already zoomed. If it is unbounded, that design is not available and the reconciliation
    /// has to stay.
    /// </para>
    /// The test does not assert a bound it cannot know in advance; it measures the reach and fails
    /// only if a change reaches back further than the ZigZag's own optimise window can explain,
    /// which is what would make the boundary undefinable.
    /// </summary>
    [TestMethod]
    public async Task MeasureHowFarBackAChangeReaches()
    {
        var (symbol, interval, candles) = LoadScenario();

        ZigZagIndicator indicator = new(TrendType.Primary, false);
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        List<ZoneKey> previous = [];
        int worstZoneReach = 0;
        int worstPivotReach = 0;
        int changes = 0;
        string worstMoment = "";

        foreach (CryptoCandle candle in candles.Values)
        {
            indicator.Calculate(candle, batchProcess: true);
            indicator.FinishBatch();

            // Full recalculation every step: this measures what the ANSWER does, independent of any
            // cursor. A cursor that skips work can only be correct if the answer itself is stable.
            foreach (ZigZagResult pivot in indicator.ZigZagList)
                pivot.Dominant = false;
            await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

            List<CryptoZone> rebuilt = [];
            ZoneDlz.CreateZonesFromZigZag(symbol, interval, indicator.ZigZagList, rebuilt);
            List<ZoneKey> current = [.. rebuilt.Select(ZoneKey.Of)];

            // First position where the two lists differ, counted back from the end.
            int common = 0;
            while (common < current.Count && common < previous.Count
                   && current[common].Equals(previous[common]))
                common++;

            if (common < previous.Count)
            {
                changes++;
                int zoneReach = previous.Count - common;
                if (zoneReach > worstZoneReach)
                {
                    worstZoneReach = zoneReach;
                    worstMoment = $"{candle.OpenTime.ToDateTime():yyyy-MM-dd HH:mm}, " +
                                  $"{previous.Count} zones, changed from index {common}";
                }

                // Same distance expressed in pivots, which is the unit the optimise window works in.
                CandleTime changedFrom = new((uint)previous[common].OpenTime);
                int pivotIndex = indicator.ZigZagList.FindIndex(p => p.Candle.OpenTime >= changedFrom);
                if (pivotIndex >= 0)
                    worstPivotReach = Math.Max(worstPivotReach, indicator.ZigZagList.Count - pivotIndex);
            }

            previous = current;
        }

        Console.WriteLine($"candles          : {candles.Count}");
        Console.WriteLine($"pivots at the end: {indicator.ZigZagList.Count}");
        Console.WriteLine($"zones at the end : {previous.Count}");
        Console.WriteLine($"steps that changed an existing zone: {changes}");
        Console.WriteLine($"deepest reach back, in zones : {worstZoneReach}");
        Console.WriteLine($"deepest reach back, in pivots: {worstPivotReach}");
        Console.WriteLine($"deepest moment   : {worstMoment}");

        // Measured on this data: 6 of 3315 steps changed an existing zone, and the deepest reach
        // was 1 zone / 2 pivots. The theoretical worst case is wider - OptimizeList starts at
        // ZigZagList.Count - 10 and reads two more positions back, so 12 pivots - and
        // RecalculateSwingLowAndHigh walks back to the last low AND the last high, a few more on an
        // alternating list. 25 is that window with room to spare: below it a committed cursor is
        // definable, above it something rewrites history far deeper than the optimise window and
        // the whole idea is off the table. The assert guards the ceiling; the printed numbers are
        // what you actually design against.
        Assert.IsTrue(worstPivotReach <= 25,
            $"a change reached {worstPivotReach} pivots back, far beyond the optimise window - " +
            "a committed cursor cannot be defined against a moving target");
    }
}

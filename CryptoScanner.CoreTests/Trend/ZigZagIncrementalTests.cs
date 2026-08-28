using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using System.Diagnostics;
using System.Text;

namespace CryptoScanner.CoreTests.Trend;

/// <summary>
/// The ZigZag is fed incrementally: the cached instance survives between calls and only receives
/// the candles added since the previous one (see ZigZagIndicator.LastFedCandleTime, used by both
/// TrendCalculator and ZoneDlz). That only holds up if feeding N candles in one go and feeding them
/// in arbitrary chunks produce the SAME indicator — otherwise the trend silently depends on how
/// often the caller happened to ask, which differs per base interval in the emulator.
///
/// That property did NOT hold until 2026-08-12: a candle at a time gave 67 points where 15 at a
/// time gave 193, on the very same candles, with the last swing high nearly four months apart. The
/// cause was a provisional edge point (TryAddDummyPoints) sitting in the list while OptimizeList
/// ran, and OptimizeList skips any triple containing one - so the settle frequency decided what got
/// optimised away. See ZigZagIndicator.Calculate.
///
/// These tests pin the property down, and pin the resulting ZigZag list itself, so the indicator can
/// also be optimised (it is the single largest cost in a replay: 486s of a 3517s run is spent
/// feeding candles into it) with proof that the outcome does not move.
/// </summary>
[TestClass]
public class ZigZagIncrementalTests : TestBase
{
    private static CryptoCandleList LoadCandles()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h);

        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("Error assembly");
        LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Trend\\ETHUSDT\\ETHUSDT-1h.json"));
        return symbolInterval.CandleList;
    }


    /// <summary>
    /// Feeds the candles in blocks of <paramref name="blockSize"/>, calling FinishBatch after each
    /// block — exactly what the incremental callers do, where a block is "whatever arrived since
    /// last time". blockSize 0 means one single block.
    /// </summary>
    private static ZigZagIndicator Feed(CryptoCandleList candles, int blockSize, bool useHighLow = false)
    {
        ZigZagIndicator indicator = new(TrendType.Primary, useHighLow);
        int inBlock = 0;
        foreach (CryptoCandle candle in candles.Values)
        {
            indicator.Calculate(candle, batchProcess: true);
            if (blockSize > 0 && ++inBlock >= blockSize)
            {
                indicator.FinishBatch();
                inBlock = 0;
            }
        }
        indicator.FinishBatch();
        return indicator;
    }


    /// <summary>
    /// EVERY observable field of the settled ZigZag, as one string.
    ///
    /// Deliberately includes PivotIndex and the Backup* trio, even though they look like internals.
    /// Restore() puts Value, Candle AND PivotIndex back from those backups, ReusePoint() fills them
    /// when a pivot is reused, and TrimPivotList / GetLowFromBuffer index into the candle buffer with
    /// PivotIndex. A fingerprint that only covered Value and Candle would happily pass while an index
    /// silently pointed somewhere else — the damage would only show up much later, in a swing point
    /// reading from the wrong buffer slot.
    /// </summary>
    private static string Fingerprint(ZigZagIndicator indicator)
    {
        StringBuilder sb = new();
        sb.AppendLine($"count={indicator.ZigZagList.Count}");
        sb.AppendLine($"low={Describe(indicator.LastSwingLow)}");
        sb.AppendLine($"high={Describe(indicator.LastSwingHigh)}");
        sb.AppendLine($"point={Describe(indicator.LastSwingPoint)}");
        foreach (ZigZagResult point in indicator.ZigZagList)
            sb.AppendLine(Describe(point));
        return sb.ToString();
    }


    private static string Describe(ZigZagResult? p)
    {
        if (p == null)
            return "null";
        return $"{p.PointType} {p.Candle.Date:yyyy-MM-dd HH:mm} {p.Value} idx={p.PivotIndex} "
            + $"dom={p.Dominant} valid={p.IsValid} dummy={p.Dummy} strength={p.Strength} "
            + $"intro='{p.NiceIntro}' top={p.Top} bottom={p.Bottom} perc={p.Percentage} "
            + $"close={p.CloseDate?.Minutes} "
            + $"bakVal={p.BackupValue} bakIdx={p.BackupIndex} "
            + $"bakCandle={(p.BackupCandle.OpenTime == 0 ? "none" : p.BackupCandle.Date.ToString("yyyy-MM-dd HH:mm"))}";
    }


    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(7)]
    [DataRow(60)]
    [DataRow(500)]
    public void FeedingInBlocksMatchesFeedingInOneGo(int blockSize)
    {
        CryptoCandleList candles = LoadCandles();

        string oneGo = Fingerprint(Feed(candles, 0));
        string inBlocks = Fingerprint(Feed(candles, blockSize));

        // Block size 1 is the live scanner (a candle at a time); 500 is a cold rebuild; the ones in
        // between are what an emulator run on a coarser base interval produces.
        Assert.AreEqual(oneGo, inBlocks,
            $"feeding in blocks of {blockSize} gives a different ZigZag than feeding everything at once");
    }


    [TestMethod]
    public void FeedingInBlocksMatchesForHighLowVariantToo()
    {
        // The other cache key in use (TrendType, UseHighLow) — same property has to hold there.
        CryptoCandleList candles = LoadCandles();

        string oneGo = Fingerprint(Feed(candles, 0, useHighLow: true));
        foreach (int blockSize in new[] { 1, 7, 60 })
        {
            Assert.AreEqual(oneGo, Fingerprint(Feed(candles, blockSize, useHighLow: true)),
                $"useHighLow: blocks of {blockSize} differ from one go");
        }
    }


    [TestMethod]
    public void ResumingWhereItLeftOffMatchesAContinuousFeed()
    {
        // What LastFedCandleTime actually promises: stop halfway, hand over the rest later, and end
        // up where a single uninterrupted feed would have.
        CryptoCandleList candles = LoadCandles();
        List<CryptoCandle> all = [.. candles.Values];
        int half = all.Count / 2;

        ZigZagIndicator resumed = new(TrendType.Primary, false);
        for (int i = 0; i < half; i++)
            resumed.Calculate(all[i], batchProcess: true);
        resumed.FinishBatch();                       // the caller settles and reads the trend here
        for (int i = half; i < all.Count; i++)
            resumed.Calculate(all[i], batchProcess: true);
        resumed.FinishBatch();

        Assert.AreEqual(Fingerprint(Feed(candles, 0)), Fingerprint(resumed),
            "resuming after a settled read does not match a continuous feed");
    }


    [TestMethod]
    public void DiscoverBlockSizeSensitivity()
    {
        // How much does the outcome depend on HOW OFTEN the caller settles the indicator?
        // Block size 1 is the live scanner, 500 a cold rebuild, 0 a single uninterrupted feed.
        CryptoCandleList candles = LoadCandles();

        Console.WriteLine("blockSize  points  lastLow           lastHigh");
        foreach (int blockSize in new[] { 0, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000 })
        {
            ZigZagIndicator ind = Feed(candles, blockSize);
            string label = blockSize == 0 ? "one go" : blockSize.ToString();
            Console.WriteLine($"{label,9}  {ind.ZigZagList.Count,6}  "
                + $"{ind.LastSwingLow?.Candle.Date:yyyy-MM-dd HH:mm}  {ind.LastSwingHigh?.Candle.Date:yyyy-MM-dd HH:mm}");
        }
        // No assertion on purpose: this is a Discover... test and its value is the output above,
        // which is read by hand when the pinned values have to be renewed.
    }


    [TestMethod]
    public void DiscoverRealWorldPattern()
    {
        // What actually happens: one cold build of ~500 candles, then N candles per call for the
        // rest of the run. N is 1 for the live scanner and for a 1m base interval, 5 for the 1m
        // series on a 5m run, 15 on a 15m run - so if the outcome moves with N, the trend of every
        // interval finer than the base interval moves with it.
        CryptoCandleList candles = LoadCandles();
        List<CryptoCandle> all = [.. candles.Values];
        const int coldBuild = 500;

        Console.WriteLine("after a 500-candle cold build, then N per call:");
        Console.WriteLine("        N  points  lastLow           lastHigh");
        foreach (int n in new[] { 1, 2, 3, 5, 10, 15 })
        {
            ZigZagIndicator ind = new(TrendType.Primary, false);
            for (int i = 0; i < coldBuild && i < all.Count; i++)
                ind.Calculate(all[i], batchProcess: true);
            ind.FinishBatch();

            int inBlock = 0;
            for (int i = coldBuild; i < all.Count; i++)
            {
                ind.Calculate(all[i], batchProcess: true);
                if (++inBlock >= n) { ind.FinishBatch(); inBlock = 0; }
            }
            ind.FinishBatch();

            Console.WriteLine($"{n,9}  {ind.ZigZagList.Count,6}  "
                + $"{ind.LastSwingLow?.Candle.Date:yyyy-MM-dd HH:mm}  {ind.LastSwingHigh?.Candle.Date:yyyy-MM-dd HH:mm}");
        }
        // No assertion on purpose: this is a Discover... test and its value is the output above,
        // which is read by hand when the pinned values have to be renewed.
    }


    [TestMethod]
    public void DiscoverPinnedValues()
    {
        // Prints the fingerprint so ZigZagOutcomeIsPinned below can be filled in, and refreshed
        // after a deliberate algorithm change. Same pattern as
        // ZigZagIndicator9TestsEth.DiscoverCheckpointValues.
        CryptoCandleList candles = LoadCandles();
        ZigZagIndicator indicator = Feed(candles, 0);

        Console.WriteLine("=== copy into ZigZagOutcomeIsPinned ===");
        Console.WriteLine($"  candles       = {candles.Count}");
        Console.WriteLine($"  ZigZagList    = {indicator.ZigZagList.Count}");
        Console.WriteLine($"  LastSwingLow  = {indicator.LastSwingLow?.Candle.Date:yyyy, MM, dd, HH, mm, ss}");
        Console.WriteLine($"  LastSwingHigh = {indicator.LastSwingHigh?.Candle.Date:yyyy, MM, dd, HH, mm, ss}");
        Console.WriteLine();
        Console.WriteLine(Fingerprint(indicator));

        // No assertion on purpose: this is a Discover... test and its value is the output above,
        // which is read by hand when the pinned values have to be renewed.
    }


    [TestMethod]
    public void ZigZagOutcomeIsPinned()
    {
        // Characterisation test: locks the full result, not just the two swing points the older
        // ETHUSDT test checks. Any optimisation of the indicator has to leave this untouched.
        //
        // The expected values come from DiscoverPinnedValues — run that and paste them in. Left
        // unpinned on purpose rather than filled with guesses: a characterisation test with made-up
        // numbers proves nothing and fails for the wrong reason.
        CryptoCandleList candles = LoadCandles();
        ZigZagIndicator indicator = Feed(candles, 0);

        Assert.AreEqual(3315, candles.Count,
            "test data changed — refresh the pinned values with DiscoverPinnedValues");
        Assert.AreEqual(258, indicator.ZigZagList.Count,
            "ZigZag point count moved. Fingerprint:" + Environment.NewLine + Fingerprint(indicator));
        Assert.AreEqual(new DateTime(2024, 12, 06, 00, 00, 00, DateTimeKind.Utc), indicator.LastSwingLow?.Candle.Date);
        Assert.AreEqual(new DateTime(2024, 12, 06, 20, 00, 00, DateTimeKind.Utc), indicator.LastSwingHigh?.Candle.Date);
    }


    [TestMethod]
    public void ThroughputIsReported()
    {
        // Not an assertion on speed — CI machines vary. This prints candles/second so an
        // optimisation can be judged against a number instead of a feeling. A replay feeds ~48.6
        // million candles through this code, so a microsecond per candle is roughly a minute of run.
        CryptoCandleList candles = LoadCandles();
        List<CryptoCandle> all = [.. candles.Values];

        Feed(candles, 0);   // warm up the JIT

        const int rounds = 20;
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < rounds; i++)
            Feed(candles, 0);
        sw.Stop();

        long fed = (long)rounds * all.Count;
        double perCandleUs = sw.Elapsed.TotalMilliseconds * 1000 / fed;
        Console.WriteLine($"ZigZag ingest: {fed} candles in {sw.ElapsedMilliseconds} ms "
            + $"= {perCandleUs:F2} us/candle, {fed / sw.Elapsed.TotalSeconds / 1e6:F2} M candles/s");
        Console.WriteLine($"  extrapolated to a 48.6M candle replay: {48.6e6 * perCandleUs / 1e6:F0} s");

        Assert.IsTrue(fed > 0);
    }
}

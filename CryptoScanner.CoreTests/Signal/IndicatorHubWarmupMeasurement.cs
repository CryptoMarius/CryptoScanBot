using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Indicators;

using Microsoft.Data.Sqlite;

using System.Diagnostics;
using System.Text;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// What the emulator's base interval costs the indicator hub.
///
/// <para>
/// The hub is incremental: one candle in, one CryptoData out. It only gets to be incremental when
/// the candle it is asked for directly follows the one it last saw - see the gap test in
/// IndicatorEngine.PrepareViaHub. Anything else is a warm-up: a fresh hub fed 260 candles, a
/// BuildCurrent per candle, and a BandRangeTracker rebuilt over 750 more.
/// </para>
///
/// <para>
/// The replay calls the pipeline once per BASE candle (ReplayRunner.ProcessComputeAsync), and
/// PositionMonitor sets LastCandle1mCloseTime to that base candle's close. So on a 15m base run the
/// 1m indicator interval is asked for a candle 15 minutes further along every tick, and the hub can
/// never continue. Emulator run 229 (23-08-2026) shows what that adds up to: 731,138 pipeline
/// candles against 98 incremental hub calls.
/// </para>
///
/// <para>
/// This measures it directly instead of inferring it: the same call pattern, on a real 1m series,
/// for a 1m base and a 15m base side by side. Needs a candles.db, so it is not part of the normal
/// suite - run it with <c>dotnet test --filter TestCategory=Measurement</c>.
/// </para>
/// </summary>
[TestClass]
public class IndicatorHubWarmupMeasurement : TestBase
{
    private static readonly string[] CandleDatabaseCandidates =
    [
        @"E:\CryptoScanBot\Data\Binance\Emulator\Binance Futures.db",
        @"E:\CryptoScanBot\Data\Binance\Futures\Binance Futures.db",
    ];

    private const int IntervalId1m = (int)CryptoIntervalPeriod.interval1m + 1;

    /// <summary>Enough 1m candles to warm up (260) plus the band tracker (750) and still leave a
    /// stretch to measure over.</summary>
    private const int CandlesToLoad = 4000;

    /// <summary>How many pipeline ticks to measure per base interval.</summary>
    private const int TicksToMeasure = 400;


    [TestMethod]
    [TestCategory("Measurement")]
    public void MeasureWhatTheBaseIntervalCostsTheHub()
    {
        string? databasePath = CandleDatabaseCandidates.FirstOrDefault(File.Exists);
        if (databasePath == null)
        {
            Assert.Inconclusive($"No candle database found. Looked for: {string.Join(", ", CandleDatabaseCandidates)}");
            return;
        }

        List<CryptoCandle> candles = ReadBusiestSymbol1m(databasePath, CandlesToLoad);
        if (candles.Count < CandlesToLoad)
        {
            Assert.Inconclusive($"{databasePath} holds only {candles.Count} usable 1m candles.");
            return;
        }

        StringBuilder report = new();
        report.AppendLine($"database        : {databasePath}");
        report.AppendLine($"1m candles      : {candles.Count:N0}");
        report.AppendLine($"ticks measured  : {TicksToMeasure} per base interval");
        report.AppendLine();
        report.AppendLine("base   ticks  warmups  incremental  total ms   ms/tick   hub candles fed" +
            "   | per warmup: collect  hubFeed  bandRange");

        // A coarser base means fewer ticks over the same stretch of time, which is the whole point of
        // the setting. Measured per tick so the two columns are comparable, and the caller can
        // multiply by however many ticks its own run has.
        foreach (uint baseDuration in new uint[] { 1, 5, 15 })
        {
            Result result = MeasureOneBaseInterval(candles, baseDuration);
            long warmups = Math.Max(1, result.Warmups);
            report.AppendLine(
                $"{baseDuration,4}m {result.Ticks,7} {result.Warmups,8} {result.Incremental,12} " +
                $"{result.Milliseconds,9:F1} {result.Milliseconds / Math.Max(1, result.Ticks),9:F2} " +
                $"{result.CandlesFed,17:N0}   | " +
                $"{result.CollectMs / warmups,16:F2} {result.HubFeedMs / warmups,8:F2} " +
                $"{result.BandRangeMs / warmups,10:F2}");
        }

        Console.WriteLine(report.ToString());
    }


    private readonly record struct Result(int Ticks, long Warmups, long Incremental, double Milliseconds,
        long CandlesFed, double CollectMs, double HubFeedMs, double BandRangeMs);


    /// <summary>
    /// What a big in-memory CandleList costs the warm-up.
    ///
    /// <para>
    /// A warm-up looks at 260 candles for the hub and 750 for the band tracker, both fixed - so its
    /// cost should not depend on how many candles happen to be in memory. It does.
    /// BandRangeTracker.CollectBuildCandles asks CryptoCandleList.GetLastValuesUpTo for the last 750,
    /// and that method enumerates the WHOLE dictionary from the oldest key, copies every candle it
    /// passes into a list, and only then trims to 750. CryptoCandle is a struct, so that is a real
    /// copy per candle, once per pipeline tick.
    /// </para>
    ///
    /// <para>
    /// Which is what made this the biggest post in emulator run 229 rather than a rounding error: the
    /// DLZ zoom read whole candle series into memory (23.6 million candles pruned at chunk 30), and
    /// every one of them lengthened this walk. Two defects feeding each other.
    /// </para>
    /// </summary>
    [TestMethod]
    [TestCategory("Measurement")]
    public void MeasureWhatALargeCandleListCostsTheWarmup()
    {
        string? databasePath = CandleDatabaseCandidates.FirstOrDefault(File.Exists);
        if (databasePath == null)
        {
            Assert.Inconclusive($"No candle database found. Looked for: {string.Join(", ", CandleDatabaseCandidates)}");
            return;
        }

        List<CryptoCandle> candles = ReadBusiestSymbol1m(databasePath, 300_000);
        if (candles.Count < 20_000)
        {
            Assert.Inconclusive($"{databasePath} holds only {candles.Count} usable 1m candles.");
            return;
        }

        StringBuilder report = new();
        report.AppendLine($"database        : {databasePath}");
        report.AppendLine($"1m candles read : {candles.Count:N0}");
        report.AppendLine($"base interval   : 15m, 50 ticks per row");
        report.AppendLine();
        report.AppendLine("candles in memory   ms/tick   | per warmup: collect  hubFeed  bandRange");

        foreach (int inMemory in new[] { 4_000, 20_000, 100_000, candles.Count })
        {
            if (inMemory > candles.Count)
                continue;
            // The tail of the series, so the measured ticks sit at the newest end either way and only
            // the amount of history behind them differs.
            List<CryptoCandle> window = candles.GetRange(candles.Count - inMemory, inMemory);
            Result result = MeasureOneBaseInterval(window, 15, ticksToMeasure: 50);
            long warmups = Math.Max(1, result.Warmups);
            report.AppendLine(
                $"{inMemory,17:N0} {result.Milliseconds / Math.Max(1, result.Ticks),9:F2}   | " +
                $"{result.CollectMs / warmups,16:F2} {result.HubFeedMs / warmups,8:F2} " +
                $"{result.BandRangeMs / warmups,10:F2}");
        }

        Console.WriteLine(report.ToString());
    }


    /// <summary>
    /// Replays the pipeline's call pattern: one PrepareIndicators per base candle, on the 1m
    /// indicator interval, with the candle time the PositionMonitor would have handed it.
    /// </summary>
    private static Result MeasureOneBaseInterval(List<CryptoCandle> candles, uint baseDuration,
        int ticksToMeasure = TicksToMeasure)
    {
        InitTestSession();
        ZoneDlzTestSettings();

        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        // The three measurements share one TESTUSDT, and PrepareIndicators returns immediately for a
        // candle that is already in Data. Without this the second run reads the first run's answers
        // and reports warmups that never happened - which is exactly what the first version of this
        // measurement did.
        symbolInterval.Data.Clear();
        symbolInterval.IndicatorHub = null;
        symbolInterval.IndicatorHubLastAdded = null;
        symbolInterval.IndicatorHubAddCount = 0;
        symbolInterval.BandRange = null;

        foreach (CryptoCandle candle in candles)
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);

        // Measure at the NEWEST end of the series, not at the oldest. GetLastValuesUpTo walks from
        // the first key until it passes the one asked for, so the cost of a warm-up depends on how
        // far INTO the list the clock stands - which in a replay is "further every tick". Measuring
        // near the front would report the cheapest position in the run and call it the average.
        CandleTime start = candles[^1].OpenTime - (uint)ticksToMeasure * baseDuration - 1;
        // Align the way the replay does, so the base candles fall on their own boundaries.
        start -= start % baseDuration;

        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = true;
        try
        {
            // One untimed pass so the row measured first does not also pay for the JIT.
            IndicatorEngine.PrepareIndicators(symbol, interval, start - 1);
            PipelineProfiler.Reset();

            long begin = Stopwatch.GetTimestamp();
            int ticks = 0;
            for (int index = 0; index < ticksToMeasure; index++)
            {
                // PositionMonitor: LastCandle1mCloseTime = base candle open + base duration.
                CandleTime closeTime = start + (uint)index * baseDuration + baseDuration;
                if (closeTime > candles[^1].OpenTime)
                    break;

                // SignalPrepare, for the 1m indicator interval.
                IndicatorEngine.PrepareIndicators(symbol, interval, closeTime - interval.Duration);
                ticks++;
            }
            double milliseconds = 1000.0 * (Stopwatch.GetTimestamp() - begin) / Stopwatch.Frequency;

            static double Ms(long ticksRaw) => 1000.0 * ticksRaw / Stopwatch.Frequency;
            return new Result(ticks, PipelineProfiler.PrepWarmupCalls, PipelineProfiler.HubIncrementalCalls,
                milliseconds, PipelineProfiler.PrepHubFeedCandles,
                Ms(PipelineProfiler.PrepCollectTicks), Ms(PipelineProfiler.PrepHubFeedTicks),
                Ms(PipelineProfiler.PrepBandRangeTicks));
        }
        finally
        {
            PipelineProfiler.Enabled = false;
        }
    }


    /// <summary>Settings the indicator hub needs; deliberately the defaults, so this measures the
    /// engine as configured rather than a special case.</summary>
    private static void ZoneDlzTestSettings()
    {
        IndicatorConfiguration.Bump();
    }


    /// <summary>
    /// The tail of the 1m series of the symbol with the most of them. Read straight from the raw
    /// columns - the measurement only needs times and prices, not the exchange registration.
    /// </summary>
    private static List<CryptoCandle> ReadBusiestSymbol1m(string databasePath, int count)
    {
        using SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        int symbolId;
        using (SqliteCommand pick = connection.CreateCommand())
        {
            pick.CommandText =
                "SELECT SymbolId, count(*) FROM Candle WHERE IntervalId = $IntervalId " +
                "GROUP BY SymbolId ORDER BY count(*) DESC LIMIT 1";
            pick.Parameters.AddWithValue("$IntervalId", IntervalId1m);
            using SqliteDataReader reader = pick.ExecuteReader();
            if (!reader.Read())
                return [];
            symbolId = reader.GetInt32(0);
        }

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT OpenTime, Ticks, Open, High, Low, Close, Volume FROM Candle " +
            "WHERE SymbolId = $SymbolId AND IntervalId = $IntervalId " +
            "ORDER BY OpenTime DESC LIMIT $Count";
        command.Parameters.AddWithValue("$SymbolId", symbolId);
        command.Parameters.AddWithValue("$IntervalId", IntervalId1m);
        command.Parameters.AddWithValue("$Count", count);

        List<CryptoCandle> candles = [];
        using SqliteDataReader rows = command.ExecuteReader();
        while (rows.Read())
        {
            byte ticksRaw = (byte)rows.GetInt32(1);
            decimal tickSize = 1m;
            for (int decimals = ticksRaw & 0x0F; decimals > 0; decimals--)
                tickSize /= 10m;

            candles.Add(new CryptoCandle
            {
                OpenTime = new CandleTime((uint)rows.GetInt64(0)),
                TickDecimalsRaw = ticksRaw,
                Open = rows.GetInt64(2) * tickSize,
                High = rows.GetInt64(3) * tickSize,
                Low = rows.GetInt64(4) * tickSize,
                Close = rows.GetInt64(5) * tickSize,
                Volume = (decimal)rows.GetDouble(6),
            });
        }
        candles.Reverse();
        return candles;
    }
}

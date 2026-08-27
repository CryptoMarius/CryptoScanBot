using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using Microsoft.Data.Sqlite;

using System.Text;

namespace CryptoScanner.CoreTests.Trend;

/// <summary>
/// How deep into the pivot list does one new candle still change something?
///
/// <para>
/// That number is what <see cref="ZigZagIndicator.MutableTailLength"/> has to cover. Everything
/// before it is settled, and the DLZ calculation skips it: no verdict recomputed, no zoom, no
/// candles read. Too small and a verdict gets frozen that the next candle would still have moved -
/// the bug class the settled/provisional split was built to close. Too large and every
/// recalculation re-judges pivots whose answer was never going to change.
/// </para>
///
/// <para>
/// ZoneDlzIncrementalTests.MeasureHowFarBackAChangeReaches already asks this question, in ZONES and
/// on one bundled candle series (ETHUSDT 1h, 3315 candles, 6 changing steps). Six events is thin
/// ground for a constant. This measures the same thing in PIVOTS - the unit MutableTailLength is
/// actually expressed in - over every 1h series in a real candle database, so the answer rests on
/// thousands of events instead.
/// </para>
///
/// <para>
/// Not part of the normal suite: it needs a candles.db that only exists on a machine that has run
/// the scanner. Run it explicitly:
/// <c>dotnet test --filter TestCategory=Measurement</c>, and read the output.
/// </para>
/// </summary>
[TestClass]
public class ZigZagTailReachMeasurement : TestBase
{
    /// <summary>
    /// Candle databases to look for, first one that exists wins. A machine without any of them gets
    /// an inconclusive result rather than a failure - there is nothing to measure, not a defect.
    /// </summary>
    private static readonly string[] CandleDatabaseCandidates =
    [
        @"E:\CryptoScanBot\Data\Binance\Emulator\Binance Perpetual.db",
        @"E:\CryptoScanBot\Data\Binance\Emulator\Binance Futures.db",
        @"E:\CryptoScanBot\Data\Binance\Futures\Binance Perpetual.db",
        @"E:\CryptoScanBot\Data\Binance\Futures\Binance Futures.db",
    ];

    /// <summary>IntervalId of 1h in the candle database: the enum value plus one.</summary>
    private const int IntervalId1h = (int)CryptoIntervalPeriod.interval1h + 1;

    /// <summary>Enough symbols to make the tail of the distribution mean something, few enough to
    /// keep the measurement under a minute.</summary>
    private const int MaxSymbols = 60;


    /// <summary>One pivot, reduced to what decides whether two snapshots agree.</summary>
    private readonly record struct PivotKey(char PointType, uint OpenTime, double Value);


    [TestMethod]
    [TestCategory("Measurement")]
    public void MeasureHowDeepAChangeReachesInPivots()
    {
        string? databasePath = CandleDatabaseCandidates.FirstOrDefault(File.Exists);
        if (databasePath == null)
        {
            Assert.Inconclusive($"No candle database found. Looked for: {string.Join(", ", CandleDatabaseCandidates)}");
            return;
        }

        using SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        List<(int SymbolId, string Name)> symbols = ReadSymbolsWithEnough1hCandles(connection);
        if (symbols.Count == 0)
        {
            Assert.Inconclusive($"{databasePath} holds no symbol with enough 1h candles.");
            return;
        }

        // Index = how deep the change reached, value = how often. Index 0 is "only appended", which
        // is not a change to anything that already existed.
        long[] histogram = new long[ZigZagIndicator.MutableTailLength + 2];
        long overflow = 0;
        int deepest = 0;
        string deepestMoment = "";
        long candlesFed = 0;
        long steps = 0;
        long changingSteps = 0;

        foreach ((int symbolId, string name) in symbols.Take(MaxSymbols))
        {
            List<CryptoCandle> candles = ReadCandles(connection, symbolId, IntervalId1h);
            if (candles.Count < 300)
                continue;

            ZigZagIndicator indicator = new(TrendType.Primary, false);
            List<PivotKey> previous = [];

            foreach (CryptoCandle candle in candles)
            {
                indicator.Calculate(candle, batchProcess: true);
                indicator.FinishBatch();
                candlesFed++;

                // Dummy points are a marker for the right-hand edge, added and removed on every
                // candle by construction. Counting their churn would measure the marker, not the
                // structure the verdicts rest on.
                List<PivotKey> current = [.. indicator.ZigZagList
                    .Where(pivot => !pivot.Dummy)
                    .Select(pivot => new PivotKey(pivot.PointType, pivot.Candle.OpenTime.Minutes, pivot.Value))];

                if (previous.Count == 0)
                {
                    previous = current;
                    continue;
                }

                steps++;

                // First position where the two snapshots disagree.
                int common = 0;
                while (common < current.Count && common < previous.Count && current[common] == previous[common])
                    common++;

                int reach = previous.Count - common;
                if (reach > 0)
                {
                    changingSteps++;
                    if (reach < histogram.Length)
                        histogram[reach]++;
                    else
                        overflow++;

                    if (reach > deepest)
                    {
                        deepest = reach;
                        deepestMoment = $"{name} {candle.OpenTime.ToDateTime():yyyy-MM-dd HH:mm}, " +
                                        $"{previous.Count} pivots, changed from index {common}";
                    }
                }

                previous = current;
            }
        }

        StringBuilder report = new();
        report.AppendLine($"database            : {databasePath}");
        report.AppendLine($"symbols measured    : {Math.Min(symbols.Count, MaxSymbols)}");
        report.AppendLine($"candles fed         : {candlesFed:N0}");
        report.AppendLine($"steps compared      : {steps:N0}");
        report.AppendLine($"steps that changed an existing pivot: {changingSteps:N0} ({(double)changingSteps / Math.Max(1, steps):P2})");
        report.AppendLine($"deepest reach back  : {deepest} pivot(s)");
        report.AppendLine($"deepest moment      : {deepestMoment}");
        report.AppendLine($"MutableTailLength   : {ZigZagIndicator.MutableTailLength}");
        report.AppendLine();
        report.AppendLine("reach  occurrences   share of changing steps");
        for (int reach = 1; reach < histogram.Length; reach++)
        {
            if (histogram[reach] == 0)
                continue;
            report.AppendLine($"{reach,5}  {histogram[reach],12:N0}   {(double)histogram[reach] / Math.Max(1, changingSteps),8:P2}");
        }
        if (overflow > 0)
            report.AppendLine($"  >{histogram.Length - 1}  {overflow,12:N0}   {(double)overflow / Math.Max(1, changingSteps),8:P2}");

        Console.WriteLine(report.ToString());

        Assert.AreEqual(0, overflow,
            $"A change reached back further than MutableTailLength ({ZigZagIndicator.MutableTailLength}) " +
            $"allows for, which means a settled verdict can still move. Deepest: {deepest} at {deepestMoment}");
    }


    /// <summary>
    /// How often does a pivot change something the DLZ can actually SEE?
    ///
    /// <para>
    /// The measurement above compares raw pivots, and a pivot counts as changed the moment its value
    /// moves at all. The dominance test is coarser than that. For a candidate pivot at index i it
    /// reads the three point types around it and ONE inequality between its neighbours
    /// (ZoneDlz.CalculateDlzAsync: <c>previous2.Value &lt; zigZag.Value</c>), and the zone that comes
    /// out is built from the CANDLE of i, not from its value. So a pivot that wobbles without
    /// crossing its neighbour changes nothing at all.
    /// </para>
    ///
    /// <para>
    /// This measures the difference between the two: the same walk, but comparing what the verdict
    /// rests on instead of the pivot itself. The gap between the two numbers is how much of the
    /// current work is spent re-deriving answers that were never going to move.
    /// </para>
    /// </summary>
    [TestMethod]
    [TestCategory("Measurement")]
    public void MeasureHowOftenAVerdictCanActuallyChange()
    {
        string? databasePath = CandleDatabaseCandidates.FirstOrDefault(File.Exists);
        if (databasePath == null)
        {
            Assert.Inconclusive($"No candle database found. Looked for: {string.Join(", ", CandleDatabaseCandidates)}");
            return;
        }

        using SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        List<(int SymbolId, string Name)> symbols = ReadSymbolsWithEnough1hCandles(connection);
        if (symbols.Count == 0)
        {
            Assert.Inconclusive($"{databasePath} holds no symbol with enough 1h candles.");
            return;
        }

        long steps = 0;
        long pivotChanges = 0;
        long verdictChanges = 0;
        int deepestPivot = 0;
        int deepestVerdict = 0;

        foreach ((int symbolId, string name) in symbols.Take(MaxSymbols))
        {
            List<CryptoCandle> candles = ReadCandles(connection, symbolId, IntervalId1h);
            if (candles.Count < 300)
                continue;

            ZigZagIndicator indicator = new(TrendType.Primary, false);
            List<PivotKey> previousPivots = [];
            List<VerdictKey> previousVerdicts = [];

            foreach (CryptoCandle candle in candles)
            {
                indicator.Calculate(candle, batchProcess: true);
                indicator.FinishBatch();

                List<ZigZagResult> real = [.. indicator.ZigZagList.Where(pivot => !pivot.Dummy)];
                List<PivotKey> pivots = [.. real.Select(pivot =>
                    new PivotKey(pivot.PointType, pivot.Candle.OpenTime.Minutes, pivot.Value))];

                // One entry per candidate pivot: the three point types the test reads, which side of
                // its predecessor the confirmer sits, and the candle the zone would be cut from.
                List<VerdictKey> verdicts = [];
                for (int i = 1; i < real.Count - 1; i++)
                {
                    verdicts.Add(new VerdictKey(
                        real[i - 1].PointType, real[i].PointType, real[i + 1].PointType,
                        Math.Sign(real[i + 1].Value - real[i - 1].Value),
                        real[i].Candle.OpenTime.Minutes));
                }

                if (previousPivots.Count > 0)
                {
                    steps++;
                    deepestPivot = Math.Max(deepestPivot, Reach(previousPivots, pivots, ref pivotChanges));
                    deepestVerdict = Math.Max(deepestVerdict, Reach(previousVerdicts, verdicts, ref verdictChanges));
                }

                previousPivots = pivots;
                previousVerdicts = verdicts;
            }
        }

        Console.WriteLine($"steps compared              : {steps:N0}");
        Console.WriteLine($"steps changing a pivot      : {pivotChanges:N0} ({(double)pivotChanges / Math.Max(1, steps):P2}), deepest {deepestPivot}");
        Console.WriteLine($"steps changing a verdict    : {verdictChanges:N0} ({(double)verdictChanges / Math.Max(1, steps):P2}), deepest {deepestVerdict}");
        Console.WriteLine($"pivot moves the verdict never saw: {pivotChanges - verdictChanges:N0} " +
            $"({(double)(pivotChanges - verdictChanges) / Math.Max(1, pivotChanges):P1} of all pivot changes)");
    }


    /// <summary>
    /// What the inequality in the dominance test actually decides.
    ///
    /// <para>
    /// ZoneDlz judges an H/L/H triple dominant when <c>previous2.Value &lt; zigZag.Value</c> - a higher
    /// high, so the low between them is the level that held. But a new high only enters the pivot list
    /// at all when it breaks the previous one (ZigZagIndicator.CanAddNewHigh: "It breaks the box"),
    /// with one escape hatch for a point more than 25% away. So the question is whether the inequality
    /// still decides anything, or whether the point-type pattern has already decided it.
    /// </para>
    ///
    /// <para>
    /// It matters for what a caller has to watch. If the inequality is effectively always true, then a
    /// confirmer that keeps moving in the same direction can never flip a verdict and only the PATTERN
    /// has to be watched. If it is regularly false, the value is a real input and a move can flip it.
    /// </para>
    /// </summary>
    [TestMethod]
    [TestCategory("Measurement")]
    public void MeasureWhatTheDominanceInequalityDecides()
    {
        string? databasePath = CandleDatabaseCandidates.FirstOrDefault(File.Exists);
        if (databasePath == null)
        {
            Assert.Inconclusive($"No candle database found. Looked for: {string.Join(", ", CandleDatabaseCandidates)}");
            return;
        }

        using SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        List<(int SymbolId, string Name)> symbols = ReadSymbolsWithEnough1hCandles(connection);
        long farApart = 0, closeTogether = 0;
        long hlhTriples = 0, hlhDominant = 0;
        long lhlTriples = 0, lhlDominant = 0;
        long otherPatterns = 0;

        foreach ((int symbolId, string _) in symbols.Take(MaxSymbols))
        {
            List<CryptoCandle> candles = ReadCandles(connection, symbolId, IntervalId1h);
            if (candles.Count < 300)
                continue;

            ZigZagIndicator indicator = new(TrendType.Primary, false);
            foreach (CryptoCandle candle in candles)
                indicator.Calculate(candle, batchProcess: true);
            indicator.FinishBatch();

            List<ZigZagResult> real = [.. indicator.ZigZagList.Where(pivot => !pivot.Dummy)];
            for (int i = 2; i < real.Count; i++)
            {
                ZigZagResult before = real[i - 2], candidate = real[i - 1], confirmer = real[i];

                if (confirmer.PointType == 'H' && candidate.PointType == 'L' && before.PointType == 'H')
                {
                    hlhTriples++;
                    if (before.Value < confirmer.Value)
                        hlhDominant++;
                    else
                        CountRejection(before.Value, confirmer.Value, ref farApart, ref closeTogether);
                }
                else if (confirmer.PointType == 'L' && candidate.PointType == 'H' && before.PointType == 'L')
                {
                    lhlTriples++;
                    if (before.Value > confirmer.Value)
                        lhlDominant++;
                    else
                        CountRejection(before.Value, confirmer.Value, ref farApart, ref closeTogether);
                }
                else
                {
                    otherPatterns++;
                }
            }
        }

        long triples = hlhTriples + lhlTriples;
        long dominant = hlhDominant + lhlDominant;
        Console.WriteLine($"H/L/H triples            : {hlhTriples:N0}, of which higher high : {hlhDominant:N0} ({(double)hlhDominant / Math.Max(1, hlhTriples):P2})");
        Console.WriteLine($"L/H/L triples            : {lhlTriples:N0}, of which lower low   : {lhlDominant:N0} ({(double)lhlDominant / Math.Max(1, lhlTriples):P2})");
        Console.WriteLine($"triples with another pattern (no verdict possible): {otherPatterns:N0}");
        Console.WriteLine($"inequality rejected the pattern : {triples - dominant:N0} of {triples:N0} ({(double)(triples - dominant) / Math.Max(1, triples):P2})");
        Console.WriteLine($"  ... of which more than 25% apart (the CanAddNewHigh/Low escape) : {farApart:N0}");
        Console.WriteLine($"  ... of which within 25% (something else moved them)            : {closeTogether:N0}");
    }


    /// <summary>Splits a rejected triple by whether the 25% escape in CanAddNewHigh/CanAddNewLow
    /// could explain how the confirmer got into the list on the wrong side of its predecessor.</summary>
    private static void CountRejection(double before, double confirmer, ref long farApart, ref long closeTogether)
    {
        double distance = 100 * Math.Abs(confirmer - before) / Math.Abs(before);
        if (distance > 25)
            farApart++;
        else
            closeTogether++;
    }


    /// <summary>What a dominance verdict about one candidate pivot rests on.</summary>
    private readonly record struct VerdictKey(char Before, char Candidate, char Confirmer,
        int ConfirmerSide, uint CandidateOpenTime);


    /// <summary>
    /// How many entries back from the end of <paramref name="previous"/> the first difference sits,
    /// counting a pure append as no change. Increments <paramref name="changes"/> when there is one.
    /// </summary>
    private static int Reach<T>(List<T> previous, List<T> current, ref long changes) where T : struct
    {
        int common = 0;
        while (common < current.Count && common < previous.Count
               && EqualityComparer<T>.Default.Equals(current[common], previous[common]))
            common++;

        int reach = previous.Count - common;
        if (reach > 0)
            changes++;
        return reach;
    }


    /// <summary>Symbols that have at least 300 hourly candles, newest series first.</summary>
    private static List<(int SymbolId, string Name)> ReadSymbolsWithEnough1hCandles(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT s.SymbolId, s.Name, count(*) as Candles " +
            "FROM Symbol s JOIN Candle c ON c.SymbolId = s.SymbolId AND c.IntervalId = $IntervalId " +
            "GROUP BY s.SymbolId, s.Name HAVING count(*) >= 300 " +
            "ORDER BY Candles DESC";
        command.Parameters.AddWithValue("$IntervalId", IntervalId1h);

        List<(int, string)> result = [];
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
            result.Add((reader.GetInt32(0), reader.IsDBNull(1) ? $"#{reader.GetInt32(0)}" : reader.GetString(1)));
        return result;
    }


    /// <summary>
    /// The whole series for one (symbol, interval), oldest first. Reads the raw columns rather than
    /// going through CandleDatabase so the measurement needs no exchange or symbol registration -
    /// the ZigZag only looks at the times and the four prices.
    /// </summary>
    private static List<CryptoCandle> ReadCandles(SqliteConnection connection, int symbolId, int intervalId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT OpenTime, Ticks, Open, High, Low, Close, Volume FROM Candle " +
            "WHERE SymbolId = $SymbolId AND IntervalId = $IntervalId ORDER BY OpenTime";
        command.Parameters.AddWithValue("$SymbolId", symbolId);
        command.Parameters.AddWithValue("$IntervalId", intervalId);

        List<CryptoCandle> candles = [];
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            byte ticksRaw = (byte)reader.GetInt32(1);
            decimal tickSize = 1m;
            for (int decimals = ticksRaw & 0x0F; decimals > 0; decimals--)
                tickSize /= 10m;

            CryptoCandle candle = new()
            {
                OpenTime = new CandleTime((uint)reader.GetInt64(0)),
                TickDecimalsRaw = ticksRaw,
                Open = reader.GetInt64(2) * tickSize,
                High = reader.GetInt64(3) * tickSize,
                Low = reader.GetInt64(4) * tickSize,
                Close = reader.GetInt64(5) * tickSize,
                Volume = (decimal)reader.GetDouble(6),
            };
            candles.Add(candle);
        }
        return candles;
    }
}

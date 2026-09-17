using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using Microsoft.Data.Sqlite;

using System.Text;

namespace CryptoScanner.CoreTests.Trend;


/// <summary>
/// Does a trend flip say anything about what the price does next?
///
/// <para>
/// The "trend" and "trend.secondary" strategies enter on a flip of the Dow interpretation
/// (<see cref="TrendInterval.InterpretZigZagPoints"/>): bearish to bullish for a long, bullish to
/// bearish for a short. They were the worst runs of the batch of 5 to 15 September 2026 - run 910
/// lost 0.480 USDT per position, the worst per-position figure of all 129 runs with the standard
/// ladder. That can have two very different causes: the flip itself carries no information, or it
/// does and everything around it (the pullback entry, the ladder, the fixed take profit) throws it
/// away. Those need different answers, so they have to be told apart.
/// </para>
///
/// <para>
/// This measures the flip on its own, with no entry rule, no ladder and no exit attached: feed the
/// real candles into the real indicator, note every flip, and look at what the price did over the
/// next 1, 5, 10, 20 and 50 candles. Against it stands the unconditional move over the same
/// candles - what you would have got by entering at a random moment. A flip that carries
/// information beats that baseline; one that does not, does not.
/// </para>
///
/// <para>
/// Not part of the normal suite: it needs a candles.db that only exists on a machine that has run
/// the scanner. Run it explicitly:
/// <c>dotnet test --filter TestCategory=Measurement</c>, and read the output.
/// </para>
/// </summary>
[TestClass]
public class TrendFlipPredictiveMeasurement : TestBase
{
    private static readonly string[] CandleDatabaseCandidates =
    [
        @"E:\CryptoScanBot\Data\Emulator\Binance Perpetual.db",
        @"E:\CryptoScanBot\Data\Binance\Emulator\Binance Perpetual.db",
        @"E:\CryptoScanBot\Data\Binance\Futures\Binance Perpetual.db",
    ];

    /// <summary>The intervals the strategy is actually run on. Below 10m it refuses to fire
    /// (SignalTrendLongBase.IsSignal), so there is nothing to measure there.</summary>
    private static readonly (string Name, CryptoIntervalPeriod Period)[] Intervals =
    [
        ("15m", CryptoIntervalPeriod.interval15m),
        ("30m", CryptoIntervalPeriod.interval30m),
        ("1h", CryptoIntervalPeriod.interval1h),
    ];

    /// <summary>How far ahead to look, in candles of the interval being measured.</summary>
    private static readonly int[] Horizons = [1, 5, 10, 20, 50];

    private const int MaxSymbols = 40;
    private const int MinimumCandles = 500;

    /// <summary>
    /// How many candles of history the trend is allowed to stand on, matching what the running
    /// scanner keeps: CandleTools.CandleCountFetch is 500 for every interval above 1m, and
    /// CleanCandleDataAsync trims the candles, the indicator data AND the ZigZag pivots against that
    /// same boundary every round. Letting the pivot list grow over the whole series instead would
    /// measure a trend nothing in production ever computes - and it makes the walk quadratic, since
    /// the Dow interpretation re-reads the entire pivot list on every candle.
    /// </summary>
    private const int TrendWindowCandles = CryptoScanner.Core.Core.CandleTools.CandleCountFetch;


    /// <summary>
    /// One bucket of forward moves. Keeps the values rather than a running sum because the mean on
    /// its own is unreadable here: a single candle that doubles pulls it further than a thousand
    /// ordinary ones, so the median is the number that says what usually happens and the mean says
    /// what the tail does.
    /// </summary>
    private sealed class Bucket
    {
        private readonly List<double> Moves = [];
        private long Positive;

        public void Add(double movePercentage)
        {
            Moves.Add(movePercentage);
            if (movePercentage > 0)
                Positive++;
        }

        public long Count => Moves.Count;
        public double Mean => Moves.Count == 0 ? 0 : Moves.Sum() / Moves.Count;
        public double HitRate => Moves.Count == 0 ? 0 : (double)Positive / Moves.Count;

        public double Median()
        {
            if (Moves.Count == 0)
                return 0;
            Moves.Sort();
            return Moves[Moves.Count / 2];
        }
    }


    [TestMethod]
    [TestCategory("Measurement")]
    public void MeasureWhetherATrendFlipPredictsAnything()
    {
        string? databasePath = CandleDatabaseCandidates.FirstOrDefault(File.Exists);
        if (databasePath == null)
        {
            Assert.Inconclusive($"No candle database found. Looked for: {string.Join(", ", CandleDatabaseCandidates)}");
            return;
        }

        using SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        StringBuilder report = new();
        report.AppendLine($"database : {databasePath}");
        report.AppendLine($"symbols  : at most {MaxSymbols} per interval, at least {MinimumCandles} candles each");
        report.AppendLine();

        foreach (TrendType trendType in new[] { TrendType.Primary, TrendType.Secondary })
        {
            foreach ((string intervalName, CryptoIntervalPeriod period) in Intervals)
            {
                Measure(connection, trendType, intervalName, period, report);
            }
        }

        Console.WriteLine(report.ToString());
    }


    private static void Measure(SqliteConnection connection, TrendType trendType,
        string intervalName, CryptoIntervalPeriod period, StringBuilder report)
    {
        int intervalId = (int)period + 1;
        List<(int SymbolId, string Name)> symbols = ReadSymbols(connection, intervalId);
        if (symbols.Count == 0)
        {
            report.AppendLine($"{trendType} {intervalName}: no symbol with enough candles.");
            return;
        }

        // Three buckets per horizon: after a flip to bullish, after a flip to bearish, and every
        // candle regardless (the baseline). The baseline is what makes the other two readable.
        Dictionary<int, Bucket> bullish = Horizons.ToDictionary(h => h, _ => new Bucket());
        Dictionary<int, Bucket> bearish = Horizons.ToDictionary(h => h, _ => new Bucket());
        Dictionary<int, Bucket> baseline = Horizons.ToDictionary(h => h, _ => new Bucket());
        long symbolsUsed = 0;
        long candlesFed = 0;

        // How long a trend survives before it flips back, in candles. A flip that is undone two
        // candles later is not a trend, whatever the forward move says.
        List<int> flipSpacing = [];

        foreach ((int symbolId, string _) in symbols.Take(MaxSymbols))
        {
            List<CryptoCandle> candles = ReadCandles(connection, symbolId, intervalId);
            if (candles.Count < MinimumCandles)
                continue;
            symbolsUsed++;

            ZigZagIndicator indicator = new(trendType, useHighLow: true);
            CryptoTrendIndicator previousTrend = CryptoTrendIndicator.Unknown;
            int previousFlipIndex = -1;

            for (int i = 0; i < candles.Count; i++)
            {
                indicator.Calculate(candles[i], batchProcess: true);
                indicator.FinishBatch();
                candlesFed++;

                // Same rolling window the live scanner keeps (see TrendWindowCandles).
                int cutoffIndex = i - TrendWindowCandles;
                if (cutoffIndex >= 0)
                    indicator.TrimBefore(candles[cutoffIndex].OpenTime);

                // Nothing to say until the window has actually filled; before that the trend rests
                // on a handful of pivots and would flip on almost anything.
                if (i < TrendWindowCandles)
                {
                    previousTrend = CryptoTrendIndicator.Unknown;
                    continue;
                }

                CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(indicator, null);

                foreach (int horizon in Horizons)
                {
                    double? move = ForwardMove(candles, i, horizon);
                    if (move == null)
                        continue;
                    baseline[horizon].Add(move.Value);

                    if (trend == previousTrend || previousTrend == CryptoTrendIndicator.Unknown)
                        continue;
                    if (trend == CryptoTrendIndicator.Bullish)
                        bullish[horizon].Add(move.Value);
                    else if (trend == CryptoTrendIndicator.Bearish)
                        bearish[horizon].Add(move.Value);
                }

                if (trend != previousTrend && previousTrend != CryptoTrendIndicator.Unknown)
                {
                    if (previousFlipIndex >= 0)
                        flipSpacing.Add(i - previousFlipIndex);
                    previousFlipIndex = i;
                }
                previousTrend = trend;
            }
        }

        long flips = bullish[Horizons[0]].Count + bearish[Horizons[0]].Count;
        flipSpacing.Sort();
        int medianSpacing = flipSpacing.Count == 0 ? 0 : flipSpacing[flipSpacing.Count / 2];

        report.AppendLine($"=== {trendType} trend, {intervalName} candles");
        report.AppendLine($"    symbols {symbolsUsed}, candles {candlesFed:N0}, flips {flips:N0} " +
                          $"(1 per {(flips == 0 ? 0 : candlesFed / flips):N0} candles), " +
                          $"median candles between two flips {medianSpacing}");
        report.AppendLine("    horizon |        na omslag bullish |        na omslag bearish |          elke candle");
        report.AppendLine("            | mediaan    gem.  stijgt% | mediaan    gem.  stijgt% | mediaan    gem.  stijgt%");
        foreach (int horizon in Horizons)
        {
            report.AppendLine(
                $"    {horizon,6}c | {Row(bullish[horizon])} | {Row(bearish[horizon])} | {Row(baseline[horizon])}");
        }
        report.AppendLine();
    }


    /// <summary>One bucket as median, mean and share of moves that went up.</summary>
    private static string Row(Bucket bucket) =>
        $"{bucket.Median(),7:+0.00;-0.00}% {bucket.Mean,6:+0.00;-0.00}% {bucket.HitRate,8:P1}";


    /// <summary>Close-to-close move from candle <paramref name="index"/> to
    /// <paramref name="horizon"/> candles later, in percent. Null past the end of the series.</summary>
    private static double? ForwardMove(List<CryptoCandle> candles, int index, int horizon)
    {
        int target = index + horizon;
        if (target >= candles.Count)
            return null;
        decimal from = candles[index].Close;
        if (from <= 0 || candles[target].Close <= 0)
            return null;
        return (double)((candles[target].Close - from) / from) * 100.0;
    }


    private static List<(int SymbolId, string Name)> ReadSymbols(SqliteConnection connection, int intervalId)
    {
        // Only the tradable perpetuals. The database also holds four "$"-prefixed index series
        // (BMP/BMX, basis and premium) whose prices are legitimately NEGATIVE and which carry by far
        // the most candles - 349,920 against 7,341 for a real perpetual. Ordering on candle count
        // therefore handed the whole measurement to them, and a percentage move off a negative price
        // is meaningless: the unconditional average came out at -1.1% per 15m candle, which is not a
        // market but a division by a sign flip.
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT s.SymbolId, s.Name, count(*) as Candles " +
            "FROM Symbol s JOIN Candle c ON c.SymbolId = s.SymbolId AND c.IntervalId = $IntervalId " +
            "WHERE s.Name LIKE '%.PERP' AND s.Name NOT LIKE '$%' " +
            "GROUP BY s.SymbolId, s.Name HAVING count(*) >= $Minimum AND min(c.Low) > 0 " +
            "ORDER BY Candles DESC, s.Name";
        command.Parameters.AddWithValue("$IntervalId", intervalId);
        command.Parameters.AddWithValue("$Minimum", MinimumCandles);

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

using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicators;

using Microsoft.Data.Sqlite;

using Skender.Stock.Indicators;

using System.Globalization;
using System.Text;

namespace CryptoScanner.CoreTests.Analyzer.Indicators;

/// <summary>
/// Exports the band range index straight from <see cref="BandRangeTracker"/> so an outside
/// calculation can be checked against it.
///
/// <para>
/// The tracker is fed exactly the way <see cref="BandRangeTracker.Build"/> feeds it: candles in
/// ascending order through a private Bollinger hub with the settings from the Indicators tab, and
/// one <see cref="BandRangeTracker.Add"/> per candle. After every candle the index, the median
/// width, the ratio and the measurement count are written out, so the comparison is not limited to
/// the last candle.
/// </para>
///
/// <para>
/// Candles are read straight out of a candle database (&lt;Exchange&gt; &lt;Type&gt;.db). Prices in
/// that file are integers counting price ticks; they are fed as-is, with TickDecimals = 0. Every
/// number the tracker produces is a ratio or a percentage, so the tick scale cancels out.
/// </para>
///
/// <para>
/// Not a pass/fail test - it writes a file. Point CandleDatabasePath at a candle database and run
/// it; without that file the test reports inconclusive rather than failing a build.
/// </para>
/// </summary>
[TestClass]
public class BandRangeIndexExportTests
{
    /// <summary>Candle database to read. Empty = skip.</summary>
    private const string CandleDatabasePath = @"E:\CryptoScanBot\Data\Binance\Emulator\Binance Perpetual.db";

    /// <summary>Where the export lands.</summary>
    private const string OutputPath = @"E:\CryptoScanBot\Data\Reports\EntryTiming\band_index_csharp.csv";

    /// <summary>IntervalId 6 = 15m (CryptoIntervalPeriod + 1).</summary>
    private const int IntervalId = 6;

    /// <summary>Symbols to export. Kept small: the point is a reference, not a full sweep.</summary>
    private static readonly string[] Symbols = ["BTCUSDT", "ETHUSDT", "DOGEUSDT", "XRPUSDT", "SOLUSDT"];

    /// <summary>Same window the tracker itself uses when it is built.</summary>
    private const int CacheSize = 100;

    private sealed record Row(long OpenTime, double? Index, double? Width, double? Ratio, int Count);


    [TestMethod]
    public void ExportBandRangeIndex()
    {
        if (!File.Exists(CandleDatabasePath))
        {
            Assert.Inconclusive($"candle database not found: {CandleDatabasePath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
        var settings = new SettingsGeneralBB();   // same defaults as the Indicators tab: 20 / 2

        using var connection = new SqliteConnection($"Data Source={CandleDatabasePath};Mode=ReadOnly");
        connection.Open();

        StringBuilder output = new();
        output.AppendLine("symbol,opentime,index,median_width,ratio,count");

        int exported = 0;
        foreach (string symbolName in Symbols)
        {
            int? symbolId = FindSymbolId(connection, symbolName);
            if (symbolId == null)
            {
                Console.WriteLine($"{symbolName}: not in this candle database");
                continue;
            }

            List<(long OpenTime, int Open, int High, int Low, int Close, double Volume)> candles =
                ReadCandles(connection, symbolId.Value);
            if (candles.Count < 800)
            {
                Console.WriteLine($"{symbolName}: only {candles.Count} candles, skipped");
                continue;
            }

            foreach (Row row in Track(candles, settings))
            {
                output.AppendLine(string.Join(",",
                    symbolName,
                    row.OpenTime.ToString(CultureInfo.InvariantCulture),
                    Format(row.Index),
                    Format(row.Width),
                    Format(row.Ratio),
                    row.Count.ToString(CultureInfo.InvariantCulture)));
                exported++;
            }
            Console.WriteLine($"{symbolName}: {candles.Count} candles fed");
        }

        File.WriteAllText(OutputPath, output.ToString());
        Console.WriteLine($"{exported} rows written to {OutputPath}");
        Assert.IsTrue(exported > 0, "nothing exported");
    }


    /// <summary>Feeds the tracker candle by candle, mirroring BandRangeTracker.Build.</summary>
    private static IEnumerable<Row> Track(
        List<(long OpenTime, int Open, int High, int Low, int Close, double Volume)> candles,
        SettingsGeneralBB settings)
    {
        BandRangeTracker tracker = new();
        IndicatorRegistry registry = new(CacheSize);
        BollingerBandsHub bands = registry.BollingerBands(settings.Length, settings.Deviation);

        List<Row> rows = [];
        foreach (var source in candles)
        {
            CryptoCandle candle = new()
            {
                OpenTime = new CandleTime((uint)source.OpenTime),
                TickDecimals = 0,             // prices are the raw tick counts
            };
            candle.Open = source.Open;
            candle.High = source.High;
            candle.Low = source.Low;
            candle.Close = source.Close;
            candle.Volume = (decimal)source.Volume;

            registry.QuoteHub.Add(new Quote(candle.Timestamp, candle.Open, candle.High,
                                            candle.Low, candle.Close, candle.Volume));

            var results = bands.Results;
            if (results.Count == 0)
                continue;
            var last = results[^1];
            if (last.Sma == null || last.UpperBand == null || last.LowerBand == null)
                continue;

            tracker.Add(candle, last.Sma.Value, last.UpperBand.Value, last.LowerBand.Value);
            rows.Add(new Row(source.OpenTime, tracker.Index, tracker.MedianWidth, tracker.Ratio,
                             tracker.MeasurementCount));
        }
        return rows;
    }


    private static int? FindSymbolId(SqliteConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SymbolId FROM Symbol WHERE ExchangeName = $name COLLATE NOCASE";
        command.Parameters.AddWithValue("$name", name);
        object? result = command.ExecuteScalar();
        return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
    }


    private static List<(long, int, int, int, int, double)> ReadCandles(SqliteConnection connection, int symbolId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT OpenTime, Open, High, Low, Close, Volume FROM Candle " +
            "WHERE SymbolId = $SymbolId AND IntervalId = $IntervalId ORDER BY OpenTime";
        command.Parameters.AddWithValue("$SymbolId", symbolId);
        command.Parameters.AddWithValue("$IntervalId", IntervalId);

        List<(long, int, int, int, int, double)> candles = [];
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            candles.Add((reader.GetInt64(0), reader.GetInt32(1), reader.GetInt32(2),
                         reader.GetInt32(3), reader.GetInt32(4), reader.GetDouble(5)));
        }
        return candles;
    }


    private static string Format(double? value) =>
        value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : "";
}

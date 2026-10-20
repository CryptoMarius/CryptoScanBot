using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Emulator;
using CryptoScanner.Emulator.Engine;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CandleSync;

/// <summary>
/// Brings the emulator's candle database of one exchange up to date, without opening an emulator:
/// <code>
/// CryptoScanBot.CandleSync.exe --exchange "Binance Perpetual" --candles E:\CryptoScanBot\Data\Emulator
///                              --from 2026-01-01 [--scanner E:\CryptoScanBot\Data\Binance\Perpetual]
/// </code>
/// <list type="number">
/// <item>The symbols are fetched from the exchange, in a work folder of its own
/// (&lt;candles&gt;\CandleSync\&lt;exchange&gt;), so no emulator or scanner folder is touched.</item>
/// <item>With --scanner: the newest 1m candles of that scanner are copied first, only what lies
/// after the last 1m candle the emulator database already has. A running scanner keeps them up to
/// date anyway, so this saves the exchange the work.</item>
/// <item>Then what is still missing comes from the exchange, up to now, and every higher interval
/// is derived from 1m - the emulator's own routine (<see cref="EmulatorCandleSync"/>), the same as
/// the "Update candles" button.</item>
/// </list>
/// The symbols are the ones the emulator database already holds plus the ones the scanner has
/// candles of. On HyperLiquid that is also what keeps the history whole: the exchange serves only its
/// newest 5000 candles per interval (3.5 days of 1m), so run this at least every three days.
/// </summary>
internal static class Program
{
    private static readonly Regex SymbolProgress = new(@"(\d+)/(\d+)", RegexOptions.Compiled);
    private static bool _lineOpen;
    private static Dictionary<string, string> Options = [];


    public static async Task<int> Main(string[] args)
    {
        // The status texts carry an ellipsis and arrows
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Options = ParseArguments(args);
        if (!Options.TryGetValue("exchange", out string? exchangeName) || !Options.TryGetValue("candles", out string? candleFolder)
            || !Options.TryGetValue("from", out string? fromText)
            || !DateTime.TryParseExact(fromText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime fromDate))
        {
            Console.WriteLine("Usage: CryptoScanBot.CandleSync --exchange \"Binance Perpetual\" --candles <folder> --from yyyy-mm-dd [--scanner <folder>]");
            Console.WriteLine("  --exchange  the exchange, under the name the scanner uses");
            Console.WriteLine("  --candles   the folder with the emulator candle databases (<exchange>.db)");
            Console.WriteLine("  --from      the first day the emulator needs");
            Console.WriteLine("  --scanner   optional: the data folder of a scanner of this exchange, its newest candles are copied first");
            Console.WriteLine("  --symbols   optional, for a trial: only these symbols (comma separated scanner names)");
            return 2;
        }
        Options.TryGetValue("scanner", out string? scannerFolder);

        // The scanner's own command line parser is never fed these arguments (it does not know them)
        ApplicationParams.Options = new ApplicationParams();

        // The same process setup as the emulator's Program.Main and App.OnFrameworkInitializationCompleted
        ExchangeProvider.Register();
        GlobalData.IsEmulatorMode = true;
        GlobalData.Clock = new EmulatorClock { UtcNow = DateTime.UtcNow };
        var assembly = Assembly.GetExecutingAssembly().GetName();
        GlobalData.AppVersion = assembly.Version!.ToString();
        GlobalData.AppPath = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        GlobalData.Services = EmulatorServices.Build();
        Analyzers.AnalyzerRegistration.RegisterAll();

        // Ctrl+C finishes the symbol in progress (it is saved) and then stops; a second one ends at once
        using CancellationTokenSource cancel = new();
        Console.CancelKeyPress += (_, e) =>
        {
            if (cancel.IsCancellationRequested)
                return;
            e.Cancel = true;
            cancel.Cancel();
            WriteLine("Stopping after the symbol in progress (Ctrl+C again to stop at once)...");
        };

        // Lines that matter go on their own line; the rest of the fetch log stays in the log file
        GlobalData.LogToLogTabEvent += text =>
        {
            if (text.Contains("FAILED") || text.Contains("ERROR"))
                WriteLine("    " + text);
        };

        try
        {
            return await RunAsync(exchangeName, Path.GetFullPath(candleFolder), fromDate, scannerFolder, cancel.Token);
        }
        catch (Exception ex)
        {
            WriteLine($"FAILED: {ex.Message}");
            GlobalData.AddErrorToLogTab($"CandleSync: FAILED - {ex}");
            return 1;
        }
        finally
        {
            DataFolderLock.Release();
        }
    }


    private static async Task<int> RunAsync(string exchangeName, string candleFolder, DateTime fromDate, string? scannerFolder,
        CancellationToken cancellationToken)
    {
        Stopwatch watch = Stopwatch.StartNew();
        WriteLine($"CryptoScanBot.CandleSync {GlobalData.AppVersion} - {exchangeName}, candles in {candleFolder}, from {fromDate:yyyy-MM-dd}" +
            (string.IsNullOrEmpty(scannerFolder) ? ", no scanner" : $", scanner {scannerFolder}"));

        // A work folder of its own for the symbol list and the settings: never an emulator's or a
        // scanner's data folder, those are in use by their own program. Claimed, so two syncs of the
        // same exchange cannot run at the same time.
        string workFolder = Path.Combine(candleFolder, "CandleSync", exchangeName);
        Directory.CreateDirectory(workFolder);
        if (!DataFolderLock.TryAcquire(workFolder))
        {
            WriteLine($"SKIPPED: a sync of {exchangeName} is already running ({DataFolderLock.HolderDescription})");
            return 1;
        }

        // The scanner's settings (quotes, products) decide which symbols are brought along. Copied on
        // every run, so a product or quote switched on or off in the scanner counts the next day.
        string? scannerCandleDb = null;
        if (!string.IsNullOrEmpty(scannerFolder))
        {
            string settings = Path.Combine(scannerFolder, "CryptoScanBot-settings.json");
            string workSettings = Path.Combine(workFolder, "CryptoScanBot-settings.json");
            if (File.Exists(settings))
                File.Copy(settings, workSettings, true);
            scannerCandleDb = Path.Combine(scannerFolder, exchangeName + ".db");
            if (!File.Exists(scannerCandleDb))
            {
                WriteLine($"No candle database of the scanner at {scannerCandleDb}, everything comes from the exchange");
                scannerCandleDb = null;
            }
        }

        // The same order as the emulator's App.OnSetupClosed
        GlobalData.AppDataFolder = workFolder;
        GlobalData.CandleDataFolder = candleFolder;
        ScannerLog.InitializeLogging(true);
        GlobalData.AddTextToLogTab($"CandleSync: {exchangeName}, candles {candleFolder}, from {fromDate:yyyy-MM-dd}, scanner {scannerFolder}");

        Status($"starting {exchangeName}...");
        await EmulatorBootstrap.InitializeAsync(exchangeName);
        if (GlobalData.ActiveExchange?.Name != exchangeName)
        {
            WriteLine($"FAILED: exchange \"{exchangeName}\" is not known (or switched off)");
            return 1;
        }

        await EmulatorCandleSync.FetchSymbolsAsync(Status);

        // Which symbols: what the emulator database holds plus what the scanner has candles of
        List<CryptoSymbol> symbols = CollectSymbols(scannerCandleDb, out int unknown);

        // --symbols narrows it down (for a trial on a few coins)
        if (Options.TryGetValue("symbols", out string? only))
        {
            HashSet<string> wanted = new(only.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);
            symbols = symbols.Where(s => wanted.Contains(s.Name)).ToList();
        }
        WriteLine($"{symbols.Count} symbol(s) to bring up to date" +
            (unknown > 0 ? $" ({unknown} in the databases are left out: delisted, or a product or quote that is switched off)" : ""));
        if (symbols.Count == 0)
        {
            WriteLine("Nothing to do: the emulator database is empty and there is no scanner to take the symbols from");
            return 0;
        }

        if (scannerCandleDb != null)
            CopyFromScanner(scannerCandleDb, symbols, fromDate);

        CandleFetchResult result = await EmulatorCandleSync.FetchCandlesForSymbolsAsync(
            symbols.Select(s => s.Name).ToList(), fromDate, DateTime.UtcNow, true, Status, cancellationToken);
        string elapsed = watch.Elapsed.ToString(@"hh\:mm\:ss");
        if (result.Error != null)
        {
            WriteLine($"FAILED after {elapsed}: {result.Error}");
            return 1;
        }
        WriteLine($"{exchangeName} {(result.Cancelled ? "stopped" : "done")}: {result.Done}/{result.Total} symbol(s) up to now in {elapsed}");
        return 0;
    }


    /// <summary>
    /// The symbols of the emulator candle database and of the scanner's, as the exchange knows them
    /// now. The databases are keyed on the exchange instrument (Symbol.ExchangeName), so that is what
    /// is matched; a symbol the exchange no longer lists is counted in <paramref name="unknown"/>.
    /// </summary>
    private static List<CryptoSymbol> CollectSymbols(string? scannerCandleDb, out int unknown)
    {
        HashSet<string> instruments = [];
        using (var candleDb = new CandleDatabase(GlobalData.ActiveExchange!))
        {
            candleDb.Open();
            ReadInstruments(candleDb.Connection, instruments);
        }
        if (scannerCandleDb != null)
        {
            using var scanner = OpenReadOnly(scannerCandleDb);
            ReadInstruments(scanner, instruments);
        }

        // Only what the settings let the scanner work with: an active product (HyperLiquid's PERP,
        // not its switched off deployers) and a quote with FetchCandles on. The scanner's database
        // still holds candles of markets that were switched off later; those are not brought along.
        Dictionary<string, CryptoSymbol> byInstrument = [];
        foreach (CryptoSymbol symbol in GlobalData.ActiveExchange!.SymbolListName.Values)
        {
            if (symbol.Status != 1 || string.IsNullOrEmpty(symbol.ExchangeName))
                continue;
            if (!GlobalData.IsProductActive(symbol.Product))
                continue;
            if (!GlobalData.Settings.QuoteCoins.TryGetValue(symbol.Quote, out var quoteData) || !quoteData.FetchCandles)
                continue;
            byInstrument.TryAdd(symbol.ExchangeName, symbol);
        }

        List<CryptoSymbol> result = [];
        unknown = 0;
        foreach (string instrument in instruments.Order())
        {
            if (byInstrument.TryGetValue(instrument, out CryptoSymbol? symbol))
                result.Add(symbol);
            else
                unknown++;
        }
        return result;
    }


    private static void ReadInstruments(SqliteConnection connection, HashSet<string> instruments)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT ExchangeName FROM Symbol WHERE ExchangeName IS NOT NULL";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            instruments.Add(reader.GetString(0));
    }


    private static SqliteConnection OpenReadOnly(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        return connection;
    }


    /// <summary>
    /// Copies the scanner's 1m candles that lie after the last 1m candle the emulator database has of
    /// a symbol (and not before <paramref name="fromDate"/>). Only that tail: older candles in the
    /// emulator database are never touched, and the higher intervals are derived by the fetch that
    /// follows, from the same 1m. The scanner's candles come from the exchange itself, so they are
    /// stored as they are - the same rows the emulator's own fetch would have written.
    /// </summary>
    private static void CopyFromScanner(string scannerCandleDb, List<CryptoSymbol> symbols, DateTime fromDate)
    {
        long fromMinutes = CandleTime.AlignFromDateTime(fromDate, 1).Minutes;
        long copiedTotal = 0;
        int copiedSymbols = 0;

        using var scanner = OpenReadOnly(scannerCandleDb);
        using var candleDb = new CandleDatabase(GlobalData.ActiveExchange!);
        candleDb.Open();
        SqliteConnection target = candleDb.Connection;

        int number = 0;
        foreach (CryptoSymbol symbol in symbols)
        {
            number++;
            Status($"Copying from the scanner {symbol.Name} ({number}/{symbols.Count})…");

            long? scannerId = Scalar(scanner, "SELECT SymbolId FROM Symbol WHERE ExchangeName = $Key", symbol.ExchangeName);
            if (scannerId == null)
                continue;

            // The symbol's row in the emulator database, registered the way CandleDatabase does it
            using (var register = target.CreateCommand())
            {
                register.CommandText = "INSERT OR IGNORE INTO Symbol (ExchangeName, Name) VALUES ($Key, $Name)";
                register.Parameters.AddWithValue("$Key", symbol.ExchangeName);
                register.Parameters.AddWithValue("$Name", symbol.Name);
                register.ExecuteNonQuery();
            }
            long targetId = Scalar(target, "SELECT SymbolId FROM Symbol WHERE ExchangeName = $Key", symbol.ExchangeName)!.Value;

            long? last;
            using (var max = target.CreateCommand())
            {
                max.CommandText = "SELECT MAX(OpenTime) FROM Candle WHERE SymbolId = $Id AND IntervalId = 1";
                max.Parameters.AddWithValue("$Id", targetId);
                object? value = max.ExecuteScalar();
                last = value is long l ? l : null;
            }
            long after = Math.Max(last ?? fromMinutes - 1, fromMinutes - 1);

            using var read = scanner.CreateCommand();
            read.CommandText = "SELECT OpenTime, Ticks, Open, High, Low, Close, Volume FROM Candle " +
                "WHERE SymbolId = $Id AND IntervalId = 1 AND OpenTime > $After ORDER BY OpenTime";
            read.Parameters.AddWithValue("$Id", scannerId.Value);
            read.Parameters.AddWithValue("$After", after);

            using var tx = target.BeginTransaction();
            using var write = target.CreateCommand();
            write.Transaction = tx;
            write.CommandText = "INSERT OR IGNORE INTO Candle (SymbolId, IntervalId, OpenTime, Ticks, Open, High, Low, Close, Volume) " +
                "VALUES ($Id, 1, $T, $Ticks, $O, $H, $L, $C, $V)";
            var pId = write.Parameters.Add("$Id", SqliteType.Integer);
            var pT = write.Parameters.Add("$T", SqliteType.Integer);
            var pTicks = write.Parameters.Add("$Ticks", SqliteType.Integer);
            var pO = write.Parameters.Add("$O", SqliteType.Integer);
            var pH = write.Parameters.Add("$H", SqliteType.Integer);
            var pL = write.Parameters.Add("$L", SqliteType.Integer);
            var pC = write.Parameters.Add("$C", SqliteType.Integer);
            var pV = write.Parameters.Add("$V", SqliteType.Real);
            pId.Value = targetId;

            int copied = 0;
            using (var reader = read.ExecuteReader())
            {
                while (reader.Read())
                {
                    pT.Value = reader.GetInt64(0);
                    pTicks.Value = reader.GetInt64(1);
                    pO.Value = reader.GetInt64(2);
                    pH.Value = reader.GetInt64(3);
                    pL.Value = reader.GetInt64(4);
                    pC.Value = reader.GetInt64(5);
                    pV.Value = reader.GetDouble(6);
                    copied += write.ExecuteNonQuery();
                }
            }
            tx.Commit();

            if (copied > 0)
            {
                copiedSymbols++;
                copiedTotal += copied;
            }
        }

        WriteLine($"Copied from the scanner: {copiedTotal:N0} candle(s) of 1m for {copiedSymbols} symbol(s)");
        GlobalData.AddTextToLogTab($"CandleSync: copied {copiedTotal} 1m candles of {copiedSymbols} symbols from {scannerCandleDb}");
    }


    private static long? Scalar(SqliteConnection connection, string sql, string key)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$Key", key);
        object? value = cmd.ExecuteScalar();
        return value is long l ? l : null;
    }


    /// <summary>--name value pairs; a name without a value is ignored.</summary>
    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].StartsWith("--"))
            {
                options[args[i][2..]] = args[i + 1].Trim().Trim('"');
                i++;
            }
        }
        return options;
    }


    /// <summary>
    /// The status of the fetch, on one line that is rewritten in place, with a bar for the symbols
    /// done. Redirected output (a log file of the task scheduler) gets plain lines.
    /// </summary>
    private static void Status(string text)
    {
        string bar = "";
        Match match = SymbolProgress.Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int done) && int.TryParse(match.Groups[2].Value, out int total) && total > 0)
        {
            int filled = Math.Min(20, 20 * done / total);
            bar = $"[{new string('#', filled)}{new string('-', 20 - filled)}] ";
        }

        string line = bar + text;
        lock (SymbolProgress)
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} {line}");
                return;
            }
            int width = ConsoleWidth();
            if (line.Length > width - 1)
                line = line[..(width - 1)];
            Console.Write("\r" + line.PadRight(width - 1));
            _lineOpen = true;
        }
    }


    /// <summary>A line that stays, written below the status line.</summary>
    private static void WriteLine(string text)
    {
        lock (SymbolProgress)
        {
            if (_lineOpen && !Console.IsOutputRedirected)
                Console.Write("\r" + new string(' ', ConsoleWidth() - 1) + "\r");
            _lineOpen = false;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} {text}");
        }
    }


    private static int ConsoleWidth()
    {
        try
        {
            return Math.Max(40, Console.WindowWidth);
        }
        catch
        {
            return 120;
        }
    }
}

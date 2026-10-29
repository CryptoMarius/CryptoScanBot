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
/// The symbols are the ones the emulator database already holds 1m of, plus every symbol of the
/// exchange whose daily volume reached --min-volume (default 250,000 in the quote) on at least one day
/// since --from. So a missing database is created and filled without a scanner, and a coin that was
/// liquid in January but is quiet now is not left out. On HyperLiquid it is also what keeps the history
/// whole: the exchange serves only its newest 5000 candles per interval (3.5 days of 1m), so run this
/// at least every three days.
/// </summary>
internal static class Program
{
    private static readonly Regex SymbolProgress = new(@"(\d+)/(\d+)", RegexOptions.Compiled);
    private static bool _lineOpen;
    // "HyperLiquid.Spot " in front of every console line: the batch file runs one exchange after another
    private static string _exchangeLabel = "";
    private static Dictionary<string, string> Options = [];

    /// <summary>The daily volume a symbol must reach on one day to be brought along (--min-volume)</summary>
    private const decimal DefaultMinimalDailyVolume = 250_000;
    private static decimal MinimalDailyVolume = DefaultMinimalDailyVolume;


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
            Console.WriteLine("  --min-volume optional: the daily volume (in the quote) a new symbol must reach on one day, default 250000");
            return 2;
        }
        Options.TryGetValue("scanner", out string? scannerFolder);
        MinimalDailyVolume = DefaultMinimalDailyVolume;
        if (Options.TryGetValue("min-volume", out string? volumeText))
        {
            if (!decimal.TryParse(volumeText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal volume) || volume < 0)
            {
                Console.WriteLine($"--min-volume {volumeText} is not a number");
                return 2;
            }
            MinimalDailyVolume = volume;
        }

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
            CountProgress(text);
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
        _exchangeLabel = exchangeName.Replace(' ', '.') + " ";
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

        // Without a scanner there are no settings to copy, and in the fresh ones no quote fetches
        // candles, so not a single symbol would pass. The scanner switches the exchange's default quote
        // on at its start (ScannerSession); the emulator's bootstrap does not, so it is done here. Not
        // on "the quote is unknown" as the scanner tests: loading the symbols already adds every quote
        // of the exchange, switched off. Only when no quote at all fetches candles, so settings copied
        // from a scanner are left as they are.
        string? defaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
        if (!string.IsNullOrEmpty(defaultQuote) && !GlobalData.Settings.QuoteCoins.Values.Any(quote => quote.FetchCandles))
        {
            CryptoQuoteData defaultQuoteData = GlobalData.AddQuoteData(defaultQuote);
            defaultQuoteData.FetchCandles = true;
            defaultQuoteData.MinimalVolume = ExchangeBase.ExchangeOptions.MinimalVolume;
            WriteLine($"No quote in the settings: the exchange's default quote {defaultQuote} is used");
        }

        await EmulatorCandleSync.FetchSymbolsAsync(Status);

        // Which symbols: what the emulator database holds 1m of, and of the others of the exchange the
        // ones whose daily volume reached the minimum on at least one day of the period
        List<CryptoSymbol> symbols = CollectSymbols(out List<CryptoSymbol> candidates, out int unknown);

        // --symbols narrows it down (for a trial on a few coins), before the volume check asks the
        // exchange anything
        if (Options.TryGetValue("symbols", out string? only))
        {
            HashSet<string> wanted = new(only.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);
            symbols = symbols.Where(s => wanted.Contains(s.Name)).ToList();
            candidates = candidates.Where(s => wanted.Contains(s.Name)).ToList();
        }
        int present = symbols.Count;
        List<CryptoSymbol> added = await SelectByDailyVolumeAsync(candidates, fromDate, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            WriteLine($"{exchangeName} stopped during the volume check");
            return 0;
        }
        symbols.AddRange(added);
        WriteLine($"{symbols.Count} symbol(s) to bring up to date: {present} already in the emulator database, " +
            $"{added.Count} of {candidates.Count} others reached {MinimalDailyVolume:N0} on at least one day since {fromDate:yyyy-MM-dd}" +
            (unknown > 0 ? $" ({unknown} in the database are left out: delisted, or a product or quote that is switched off)" : ""));
        if (symbols.Count == 0)
        {
            WriteLine("Nothing to do: no symbol in the emulator database and none reached the minimum volume");
            return 0;
        }

        Dictionary<string, long> firstCopied = scannerCandleDb != null
            ? CopyFromScanner(scannerCandleDb, symbols, fromDate)
            : [];

        // A connection of its own to measure, per symbol, what really came in (see CountProgress),
        // and to keep CandleSyncState
        using var countDb = new CandleDatabase(GlobalData.ActiveExchange!);
        countDb.Open();
        _countConnection = countDb.Connection;

        // Where the derivation of the higher intervals starts, per symbol. Until 25-10-2026 every
        // LastCandleSynchronized was cleared here, so that 1m arriving later in older history (Altrady's
        // fill, a scanner copy) would be turned into higher intervals as well - and every run read the
        // whole year from disk and derived it again, ~12 s per symbol. The stored pointers cannot be
        // used instead: for a symbol listed after the start of the window they stay at that start for
        // good, because the walk forward stops at the first candle that does not exist.
        //
        // CandleSyncState says how far the previous run derived (DerivedUpTo) and from where
        // altrady_candles.py added 1m since (DeriveFrom). The earliest of those two and the first
        // minute copied from the scanner, back to the start of its week (the largest interval needs
        // its whole week of 1m), is where this run starts. No row: everything from the start date.
        DateTime fetchUntil = DateTime.UtcNow;
        Dictionary<string, (long? DerivedUpTo, long? DeriveFrom)> state = ReadSyncState(countDb.Connection);
        CryptoInterval largest = GlobalData.IntervalList[^1];
        int fromScratch = 0;
        foreach (CryptoSymbol symbol in symbols)
        {
            CandleTime? deriveFrom = null;
            if (state.TryGetValue(symbol.ExchangeName, out var row) && row.DerivedUpTo.HasValue)
            {
                long minutes = row.DerivedUpTo.Value;
                if (row.DeriveFrom.HasValue && row.DeriveFrom.Value < minutes)
                    minutes = row.DeriveFrom.Value;
                if (firstCopied.TryGetValue(symbol.ExchangeName, out long copied) && copied < minutes)
                    minutes = copied;
                deriveFrom = IntervalTools.StartOfIntervalCandle(new CandleTime((uint)minutes), largest.Duration);
            }
            else
                fromScratch++;

            foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
                symbolInterval.LastCandleSynchronized = deriveFrom;
        }
        if (fromScratch > 0)
            WriteLine($"{fromScratch} symbol(s) without an earlier sync: derived from {fromDate:yyyy-MM-dd} on, once");

        CandleFetchResult result = await EmulatorCandleSync.FetchCandlesForSymbolsAsync(
            symbols.Select(s => s.Name).ToList(), fromDate, fetchUntil, true, Status, cancellationToken,
            oneMinuteOnly: true, symbolSaved: symbol => WriteSyncState(countDb.Connection, symbol, fetchUntil));
        string elapsed = watch.Elapsed.ToString(@"hh\:mm\:ss");
        if (result.Error != null)
        {
            WriteLine($"FAILED after {elapsed}: {result.Error}");
            return 1;
        }
        WriteLine($"{exchangeName} {(result.Cancelled ? "stopped" : "done")}: {result.Done}/{result.Total} symbol(s) up to now in {elapsed}");

        // Once a week the candle file is rewritten in key order (see CandleDatabase.VacuumIfDue).
        // The vacuum needs the file to itself, so the counting connection goes first.
        if (!result.Cancelled)
        {
            countDb.Close();
            _countConnection = null;
            string? line = CandleDatabase.VacuumIfDue(GlobalData.ActiveExchange!, CandleDatabase.VacuumInterval);
            if (line != null)
                WriteLine(line);
        }
        return 0;
    }


    /// <summary>
    /// The symbols the emulator candle database holds 1m of, as the exchange knows them now, and in
    /// <paramref name="candidates"/> the other symbols the settings allow. The database is keyed on the
    /// exchange instrument (Symbol.ExchangeName), so that is what is matched; a symbol the exchange no
    /// longer lists is counted in <paramref name="unknown"/>. The barometer symbols are left out without
    /// being counted: they are in the database, but the emulator measures their candles itself.
    /// <para>
    /// Until 25-10-2026 the Symbol table of the scanner's candle database was read here as well. That
    /// table holds nearly every market the exchange lists (Bybit Perpetual 815, of which the scanner had
    /// 1m of 135), so a new emulator database would have been filled with all of them, and without a
    /// scanner it stayed empty. The Symbol table of the emulator database is no measure either: it
    /// holds every symbol the emulator ever registered (Binance Perpetual 922, with 1m of 534).
    /// </para>
    /// </summary>
    private static List<CryptoSymbol> CollectSymbols(out List<CryptoSymbol> candidates, out int unknown)
    {
        HashSet<string> instruments = [];
        using (var candleDb = new CandleDatabase(GlobalData.ActiveExchange!))
        {
            candleDb.Open();
            ReadInstruments(candleDb.Connection, instruments);
        }

        // Only what the settings let the scanner work with: an active product (HyperLiquid's PERP,
        // not its switched off deployers) and a quote with FetchCandles on. The scanner's database
        // still holds candles of markets that were switched off later; those are not brought along.
        Dictionary<string, CryptoSymbol> byInstrument = [];
        HashSet<string> barometers = [];
        foreach (CryptoSymbol symbol in GlobalData.ActiveExchange!.SymbolListName.Values)
        {
            if (string.IsNullOrEmpty(symbol.ExchangeName))
                continue;
            // $BMP, $BMX and $BMS are no instruments of the exchange: the emulator measures their
            // candles itself (BarometerReplay). Until 25-10-2026 they were fetched like a coin, which
            // failed with "Symbol not found", after which the derivation of the higher intervals
            // overwrote the stored barometer series at every whole 30m, 1h, 4h and 1d.
            if (symbol.IsBarometerSymbol())
            {
                barometers.Add(symbol.ExchangeName);
                continue;
            }
            if (symbol.Status != 1)
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
            else if (!barometers.Contains(instrument))
                unknown++;
        }
        candidates = byInstrument.Where(pair => !instruments.Contains(pair.Key)).OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
        return result;
    }


    /// <summary>
    /// The symbols of <paramref name="candidates"/> that traded at least <see cref="MinimalDailyVolume"/>
    /// on one day from <paramref name="fromDate"/> up to now. Today's ticker counts first (no request);
    /// otherwise the daily candles of the period are asked, one or two requests per symbol, and the
    /// highest daily volume decides. Those candles are only looked at: they are dropped again, the sync
    /// derives 1d from 1m like every other interval.
    /// <para>
    /// The daily candle carries the quote volume, the same unit as the ticker (see EmulatorVolume),
    /// and the same value the emulator compares with MinimalVolume per replayed day. A symbol that
    /// never reached the minimum on any day could never take part in a run with that minimum either.
    /// Symbols the exchange has delisted are not in its list and cannot be found this way.
    /// </para>
    /// </summary>
    private static async Task<List<CryptoSymbol>> SelectByDailyVolumeAsync(List<CryptoSymbol> candidates, DateTime fromDate,
        CancellationToken cancellationToken)
    {
        List<CryptoSymbol> result = [];
        if (candidates.Count == 0)
            return result;

        CryptoInterval interval1d = GlobalData.IntervalListPeriod[Core.Enums.CryptoIntervalPeriod.interval1d];
        bool dailySupported = GlobalData.ActiveExchange!.IsIntervalSupported(interval1d.IntervalPeriod);
        CandleTime from = IntervalTools.StartOfIntervalCandle(CandleTime.AlignFromDateTime(fromDate, interval1d.Duration), interval1d.Duration);
        // Only whole days; today is covered by the ticker
        CandleTime until = IntervalTools.StartOfIntervalCandle(CandleTime.AlignFromDateTime(DateTime.UtcNow, interval1d.Duration), interval1d.Duration);
        if (!dailySupported)
            WriteLine($"{GlobalData.ActiveExchange!.Name} serves no daily candles: the new symbols are chosen on today's ticker volume only");

        int number = 0;
        int byTicker = 0;
        foreach (CryptoSymbol symbol in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            number++;

            if ((decimal)symbol.Volume >= MinimalDailyVolume)
            {
                byTicker++;
                result.Add(symbol);
                continue;
            }
            if (!dailySupported)
                continue;

            Status($"Checking the daily volume of {symbol.Name} ({number}/{candidates.Count}), {result.Count} reached {MinimalDailyVolume:N0} so far…");
            try
            {
                await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval1d, from, until);
                decimal highest = 0;
                foreach (CryptoCandle candle in symbol.GetSymbolInterval(interval1d.IntervalPeriod).CandleList.Values)
                {
                    if (candle.OpenTime >= from && candle.Volume > highest)
                        highest = candle.Volume;
                }
                if (highest >= MinimalDailyVolume)
                    result.Add(symbol);
                GlobalData.AddTextToLogTab($"CandleSync: volume check {symbol.Name} highest daily volume {highest:N0}" +
                    (highest >= MinimalDailyVolume ? " - taken" : ""));
            }
            catch (Exception ex)
            {
                GlobalData.AddTextToLogTab($"CandleSync: volume check {symbol.Name} FAILED ({ex.Message})");
            }
            finally
            {
                symbol.ClearCandles();
            }
        }
        GlobalData.AddTextToLogTab($"CandleSync: volume check {number}/{candidates.Count} symbol(s), {result.Count} taken, {byTicker} of them on the ticker");
        return result;
    }


    /// <summary>
    /// The table CandleSync and altrady_candles.py share, per symbol: up to where the higher intervals
    /// were derived (DerivedUpTo, written here) and from where 1m was added since (DeriveFrom, written
    /// by altrady_candles.py and cleared here). Both in minutes, like Candle.OpenTime. No foreign key:
    /// the emulator's own tables never notice this one.
    /// </summary>
    private const string SyncStateTable =
        "CREATE TABLE IF NOT EXISTS CandleSyncState (SymbolId INTEGER PRIMARY KEY, DerivedUpTo INTEGER, DeriveFrom INTEGER)";


    private static Dictionary<string, (long? DerivedUpTo, long? DeriveFrom)> ReadSyncState(SqliteConnection connection)
    {
        using (var create = connection.CreateCommand())
        {
            create.CommandText = SyncStateTable;
            create.ExecuteNonQuery();
        }

        Dictionary<string, (long?, long?)> state = [];
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT s.ExchangeName, c.DerivedUpTo, c.DeriveFrom FROM CandleSyncState c " +
            "JOIN Symbol s ON s.SymbolId = c.SymbolId WHERE s.ExchangeName IS NOT NULL";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            state[reader.GetString(0)] = (reader.IsDBNull(1) ? null : reader.GetInt64(1), reader.IsDBNull(2) ? null : reader.GetInt64(2));
        return state;
    }


    /// <summary>The symbol is derived up to <paramref name="fetchUntil"/>; what altrady_candles.py added is done.</summary>
    private static void WriteSyncState(SqliteConnection connection, CryptoSymbol symbol, DateTime fetchUntil)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO CandleSyncState (SymbolId, DerivedUpTo, DeriveFrom) " +
            "SELECT SymbolId, $Until, NULL FROM Symbol WHERE ExchangeName = $Key " +
            "ON CONFLICT(SymbolId) DO UPDATE SET DerivedUpTo = excluded.DerivedUpTo, DeriveFrom = NULL";
        cmd.Parameters.AddWithValue("$Until", (long)CandleTime.AlignFromDateTime(fetchUntil, 1).Minutes);
        cmd.Parameters.AddWithValue("$Key", symbol.ExchangeName);
        cmd.ExecuteNonQuery();
    }


    private static void ReadInstruments(SqliteConnection connection, HashSet<string> instruments)
    {
        // Only the symbols with at least one 1m candle (the primary key makes the EXISTS a single seek)
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT s.ExchangeName FROM Symbol s WHERE s.ExchangeName IS NOT NULL " +
            "AND EXISTS (SELECT 1 FROM Candle c WHERE c.SymbolId = s.SymbolId AND c.IntervalId = $IntervalId)";
        cmd.Parameters.AddWithValue("$IntervalId", GlobalData.IntervalList[0].Id);
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
    /// Returns per instrument the first minute that was copied, so the derivation starts there.
    /// </summary>
    private static Dictionary<string, long> CopyFromScanner(string scannerCandleDb, List<CryptoSymbol> symbols, DateTime fromDate)
    {
        long fromMinutes = CandleTime.AlignFromDateTime(fromDate, 1).Minutes;
        long copiedTotal = 0;
        int copiedSymbols = 0;
        Dictionary<string, long> firstCopied = [];

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
                    int inserted = write.ExecuteNonQuery();
                    if (inserted > 0 && copied == 0)
                        firstCopied[symbol.ExchangeName] = reader.GetInt64(0);
                    copied += inserted;
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
        return firstCopied;
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


    // Progress of the fetch, kept from the status texts and the log lines of EmulatorCandleSync. The
    // console then looks like altrady_candles.py: a bar that moves, and a line per symbol that stays.
    private static readonly Regex ApiRequest = new(@" fetch from .* received: (\d+)", RegexOptions.Compiled);
    private static readonly Regex ArchiveFile = new(@" archive \S+ \(\d+/\d+\) (\d+) candles", RegexOptions.Compiled);
    private static readonly Regex SymbolInParentheses = new(@"\s*\(\d+/\d+\)", RegexOptions.Compiled);
    private static readonly Regex SymbolDone = new(@"^Fetch candles: (\S+) — done", RegexOptions.Compiled);
    private static int _current;
    private static int _total;
    private static int _requests;
    private static int _symbolRequests;
    private static long _symbolCandles;
    private static Stopwatch? _fetchWatch;
    private static SqliteConnection? _countConnection;
    private static long _symbolBefore;


    /// <summary>All candles of a symbol in the emulator candle database, every interval.</summary>
    private static long CountSymbol(string symbolName)
    {
        if (_countConnection == null || GlobalData.ActiveExchange == null
            || !GlobalData.ActiveExchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            return 0;
        using var cmd = _countConnection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Candle WHERE SymbolId = (SELECT SymbolId FROM Symbol WHERE ExchangeName = $Key)";
        cmd.Parameters.AddWithValue("$Key", symbol.ExchangeName);
        return cmd.ExecuteScalar() is long count ? count : 0;
    }


    /// <summary>
    /// Counts the requests (api calls and archive files) and the candles they brought, and writes the
    /// line that stays when a symbol is done - in the same form as altrady_candles.py.
    /// </summary>
    private static void CountProgress(string text)
    {
        Match request = ApiRequest.Match(text);
        if (!request.Success)
            request = ArchiveFile.Match(text);
        if (request.Success)
        {
            _requests++;
            _symbolRequests++;
            _symbolCandles += long.Parse(request.Groups[1].Value, CultureInfo.InvariantCulture);
            return;
        }

        Match done = SymbolDone.Match(text);
        if (done.Success && _total > 0)
        {
            double hours = (_fetchWatch?.Elapsed.TotalHours) ?? 0;
            long added = CountSymbol(done.Groups[1].Value) - _symbolBefore;
            WriteLine($"{_current}/{_total} {done.Groups[1].Value}: {_symbolRequests} requests to the exchange, {_symbolCandles:N0} candles received, " +
                $"{added:N0} added (all intervals, the derived ones included); " +
                $"{100.0 * _current / _total:0.0}% done after {hours:0.0} h");
            _symbolRequests = 0;
            _symbolCandles = 0;
        }
    }


    /// <summary>
    /// The status of the fetch, on one line that is rewritten in place: a bar over all symbols, the
    /// percentage, the symbol and what happens to it, the requests so far and the time left - the same
    /// line altrady_candles.py shows. Redirected output (a log file of the task scheduler) gets plain lines.
    /// </summary>
    private static void Status(string text)
    {
        Match match = SymbolProgress.Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int current) && int.TryParse(match.Groups[2].Value, out int total) && total > 0)
        {
            _current = current;
            _total = total;
            // The count before this symbol, to tell afterwards how many candles really came in
            if (text.StartsWith("Fetching candles for ") && _countConnection != null)
            {
                string name = text["Fetching candles for ".Length..].Split(' ')[0];
                _symbolBefore = CountSymbol(name);
            }
            if (text.StartsWith("Fetching candles") && _fetchWatch == null)
                _fetchWatch = Stopwatch.StartNew();
        }

        string line = text;
        if (match.Success && _total > 0)
        {
            double fraction = (_current - 1.0) / _total;
            int filled = Math.Clamp((int)(30 * fraction), 0, 30);
            string left = "";
            if (_fetchWatch != null && _current > 1)
                left = $"  ~{_fetchWatch.Elapsed.TotalHours / (_current - 1) * (_total - _current + 1):0.0} h left";
            // The symbol's own (i/n) is in the "coin" part already
            string what = SymbolInParentheses.Replace(text, "", 1).Replace("  ", " ").Trim();
            line = $"[{new string('#', filled)}{new string('-', 30 - filled)}] {100 * fraction,5:0.0}%  coin {_current}/{_total}  {what}{left}";
        }
        line = _exchangeLabel + line;
        lock (SymbolProgress)
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}");
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
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {_exchangeLabel}{text}");
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

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Globalization;
using System.IO.Compression;
using System.Net;

namespace CryptoScanner.Core.Exchange.Binance;

/// <summary>One kline row of an archive file.</summary>
internal readonly record struct BinanceArchiveKline(DateTime OpenTime, decimal Open, decimal High, decimal Low,
    decimal Close, decimal QuoteVolume);

/// <summary>
/// One archive file to fetch: a month or a day, [Start, End) in UTC. Recent marks a month that may
/// not be published yet, so a missing monthly file falls back to the daily files of that month.
/// </summary>
internal readonly record struct BinanceArchivePiece(bool Monthly, DateTime Start, DateTime End, bool Recent);

internal enum BinanceArchiveStatus
{
    Found,
    NotFound,
    Failed,
}

/// <summary>
/// Bulk kline history from the public Binance download archive (https://data.binance.vision).
/// <para>
/// The archive holds a zip with a csv per symbol, interval and month (and per day for the days
/// that are not in a monthly file yet). It is not part of the api, so it costs no request weight:
/// a month of 1m candles is one download of a few MB, where the api needs 45 klines calls of
/// weight 5 for it - and that weight is shared by every scanner on this address. The candles are the
/// same klines the api returns, including the quote volume the scanner stores.
/// </para>
/// <para>
/// What the archive cannot serve: today (a daily file appears the day after) and a month until the
/// first days of the next month. Those stretches are left to the api by returning how far the
/// archive got without a hole.
/// </para>
/// </summary>
public static class BinanceArchive
{
    private const string BaseUrl = "https://data.binance.vision/data/";

    /// <summary>Market path of the USD-M perpetuals in the archive.</summary>
    public const string MarketPerpetual = "futures/um";

    /// <summary>Market path of spot in the archive.</summary>
    public const string MarketSpot = "spot";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    /// <summary>
    /// Files downloaded at the same time. One connection gets about 1 MB/s from the archive, so the
    /// files of a symbol come in side by side: four monthly files of SOLUSDT took 7.2 seconds one
    /// after the other and 1.9 seconds together (measured 11-10-2026). They are still added in order.
    /// </summary>
    private const int ParallelDownloads = 6;

    /// <summary>
    /// The archive name of an interval, or null when the archive is not used for it. Limited to
    /// intervals up to a day: their candles never cross the day and month boundaries the archive
    /// files are cut on. A week starts on a Monday and would straddle two files.
    /// </summary>
    internal static string? ArchiveIntervalName(CryptoIntervalPeriod period)
    {
        return period switch
        {
            CryptoIntervalPeriod.interval1m => "1m",
            CryptoIntervalPeriod.interval3m => "3m",
            CryptoIntervalPeriod.interval5m => "5m",
            CryptoIntervalPeriod.interval15m => "15m",
            CryptoIntervalPeriod.interval30m => "30m",
            CryptoIntervalPeriod.interval1h => "1h",
            CryptoIntervalPeriod.interval2h => "2h",
            CryptoIntervalPeriod.interval4h => "4h",
            CryptoIntervalPeriod.interval6h => "6h",
            CryptoIntervalPeriod.interval8h => "8h",
            CryptoIntervalPeriod.interval12h => "12h",
            CryptoIntervalPeriod.interval1d => "1d",
            _ => null,
        };
    }


    /// <summary>
    /// The files that cover [<paramref name="from"/>, <paramref name="until"/>), in order. Only whole
    /// days before today: a month that has ended becomes one monthly file, the days of the current
    /// month each get their own. Months since the start of the previous month are marked Recent,
    /// because their monthly file may not have been published yet.
    /// </summary>
    internal static List<BinanceArchivePiece> PlanPieces(DateTime from, DateTime until, DateTime nowUtc)
    {
        List<BinanceArchivePiece> pieces = [];
        DateTime today = nowUtc.Date;
        DateTime end = until < today ? until : today;
        DateTime recentFrom = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);

        DateTime cursor = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        while (cursor < end)
        {
            DateTime monthStart = new(cursor.Year, cursor.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime monthEnd = monthStart.AddMonths(1);
            if (monthEnd <= today)
            {
                pieces.Add(new BinanceArchivePiece(true, monthStart, monthEnd, monthStart >= recentFrom));
                cursor = monthEnd;
            }
            else
            {
                // The current month: day by day, and only days that have ended (end <= today).
                if (cursor.AddDays(1) > today)
                    break;
                pieces.Add(new BinanceArchivePiece(false, cursor, cursor.AddDays(1), false));
                cursor = cursor.AddDays(1);
            }
        }
        return pieces;
    }


    /// <summary>
    /// Read the csv of an archive file. The perpetual files start with a header line, the spot files
    /// do not; and the spot files of 2025 onwards give the times in microseconds instead of
    /// milliseconds. Columns: open_time, open, high, low, close, volume, close_time, quote_volume, ...
    /// </summary>
    internal static List<BinanceArchiveKline> ParseCsv(TextReader reader)
    {
        List<BinanceArchiveKline> klines = [];
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0 || !char.IsDigit(line[0]))
                continue;

            string[] columns = line.Split(',');
            if (columns.Length < 8)
                continue;

            long openTime = long.Parse(columns[0], CultureInfo.InvariantCulture);
            // Milliseconds stay below 1e14 until the year 5138; microseconds of today are above it.
            if (openTime > 100_000_000_000_000)
                openTime /= 1000;

            klines.Add(new BinanceArchiveKline(
                DateTimeOffset.FromUnixTimeMilliseconds(openTime).UtcDateTime,
                decimal.Parse(columns[1], NumberStyles.Float, CultureInfo.InvariantCulture),
                decimal.Parse(columns[2], NumberStyles.Float, CultureInfo.InvariantCulture),
                decimal.Parse(columns[3], NumberStyles.Float, CultureInfo.InvariantCulture),
                decimal.Parse(columns[4], NumberStyles.Float, CultureInfo.InvariantCulture),
                decimal.Parse(columns[7], NumberStyles.Float, CultureInfo.InvariantCulture)));
        }
        return klines;
    }


    private static string PieceUrl(string market, string symbolName, string intervalName, BinanceArchivePiece piece)
    {
        string period = piece.Monthly ? "monthly" : "daily";
        string date = piece.Monthly
            ? piece.Start.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : piece.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{BaseUrl}{market}/{period}/klines/{symbolName}/{intervalName}/{symbolName}-{intervalName}-{date}.zip";
    }


    private static async Task<(BinanceArchiveStatus status, List<BinanceArchiveKline> klines, long bytes)> DownloadAsync(string url, string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return (BinanceArchiveStatus.NotFound, [], 0);
            if (!response.IsSuccessStatusCode)
            {
                GlobalData.AddTextToLogTab($"{prefix} archive {url} answered {(int)response.StatusCode}");
                return (BinanceArchiveStatus.Failed, [], 0);
            }

            // The zip is read from memory: ZipArchive needs a seekable stream, and the files are small.
            using MemoryStream memory = new();
            await response.Content.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            using ZipArchive zip = new(memory, ZipArchiveMode.Read);
            List<BinanceArchiveKline> klines = [];
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                if (!entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    continue;
                using StreamReader reader = new(entry.Open());
                klines.AddRange(ParseCsv(reader));
            }
            return (BinanceArchiveStatus.Found, klines, memory.Length);
        }
        catch (Exception error)
        {
            // A download that was called off (the fetch stopped early) is not worth a line
            if (!cancellationToken.IsCancellationRequested)
                GlobalData.AddTextToLogTab($"{prefix} archive {url} failed: {error.Message}");
            return (BinanceArchiveStatus.Failed, [], 0);
        }
    }


    /// <summary>
    /// <see cref="DownloadAsync"/> behind the gate that limits how many files come in at once.
    /// </summary>
    private static async Task<(BinanceArchiveStatus status, List<BinanceArchiveKline> klines, long bytes)> DownloadGatedAsync(
        string url, string prefix, SemaphoreSlim gate, CancellationToken cancellationToken)
    {
        try
        {
            await gate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return (BinanceArchiveStatus.Failed, [], 0);
        }
        try
        {
            return await DownloadAsync(url, prefix, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }


    /// <summary>
    /// One log line per archive file, so a long fetch shows where it is: which month or day, how far
    /// along the list of files, and what came in. A fetch of a symbol-year of 1m takes some twenty
    /// seconds, and without these lines the log stays silent for all of that.
    /// </summary>
    private static void LogPiece(string prefix, string intervalName, BinanceArchivePiece piece, int number, int total,
        BinanceArchiveStatus status, int candles, long bytes)
    {
        string date = piece.Monthly
            ? piece.Start.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : piece.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string result = status switch
        {
            BinanceArchiveStatus.Found => $"{candles} candles {bytes / (1024.0 * 1024.0):0.0} MB",
            BinanceArchiveStatus.NotFound => "not in archive",
            _ => "failed",
        };
        GlobalData.AddTextToLogTab($"{prefix} archive {date} ({number}/{total}) {result}");
        GlobalData.CandleFetchProgress?.Invoke($"{intervalName} archive {date} ({number}/{total})");
    }


    /// <summary>
    /// True when every candle of the part of <paramref name="piece"/> inside [<paramref name="from"/>,
    /// <paramref name="until"/>) is in the list already.
    /// <para>
    /// The emulator builds 3m up to 1d from 1m before it asks for them, but each interval starts its
    /// warm-up at its own moment before the replay window (3m some fifty minutes before 1m, 1h two
    /// months). Those first candles cannot be built, the walk stops at the first one that is missing
    /// and the whole period is handed over. The api skipped the candles that were present after one
    /// call; without this check the archive downloaded every month again - about sixty files per
    /// symbol where ten were needed (CRVUSDT and DASHUSDT, 11-10-2026).
    /// </para>
    /// </summary>
    internal static bool IsPiecePresent(CryptoCandleList candles, uint duration, BinanceArchivePiece piece,
        DateTime from, DateTime until)
    {
        DateTime start = piece.Start > from ? piece.Start : from;
        DateTime end = piece.End < until ? piece.End : until;
        CandleTime time = CandleTime.FromDateTime(start);
        CandleTime endTime = CandleTime.FromDateTime(end);
        while (time < endTime)
        {
            if (!candles.ContainsKey(time))
                return false;
            time += duration;
        }
        return true;
    }


    private static async Task<int> AddCandlesAsync(CryptoSymbol symbol, CryptoInterval interval,
        List<BinanceArchiveKline> klines, DateTime from, DateTime until)
    {
        int added = 0;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (BinanceArchiveKline kline in klines)
            {
                if (kline.OpenTime < from || kline.OpenTime >= until || kline.Close <= 0)
                    continue;
                CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                    kline.Open, kline.High, kline.Low, kline.Close, kline.QuoteVolume);
                added++;
            }
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
        return added;
    }


    /// <summary>
    /// Add the archived candles of [<paramref name="from"/>, <paramref name="until"/>) and return how
    /// far the coverage reaches without a hole; the api continues from there.
    /// <para>
    /// A missing monthly file of an older month means the symbol has no history there (not listed
    /// yet): accepted, the same way the api skips such a stretch. A missing file of a recent month or
    /// day means "not published yet", and a failed download is a failure - both end the coverage.
    /// </para>
    /// </summary>
    public static async Task<CandleTime> FetchAsync(string market, CryptoSymbol symbol, CryptoInterval interval,
        CandleTime from, CandleTime until)
    {
        string? intervalName = ArchiveIntervalName(interval.IntervalPeriod);
        if (intervalName == null)
            return from;

        DateTime fromDate = DateTime.SpecifyKind(from.ToDateTime(), DateTimeKind.Utc);
        DateTime untilDate = DateTime.SpecifyKind(until.ToDateTime(), DateTimeKind.Utc);
        List<BinanceArchivePiece> pieces = PlanPieces(fromDate, untilDate, DateTime.UtcNow);
        if (pieces.Count == 0)
            return from;

        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval.Name}";
        DateTime covered = fromDate;
        int files = 0;
        int candles = 0;
        bool stopped = false;
        // Files whose candles are all in memory already are not downloaded (see IsPiecePresent)
        CryptoCandleList candleList = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
        bool[] present = pieces.Select(piece => IsPiecePresent(candleList, interval.Duration, piece, fromDate, untilDate)).ToArray();
        int skipped = present.Count(p => p);

        // Numbering for the log lines; grows when a month is split into its days
        int number = 0;
        int total = pieces.Count - skipped;

        // All files are asked for up front, a few at a time (ParallelDownloads), and taken in order
        // below. Leaving the loop early calls off whatever has not come in yet.
        using CancellationTokenSource downloadCancel = CancellationTokenSource.CreateLinkedTokenSource(ExchangeBase.CancellationToken);
        using SemaphoreSlim gate = new(ParallelDownloads);
        List<Task<(BinanceArchiveStatus status, List<BinanceArchiveKline> klines, long bytes)>> downloads = pieces
            .Select((piece, index) => present[index]
                ? Task.FromResult<(BinanceArchiveStatus, List<BinanceArchiveKline>, long)>((BinanceArchiveStatus.Found, [], 0))
                : DownloadGatedAsync(PieceUrl(market, symbol.ExchangeName, intervalName, piece), prefix, gate, downloadCancel.Token))
            .ToList();
        try
        {
            for (int index = 0; index < pieces.Count; index++)
            {
                BinanceArchivePiece piece = pieces[index];
                if (ExchangeBase.CancellationToken.IsCancellationRequested)
                {
                    stopped = true;
                    break;
                }

                if (present[index])
                {
                    covered = piece.End < untilDate ? piece.End : untilDate;
                    continue;
                }

                var (status, klines, bytes) = await downloads[index];
                downloads[index] = Task.FromResult<(BinanceArchiveStatus, List<BinanceArchiveKline>, long)>((status, [], 0)); // release the klines
                number++;
                int added = 0;
                if (status == BinanceArchiveStatus.Found)
                    added = await AddCandlesAsync(symbol, interval, klines, fromDate, untilDate);
                LogPiece(prefix, intervalName, piece, number, total, status, added, bytes);
                if (status == BinanceArchiveStatus.Found)
                {
                    files++;
                    candles += added;
                }
                else if (status == BinanceArchiveStatus.NotFound && piece.Monthly && !piece.Recent)
                {
                    // No history in that month
                }
                else if (status == BinanceArchiveStatus.NotFound && piece.Monthly)
                {
                    // Monthly file not published yet: the days of that month one by one
                    DateTime day = piece.Start < fromDate.Date ? fromDate.Date : piece.Start;
                    DateTime today = DateTime.UtcNow.Date;
                    DateTime lastDay = piece.End;
                    if (untilDate < lastDay)
                        lastDay = untilDate;
                    if (today < lastDay)
                        lastDay = today;
                    if (lastDay > day)
                        total += (int)Math.Ceiling((lastDay - day).TotalDays);
                    while (day < piece.End && day < untilDate && day.AddDays(1) <= today)
                    {
                        BinanceArchivePiece dayPiece = new(false, day, day.AddDays(1), false);
                        var (dayStatus, dayKlines, dayBytes) = await DownloadAsync(PieceUrl(market, symbol.ExchangeName, intervalName, dayPiece), prefix);
                        number++;
                        int dayAdded = 0;
                        if (dayStatus == BinanceArchiveStatus.Found)
                            dayAdded = await AddCandlesAsync(symbol, interval, dayKlines, fromDate, untilDate);
                        LogPiece(prefix, intervalName, dayPiece, number, total, dayStatus, dayAdded, dayBytes);
                        if (dayStatus != BinanceArchiveStatus.Found)
                        {
                            stopped = true;
                            break;
                        }
                        files++;
                        candles += dayAdded;
                        covered = dayPiece.End < untilDate ? dayPiece.End : untilDate;
                        day = day.AddDays(1);
                    }
                    if (!stopped && day < piece.End && day < untilDate)
                        stopped = true;
                    if (stopped)
                        break;
                    continue;
                }
                else
                {
                    // A day that is not published yet, or a download that failed
                    stopped = true;
                    break;
                }

                covered = piece.End < untilDate ? piece.End : untilDate;
            }
        }
        finally
        {
            // Nothing may still be running on the gate and the token when they are disposed
            downloadCancel.Cancel();
            await Task.WhenAll(downloads);
        }

        CandleTime coveredTime = CandleTime.FromDateTime(covered);
        if (files > 0 || coveredTime > from)
            GlobalData.AddTextToLogTab($"{prefix} archive {from.ToDateTime():yyyy-MM-dd HH:mm}..{covered:yyyy-MM-dd HH:mm} " +
                $"received: {candles} from {files} file(s){(skipped > 0 ? $", {skipped} already present" : "")}{(stopped ? ", rest via api" : "")}");
        return coveredTime > from ? coveredTime : from;
    }
}

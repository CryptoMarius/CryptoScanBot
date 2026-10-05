using CryptoScanner.Core.Model;

using Dapper;

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.Core.Context;

/// <summary>
/// Builds <see cref="CryptoEmulatorRun.PositionDigestJson"/>: one line per position of a run, so
/// the analyses that need INDIVIDUAL positions survive the deletion of the Position table.
/// <para>
/// It lives in Core rather than in the emulator because two callers need it - the emulator writes
/// a digest at the end of every run, and the database migration backfills the runs that already
/// existed. Keeping one builder keeps one format.
/// </para>
/// </summary>
public static class PositionDigest
{
    private const string SelectColumns =
        "select SymbolId, Side, CreateTime, CloseTime, Profit, Invested, PartCount, Status, " +
        "       Strategy, IntervalId, TrendPercentagePrimary, TrendPercentageSecondary, " +
        "       StochOscillator, StochSignal, Rsi, BollingerBandsPercentage, MacdHistogram, " +
        "       Barometer1h, Trend1h, EventText, PriceMinPerc, PriceMaxPerc " +
        "from position where EmulatorRunId = @id order by Id";


    /// <summary>
    /// The digest of one run, or null when the run has no positions left. Null means "nothing to
    /// say", never "no positions were traded" - a caller must not overwrite an existing digest
    /// with it, which is why the emulator only computes a summary for runs that still have their
    /// positions (EmulatorDb.CanRecalculate).
    /// <para>
    /// EVERY position is included, not only the traded ones: a cancelled or timed-out entry is
    /// what a filter did or did not let through, and the counters on the run row cannot say which
    /// symbol or which moment that was.
    /// </para>
    /// </summary>
    public static string? Build(CryptoDatabase database, int runId)
    {
        var rows = database.Connection.Query<DigestRow>(SelectColumns, new { id = runId }).AsList();
        if (rows.Count == 0)
            return null;

        List<object?[]> values = new(rows.Count);
        foreach (DigestRow r in rows)
        {
            values.Add(
            [
                r.SymbolId, r.Side, Minutes(r.CreateTime), Minutes(r.CloseTime),
                Number(r.Profit, 4), Number(r.Invested, 2), r.PartCount, r.Status,
                r.Strategy, r.IntervalId,
                Number(r.TrendPercentagePrimary, 3), Number(r.TrendPercentageSecondary, 3),
                Number(r.StochOscillator, 3), Number(r.StochSignal, 3), Number(r.Rsi, 3),
                Number(r.BollingerBandsPercentage, 3), Number(r.MacdHistogram, 6),
                Number(r.Barometer1h, 3), Number(r.Trend1h, 3),
                string.IsNullOrEmpty(r.EventText) ? null : r.EventText,
                Adverse(r),
            ]);
        }

        return JsonSerializer.Serialize(new
        {
            v = CryptoPositionDigest.CurrentVersion,
            cols = CryptoPositionDigest.Columns,
            rows = values,
        });
    }


    /// <summary>
    /// How far the price went against the position at its worst, in percent of the signal price,
    /// negative when it went against it: the lowest low for a long, the highest high (sign flipped)
    /// for a short. Null when the extremes were never measured.
    /// </summary>
    internal static double? Adverse(DigestRow r)
    {
        double? perc = Number(r.Side == (int)Enums.CryptoTradeSide.Long ? r.PriceMinPerc : r.PriceMaxPerc, 2);
        if (perc == null)
            return null;
        return r.Side == (int)Enums.CryptoTradeSide.Long ? perc : -perc;
    }


    /// <summary>
    /// The marker in front of a compressed digest. A stored digest is either plain JSON (it starts
    /// with "{") or this prefix followed by the base64 of the gzipped JSON.
    /// </summary>
    public const string CompressedPrefix = "gz1:";


    /// <summary>
    /// The digest as it is stored on the run row: gzipped and base64-encoded behind
    /// <see cref="CompressedPrefix"/>. Since 04-10-2026 (database version 104): the digests were
    /// 556 MB of the 718 MB Session1 database, and the JSON of numbers compresses well. A value that
    /// is empty or already compressed is returned as it is.
    /// </summary>
    public static string? Pack(string? json)
    {
        if (string.IsNullOrEmpty(json) || json.StartsWith(CompressedPrefix, StringComparison.Ordinal))
            return json;

        using MemoryStream buffer = new();
        using (GZipStream zip = new(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            zip.Write(bytes, 0, bytes.Length);
        }
        return CompressedPrefix + Convert.ToBase64String(buffer.ToArray());
    }


    /// <summary>The digest as JSON again, whether it was stored compressed or not.</summary>
    public static string? Unpack(string? stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(CompressedPrefix, StringComparison.Ordinal))
            return stored;

        byte[] packed = Convert.FromBase64String(stored[CompressedPrefix.Length..]);
        using MemoryStream source = new(packed);
        using GZipStream zip = new(source, CompressionMode.Decompress);
        using StreamReader reader = new(zip, Encoding.UTF8);
        return reader.ReadToEnd();
    }


    /// <summary>Minutes since CandleTime.Epoch, the unit the candle database already speaks.</summary>
    private static long? Minutes(DateTime? moment) =>
        moment == null ? null : (long)(moment.Value - CandleTime.Epoch).TotalMinutes;


    /// <summary>
    /// The Position columns are TEXT, so a value arrives as a string and an empty one is not a zero
    /// but an absent measurement - rounding it to 0 would put a symbol at RSI 0 in every analysis
    /// that reads the digest.
    /// </summary>
    private static double? Number(string? value, int decimals)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed)
            ? Math.Round(parsed, decimals)
            : null;
    }


    internal sealed class DigestRow
    {
        public int SymbolId { get; set; }
        public int Side { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public string? Profit { get; set; }
        public string? Invested { get; set; }
        public int PartCount { get; set; }
        public int Status { get; set; }
        public string? Strategy { get; set; }
        public int IntervalId { get; set; }
        public string? TrendPercentagePrimary { get; set; }
        public string? TrendPercentageSecondary { get; set; }
        public string? StochOscillator { get; set; }
        public string? StochSignal { get; set; }
        public string? Rsi { get; set; }
        public string? BollingerBandsPercentage { get; set; }
        public string? MacdHistogram { get; set; }
        public string? Barometer1h { get; set; }
        public string? Trend1h { get; set; }
        public string? EventText { get; set; }
        public string? PriceMinPerc { get; set; }
        public string? PriceMaxPerc { get; set; }
    }
}

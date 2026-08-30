using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;

using Dapper;

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CryptoScanner.Emulator.Engine;


/// <summary>
/// Recognises a run that has already been measured, so the queue does not spend a quarter of an
/// hour reproducing a number that is already in the database.
/// <para>
/// The checksum is taken over the two JSON blobs that are stored WITH the run - the run
/// configuration and the full scanner settings - so it covers exactly what was replayed rather
/// than a hand-picked selection of fields. Anything that changes the outcome changes the
/// checksum, because anything that changes the outcome is in one of those two blobs.
/// </para>
/// </summary>
public static class EmulatorRunFingerprint
{
    /// <summary>
    /// One earlier run that matches, with what it produced - enough to explain a skip without
    /// making the caller query for it again.
    /// </summary>
    public sealed class Match
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int PositionCount { get; set; }
        public decimal Profit { get; set; }
    }


    /// <summary>
    /// The checksum of one run, over its configuration and the scanner settings behind it.
    /// <para>
    /// Four fields of the run configuration are removed first, because none of them reaches the
    /// replay: the label is free-form text naming the experiment, the two sort fields only say how
    /// the Results grid was ordered, and the duplicate window is a setting for this check itself.
    /// Leaving the label in would make every re-run of the same experiment under a new name look
    /// like something that had never been measured, which is precisely the case this is meant to
    /// catch; leaving the window in would make changing that number invalidate every checksum.
    /// </para>
    /// </summary>
    public static string Compute(string configJson, string? settingsJson)
    {
        string canonicalConfig = StripFieldsOutsideTheReplay(configJson);
        byte[] bytes = Encoding.UTF8.GetBytes(canonicalConfig + "\n" + (settingsJson ?? ""));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }


    /// <summary>
    /// The moment before which an identical run no longer counts as already measured, or null when
    /// the check is switched off (<paramref name="duplicateCheckDays"/> below zero).
    /// <para>
    /// The floor is the build time of the running emulator: an earlier run on the SAME build cannot
    /// produce a different answer, while a run from before it can - the code in between may have
    /// changed what a replay does. How much that matters is on record: runs 507 and 509 differ only
    /// in code and produced +432.07 and +734.17. Their checksums differ too (a setting was renamed
    /// in the same change), so no case of an IDENTICAL checksum straddling a code change has
    /// actually been observed - the floor is precaution, not a repair.
    /// </para>
    /// <para>
    /// On its own that floor is too strict to be useful, because the emulator is rebuilt often and
    /// the window is then empty. <paramref name="duplicateCheckDays"/> widens it: whichever of the
    /// two reaches further back wins. The price is that a run from before a code change can be
    /// taken for the same measurement, which is why the outcome is a recorded duplicate and not a
    /// silent skip.
    /// </para>
    /// <para>
    /// The build time falls back to one day ago when it cannot be read, short enough that a rebuild
    /// is never masked for long.
    /// </para>
    /// </summary>
    public static DateTime? GetRecentSince(int duplicateCheckDays)
    {
        if (duplicateCheckDays < 0)
            return null;

        DateTime buildTime;
        try
        {
            string? location = Assembly.GetEntryAssembly()?.Location;
            buildTime = !string.IsNullOrEmpty(location) && File.Exists(location)
                ? File.GetLastWriteTimeUtc(location)
                : DateTime.UtcNow.AddDays(-1);
        }
        catch
        {
            // Reading the file date is not worth failing a run over.
            buildTime = DateTime.UtcNow.AddDays(-1);
        }

        DateTime byDays = DateTime.UtcNow.AddDays(-duplicateCheckDays);
        return byDays < buildTime ? byDays : buildTime;
    }


    /// <summary>
    /// The most recent completed run with the same checksum, or null. Only runs that finished at
    /// or after <paramref name="notBefore"/> are considered, and only ones that actually completed
    /// - a cancelled or failed run measured nothing and must not block a retry, and a run that was
    /// itself recorded as a duplicate holds no numbers to point at.
    /// </summary>
    public static Match? FindRecentMatch(string fingerprint, DateTime notBefore)
    {
        try
        {
            using var database = new CryptoDatabase();
            database.Open();

            // Recompute the checksum of the candidates rather than storing it in a column: the set
            // is small (only runs since the current build) and it keeps the comparison honest when
            // the way the checksum is taken changes.
            var candidates = database.Connection.Query<CandidateRow>(
                "select Id, Label, FinishedAt, PositionCount, Profit, ConfigJson, SettingsJson " +
                "from EmulatorRun " +
                "where FinishedAt is not null and FinishedAt >= @notBefore and Result = 'completed' " +
                "order by Id desc",
                new { notBefore });

            foreach (var row in candidates)
            {
                if (Compute(row.ConfigJson ?? "", row.SettingsJson) != fingerprint)
                    continue;

                return new Match
                {
                    Id = row.Id,
                    Label = row.Label,
                    FinishedAt = row.FinishedAt,
                    PositionCount = row.PositionCount,
                    Profit = row.Profit,
                };
            }
        }
        catch (Exception ex)
        {
            // A failing lookup must never stop a run - worst case the run is done twice.
            GlobalData.AddTextToLogTab($"Queue: duplicate check failed ({ex.Message}) — running anyway");
        }

        return null;
    }


    /// <summary>
    /// Removes the fields that do not reach the replay from the configuration JSON, leaving the
    /// remaining properties in their original order. Returns the input unchanged when it cannot be
    /// parsed, so a malformed blob makes the run look unique instead of throwing.
    /// </summary>
    private static string StripFieldsOutsideTheReplay(string configJson)
    {
        try
        {
            if (JsonNode.Parse(configJson) is not JsonObject obj)
                return configJson;

            obj.Remove("Label");
            obj.Remove("SortColumn");
            obj.Remove("SortDescending");
            obj.Remove("DuplicateCheckDays");
            return obj.ToJsonString();
        }
        catch (JsonException)
        {
            return configJson;
        }
    }


    private sealed class CandidateRow
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int PositionCount { get; set; }
        public decimal Profit { get; set; }
        public string? ConfigJson { get; set; }
        public string? SettingsJson { get; set; }
    }
}

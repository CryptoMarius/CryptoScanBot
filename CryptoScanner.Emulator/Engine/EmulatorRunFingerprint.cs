using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

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
/// than a hand-picked selection of fields. Such a list is one someone forgets to extend, and a
/// forgotten field would make two different runs quietly count as the same measurement.
/// </para>
/// <para>
/// The settings blob is not hashed as stored but read back into a <see cref="SettingsBasic"/> and
/// written out again first, so what is compared is what the CURRENT code makes of that snapshot.
/// Without that, one added or removed setting invalidates every earlier checksum and a restarted
/// queue replays hours of runs it already has the numbers for. See <see cref="Canonicalise"/>.
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
        string canonicalSettings = Canonicalise(settingsJson ?? "");
        byte[] bytes = Encoding.UTF8.GetBytes(canonicalConfig + "\n" + canonicalSettings);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }


    /// <summary>
    /// The stored settings snapshot read back into a <see cref="SettingsBasic"/> and written out
    /// again, so the checksum is taken over what the CURRENT code makes of that snapshot rather than
    /// over the text it happened to be stored as.
    /// <para>
    /// This is what makes the checksum survive a build. Hashing the raw text means a single added or
    /// removed setting invalidates EVERY earlier checksum, and a restarted queue then replays hours
    /// of runs whose numbers are already in the database - which is what happened on 02-09-2026,
    /// when two candlepattern settings arrived and six pattern settings left. The round trip fixes
    /// all three ways the text can drift while the run does not:
    /// </para>
    /// <para>
    /// A setting that was ADDED is missing from an older snapshot and gets its default on the way
    /// in, which is exactly what the new snapshot holds. One that was REMOVED is silently dropped by
    /// the deserializer, because the class no longer has a property for it. And a property that was
    /// MOVED within its class comes out in the new order on both sides, where hashing the raw text
    /// would have seen two different documents.
    /// </para>
    /// <para>
    /// The price, accepted deliberately: a setting that left the code while an older run had it at a
    /// non-default value now counts as the same measurement. That run WAS measured differently - but
    /// so is every run from before any code change that did not touch a setting, and the check has
    /// never protected against those either (see <see cref="GetRecentSince"/>: the build-time floor
    /// is widened by DuplicateCheckDays for exactly that reason). Guarding one of the two and not
    /// the other bought nothing.
    /// </para>
    /// </summary>
    internal static string Canonicalise(string settingsJson)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<SettingsBasic>(
                settingsJson, Core.Json.JsonTools.DeSerializerOptions);
            if (settings == null)
                return settingsJson;

            // The analyzer blocks need the same treatment, separately: SettingsSignal keeps them as
            // Dictionary<string, JsonElement> on purpose - only the plugin knows its concrete type,
            // and a Dictionary of the base type would silently drop every derived property. That
            // means a plain round trip hands those blocks back VERBATIM, so a setting added to a
            // plugin still changed the checksum. Which is precisely what happened: two settings
            // arrived on failedbreakout on 02-09-2026 and the anchor was replayed anyway.
            settings.Signal.AnalyzerSettings = CanonicaliseAnalyzers(
                settings.Signal.AnalyzerSettings, ActiveStrategies(settings));

            StripThingsThatCannotChangeAReplay(settings);

            return JsonSerializer.Serialize(settings, Core.Json.JsonTools.JsonSerializerIndented);
        }
        catch (JsonException)
        {
            // A blob that cannot be read makes the run look unique rather than throwing: measuring
            // one run twice is cheap, skipping one that was never measured is not.
            return settingsJson;
        }
        catch (NotSupportedException)
        {
            // Same reasoning - a converter that cannot handle an old value must not stop a run.
            return settingsJson;
        }
    }


    /// <summary>
    /// The strategies this run actually evaluates, taken from the per-side strategy lists that the
    /// queue loop narrows to the entry's own algorithm.
    /// </summary>
    private static HashSet<string> ActiveStrategies(SettingsBasic settings)
    {
        HashSet<string> active = new(StringComparer.OrdinalIgnoreCase);
        foreach (string name in settings.Signal.Long.Strategy)
            active.Add(name);
        foreach (string name in settings.Signal.Short.Strategy)
            active.Add(name);
        return active;
    }


    /// <summary>
    /// The analyzer blocks that can influence THIS run, each read into the plugin's own settings type
    /// and written out again so a property added to or removed from a plugin lands on both sides the
    /// same way.
    /// <para>
    /// Blocks of strategies the run does not evaluate are dropped. A dbr run is not a different
    /// measurement because vbs got a different band width or because a plugin left the build - and
    /// on 03-09-2026 exactly that replayed sixteen runs whose numbers were already in the database,
    /// twice over. The proof it changes nothing: those replays came out identical to the cent
    /// (CA1 +550.85, CB4 +547.59, CB9 +607.04), while the snapshots differed in 34 places, twenty of
    /// them analyzer blocks of strategies that were not running.
    /// </para>
    /// <para>
    /// Zone settings are NOT in here - they live under Signal.ZonesDlz/ZonesFvg/ZonesSmc and stay
    /// part of the checksum, because a candlepattern or failedbreakout run with a zone requirement
    /// really does depend on them.
    /// </para>
    /// <para>
    /// An empty strategy list keeps everything, so a run whose sides are configured some other way
    /// is never silently reduced to a copy of another.
    /// </para>
    /// </summary>
    private static Dictionary<string, JsonElement> CanonicaliseAnalyzers(
        Dictionary<string, JsonElement> stored, HashSet<string> active)
    {
        Dictionary<string, JsonElement> result = [];
        foreach ((string name, JsonElement block) in stored)
        {
            if (active.Count > 0 && !active.Contains(name))
                continue;

            var settings = Core.Contracts.PluginManager.MaterializeSettings(name, stored);
            JsonElement value = settings == null
                ? block
                : JsonSerializer.SerializeToElement(settings, settings.GetType(),
                    Core.Json.JsonTools.JsonSerializerIndented);

            result[name] = WithoutPresentationFields(value);
        }
        return result;
    }


    /// <summary>
    /// The sound and colour fields every strategy settings class inherits, removed. They decide what
    /// the scanner plays and paints, never what a replay does, and a renamed .wav file is not a new
    /// measurement.
    /// </summary>
    private static readonly HashSet<string> PresentationFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "PlaySound", "PlaySpeech", "ColorLong", "ColorShort", "SoundFileLong", "SoundFileShort",
    };

    private static JsonElement WithoutPresentationFields(JsonElement block)
    {
        if (block.ValueKind != JsonValueKind.Object)
            return block;

        JsonObject? obj = JsonNode.Parse(block.GetRawText()) as JsonObject;
        if (obj == null)
            return block;

        foreach (string field in PresentationFields)
            obj.Remove(field);

        return JsonSerializer.SerializeToElement(obj);
    }


    /// <summary>
    /// Flattens the settings that steer logging and the interface rather than the replay: the debug
    /// symbol, the heartbeat sound, and every Log* switch on SettingsSignal. Two runs that differ
    /// only in what they wrote to the log file produced the same trades.
    /// </summary>
    private static void StripThingsThatCannotChangeAReplay(SettingsBasic settings)
    {
        settings.General.DebugSymbol = "";
        settings.General.SoundHeartBeatMinutes = 0;
        settings.Signal.SoundsActive = false;

        foreach (PropertyInfo property in typeof(SettingsSignal).GetProperties())
        {
            if (property.PropertyType == typeof(bool) && property.CanWrite
                && property.Name.StartsWith("Log", StringComparison.Ordinal))
                property.SetValue(settings.Signal, false);
        }
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

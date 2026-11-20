using CryptoScanner.Core.Const;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

using Dapper;

using Microsoft.Data.Sqlite;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Checks that a queue entry which repeats an earlier run (<see cref="EmulatorQueueEntry.SourceRun"/>)
/// really produces the settings of that run, before the batch spends hours on it.
/// <para>
/// Why it exists: on 13-11-2026 the repeats of Session0 turned out to differ from their source runs in
/// 1.885 of 3.808 entries. The script that wrote them on 17-10 did not read the old flat
/// "Trading.CheckPriceAboveMa200" nor the MA200 switch of the old strategy blocks, and the conversion to
/// the default-based queue of 09-11 then froze the session values of that day (MA200 on, 10 confirmation
/// candles) into the entries. <see cref="QueueBaseline"/> runs whatever an entry says faithfully; nothing
/// compared what the entry says with the run it claims to repeat. This does, with the settings of the
/// code itself: the entry is applied exactly as for a real run and the snapshot that would be stored on
/// the run row is compared with the snapshot stored on the source row.
/// </para>
/// <para>
/// What is compared, after both sides are put in the same shape:
/// - the entry conditions that are actually read for the strategy: its own set when the plugin block has
///   one, otherwise the trading set (SignalBase.ResolveEntryConditions). A difference in a set that is not
///   read is no difference: sbm runs 22-24 and 41-43 differed in the trading MA200 and gave the same result
///   to the cent;
/// - the settings block of the plugin the algorithm belongs to (the old Session0 blocks are found through
///   the former names of the plugin: "Bre" for dbr, "Baba" for vbs);
/// - the trading settings and the signal settings.
/// Left out: the frame of a session (paper assets, asset management, the bot switch, the strategy lists the
/// batch sets itself), sounds, colours and log switches, and the old strategy blocks of OTHER strategies.
/// </para>
/// <para>
/// A setting that exists on one side only cannot be compared. A switch that is ON there is reported (a
/// feature the other side did not have changes the run); any other value is counted as not comparable and
/// named in the log, so it stays visible without stopping the entry.
/// </para>
/// </summary>
public static class QueueSourceCheck
{
    /// <summary>The outcome of one comparison.</summary>
    public sealed class Result
    {
        /// <summary>Differences that stop the entry.</summary>
        public List<string> Differences { get; } = [];

        /// <summary>Differences the entry lists in AcceptedSourceDifferences.</summary>
        public List<string> Accepted { get; } = [];

        /// <summary>Settings that exist on one side only and are not a switch that is on.</summary>
        public List<string> NotComparable { get; } = [];
    }


    // Settings of the session frame, set per entry or by the batch itself, not by the run being repeated
    private static readonly HashSet<string> FramePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "Trading.PaperAssetDefaults",
        "Trading.UseAssetManagement",
        "Trading.Active",
        "Trading.ActiveBackup",
        "Trading.Long.Strategy",
        "Trading.Short.Strategy",
        "Signal.Long.Strategy",
        "Signal.Short.Strategy",
    };

    // Presentation only: never part of what a run measures
    private static readonly HashSet<string> CosmeticNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PlaySound", "PlaySpeech", "ColorLong", "ColorShort", "SoundFileLong", "SoundFileShort", "SoundsActive",
    };

    // Settings whose behaviour is fixed now, with the value that describes it (SignalGridExpander keeps
    // the same list for queue entries). A source run that holds that value differs in nothing. A switch
    // counts as 1 when on and 0 when off.
    private static readonly Dictionary<string, decimal> FixedRemovedValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trading.DcaOrderType"] = 1m,
        // Renamed to UseNewIndicatorHub on 01-08-2026 and removed on 08-08-2026: the hub is the only path
        ["Signal.UseIndicatorHub"] = 1m,
        ["Signal.UseNewIndicatorHub"] = 1m,
    };

    // The settings block of a strategy in the Signal settings before the plugins (Session0). Found by the
    // plugin name, case-insensitive ("AtrRb" for atrrb, "StoRsi" for storsi), except for the two
    // strategies renamed since: the plugins deliberately carry no former names any more (MacPlugin).
    private static readonly Dictionary<string, string[]> OldSignalBlockNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dbr"] = ["Bre"],
        ["vbs"] = ["Baba"],
    };

    // An entry-condition value that only matters while the switch it belongs to is on
    private static readonly Dictionary<string, string[]> EntryConditionSwitch = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(SettingsEntryConditions.IncreasingMacdCandles)] = [nameof(SettingsEntryConditions.CheckIncreasingMacd)],
        [nameof(SettingsEntryConditions.TrendPrimaryDirectionCount)] = [nameof(SettingsEntryConditions.CheckTrendPrimaryDirection)],
        [nameof(SettingsEntryConditions.TrendSecondaryDirectionCount)] = [nameof(SettingsEntryConditions.CheckTrendSecondaryDirection)],
        [nameof(SettingsEntryConditions.Ma200MinDistancePercentage)] = [nameof(SettingsEntryConditions.CheckPriceAboveMa200)],
        [nameof(SettingsEntryConditions.Ma200ConfirmationCandles)] = [nameof(SettingsEntryConditions.CheckPriceAboveMa200)],
        [nameof(SettingsEntryConditions.EntryMaxAdversePercentage)] = [nameof(SettingsEntryConditions.EntryWaitCandles)],
        [nameof(SettingsEntryConditions.SupportMinimumRoomAtr)] = [nameof(SettingsEntryConditions.SkipShortAboveSupport)],
        [nameof(SettingsEntryConditions.StochExtremeLookback)] =
        [
            nameof(SettingsEntryConditions.StochMinExtremeBars),
            nameof(SettingsEntryConditions.StochMinExtremeArea),
            nameof(SettingsEntryConditions.StochMinExtremeZScore),
        ],
    };

    // The MA200 switch the old Session0 strategy blocks (Bre, Baba, AtrRb) carried before 21-07-2026, when
    // it moved to the entry conditions. On means the same filter as the entry-condition MA200.
    private static readonly string[] OldBlockMa200Names =
    [
        nameof(SettingsEntryConditions.CheckPriceAboveMa200),
        nameof(SettingsEntryConditions.Ma200MinDistancePercentage),
        nameof(SettingsEntryConditions.Ma200ConfirmationCandles),
    ];


    /// <summary>
    /// The settings snapshot of the run that would start now: the plugin settings synced into the
    /// AnalyzerSettings blocks and the whole of GlobalData.Settings serialized with the options the
    /// run row uses. The run start calls this too, so the check compares exactly what gets stored.
    /// </summary>
    public static string CaptureSettingsJson()
    {
        PluginManager.CollectSettings(GlobalData.Settings.Signal.AnalyzerSettings);
        return JsonSerializer.Serialize(GlobalData.Settings, Core.Json.JsonTools.JsonSerializerIndented);
    }


    /// <summary>
    /// Applies <paramref name="entry"/> on the defaults exactly as the batch does, compares the result
    /// with its source run and puts everything back. Returns why the entry cannot run, or null when it
    /// has no source run or matches it. Settings that could not be compared go to the log.
    /// </summary>
    public static string? CheckEntry(EmulatorQueueEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SourceRun))
            return null;

        var (sourceJson, loadError) = LoadSourceSettings(entry.SourceRun!);
        if (sourceJson == null)
            return $"source run {entry.SourceRun}: {loadError}";

        IStrategyPlugin? plugin = FindPlugin(entry.Algorithm);
        if (plugin == null)
            return $"source run {entry.SourceRun}: no plugin found for algorithm \"{entry.Algorithm}\"";

        string currentJson;
        List<SignalGridExpander.Override> overrides = [];
        QueueBaseline.Install();
        try
        {
            overrides = QueueEntryApplier.Apply(entry);
            currentJson = CaptureSettingsJson();
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
            QueueBaseline.Restore();
        }

        Result result = Compare(currentJson, sourceJson, plugin.StrategyName, plugin.FormerStrategyNames,
            entry.AcceptedSourceDifferences);

        if (result.NotComparable.Count > 0)
            GlobalData.AddTextToLogTab($"Queue: \"{entry.Label}\" source run {entry.SourceRun}: "
                + $"{result.NotComparable.Count} setting(s) exist on one side only and were not compared: "
                + string.Join(", ", result.NotComparable.Take(20)) + (result.NotComparable.Count > 20 ? ", ..." : ""));

        if (result.Differences.Count == 0)
            return null;

        return $"differs from source run {entry.SourceRun} in {result.Differences.Count} setting(s): "
            + string.Join("; ", result.Differences.Take(15)) + (result.Differences.Count > 15 ? "; ..." : "")
            + ". Make the entry match, or list the path in AcceptedSourceDifferences when it differs on purpose";
    }


    /// <summary>The plugin an algorithm belongs to, by plugin name, former name or strategy name.</summary>
    private static IStrategyPlugin? FindPlugin(string? algorithm)
    {
        if (string.IsNullOrEmpty(algorithm))
            return null;
        return PluginManager.FindByName(algorithm)
            ?? PluginManager.LoadedPlugins.Values.FirstOrDefault(p =>
                p.Strategies.Any(s => s.Name.Equals(algorithm, StringComparison.OrdinalIgnoreCase)));
    }


    /// <summary>
    /// The stored settings of "Session1/1951" (a session folder next to the current one) or "1951" (a
    /// run of the current session). Read-only; the source database is never written.
    /// </summary>
    public static (string? Json, string? Error) LoadSourceSettings(string sourceRun)
    {
        string text = sourceRun.Trim().Replace('\\', '/');
        string session = "";
        string idText = text;
        int slash = text.LastIndexOf('/');
        if (slash >= 0)
        {
            session = text[..slash];
            idText = text[(slash + 1)..];
        }
        if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int runId))
            return (null, "expected \"Session1/1951\" or \"1951\"");

        string folder = GlobalData.AppDataFolder;
        if (session.Length > 0)
        {
            string? root = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(GlobalData.AppDataFolder));
            if (root == null)
                return (null, "the data folder has no parent folder");
            folder = Path.Combine(root, session);
        }
        string dbFile = Path.Combine(folder, Constants.AppName + ".db");
        if (!File.Exists(dbFile))
            return (null, $"database not found ({dbFile})");

        using var connection = new SqliteConnection($"Data Source={dbFile};Mode=ReadOnly");
        connection.Open();
        string? json = connection.ExecuteScalar<string?>(
            "select SettingsJson from EmulatorRun where Id = @id", new { id = runId });
        if (string.IsNullOrWhiteSpace(json))
            return (null, $"run {runId} not found or without settings in {dbFile}");
        return (json, null);
    }


    /// <summary>
    /// Compares two settings snapshots for the plugin <paramref name="pluginName"/>. Pure: no global
    /// state is read or written, so the rules can be tested on small pieces of JSON.
    /// </summary>
    public static Result Compare(string currentJson, string sourceJson, string pluginName,
        IEnumerable<string>? formerNames = null, IEnumerable<string>? accepted = null)
    {
        string[] names = [pluginName, .. formerNames ?? [],
            .. OldSignalBlockNames.TryGetValue(pluginName, out string[]? oldNames) ? oldNames : []];
        JsonObject current = Normalize(JsonNode.Parse(currentJson)!.AsObject(), names, isSource: false);
        JsonObject source = Normalize(JsonNode.Parse(sourceJson)!.AsObject(), names, isSource: true);

        Result result = new();
        List<string> acceptedPaths = accepted?.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).ToList() ?? [];

        foreach (string group in new[] { "EntryConditions", "Plugin", "Trading", "Signal" })
            CompareNode(group, current[group], source[group], group, result);

        // Move what the entry accepts on purpose out of the differences
        for (int i = result.Differences.Count - 1; i >= 0; i--)
        {
            string path = result.Differences[i].Split(' ')[0];
            if (acceptedPaths.Any(a => path.Equals(a, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(a + ".", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(a + "[", StringComparison.OrdinalIgnoreCase)))
            {
                result.Accepted.Insert(0, result.Differences[i]);
                result.Differences.RemoveAt(i);
            }
        }
        return result;
    }


    /// <summary>
    /// Puts a snapshot in the shape both sides are compared in: four groups, EntryConditions (the set
    /// that is read), Plugin (the block of this plugin), Trading and Signal, without the frame, the
    /// cosmetic settings and the blocks of other plugins.
    /// </summary>
    private static JsonObject Normalize(JsonObject settings, string[] pluginNames, bool isSource)
    {
        JsonObject signal = settings["Signal"] as JsonObject ?? [];
        JsonObject trading = settings["Trading"] as JsonObject ?? [];

        // The block of this plugin: AnalyzerSettings since the plugins (18-07-2026), a field of Signal before
        JsonObject? plugin = null;
        if (signal["AnalyzerSettings"] is JsonObject analyzers)
            plugin = FindByAnyName(analyzers, pluginNames) as JsonObject;
        if (plugin == null && isSource)
            plugin = FindByAnyName(signal, pluginNames) as JsonObject;
        plugin = (JsonObject?)plugin?.DeepClone() ?? [];

        // The entry conditions that are read: the plugin's own set, otherwise the trading set
        JsonObject entryConditions = (JsonObject)JsonSerializer.SerializeToNode(new SettingsEntryConditions(),
            Core.Json.JsonTools.JsonSerializerIndented)!;
        HashSet<string> conditionNames = new(entryConditions.Select(p => p.Key), StringComparer.OrdinalIgnoreCase);
        JsonObject? read = plugin["EntryConditions"] as JsonObject;
        if (read == null)
        {
            read = trading["EntryConditions"] as JsonObject;
            if (read == null)
            {
                // The flat trading block of the oldest Session0 runs: the conditions sat on Trading itself
                read = [];
                foreach (var (key, value) in trading)
                {
                    if (conditionNames.Contains(key))
                        read[key] = value?.DeepClone();
                }
            }
        }
        foreach (var (key, value) in read)
        {
            string? name = conditionNames.FirstOrDefault(n => n.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (name != null)
                entryConditions[name] = value?.DeepClone();
        }

        // The MA200 switch of an old strategy block filtered the same way as the entry-condition MA200
        if (isSource && plugin["CheckPriceAboveMa200"] is JsonValue oldSwitch
            && oldSwitch.TryGetValue(out bool oldOn) && oldOn)
        {
            entryConditions[nameof(SettingsEntryConditions.CheckPriceAboveMa200)] = true;
            entryConditions[nameof(SettingsEntryConditions.Ma200MinDistancePercentage)] =
                plugin[nameof(SettingsEntryConditions.Ma200MinDistancePercentage)]?.DeepClone() ?? 0;
            entryConditions[nameof(SettingsEntryConditions.Ma200ConfirmationCandles)] =
                plugin[nameof(SettingsEntryConditions.Ma200ConfirmationCandles)]?.DeepClone() ?? 0;
        }
        plugin.Remove("EntryConditions");
        if (isSource)
        {
            foreach (string name in OldBlockMa200Names)
                plugin.Remove(name);
        }

        JsonObject tradingCopy = (JsonObject)trading.DeepClone();
        tradingCopy.Remove("EntryConditions");
        foreach (string name in conditionNames)
            tradingCopy.Remove(name);

        JsonObject signalCopy = (JsonObject)signal.DeepClone();
        signalCopy.Remove("AnalyzerSettings");

        return new JsonObject
        {
            ["EntryConditions"] = entryConditions,
            ["Plugin"] = plugin,
            ["Trading"] = tradingCopy,
            ["Signal"] = signalCopy,
        };
    }


    private static JsonNode? FindByAnyName(JsonObject parent, string[] names)
    {
        foreach (string name in names)
        {
            foreach (var (key, value) in parent)
            {
                if (key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return value;
            }
        }
        return null;
    }


    private static void CompareNode(string path, JsonNode? current, JsonNode? source, string group, Result result)
    {
        if (FramePaths.Contains(path))
            return;

        if (current is JsonObject currentObject && source is JsonObject sourceObject)
        {
            // A filter that is off on both sides: its other values do nothing
            if (IsOff(currentObject["Active"]) && IsOff(sourceObject["Active"]) && currentObject.ContainsKey("Active")
                && sourceObject.ContainsKey("Active"))
                return;

            HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, _) in currentObject)
                keys.Add(key);
            foreach (var (key, _) in sourceObject)
                keys.Add(key);

            foreach (string key in keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (CosmeticNames.Contains(key) || key.StartsWith("Log", StringComparison.Ordinal))
                    continue;

                string childPath = $"{path}.{key}";
                if (group == "EntryConditions" && path == "EntryConditions" && !EntryConditionMatters(key, currentObject, sourceObject))
                    continue;

                bool inCurrent = TryGet(currentObject, key, out JsonNode? currentChild);
                bool inSource = TryGet(sourceObject, key, out JsonNode? sourceChild);

                if (inCurrent && inSource)
                    CompareNode(childPath, currentChild, sourceChild, group, result);
                else if (inCurrent)
                    OneSided(childPath, currentChild, "this build has it, the source run did not", group, result);
                else
                {
                    // The old strategy blocks of other strategies sat in Signal before the plugins; they
                    // were not read for this strategy
                    if (group == "Signal" && path == "Signal" && sourceChild is JsonObject)
                        continue;
                    OneSided(childPath, sourceChild, "the source run had it, this build does not", group, result);
                }
            }
            return;
        }

        if (current is JsonArray currentArray && source is JsonArray sourceArray)
        {
            // A list of names (intervals, symbols) is a set: the order does not change the run
            if (currentArray.All(n => n is JsonValue v && v.GetValueKind() == JsonValueKind.String)
                && sourceArray.All(n => n is JsonValue v && v.GetValueKind() == JsonValueKind.String))
            {
                var a = currentArray.Select(n => n!.GetValue<string>()).OrderBy(s => s, StringComparer.Ordinal).ToList();
                var b = sourceArray.Select(n => n!.GetValue<string>()).OrderBy(s => s, StringComparer.Ordinal).ToList();
                if (!a.SequenceEqual(b))
                    result.Differences.Add($"{path} source [{string.Join(", ", b)}] entry [{string.Join(", ", a)}]");
                return;
            }

            if (currentArray.Count != sourceArray.Count)
            {
                result.Differences.Add($"{path} source {Show(source)} entry {Show(current)}");
                return;
            }
            for (int i = 0; i < currentArray.Count; i++)
                CompareNode($"{path}[{i}]", currentArray[i], sourceArray[i], group, result);
            return;
        }

        if (!SameValue(current, source))
            result.Differences.Add($"{path} source {Show(source)} entry {Show(current)}");
    }


    /// <summary>A value of an entry condition counts only while the switch it belongs to is on (on either side).</summary>
    private static bool EntryConditionMatters(string key, JsonObject current, JsonObject source)
    {
        if (!EntryConditionSwitch.TryGetValue(key, out string[]? switches))
            return true;
        foreach (string name in switches)
        {
            if (!IsOff(current[name]) || !IsOff(source[name]))
                return true;
        }
        return false;
    }


    /// <summary>
    /// A setting on one side only. A switch that is on, anywhere below it, is a difference; everything
    /// else cannot be compared and is only counted. A removed setting holding the value its behaviour is
    /// fixed at differs in nothing.
    /// </summary>
    private static void OneSided(string path, JsonNode? node, string why, string group, Result result)
    {
        if (FramePaths.Contains(path))
            return;
        if (FixedRemovedValues.TryGetValue(path, out decimal fixedValue) && TryNumberOrSwitch(node, out decimal number)
            && number == fixedValue)
            return;

        List<string> switchesOn = [];
        CollectSwitchesOn(path, node, switchesOn);
        if (switchesOn.Count > 0)
        {
            foreach (string on in switchesOn)
                result.Differences.Add($"{on} is on ({why})");
            return;
        }
        result.NotComparable.Add(path);
    }


    private static void CollectSwitchesOn(string path, JsonNode? node, List<string> found)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("Active") && IsOff(obj["Active"]))
                    return;
                foreach (var (key, value) in obj)
                {
                    if (CosmeticNames.Contains(key) || key.StartsWith("Log", StringComparison.Ordinal))
                        continue;
                    CollectSwitchesOn($"{path}.{key}", value, found);
                }
                break;
            case JsonValue value when value.GetValueKind() == JsonValueKind.True:
                found.Add(path);
                break;
        }
    }


    private static bool TryGet(JsonObject obj, string key, out JsonNode? value)
    {
        foreach (var (k, v) in obj)
        {
            if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = v;
                return true;
            }
        }
        value = null;
        return false;
    }


    private static bool IsOff(JsonNode? node)
    {
        if (node == null)
            return true;
        if (node is JsonValue value)
        {
            JsonValueKind kind = value.GetValueKind();
            if (kind == JsonValueKind.False || kind == JsonValueKind.Null)
                return true;
            if (kind == JsonValueKind.Number && TryNumber(node, out decimal d))
                return d == 0m;
            if (kind == JsonValueKind.String)
                return string.IsNullOrEmpty(value.GetValue<string>());
        }
        if (node is JsonArray array)
            return array.Count == 0;
        return false;
    }


    private static bool TryNumber(JsonNode? node, out decimal number)
    {
        number = 0m;
        if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.Number)
            return false;
        return decimal.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }


    private static bool TryNumberOrSwitch(JsonNode? node, out decimal number)
    {
        if (TryNumber(node, out number))
            return true;
        if (node is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            number = value.GetValueKind() == JsonValueKind.True ? 1m : 0m;
            return true;
        }
        return false;
    }


    private static bool SameValue(JsonNode? a, JsonNode? b)
    {
        if (a == null || b == null)
            return IsNull(a) && IsNull(b);
        if (TryNumber(a, out decimal x) && TryNumber(b, out decimal y))
            return x == y;
        return JsonNode.DeepEquals(a, b);
    }


    private static bool IsNull(JsonNode? node) =>
        node == null || node is JsonValue v && v.GetValueKind() == JsonValueKind.Null;


    private static string Show(JsonNode? node) => node?.ToJsonString() ?? "null";
}

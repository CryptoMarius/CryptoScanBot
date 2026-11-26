using CryptoScanner.Core.Context;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Signal;

using Dapper;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Writes a priority queue file that repeats runs of this session: one entry per run, starting from
/// the settings stored on that run (<see cref="EmulatorQueueEntry.SettingsFromRun"/>) with the coins,
/// period, base interval and barometer switch of its stored run configuration, and with Force so the
/// duplicate check does not skip it. Behind the "Run again" choice of the Results tab (24-11-2026).
/// Each entry also names the run it replaces (<see cref="EmulatorQueueEntry.ReplacesRun"/>), so the
/// original is deleted once the repeat has completed.
/// <para>
/// A queue file rather than a run started on the spot: the queue is what the emulator runs with
/// nobody watching, a run that is going cannot be interrupted, and the file name puts the repeats
/// before everything else in the folder (<see cref="EmulatorQueueFolder.PriorityPrefix"/>) - also
/// between two entries of a file that is already running.
/// </para>
/// </summary>
public static class EmulatorRerunQueue
{
    /// <summary>The outcome of <see cref="Write"/>.</summary>
    public sealed class Result
    {
        /// <summary>The file written, or null when none of the runs could be repeated.</summary>
        public string? FilePath { get; set; }

        /// <summary>The runs that went into the file.</summary>
        public List<int> Queued { get; } = [];

        /// <summary>The runs that could not be repeated, with the reason.</summary>
        public List<string> Skipped { get; } = [];
    }


    private sealed class RunData
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public string? ConfigJson { get; set; }
        public string? SettingsJson { get; set; }
    }


    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };


    /// <summary>Builds the entries for <paramref name="runIds"/> and writes them to the Queue folder.</summary>
    public static Result Write(IReadOnlyList<int> runIds)
    {
        Result result = new();
        List<EmulatorQueueEntry> entries = [];

        foreach (int runId in runIds)
        {
            RunData? run = Load(runId);
            if (run == null)
            {
                result.Skipped.Add($"#{runId}: not found");
                continue;
            }

            string? reason = BuildEntry(run, out EmulatorQueueEntry? entry);
            if (entry == null)
            {
                result.Skipped.Add($"#{runId}: {reason}");
                continue;
            }
            entries.Add(entry);
            result.Queued.Add(runId);
        }

        if (entries.Count == 0)
            return result;

        string folder = EmulatorQueueFolder.Folder;
        Directory.CreateDirectory(folder);

        // A Debug emulator writes a file only a Debug emulator runs; a Release emulator one that both run
#if DEBUG
        string suffix = EmulatorQueueFolder.DebugOnlySuffix;
#else
        string suffix = ".json";
#endif
        string ids = string.Join("-", result.Queued.Take(5)) + (result.Queued.Count > 5 ? "-etc" : "");
        string baseName = $"{EmulatorQueueFolder.PriorityPrefix}rmk-{DateTime.Now:yyyyMMdd-HHmmss}-{ids}";
        string target = Path.Combine(folder, baseName + suffix);
        for (int i = 1; File.Exists(target); i++)
            target = Path.Combine(folder, $"{baseName}-{i}{suffix}");

        // Written next to it under another extension and then renamed, so the queue never reads half a file
        string temporary = target + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(entries, WriteOptions));
        File.Move(temporary, target);

        // The queue waits until a file has not been written to for a while, for files that are still
        // being edited by hand. This one is complete, so it may be picked up right away.
        File.SetLastWriteTimeUtc(target, DateTime.UtcNow - EmulatorQueueFolder.SettleTime - TimeSpan.FromSeconds(1));

        result.FilePath = target;
        return result;
    }


    private static RunData? Load(int runId)
    {
        using var database = new CryptoDatabase();
        database.Open();
        return database.Connection.QueryFirstOrDefault<RunData>(
            "select Id, Label, ConfigJson, SettingsJson from EmulatorRun where Id = @id", new { id = runId });
    }


    /// <summary>
    /// The entry that repeats <paramref name="run"/>, or null with the reason why it cannot be repeated.
    /// </summary>
    private static string? BuildEntry(RunData run, out EmulatorQueueEntry? entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(run.SettingsJson))
            return "the run has no stored settings";
        if (string.IsNullOrWhiteSpace(run.ConfigJson))
            return "the run has no stored run configuration";

        JsonNode? settings;
        EmulatorRunConfig? config;
        try
        {
            settings = JsonNode.Parse(run.SettingsJson);
            config = JsonSerializer.Deserialize<EmulatorRunConfig>(run.ConfigJson, ReadOptions);
        }
        catch (JsonException ex)
        {
            return $"the stored settings cannot be read: {ex.Message}";
        }
        if (settings == null || config == null)
            return "the stored settings cannot be read";

        // A queue run measures one strategy; the strategy lists of the snapshot say which one, and
        // which side was off
        List<string> signalLong = ReadList(settings["Signal"]?["Long"]?["Strategy"]);
        List<string> signalShort = ReadList(settings["Signal"]?["Short"]?["Strategy"]);
        List<string> tradingLong = ReadList(settings["Trading"]?["Long"]?["Strategy"]);
        List<string> tradingShort = ReadList(settings["Trading"]?["Short"]?["Strategy"]);
        List<string> names = signalLong.Concat(signalShort).Concat(tradingLong).Concat(tradingShort)
            .Select(PluginManager.CurrentName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (names.Count != 1)
            return names.Count == 0
                ? "the run has no strategy"
                : $"the run has {names.Count} strategies ({string.Join(", ", names)}), a queue entry measures one";

        string algorithm = names[0];
        AlgorithmDefinition? definition = RegisterAlgorithms.AlgorithmDefinitionList.Values
            .FirstOrDefault(a => a.Name.Equals(algorithm, StringComparison.OrdinalIgnoreCase));
        if (definition == null)
            return $"strategy \"{algorithm}\" is not registered in this build";

        bool longOn = signalLong.Count > 0 || tradingLong.Count > 0;
        bool shortOn = signalShort.Count > 0 || tradingShort.Count > 0;

        string label = string.IsNullOrWhiteSpace(run.Label) ? definition.Name : run.Label.Trim();
        entry = new EmulatorQueueEntry
        {
            Label = $"{label} (RMK #{run.Id.ToString(CultureInfo.InvariantCulture)})",
            Algorithm = definition.Name,
            SettingsFromRun = run.Id.ToString(CultureInfo.InvariantCulture),
            Symbols = config.Symbols is { Count: > 0 } ? [.. config.Symbols] : null,
            FromDate = config.FromDate,
            ToDate = config.ToDate,
            BaseInterval = string.IsNullOrWhiteSpace(config.BaseInterval) ? null : config.BaseInterval,
            CalculateBarometer = config.CalculateBarometer,
            // Only when the run booked funding: null keeps the default (off) for everything older
            Funding = config.BookFunding ? true : null,
            Long = longOn ? null : new EmulatorSideConfig { Enabled = false },
            Short = shortOn ? null : new EmulatorSideConfig { Enabled = false },
            Force = true,
            ReplacesRun = run.Id,
        };
        return null;
    }


    private static List<string> ReadList(JsonNode? node)
    {
        if (node is not JsonArray array)
            return [];
        return array.Select(n => n?.GetValue<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList();
    }
}

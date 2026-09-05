using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

using System.Text.Json;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Persists and loads the queue file (<c>CryptoScanBot-Emulator-Queue.json</c>) that lives
/// alongside the regular emulator config. Each entry is a self-contained parameter set
/// (SL + TP + DCA) fed to the engine as one run — no matrix explosion.
/// </summary>
public static class EmulatorQueueFile
{
    public const string FileName = "CryptoScanBot-Emulator-Queue.json";

    public static string FilePath => Path.Combine(GlobalData.AppDataFolder, FileName);


    public static List<EmulatorQueueEntry> Load()
    {
        string path = FilePath;
        if (!File.Exists(path))
        {
            List<EmulatorQueueEntry> placeholder = BuildPlaceholder();
            Save(placeholder);
            return placeholder;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            List<EmulatorQueueEntry>? loaded = JsonSerializer.Deserialize<List<EmulatorQueueEntry>>(stream, ReadOptions);
            return loaded ?? [];
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"EmulatorQueueFile.Load FAILED: {ex.Message} — returning empty queue");
            return [];
        }
    }


    /// <summary>
    /// Reads a queue file wherever it is - the files in the Queue folder have the same shape as the
    /// one next to the data. Unlike <see cref="Load"/> this throws on a file that cannot be read,
    /// because the folder queue has to know the difference between "empty" and "broken": the first
    /// is done, the second goes to the Failed folder.
    /// </summary>
    public static List<EmulatorQueueEntry> LoadFrom(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<EmulatorQueueEntry>>(stream, ReadOptions) ?? [];
    }


    public static void Save(List<EmulatorQueueEntry> entries)
    {
        string path = FilePath;
        string json = JsonSerializer.Serialize(entries, WriteOptions);
        File.WriteAllText(path, json);
    }


    /// <summary>
    /// Turns <c>"Force": true</c> off for one entry in a queue file, after that entry has run.
    /// <para>
    /// Force exists to replay a run the duplicate check would skip. Once that replay is in the
    /// database the flag has done its work, and left in the file it makes the entry run AGAIN on
    /// every restart of the batch - which is what happened on 05-09-2026: 45 entries were skipped
    /// as duplicates and the one with Force spent forty minutes reproducing run 838. Taking the
    /// flag out of the file, rather than the entry out of the file, keeps the entry where it is
    /// documented and where the archive copy expects it.
    /// </para>
    /// <para>
    /// The file is edited in place, byte for byte: only the word <c>true</c> behind that one Force
    /// becomes <c>false</c>. Everything else - the one-line-per-entry layout, comments, the order of
    /// the properties - stays exactly as someone wrote it, which a round trip through the serializer
    /// would not do. The entry is found by its label, so a queue that another session extended in
    /// the meantime (the file is shared) still gets the right line; an entry without a label is
    /// found by its position instead.
    /// </para>
    /// </summary>
    /// <returns>True when the file was changed; false when the entry has no Force to turn off,
    /// when it is not in the file, or when the file cannot be read.</returns>
    public static bool ResetForce(string path, string? label, int index)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"EmulatorQueueFile.ResetForce FAILED reading {path}: {ex.Message}");
            return false;
        }

        long position = FindForceTrue(bytes, label, index);
        if (position < 0)
            return false;

        // "true" is four bytes, "false" is five: splice rather than overwrite.
        byte[] replacement = "false"u8.ToArray();
        byte[] result = new byte[bytes.Length + 1];
        Buffer.BlockCopy(bytes, 0, result, 0, (int)position);
        Buffer.BlockCopy(replacement, 0, result, (int)position, replacement.Length);
        Buffer.BlockCopy(bytes, (int)position + 4, result, (int)position + replacement.Length, bytes.Length - (int)position - 4);

        try
        {
            File.WriteAllBytes(path, result);
            return true;
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"EmulatorQueueFile.ResetForce FAILED writing {path}: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// The byte offset of the <c>true</c> behind the Force property of the entry that matches
    /// <paramref name="label"/> (or, without a label, sits at <paramref name="index"/>), or -1.
    /// Walks the tokens with <see cref="Utf8JsonReader"/> so the offset is exact whatever the
    /// layout of the file is.
    /// </summary>
    private static long FindForceTrue(byte[] bytes, string? label, int index)
    {
        // Utf8JsonReader does not skip a byte order mark; the offsets it reports are relative to
        // the span it is given, so remember what was cut off in front.
        int bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        bool matchByLabel = !string.IsNullOrWhiteSpace(label);

        try
        {
            var reader = new Utf8JsonReader(bytes.AsSpan(bom), new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            int entryIndex = -1;
            string? entryLabel = null;
            long forceTrueAt = -1;

            while (reader.Read())
            {
                // The entries are the objects directly inside the outer array (depth 1); their own
                // properties sit at depth 2. Nested blocks such as Trading are deeper and are skipped.
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 1)
                {
                    entryIndex++;
                    entryLabel = null;
                    forceTrueAt = -1;
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1)
                {
                    bool matches = matchByLabel
                        ? string.Equals(entryLabel, label, StringComparison.Ordinal)
                        : entryIndex == index;
                    if (matches)
                        return forceTrueAt < 0 ? -1 : forceTrueAt + bom;
                    continue;
                }

                if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 2)
                    continue;

                string? name = reader.GetString();
                if (!reader.Read())
                    break;

                if (string.Equals(name, "Label", StringComparison.OrdinalIgnoreCase)
                    && reader.TokenType == JsonTokenType.String)
                    entryLabel = reader.GetString();
                else if (string.Equals(name, "Force", StringComparison.OrdinalIgnoreCase)
                    && reader.TokenType == JsonTokenType.True)
                    forceTrueAt = reader.TokenStartIndex;
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    reader.Skip();
            }
        }
        catch (JsonException ex)
        {
            GlobalData.AddTextToLogTab($"EmulatorQueueFile.ResetForce: file cannot be parsed - {ex.Message}");
        }

        return -1;
    }


    /// <summary>
    /// The folder the queue backups live in, next to the queue file itself. They used to pile up in
    /// the data folder beside the file the emulator reads - 62 of them in five weeks, in thirteen
    /// different naming shapes, because every session invented its own name by hand.
    /// </summary>
    public static string ArchiveFolder => Path.Combine(GlobalData.AppDataFolder, "Queue-archive");


    /// <summary>
    /// Copies the queue into the archive folder before a batch starts, and returns the file it
    /// wrote - or the existing copy it matched, or null when there was nothing to copy.
    /// <para>
    /// One copy per batch, named after the moment the batch started, so a backup corresponds to
    /// exactly what ran. A batch that is restarted on an unchanged queue matches the previous copy
    /// and no second file is written: three restarts in a day would otherwise leave three identical
    /// files, which is how the pile got there in the first place.
    /// </para>
    /// <para>
    /// Never throws. A backup that cannot be written is worth a line in the log, but it is not a
    /// reason to refuse to run the batch.
    /// </para>
    /// </summary>
    public static string? ArchiveBeforeRun()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
                return null;

            Directory.CreateDirectory(ArchiveFolder);
            string current = File.ReadAllText(path);

            // Only the copies made here, by name: the hand-made backups moved into this folder carry
            // the full file name and must not be compared against or overwritten.
            FileInfo? newest = new DirectoryInfo(ArchiveFolder)
                .GetFiles("Queue-*.json")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            if (newest != null && File.ReadAllText(newest.FullName) == current)
                return newest.FullName;

            // To the minute, plus a counter for the second batch inside the same minute - which only
            // happens on a queue that changed, or the copy above would have matched.
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string target = Path.Combine(ArchiveFolder, $"Queue-{stamp}.json");
            for (int i = 2; File.Exists(target); i++)
                target = Path.Combine(ArchiveFolder, $"Queue-{stamp}-{i}.json");

            File.WriteAllText(target, current);
            return target;
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"EmulatorQueueFile.ArchiveBeforeRun FAILED: {ex.Message}");
            return null;
        }
    }


    private static List<EmulatorQueueEntry> BuildPlaceholder() =>
    [
        new EmulatorQueueEntry
        {
            Label = "conservative",
            StopLossPercentage = 2m,
            TpList = [new CryptoTpEntry { Percentage = 1.5m, Factor = 100m }],
            DcaList = [],
        },
        new EmulatorQueueEntry
        {
            Label = "with-dca",
            StopLossPercentage = 3m,
            TpList = [new CryptoTpEntry { Percentage = 1.5m, Factor = 100m }],
            DcaList = [new CryptoDcaEntry { Factor = 200m, Percentage = 3.0m }],
        },
    ];


    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

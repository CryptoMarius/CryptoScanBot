using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;

using System.Text.Json;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Reads a queue file from the Queue folder. Each entry is a self-contained parameter set
/// (SL + TP + DCA) fed to the engine as one run — no matrix explosion.
/// <para>
/// Until 06-09-2026 there was a single queue file (<c>CryptoScanBot-Emulator-Queue.json</c>) next to
/// the emulator config, read once when a batch started. It is not read any more: every queue is a
/// file in the Queue folder, which is read the moment it is its turn and moved to Done afterwards.
/// The file itself may stay in the data folder - nothing looks at it. What went with it: the
/// placeholder queue that was written when the file was missing, and the Queue-archive copy that was
/// made before a batch, because the Done folder now holds the file that actually ran.
/// </para>
/// </summary>
public static class EmulatorQueueFile
{
    /// <summary>
    /// Reads one queue file. Throws on a file that cannot be read, because the folder queue has to
    /// know the difference between "empty" and "broken": the first is done, the second goes to the
    /// Failed folder.
    /// </summary>
    public static List<EmulatorQueueEntry> LoadFrom(string path)
    {
        using FileStream stream = File.OpenRead(path);
        List<EmulatorQueueEntry> entries =
            JsonSerializer.Deserialize<List<EmulatorQueueEntry>>(stream, ReadOptions) ?? [];
        foreach (EmulatorQueueEntry entry in entries)
            RenameLegacyStrategy(entry);
        return entries;
    }


    /// <summary>
    /// A queue file written before a strategy was renamed names the old strategy, both as the
    /// entry's Algorithm and as the section its SignalOverrides sit under. Both are translated on
    /// the way in, so a file that has been waiting in the folder across a rename still runs.
    /// </summary>
    private static void RenameLegacyStrategy(EmulatorQueueEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.Algorithm))
            entry.Algorithm = PluginManager.CurrentName(entry.Algorithm);

        foreach (string section in entry.SignalOverrides.Keys.ToList())
        {
            string current = PluginManager.CurrentName(section);
            if (current == section || entry.SignalOverrides.ContainsKey(current))
                continue;
            entry.SignalOverrides[current] = entry.SignalOverrides[section];
            entry.SignalOverrides.Remove(section);
        }
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


    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

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

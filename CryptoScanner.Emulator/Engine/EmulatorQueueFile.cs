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
            Console.WriteLine($"EmulatorQueueFile.Load: {ex.Message} — returning empty queue");
            return [];
        }
    }


    public static void Save(List<EmulatorQueueEntry> entries)
    {
        string path = FilePath;
        string json = JsonSerializer.Serialize(entries, WriteOptions);
        File.WriteAllText(path, json);
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
    };
}

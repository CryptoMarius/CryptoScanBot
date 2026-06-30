using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.Emulator;

/// <summary>
/// Persists the user-editable run parameters (which symbols, what period, label) to a small
/// JSON file alongside the emulator's settings.json. Strategies, intervals and trend filters
/// are NOT in this file — those come from the regular scanner settings the Configure dialog
/// edits. Once the run UI has proper widgets this whole helper can be replaced by direct
/// bindings; for the first cut JSON is the simplest editable surface.
/// </summary>
public static class RunConfigFile
{
    public const string FileName = "CryptoScanBot-Emulator.json";

    public static string FilePath => Path.Combine(GlobalData.AppDataFolder, FileName);


    /// <summary>
    /// Reads <see cref="FileName"/> from the emulator data folder. Creates a sensible
    /// placeholder file when none exists so the user has something to edit.
    /// </summary>
    public static EmulatorRunConfig Load()
    {
        string path = FilePath;
        if (!File.Exists(path))
        {
            EmulatorRunConfig defaults = BuildPlaceholder();
            Save(defaults);
            return defaults;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            EmulatorRunConfig? loaded = JsonSerializer.Deserialize<EmulatorRunConfig>(stream, ReadOptions);
            return loaded ?? BuildPlaceholder();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RunConfigFile.Load: {ex.Message} — falling back to placeholder");
            return BuildPlaceholder();
        }
    }


    public static void Save(EmulatorRunConfig config)
    {
        string path = FilePath;
        string json = JsonSerializer.Serialize(config, WriteOptions);
        File.WriteAllText(path, json);
    }


    /// <summary>
    /// Default skeleton — one example symbol over the last week. Saved on first start so the
    /// user has a clear template to edit; never auto-overwrites an existing file.
    /// </summary>
    private static EmulatorRunConfig BuildPlaceholder()
    {
        DateTime to = DateTime.UtcNow.Date;
        DateTime from = to.AddDays(-7);
        return new EmulatorRunConfig
        {
            ExchangeName = GlobalData.ActiveExchange?.Name ?? "Binance Futures",
            Symbols = ["BTCUSDT"],
            FromDate = from,
            ToDate = to,
            Label = "first-run",
        };
    }


    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}

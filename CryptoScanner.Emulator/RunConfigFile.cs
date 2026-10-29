using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;
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

        EmulatorRunConfig? loaded;
        try
        {
            // Read in a block of its own so the file is closed again before anything writes it: the
            // move of the retired fields below saves this same file, and with the stream still open
            // that failed and Load handed out the placeholder, which the run-config window then saved
            // over the 178 coins of HyperLiquid Session1 (26-10-2026).
            using (FileStream stream = File.OpenRead(path))
                loaded = JsonSerializer.Deserialize<EmulatorRunConfig>(stream, ReadOptions);
            if (loaded == null)
                return BuildPlaceholder();

            // A file written before 27-08-2026 still says "<exchange> Futures"
            loaded.ExchangeName = GlobalData.FixLegacyExchangeName(loaded.ExchangeName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RunConfigFile.Load: {ex.Message} — falling back to placeholder");
            return BuildPlaceholder();
        }

        // Outside the try above: the configuration that was read is good, whatever happens here
        try
        {
            MoveRetiredAssetFieldsToTheSettings(loaded);
        }
        catch (Exception ex)
        {
            GlobalData.AddErrorToLogTab($"Run configuration: moving the start capital to the settings failed - {ex.Message}");
        }
        return loaded;
    }


    /// <summary>
    /// A file from before 26-10-2026 still holds the run's own start capital and asset-management
    /// switch. Both moved to the settings of the session: the start capital into the Default column of
    /// Paper assets, for every traded quote coin - but only when that column is still empty, because a
    /// filled list already won from the start capital back then - and the switch into
    /// Settings.Trading.UseAssetManagement. Done once: the fields are cleared and the file is saved.
    /// <para>
    /// Without settings that know their quote coins there is nothing to hand the amount to, so the
    /// fields are left in the file for the next load.
    /// </para>
    /// </summary>
    private static void MoveRetiredAssetFieldsToTheSettings(EmulatorRunConfig config)
    {
        if (config.StartCapital == null && config.UseAssetManagement == null)
            return;
        if (GlobalData.Settings.QuoteCoins.Count == 0)
            return;

        bool settingsChanged = false;
        if (config.StartCapital is decimal startCapital && GlobalData.Settings.Trading.PaperAssetDefaults.Count == 0)
        {
            List<CryptoPaperAssetDefault> defaults = EmulatorStartCapital.FromLegacy(startCapital, GlobalData.Settings.QuoteCoins.Values);
            if (defaults.Count > 0)
            {
                GlobalData.Settings.Trading.PaperAssetDefaults = defaults;
                settingsChanged = true;
                GlobalData.AddTextToLogTab("Start capital of the run configuration moved to the settings (Trading.PaperAssetDefaults): " +
                    string.Join(", ", defaults.Select(d => $"{d.Name} {d.Total.ToString0()}")));
            }
        }
        if (config.UseAssetManagement is bool useAssetManagement && GlobalData.Settings.Trading.UseAssetManagement != useAssetManagement)
        {
            GlobalData.Settings.Trading.UseAssetManagement = useAssetManagement;
            settingsChanged = true;
            GlobalData.AddTextToLogTab($"Asset management of the run configuration moved to the settings: {(useAssetManagement ? "on" : "off")}");
        }

        if (settingsChanged)
            SaveSettings();
        config.StartCapital = null;
        config.UseAssetManagement = null;
        Save(config);
    }


    /// <summary>
    /// Writes the settings file after the retired fields moved into it. A property so the tests can
    /// keep their hands off the real settings file, the same way PaperAssetsEditor.SaveSettings does.
    /// </summary>
    internal static Action SaveSettings { get; set; } = GlobalData.SaveConfiguration;


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
            ExchangeName = GlobalData.ActiveExchange?.Name ?? "Binance Perpetual",
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

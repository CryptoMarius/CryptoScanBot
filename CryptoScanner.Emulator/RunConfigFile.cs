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
            {
                //return BuildPlaceholder();
                // Marked so it is never saved over the file (31-10-2026), see UnreadPlaceholders
                GlobalData.AddErrorToLogTab($"Run configuration: {path} holds no configuration - a placeholder is used and the file is not overwritten");
                return BuildUnreadPlaceholder();
            }

            // A file written before 27-08-2026 still says "<exchange> Futures"
            loaded.ExchangeName = GlobalData.FixLegacyExchangeName(loaded.ExchangeName);
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"RunConfigFile.Load: {ex.Message} — falling back to placeholder");
            //return BuildPlaceholder();
            // Since 31-10-2026 in the log (an Avalonia application has no console), and the placeholder
            // is marked: a sort click, "Run algorithms..." or OK in Run parameters loads, changes one
            // field and saves, and an unmarked placeholder then replaced the coin list and the period.
            GlobalData.AddErrorToLogTab($"Run configuration: {path} cannot be read - {ex.Message}. A placeholder is used and the file is not overwritten until it is repaired.");
            return BuildUnreadPlaceholder();
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


    /// <summary>
    /// Writes <paramref name="config"/> to the file. Returns false, writes nothing and logs the reason
    /// when the configuration is the placeholder of a file that could not be read (since 31-10-2026):
    /// saving it would replace the coin list and the period of the real file.
    /// </summary>
    public static bool Save(EmulatorRunConfig config)
    {
        string path = FilePath;
        if (IsUnreadPlaceholder(config))
        {
            GlobalData.AddErrorToLogTab($"Run configuration: not saved - {path} could not be read, and saving the placeholder would overwrite it. Repair or remove the file first.");
            return false;
        }

        string json = JsonSerializer.Serialize(config, WriteOptions);
        //File.WriteAllText(path, json);
        // Through a temporary file, like RunSettingsApplier: a write that breaks off halfway left a
        // file that could not be read back, and the next Load handed out the placeholder.
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, path, overwrite: true);
        return true;
    }


    /// <summary>
    /// The placeholders handed out for a file that exists but could not be read. Kept by reference,
    /// so a caller that loads, changes a field and saves is refused, while a configuration built from
    /// scratch is not. Weak, so a forgotten placeholder does not stay alive.
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EmulatorRunConfig, object> UnreadPlaceholders = new();


    /// <summary>True when <paramref name="config"/> is the placeholder of a file that could not be read.</summary>
    public static bool IsUnreadPlaceholder(EmulatorRunConfig config) => UnreadPlaceholders.TryGetValue(config, out _);


    private static EmulatorRunConfig BuildUnreadPlaceholder()
    {
        EmulatorRunConfig placeholder = BuildPlaceholder();
        UnreadPlaceholders.AddOrUpdate(placeholder, new object());
        return placeholder;
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

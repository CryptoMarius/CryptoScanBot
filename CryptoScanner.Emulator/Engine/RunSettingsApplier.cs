using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Makes the settings a run was made with the emulator's current settings: written over the
/// settings file of the data folder, and loaded into memory the same way the emulator loads them at
/// startup. Behind the "Use these settings" choice of the Results tab.
/// </summary>
public static class RunSettingsApplier
{
    /// <summary>
    /// The snapshot laid OVER the current settings rather than in their place. A run stores the
    /// settings of the build that made it; a later build may have added a setting that the snapshot
    /// knows nothing about. Taken over as it is, every such setting would silently fall back to its
    /// default - now it keeps its current value, and everything the run did say wins.
    /// <para>
    /// Objects are merged key by key (that includes keyed collections such as the quote coins and
    /// the analyzer blocks); a value or a list from the snapshot replaces the current one whole, so a
    /// DCA ladder of two levels does not keep the third level of the current settings.
    /// </para>
    /// </summary>
    internal static JsonNode Overlay(JsonNode current, JsonNode snapshot)
    {
        if (current is JsonObject currentObject && snapshot is JsonObject snapshotObject)
        {
            foreach (var (key, value) in snapshotObject.ToList())
            {
                if (value != null && currentObject[key] is JsonNode existing)
                    currentObject[key] = Overlay(existing.DeepClone(), value.DeepClone());
                else
                    currentObject[key] = value?.DeepClone();
            }
            return currentObject;
        }
        return snapshot.DeepClone();
    }


    /// <summary>
    /// Builds the settings that would result from taking over the run's snapshot: the current
    /// settings with the snapshot laid over them, the exchange left as it is now (the emulator is
    /// bound to its exchange at startup). Returns the JSON text, or null with a reason when the
    /// result cannot be read back as settings.
    /// </summary>
    internal static string? BuildMergedSettings(string currentJson, string snapshotJson, out string reason)
    {
        reason = "";
        JsonNode? current, snapshot;
        try
        {
            current = JsonNode.Parse(currentJson);
            snapshot = JsonNode.Parse(snapshotJson);
        }
        catch (JsonException ex)
        {
            reason = $"the stored settings are not valid JSON: {ex.Message}";
            return null;
        }
        if (current is not JsonObject || snapshot is not JsonObject)
        {
            reason = "the stored settings are not a JSON object";
            return null;
        }

        // Keep the exchange the emulator was started on. Read before the overlay, which merges into
        // the current object itself.
        JsonNode? exchangeName = current["General"]?["ExchangeName"]?.DeepClone();
        JsonNode? activateExchangeName = current["General"]?["ActivateExchangeName"]?.DeepClone();

        JsonNode merged = Overlay(current, snapshot);

        if (merged["General"] is JsonObject mergedGeneral)
        {
            mergedGeneral["ExchangeName"] = exchangeName;
            mergedGeneral["ActivateExchangeName"] = activateExchangeName;
        }

        string text = merged.ToJsonString(JsonTools.JsonSerializerIndented);
        try
        {
            if (JsonSerializer.Deserialize<SettingsBasic>(text, JsonTools.DeSerializerOptions) == null)
            {
                reason = "the merged settings cannot be read";
                return null;
            }
        }
        catch (Exception ex)
        {
            reason = $"the merged settings cannot be read: {ex.Message}";
            return null;
        }
        return text;
    }


    /// <summary>
    /// Takes over the settings of run <paramref name="runId"/>: a dated copy of the current settings
    /// file is kept next to it, the merged settings are written over it, and they are loaded into
    /// memory with the same calls the emulator makes at startup (EmulatorBootstrap) - the analyzer
    /// plugins get their values back and the strategy indexes are rebuilt. Refused while a run is
    /// active, because the replay applies its own overrides on top of the settings and restores them
    /// afterwards.
    /// </summary>
    public static bool Apply(int runId, out string message)
    {
        if (GlobalData.CurrentEmulatorRunId != null)
        {
            message = "A run is active - take over settings once it has finished.";
            return false;
        }

        string? snapshotJson = EmulatorDb.GetSettingsJson(runId);
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            message = $"Run #{runId} has no stored settings.";
            return false;
        }

        // The current settings as they would be saved, plugin values included
        PluginManager.CollectSettings(GlobalData.Settings.Signal.AnalyzerSettings);
        string currentJson = JsonSerializer.Serialize(GlobalData.Settings, JsonTools.JsonSerializerIndented);

        string? merged = BuildMergedSettings(currentJson, snapshotJson, out string reason);
        if (merged == null)
        {
            message = $"Settings of run #{runId} not taken over: {reason}.";
            return false;
        }

        string folder = GlobalData.AppDataFolder;
        string fileName = Path.Combine(folder, $"{Constants.AppName}-settings.json");
        string backupName = $"{fileName}.backup-{DateTime.Now:yyyyMMdd-HHmm}-before-run-{runId}";
        if (File.Exists(fileName))
            File.Copy(fileName, backupName, overwrite: true);

        string temporary = fileName + ".tmp";
        File.WriteAllText(temporary, merged);
        File.Move(temporary, fileName, overwrite: true);

        // Read it back the way the emulator reads it at startup, fix-ups included
        GlobalData.LoadScannerConfiguration();
        if (GlobalData.SettingsLoadFailed)
        {
            message = $"Settings of run #{runId} were written but could not be read back; the previous file is in {Path.GetFileName(backupName)}.";
            return false;
        }

        PluginManager.RestoreSettings(GlobalData.Settings.Signal.AnalyzerSettings);
        GlobalData.IndexStrategySettings();
        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();
        BarometerTools.InitBarometerSymbols();
        SignalPrepare.Prepare();
        SignalExecute.Prepare();

        message = $"Settings of run #{runId} are now the current settings (previous file kept as {Path.GetFileName(backupName)}).";
        GlobalData.AddTextToLogTab(message);
        return true;
    }
}

using CryptoScanner.Core.Json;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;

using NLog;

using System.Text.Json;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Registry for analyzer strategy plugins. Strategies register via
/// <see cref="Register"/> at startup (called from the Analyzers project).
/// No runtime DLL scanning — the host project references Analyzers directly.
/// </summary>
public static class PluginManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>The plugin per strategy NAME (one plugin can serve several strategies).</summary>
    public static IReadOnlyDictionary<string, IStrategyPlugin> LoadedPlugins => _plugins;
    private static readonly Dictionary<string, IStrategyPlugin> _plugins = [];

    /// <summary>Chart overlays provided by registered plugins.</summary>
    public static IReadOnlyList<IChartOverlay> ChartOverlays => _overlays;
    private static readonly List<IChartOverlay> _overlays = [];


    //public static void RegisterOverlay(IChartOverlay overlay)
    //{
    //    if (!_overlays.Contains(overlay))
    //        _overlays.Add(overlay);
    //}

    /// <summary>Config view providers from registered plugins.</summary>
    public static IReadOnlyList<IConfigView> ConfigViews => _configViews;
    private static readonly List<IConfigView> _configViews = [];

    /// <summary>
    /// Register a strategy plugin (may contain multiple sub-strategies).
    /// Called at startup from the Analyzers project's AnalyzerRegistration.RegisterAll().
    /// </summary>
    public static void Register(IStrategyPlugin plugin)
    {
        foreach (var reg in plugin.Strategies)
        {
            if (RegisterAlgorithms.AlgorithmDefinitionList.ContainsKey(reg.Name))
            {
                Logger.Warn($"Strategy {reg.Name} already registered, skipping");
                continue;
            }

            RegisterAlgorithms.Register(new AlgorithmDefinition()
            {
                Name = reg.Name,
                AnalyzeLongType = reg.AnalyzeLongType,
                AnalyzeShortType = reg.AnalyzeShortType,
                IsZoneStrategy = reg.IsZoneStrategy,
            });

            _plugins[reg.Name] = plugin;
        }

        if (plugin.ChartOverlay != null)
            _overlays.Add(plugin.ChartOverlay);

        if (plugin.ConfigView != null)
            _configViews.Add(plugin.ConfigView);

        Logger.Info($"Registered analyzer \"{plugin.StrategyName}\" ({plugin.Strategies.Count} strategy/strategies)");
    }

    /// <summary>
    /// Restore plugin settings from the deserialized SettingsSignal.AnalyzerSettings
    /// dictionary. Called after settings JSON has been loaded.
    /// Each entry is a raw JSON block; only the plugin knows its concrete settings
    /// type, so we deserialize the block directly into that type here.
    /// </summary>
    public static void RestoreSettings(IDictionary<string, JsonElement> stored)
    {
        foreach (var plugin in _plugins.Values.Distinct())
        {
            if (!stored.TryGetValue(plugin.StrategyName, out var element))
            {
                // A settings file written before this plugin was renamed still has the old key.
                string? former = plugin.FormerStrategyNames.FirstOrDefault(stored.ContainsKey);
                if (former == null)
                    continue;
                element = stored[former];
                Logger.Info($"Settings for {plugin.StrategyName} restored from its former name \"{former}\"");
            }

            try
            {
                var concreteType = plugin.SettingsBase.GetType();
                if (element.Deserialize(concreteType, JsonTools.DeSerializerOptions) is SettingsSignalStrategyBase settings)
                    plugin.SettingsBase = settings;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to restore settings for {plugin.StrategyName}, using defaults");
            }
        }
    }

    /// <summary>
    /// Collect current plugin settings into the provided dictionary, ready
    /// for serialization as part of the normal settings JSON.
    /// Serialized with the runtime type so derived properties are included
    /// (the declared base type would only write the base-class properties).
    /// Upserts only the entries of currently registered plugins; entries of
    /// plugins that are not loaded in this host (disabled, DEBUG-only, or a
    /// host without the Analyzers project) are left untouched so their stored
    /// settings are never wiped by a save.
    /// </summary>
    public static void CollectSettings(IDictionary<string, JsonElement> target)
    {
        foreach (var plugin in _plugins.Values.Distinct())
        {
            target[plugin.StrategyName] = JsonSerializer.SerializeToElement(
                plugin.SettingsBase, plugin.SettingsBase.GetType(), JsonTools.JsonSerializerIndented);
        }
    }


    /// <summary>
    /// The plugin that answers to this name, either by its current name or by a name it carried
    /// before a rename. Null when no plugin does.
    /// </summary>
    public static IStrategyPlugin? FindByName(string strategyName)
    {
        foreach (var plugin in _plugins.Values.Distinct())
        {
            if (plugin.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase))
                return plugin;
        }
        foreach (var plugin in _plugins.Values.Distinct())
        {
            foreach (string former in plugin.FormerStrategyNames)
            {
                if (former.Equals(strategyName, StringComparison.OrdinalIgnoreCase))
                    return plugin;
            }
        }
        return null;
    }


    /// <summary>
    /// The name a strategy goes by today, given a name that may predate a rename. A name nobody
    /// claims is handed back untouched, so an unknown algorithm still fails where it used to.
    /// </summary>
    public static string CurrentName(string strategyName) =>
        FindByName(strategyName)?.StrategyName ?? strategyName;


    /// <summary>The live settings instance of a plugin, or null when no plugin uses that name.</summary>
    public static SettingsSignalStrategyBase? LiveSettings(string strategyName)
    {
        foreach (var plugin in _plugins.Values.Distinct())
        {
            if (plugin.StrategyName == strategyName)
                return plugin.SettingsBase;
        }
        return null;
    }


    /// <summary>
    /// A SEPARATE settings instance for a plugin, deserialized from a stored AnalyzerSettings block.
    /// Unlike <see cref="RestoreSettings"/> this does not touch the plugin's live settings, so a
    /// stored set can be shown alongside the running one. Returns null when the block is missing or
    /// cannot be read — the caller then falls back to the live settings.
    /// </summary>
    public static SettingsSignalStrategyBase? MaterializeSettings(string strategyName, IDictionary<string, JsonElement> stored)
    {
        // By the current name or by one it carried before a rename, so a snapshot written under the
        // old name still materializes into the concrete settings type of the plugin that owns it.
        IStrategyPlugin? plugin = FindByName(strategyName);
        if (plugin == null)
            return null;

        if (!stored.TryGetValue(strategyName, out JsonElement element)
            && !stored.TryGetValue(plugin.StrategyName, out element))
        {
            string? former = plugin.FormerStrategyNames.FirstOrDefault(stored.ContainsKey);
            if (former == null)
                return null;
            element = stored[former];
        }

        try
        {
            return element.Deserialize(plugin.SettingsBase.GetType(), JsonTools.DeSerializerOptions)
                as SettingsSignalStrategyBase;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Failed to read stored settings for {strategyName}");
            return null;
        }
    }
}

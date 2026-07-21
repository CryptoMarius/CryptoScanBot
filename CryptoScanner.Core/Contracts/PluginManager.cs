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

    public static IReadOnlyDictionary<Enums.CryptoSignalStrategy, IStrategyPlugin> LoadedPlugins => _plugins;
    private static readonly Dictionary<Enums.CryptoSignalStrategy, IStrategyPlugin> _plugins = [];

    /// <summary>Chart overlays provided by registered plugins.</summary>
    public static IReadOnlyList<IChartOverlay> ChartOverlays => _overlays;
    private static readonly List<IChartOverlay> _overlays = [];

    /// <summary>
    /// Register a stand-alone chart overlay that is not tied to a strategy plugin (e.g. the
    /// TradingBuddy band overlay). It shows up as its own checkbox in the chart's overlay list.
    /// </summary>
    public static void RegisterOverlay(IChartOverlay overlay)
    {
        if (!_overlays.Contains(overlay))
            _overlays.Add(overlay);
    }

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
            if (RegisterAlgorithms.AlgorithmDefinitionList.ContainsKey(reg.Strategy))
            {
                Logger.Warn($"Strategy {reg.Strategy} ({reg.Name}) already registered, skipping");
                continue;
            }

            RegisterAlgorithms.Register(new AlgorithmDefinition()
            {
                Name = reg.Name,
                Strategy = reg.Strategy,
                AnalyzeLongType = reg.AnalyzeLongType,
                AnalyzeShortType = reg.AnalyzeShortType,
            });

            _plugins[reg.Strategy] = plugin;
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
    public static void RestoreSettings(Dictionary<string, JsonElement> stored)
    {
        foreach (var plugin in _plugins.Values.Distinct())
        {
            if (!stored.TryGetValue(plugin.StrategyName, out var element))
                continue;

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
    public static void CollectSettings(Dictionary<string, JsonElement> target)
    {
        foreach (var plugin in _plugins.Values.Distinct())
        {
            target[plugin.StrategyName] = JsonSerializer.SerializeToElement(
                plugin.SettingsBase, plugin.SettingsBase.GetType(), JsonTools.JsonSerializerIndented);
        }
    }
}

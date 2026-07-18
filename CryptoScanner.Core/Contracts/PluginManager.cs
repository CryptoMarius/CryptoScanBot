using System.Text.Json;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;

using NLog;

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

    /// <summary>Config view providers from registered plugins.</summary>
    public static IReadOnlyList<IConfigView> ConfigViews => _configViews;
    private static readonly List<IConfigView> _configViews = [];

    /// <summary>
    /// Register a single strategy plugin. Called at startup from the Analyzers
    /// project's AnalyzerRegistration.RegisterAll().
    /// </summary>
    public static void Register(IStrategyPlugin plugin)
    {
        if (RegisterAlgorithms.AlgorithmDefinitionList.ContainsKey(plugin.Strategy))
        {
            Logger.Warn($"Strategy {plugin.Strategy} ({plugin.Name}) already registered, skipping");
            return;
        }

        RegisterAlgorithms.Register(new AlgorithmDefinition()
        {
            Name = plugin.Name,
            Strategy = plugin.Strategy,
            AnalyzeLongType = plugin.AnalyzeLongType,
            AnalyzeShortType = plugin.AnalyzeShortType,
        });

        _plugins[plugin.Strategy] = plugin;

        if (plugin.ChartOverlay != null)
            _overlays.Add(plugin.ChartOverlay);

        if (plugin.ConfigView != null)
            _configViews.Add(plugin.ConfigView);

        Logger.Info($"Registered analyzer \"{plugin.Name}\" (strategy={plugin.Strategy})");
    }

    /// <summary>
    /// Restore plugin settings from the deserialized SettingsSignal.AnalyzerSettings
    /// dictionary. Called after settings JSON has been loaded.
    /// Because the dictionary is typed as SettingsSignalStrategyBase, System.Text.Json
    /// only populates base-class properties. We round-trip through JSON to rehydrate
    /// the concrete plugin settings type so derived properties are preserved.
    /// </summary>
    public static void RestoreSettings(Dictionary<string, SettingsSignalStrategyBase> stored)
    {
        foreach (var (_, plugin) in _plugins)
        {
            if (stored.TryGetValue(plugin.Name, out var loaded) && loaded != null)
            {
                plugin.SettingsBase = RehydrateAsConcrete(plugin, loaded);
            }
        }
    }

    private static SettingsSignalStrategyBase RehydrateAsConcrete(IStrategyPlugin plugin, SettingsSignalStrategyBase deserialized)
    {
        var concreteType = plugin.SettingsBase.GetType();
        if (deserialized.GetType() == concreteType)
            return deserialized;

        try
        {
            var json = JsonSerializer.Serialize(deserialized, deserialized.GetType(), JsonTools.JsonSerializerIndented);
            var result = JsonSerializer.Deserialize(json, concreteType, JsonTools.DeSerializerOptions) as SettingsSignalStrategyBase;
            return result ?? deserialized;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Failed to rehydrate settings for {plugin.Name}, using defaults");
            return deserialized;
        }
    }

    /// <summary>
    /// Collect current plugin settings into the provided dictionary, ready
    /// for serialization as part of the normal settings JSON.
    /// </summary>
    public static void CollectSettings(Dictionary<string, SettingsSignalStrategyBase> target)
    {
        target.Clear();
        foreach (var (_, plugin) in _plugins)
        {
            target[plugin.Name] = plugin.SettingsBase;
        }
    }
}

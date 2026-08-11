using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;

using NLog;

namespace CryptoScanner.Core.Signal.Indicators;

/// <summary>
/// Startup and apply-time sanity check on the enabled strategies.
/// <para>
/// The failure mode this guards against is silent: a strategy that is ticked in the settings but
/// whose plugin is not part of this build, or whose indicator extension never runs, simply produces
/// no signals. Nothing throws, nothing is logged, and the scanner looks perfectly healthy — which is
/// exactly how the VBS bands stayed null in the test suite for weeks. Reporting it makes the
/// mismatch visible the moment the settings are applied instead of after a fruitless scan.
/// </para>
/// </summary>
public static class StrategyDiagnostics
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Compare the enabled strategy names against what is actually registered and report every
    /// mismatch. Safe to call repeatedly; it only reads state.
    /// </summary>
    public static void Report()
    {
        // Ordinal, not OrdinalIgnoreCase: SignalExecute.Prepare matches with a plain
        // List<string>.Contains, so "VBS" in the settings does NOT enable "vbs". Comparing more
        // leniently here would hide exactly the mismatch we are looking for.
        var enabled = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in GlobalData.Settings.Signal.Long.Strategy)
            enabled.Add(name);
        foreach (string name in GlobalData.Settings.Signal.Short.Strategy)
            enabled.Add(name);

        if (enabled.Count == 0)
        {
            Report("No strategy is enabled for long or short; the analyzer will never produce a signal");
            return;
        }

        // 1. Enabled but not registered — the name survives in the settings file after a strategy is
        //    renamed or excluded from the build, and then quietly matches nothing.
        var registered = new HashSet<string>(
            RegisterAlgorithms.AlgorithmDefinitionList.Values.Select(d => d.Name),
            StringComparer.Ordinal);

        foreach (string name in enabled)
        {
            if (!registered.Contains(name))
                Report($"Strategy \"{name}\" is enabled but not registered in this build; it will never produce a signal");
        }

        // 2. Enabled, registered, but the plugin lookup fails — the algorithm is known while the
        //    plugin that owns it is not, so nothing supplies its settings or its indicators.
        var pluginStrategies = new HashSet<string>(
            PluginManager.LoadedPlugins.Values.Distinct().SelectMany(p => p.Strategies).Select(s => s.Name),
            StringComparer.Ordinal);

        // 3. A plugin that owns an indicator extension but has no enabled strategy: its indicators are
        //    not computed. That is intentional (the heavy kernels stay off), but it is also the exact
        //    state in which enabling the strategy later without rebuilding the hubs reads null values.
        //    That is now handled by IndicatorConfiguration.Bump() on every settings apply, so this is
        //    only worth stating at Trace level.
        foreach (var plugin in PluginManager.LoadedPlugins.Values.Distinct())
        {
            if (plugin.CreateIndicatorExtension() == null)
                continue;

            bool anyEnabled = plugin.Strategies.Any(s => enabled.Contains(s.Name));
            if (!anyEnabled)
                Logger.Trace($"Plugin \"{plugin.StrategyName}\" has an indicator extension but no enabled strategy; its indicators are not computed");
        }

        // Report an enabled strategy whose owning plugin is missing separately from case 1, because
        // the cause and the fix differ: case 1 is a stale settings entry, this one is a strategy
        // registered outside PluginManager.
        foreach (string name in enabled)
        {
            if (registered.Contains(name) && !pluginStrategies.Contains(name))
                Logger.Debug($"Strategy \"{name}\" is enabled and registered but has no plugin; it supplies no settings or indicators of its own");
        }
    }

    private static void Report(string text)
    {
        Logger.Warn(text);
        GlobalData.AddTextToLogTab(text);
    }
}

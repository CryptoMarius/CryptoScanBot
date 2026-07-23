using CryptoScanner.Analyzers.TradingBuddy.Chart;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.TradingBuddy;

/// <summary>
/// Overlay-only plugin: no signal strategies, just the TradingBuddy band overlay on the chart.
/// Registered via PluginManager.Register so it follows the same lifecycle as all other plugins.
/// </summary>
public class TradingBuddyPlugin : IStrategyPlugin
{
    public static string StrategyInternal = "TradingBuddy";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } = [];

    public static TradingBuddySettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new TradingBuddySettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not TradingBuddySettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = new TradingBuddyOverlay();
}

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Contract that every strategy plugin must implement. A single plugin can
/// register one or more sub-strategies (e.g. StoRsi + StoRsi.Multi) that
/// all share the same settings, config view and indicator extension.
/// </summary>
public interface IStrategyPlugin
{
    /// <summary>Plugin name used as the settings key in JSON persistence.</summary>
    string StrategyName { get; }
    string StrategyNameCamelCase { get; }

    /// <summary>One or more sub-strategies this plugin provides.</summary>
    IReadOnlyList<StrategyRegistration> Strategies { get; }

    /// <summary>Base-typed accessor for the plugin's settings (sound, color, entry conditions).
    /// The concrete type is internal to the plugin.</summary>
    SettingsSignalStrategyBase SettingsBase { get; set; }

    /// <summary>Factory for per-hub indicator state. Called once per symbol+interval hub.
    /// Return null when the strategy uses only standard indicators.</summary>
    IIndicatorExtension? CreateIndicatorExtension() => null;

    /// <summary>When true, the engine ensures DLZ zone calculation is active even when
    /// the DLZ strategy itself is not in the signal list. Strategies that check DLZ
    /// zone proximity (e.g. SuperTrendBreakout) should return true.</summary>
    bool RequiresDlzZones => false;

    /// <summary>Optional chart overlay (band drawing, labels, etc.).</summary>
    IChartOverlay? ChartOverlay => null;

    /// <summary>Optional settings UI tab for the config window.</summary>
    IConfigView? ConfigView => null;
}

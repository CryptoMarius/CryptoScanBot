using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Contract that every strategy plugin must implement. The host discovers
/// implementations via assembly scanning and registers them automatically
/// into the signal pipeline — no hardcoded references needed.
/// </summary>
public interface IStrategyPlugin
{
    string Name { get; }
    CryptoSignalStrategy Strategy { get; }
    Type? AnalyzeLongType { get; }
    Type? AnalyzeShortType { get; }

    /// <summary>Base-typed accessor for the plugin's settings (sound, color, entry conditions).
    /// The concrete type is internal to the plugin.</summary>
    SettingsSignalStrategyBase SettingsBase { get; set; }

    /// <summary>Factory for per-hub indicator state. Called once per symbol+interval hub.
    /// Return null when the strategy uses only standard indicators.</summary>
    IIndicatorExtension? CreateIndicatorExtension() => null;

    /// <summary>Optional chart overlay (band drawing, labels, etc.).</summary>
    IChartOverlay? ChartOverlay => null;

    /// <summary>Optional settings UI tab for the config window.</summary>
    IConfigView? ConfigView => null;
}

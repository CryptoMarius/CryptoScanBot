using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Contract for a strategy plugin that provides a settings UI tab.
/// The host adds a tab with <see cref="TabHeader"/> and calls
/// <see cref="LoadConfig"/>/<see cref="SaveConfig"/> during settings load/save.
///
/// The settings are passed in rather than read from the plugin's static Settings property, so the
/// dialog can also show a stored set (the settings of a finished emulator run) without overwriting
/// the live one. <see cref="StrategyName"/> is the key under which those are stored in
/// SettingsSignal.AnalyzerSettings.
///
/// The return type of CreateSettingsView is object so Core stays framework-agnostic;
/// the host casts to the appropriate control type (e.g. Avalonia.Controls.Control).
/// </summary>
public interface IConfigView
{
    string TabHeader { get; }
    string StrategyName { get; }
    object CreateSettingsView();
    void LoadConfig(SettingsSignalStrategyBase settings);
    void SaveConfig(SettingsSignalStrategyBase settings);
}

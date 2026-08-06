namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Contract for a strategy plugin that provides a settings UI tab.
/// The host adds a tab with <see cref="TabHeader"/> and
/// calls <see cref="LoadConfig"/>/<see cref="SaveConfig"/> during settings load/save.
/// The plugin reads/writes its own static Settings property — no parameters needed.
/// The return type of CreateSettingsView is object so Core stays framework-agnostic;
/// the host casts to the appropriate control type (e.g. Avalonia.Controls.Control).
/// </summary>
public interface IConfigView
{
    string TabHeader { get; }
    object CreateSettingsView();
    void LoadConfig();
    void SaveConfig();
}

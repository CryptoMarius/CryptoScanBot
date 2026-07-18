using Avalonia.Controls;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Contract for a strategy plugin that provides a settings UI tab.
/// The host adds a <see cref="TabItem"/> with <see cref="TabHeader"/> and
/// calls <see cref="LoadConfig"/>/<see cref="SaveConfig"/> during settings load/save.
/// The plugin reads/writes its own static Settings property — no parameters needed.
/// </summary>
public interface IConfigView
{
    string TabHeader { get; }
    Control CreateSettingsView();
    void LoadConfig();
    void SaveConfig();
}

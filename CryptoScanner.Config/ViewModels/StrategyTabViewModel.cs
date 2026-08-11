using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyTabViewModel : ObservableObject
{
    public StrategyTabViewModel()
    {
    }

    /// <summary>
    /// Fills the plugin tabs. With <paramref name="fromStoredSettings"/> the values are read from the
    /// AnalyzerSettings blocks of the supplied settings — that is how a finished run is inspected,
    /// without touching the plugins' live settings. Otherwise the live settings are shown, because
    /// those blocks only hold what was written at the last save.
    /// </summary>
    internal void LoadConfig(SettingsSignal signal, bool fromStoredSettings)
    {
        foreach (var configView in PluginManager.ConfigViews)
        {
            SettingsSignalStrategyBase? settings = fromStoredSettings
                ? PluginManager.MaterializeSettings(configView.StrategyName, signal.AnalyzerSettings)
                : null;
            settings ??= PluginManager.LiveSettings(configView.StrategyName);
            if (settings != null)
                configView.LoadConfig(settings);
        }
    }

    internal void SaveConfig()
    {
        foreach (var configView in PluginManager.ConfigViews)
        {
            SettingsSignalStrategyBase? settings = PluginManager.LiveSettings(configView.StrategyName);
            if (settings != null)
                configView.SaveConfig(settings);
        }
    }
}

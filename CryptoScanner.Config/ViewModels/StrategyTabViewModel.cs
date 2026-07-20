using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyTabViewModel : ObservableObject
{
    public StrategyTabViewModel()
    {
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        // All strategy settings are now loaded by their plugin ConfigViews via PluginManager.
        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.LoadConfig();
        }
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        // All strategy settings are now saved by their plugin ConfigViews via PluginManager.
        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.SaveConfig();
        }
    }
}

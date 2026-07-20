using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyTabViewModel : ObservableObject
{
    public StrategyTabViewModel()
    {
    }

    internal void LoadConfig()
    {
        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.LoadConfig();
        }
    }

    internal void SaveConfig()
    {
        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.SaveConfig();
        }
    }
}

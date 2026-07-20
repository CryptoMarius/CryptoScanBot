using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings;

using System.Reflection.Metadata;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyTabViewModel : ObservableObject
{
    [ObservableProperty]
    private StrategyStobbTabViewModel _strategyStobbTabViewModel;
    [ObservableProperty]
    private StrategyDlzTabViewModel _strategyDlzTabViewModel;
    [ObservableProperty]
    private StrategyFvgTabViewModel _strategyFvgTabViewModel;
    [ObservableProperty]
    private StrategySmcTabViewModel _strategySmcTabViewModel;


    public StrategyTabViewModel()
    {
        _strategyStobbTabViewModel = new();
        _strategyDlzTabViewModel = new();
        _strategyFvgTabViewModel = new();
        _strategySmcTabViewModel = new();
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.LoadConfig(Constants.StrategyStobb, settings.Stobb);
        StrategyDlzTabViewModel.LoadConfig(Constants.StrategyDlz, settings.ZonesDlz);
        StrategyFvgTabViewModel.LoadConfig(Constants.StrategyFvg, settings.ZonesFvg);
        StrategySmcTabViewModel.LoadConfig(Constants.StrategySmc, settings.ZonesSmc);

        // Sbm, Jump, Baba, AtrRb, Bre and Bbma settings are
        // now loaded by their plugin ConfigViews via PluginManager.

        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.LoadConfig();
        }
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.SaveConfig(settings.Stobb);
        StrategyDlzTabViewModel.SaveConfig(settings.ZonesDlz);
        StrategyFvgTabViewModel.SaveConfig(settings.ZonesFvg);
        StrategySmcTabViewModel.SaveConfig(settings.ZonesSmc);

        // Nwe, Sbm, Jump, Baba, AtrRb, Bre and Bbma settings are 
        // now saved by their plugin ConfigViews via PluginManager.

        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.SaveConfig();
        }
    }
}

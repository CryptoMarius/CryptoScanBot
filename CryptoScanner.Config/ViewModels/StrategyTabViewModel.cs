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
    private StrategySbmTabViewModel _strategySbmTabViewModel;
    // StrategyStorsiTabViewModel moved to the Analyzers project (Storsi plugin ConfigView).
    [ObservableProperty]
    private StrategyJumpTabViewModel _strategyJumpTabViewModel;
    [ObservableProperty]
    private StrategyDlzTabViewModel _strategyDlzTabViewModel;
    [ObservableProperty]
    private StrategyFvgTabViewModel _strategyFvgTabViewModel;
    [ObservableProperty]
    private StrategySmcTabViewModel _strategySmcTabViewModel;
    [ObservableProperty]
    private StrategyNweTabViewModel _strategyNweTabViewModel;
    [ObservableProperty]
    private StrategyBbmaTabViewModel _strategyBbmaTabViewModel;
    // Baba, AtrRb and Bre tab view models are now managed by their respective plugin ConfigViews.


    public StrategyTabViewModel()
    {
        _strategyStobbTabViewModel = new();
        _strategySbmTabViewModel = new();
        _strategyJumpTabViewModel = new();
        _strategyDlzTabViewModel = new();
        _strategyFvgTabViewModel = new();
        _strategySmcTabViewModel = new();
        _strategyNweTabViewModel = new();
        _strategyBbmaTabViewModel = new();
        // Baba, AtrRb and Bre tab view models are now created by plugin ConfigViews.
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.LoadConfig(Constants.StrategyStobb, settings.Stobb);
        StrategySbmTabViewModel.LoadConfig(Constants.StrategySbm, settings.Sbm);
        //StrategyStorsiTabViewModel.LoadConfig(Constants.StrategyStorsi, settings.StoRsi);
        StrategyJumpTabViewModel.LoadConfig(Constants.StrategyJump, settings.Jump);
        StrategyDlzTabViewModel.LoadConfig(Constants.StrategyDlz, settings.ZonesDlz);
        StrategyFvgTabViewModel.LoadConfig(Constants.StrategyFvg, settings.ZonesFvg);
        StrategySmcTabViewModel.LoadConfig(Constants.StrategySmc, settings.ZonesSmc);
        StrategyNweTabViewModel.LoadConfig(Constants.StrategyNwe, settings.Nwe);
#if DEBUG
        StrategyBbmaTabViewModel.LoadConfig(Constants.StrategyBbma, settings.Bbma);
#endif
        // Baba, AtrRb and Bre settings are now loaded by their plugin ConfigViews via PluginManager.

        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.LoadConfig();
        }
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.SaveConfig(settings.Stobb);
        StrategySbmTabViewModel.SaveConfig(settings.Sbm);
        //StrategyStorsiTabViewModel.SaveConfig(settings.StoRsi);
        StrategyJumpTabViewModel.SaveConfig(settings.Jump);
        StrategyDlzTabViewModel.SaveConfig(settings.ZonesDlz);
        StrategyFvgTabViewModel.SaveConfig(settings.ZonesFvg);
        StrategySmcTabViewModel.SaveConfig(settings.ZonesSmc);
        StrategyNweTabViewModel.SaveConfig(settings.Nwe);
#if DEBUG
        StrategyBbmaTabViewModel.SaveConfig(settings.Bbma);
#endif
        // Baba, AtrRb and Bre settings are now saved by their plugin ConfigViews via PluginManager.

        foreach (var configView in PluginManager.ConfigViews)
        {
            configView.SaveConfig();
        }
    }
}

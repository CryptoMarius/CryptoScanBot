using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyTabViewModel : ObservableObject
{
    [ObservableProperty]
    private StrategyStobbTabViewModel _strategyStobbTabViewModel;
    [ObservableProperty]
    private StrategySbmTabViewModel _strategySbmTabViewModel;
    [ObservableProperty]
    private StrategyStorsiTabViewModel _strategyStorsiTabViewModel;
    [ObservableProperty]
    private StrategyJumpTabViewModel _strategyJumpTabViewModel;
    [ObservableProperty]
    private StrategyDlzTabViewModel _strategyDlzTabViewModel;
    [ObservableProperty]
    private StrategyFvgTabViewModel _strategyFvgTabViewModel;
    [ObservableProperty]
    private StrategyNweTabViewModel _strategyNweTabViewModel;


    public StrategyTabViewModel()
    {
        _strategyStobbTabViewModel = new();
        _strategySbmTabViewModel = new();
        _strategyStorsiTabViewModel = new();
        _strategyJumpTabViewModel = new();
        _strategyDlzTabViewModel = new();
        _strategyFvgTabViewModel = new();
        _strategyNweTabViewModel = new();
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.LoadConfig("Stobb", settings.Stobb);
        StrategySbmTabViewModel.LoadConfig("Sbm", settings.Sbm);
        StrategyStorsiTabViewModel.LoadConfig("Sbm", settings.StoRsi);
        StrategyJumpTabViewModel.LoadConfig("Jump", settings.Jump);
        StrategyDlzTabViewModel.LoadConfig("Dlz", settings.ZonesDlz);
        StrategyFvgTabViewModel.LoadConfig("Fvg", settings.ZonesFvg);
        StrategyNweTabViewModel.LoadConfig("Nwe", settings.Nwe);
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.SaveConfig(settings.Stobb);
        StrategySbmTabViewModel.SaveConfig(settings.Sbm);
        StrategyStorsiTabViewModel.SaveConfig(settings.StoRsi);
        StrategyJumpTabViewModel.SaveConfig(settings.Jump);
        StrategyDlzTabViewModel.SaveConfig(settings.ZonesDlz);
        StrategyFvgTabViewModel.SaveConfig(settings.ZonesFvg);
        StrategyNweTabViewModel.SaveConfig(settings.Nwe);
    }
}

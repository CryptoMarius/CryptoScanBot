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
    private StrategySmcTabViewModel _strategySmcTabViewModel;
    [ObservableProperty]
    private StrategyNweTabViewModel _strategyNweTabViewModel;
    [ObservableProperty]
    private StrategyBbmaTabViewModel _strategyBbmaTabViewModel;
    [ObservableProperty]
    private StrategyWaveTrendTabViewModel _strategyWaveTrendTabViewModel;
    [ObservableProperty]
    private StrategyAtrRbTabViewModel _strategyAtrRbTabViewModel;


    public StrategyTabViewModel()
    {
        _strategyStobbTabViewModel = new();
        _strategySbmTabViewModel = new();
        _strategyStorsiTabViewModel = new();
        _strategyJumpTabViewModel = new();
        _strategyDlzTabViewModel = new();
        _strategyFvgTabViewModel = new();
        _strategySmcTabViewModel = new();
        _strategyNweTabViewModel = new();
        _strategyBbmaTabViewModel = new();
        _strategyWaveTrendTabViewModel = new();
        _strategyAtrRbTabViewModel = new();
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.LoadConfig("Stobb", settings.Stobb);
        StrategySbmTabViewModel.LoadConfig("Sbm", settings.Sbm);
        StrategyStorsiTabViewModel.LoadConfig("Storsi", settings.StoRsi);
        StrategyJumpTabViewModel.LoadConfig("Jump", settings.Jump);
        StrategyDlzTabViewModel.LoadConfig("Dlz", settings.ZonesDlz);
        StrategyFvgTabViewModel.LoadConfig("Fvg", settings.ZonesFvg);
        StrategySmcTabViewModel.LoadConfig("Smc", settings.ZonesSmc);
        StrategyNweTabViewModel.LoadConfig("Nwe", settings.Nwe);
        StrategyBbmaTabViewModel.LoadConfig("BBMA", settings.Bbma);
        StrategyWaveTrendTabViewModel.LoadConfig("WaveTrend", settings.WaveTrend);
        StrategyAtrRbTabViewModel.LoadConfig("AtrRb", settings.AtrRb);
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        StrategyStobbTabViewModel.SaveConfig(settings.Stobb);
        StrategySbmTabViewModel.SaveConfig(settings.Sbm);
        StrategyStorsiTabViewModel.SaveConfig(settings.StoRsi);
        StrategyJumpTabViewModel.SaveConfig(settings.Jump);
        StrategyDlzTabViewModel.SaveConfig(settings.ZonesDlz);
        StrategyFvgTabViewModel.SaveConfig(settings.ZonesFvg);
        StrategySmcTabViewModel.SaveConfig(settings.ZonesSmc);
        StrategyNweTabViewModel.SaveConfig(settings.Nwe);
        StrategyBbmaTabViewModel.SaveConfig(settings.Bbma);
        StrategyWaveTrendTabViewModel.SaveConfig(settings.WaveTrend);
        StrategyAtrRbTabViewModel.SaveConfig(settings.AtrRb);
    }
}

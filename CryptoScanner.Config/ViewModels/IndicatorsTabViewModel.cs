using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class IndicatorsTabViewModel : ObservableObject
{
    [ObservableProperty]
    private IndicatorRsiViewModel _indicatorRsiViewModel;
    [ObservableProperty]
    private IndicatorStochViewModel _indicatorStochViewModel;
    [ObservableProperty]
    private IndicatorBollingerBandViewModel _indicatorBollingerBandViewModel;


    public IndicatorsTabViewModel()
    {
        _indicatorRsiViewModel = new();
        _indicatorStochViewModel = new();
        _indicatorBollingerBandViewModel = new();
    }

    internal void LoadConfig(SettingsBasic settings)
    {
        IndicatorRsiViewModel.LoadConfig(settings.General.SettingsRsi);
        IndicatorStochViewModel.LoadConfig(settings.General.SettingsStoch);
        IndicatorBollingerBandViewModel.LoadConfig(settings.General.SettingsBb);
    }

    internal void SaveConfig(SettingsBasic settings)
    {
        IndicatorRsiViewModel.SaveConfig(settings.General.SettingsRsi);
        IndicatorStochViewModel.SaveConfig(settings.General.SettingsStoch);
        IndicatorBollingerBandViewModel.SaveConfig(settings.General.SettingsBb);
    }
}

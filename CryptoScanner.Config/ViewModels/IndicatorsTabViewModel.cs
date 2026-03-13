using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
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
    [ObservableProperty]
    private IndicatorZigZagViewModel _indicatorPrimaryTrend;
    [ObservableProperty]
    private IndicatorZigZagViewModel _indicatorSecondaryTrend;


    public IndicatorsTabViewModel()
    {
        _indicatorRsiViewModel = new();
        _indicatorStochViewModel = new();
        _indicatorBollingerBandViewModel = new();
        _indicatorPrimaryTrend = new();
        _indicatorSecondaryTrend = new();
    }

    internal void LoadConfig(SettingsBasic settings)
    {
        IndicatorRsiViewModel.LoadConfig(settings.General.SettingsRsi);
        IndicatorStochViewModel.LoadConfig(settings.General.SettingsStoch);
        IndicatorBollingerBandViewModel.LoadConfig(settings.General.SettingsBb);
        IndicatorPrimaryTrend.LoadConfig(GlobalData.Settings.Trend.Secondary);
        IndicatorSecondaryTrend.LoadConfig(GlobalData.Settings.Trend.Secondary);
    }

    internal void SaveConfig(SettingsBasic settings)
    {
        IndicatorRsiViewModel.SaveConfig(settings.General.SettingsRsi);
        IndicatorStochViewModel.SaveConfig(settings.General.SettingsStoch);
        IndicatorBollingerBandViewModel.SaveConfig(settings.General.SettingsBb);
        IndicatorPrimaryTrend.SaveConfig(settings.Trend.Secondary);
        IndicatorSecondaryTrend.SaveConfig(settings.Trend.Secondary);
    }
}

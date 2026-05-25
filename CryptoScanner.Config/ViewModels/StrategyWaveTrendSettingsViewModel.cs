using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyWaveTrendSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _channelLength = 10;

    [ObservableProperty]
    private int _averageLength = 21;

    [ObservableProperty]
    private decimal _osLevel = -60m;

    [ObservableProperty]
    private decimal _obLevel = 60m;

    [ObservableProperty]
    private decimal _osRecoveryLevel = -50m;

    [ObservableProperty]
    private decimal _obRecoveryLevel = 50m;

    [ObservableProperty]
    private bool _requireTrendFilter = true;

    [ObservableProperty]
    private int _lookbackBars = 10;

    [ObservableProperty]
    private int _minBarsBeyondOsOb = 3;


    public void LoadConfig(string caption, SettingsSignalStrategyWaveTrend settings)
    {
        ChannelLength = settings.ChannelLength;
        AverageLength = settings.AverageLength;
        OsLevel = settings.OsLevel;
        ObLevel = settings.ObLevel;
        OsRecoveryLevel = settings.OsRecoveryLevel;
        ObRecoveryLevel = settings.ObRecoveryLevel;
        RequireTrendFilter = settings.RequireTrendFilter;
        LookbackBars = settings.LookbackBars;
        MinBarsBeyondOsOb = settings.MinBarsBeyondOsOb;
    }

    public void SaveConfig(SettingsSignalStrategyWaveTrend settings)
    {
        settings.ChannelLength = ChannelLength;
        settings.AverageLength = AverageLength;
        settings.OsLevel = OsLevel;
        settings.ObLevel = ObLevel;
        settings.OsRecoveryLevel = OsRecoveryLevel;
        settings.ObRecoveryLevel = ObRecoveryLevel;
        settings.RequireTrendFilter = RequireTrendFilter;
        settings.LookbackBars = LookbackBars;
        settings.MinBarsBeyondOsOb = MinBarsBeyondOsOb;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class VolumeFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isActive = false;

    [ObservableProperty]
    private decimal _minRelVol = 0.8m;

    [ObservableProperty]
    private decimal _maxRelVol = 999m;

    [ObservableProperty]
    private int _lookback = 20;

    [ObservableProperty]
    private bool _log = true;

    public void LoadConfig(SettingsTextualVolume settings)
    {
        IsActive = settings.IsActive;
        MinRelVol = settings.MinRelVol;
        MaxRelVol = settings.MaxRelVol;
        Lookback = settings.Lookback;
        Log = settings.Log;
    }

    public void SaveConfig(SettingsTextualVolume settings)
    {
        settings.IsActive = IsActive;
        settings.MinRelVol = MinRelVol;
        settings.MaxRelVol = MaxRelVol;
        settings.Lookback = Lookback;
        settings.Log = Log;
    }
}

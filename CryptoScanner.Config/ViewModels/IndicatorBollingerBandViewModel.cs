using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class IndicatorBollingerBandViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 14;

    [ObservableProperty]
    private double _deviation = 2.0;

    internal void LoadConfig(SettingsGeneralBB settings)
    {
        Length = settings.Length;
        Deviation = settings.Deviation;
    }

    internal void SaveConfig(SettingsGeneralBB settings)
    {
        settings.Length = Length;
        settings.Deviation = Deviation;
    }
}

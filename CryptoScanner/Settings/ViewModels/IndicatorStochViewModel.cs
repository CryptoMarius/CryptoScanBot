using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

public partial class IndicatorStochViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 14;

    [ObservableProperty]
    private int _d = 3;

    [ObservableProperty]
    private int _k = 3;

    [ObservableProperty]
    private double _oversold = 20;

    [ObservableProperty]
    private double _overbought = 80;

    internal void LoadConfig(SettingsGeneralStoch settings)
    {
        Length = settings.Length;
        D = settings.SmoothingD;
        K = settings.SmoothingK;
        Oversold = settings.Oversold;
        Overbought = settings.Overbought;
    }

    internal void SaveConfig(SettingsGeneralStoch settings)
    {
        settings.Length = Length;
        settings.SmoothingD = D;
        settings.SmoothingK = K;
        settings.Oversold = Oversold;
        settings.Overbought = Overbought;
    }
}

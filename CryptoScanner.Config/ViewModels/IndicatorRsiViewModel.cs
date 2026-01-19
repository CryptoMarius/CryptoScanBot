using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class IndicatorRsiViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 14;

    [ObservableProperty]
    private double _oversold = 30;

    [ObservableProperty]
    private double _overbought = 70;

    internal void LoadConfig(SettingsGeneralRsi settings)
    {
        Length = settings.Length;
        Oversold = settings.Oversold;
        Overbought = settings.Overbought;
    }

    internal void SaveConfig(SettingsGeneralRsi settings)
    {
        settings.Length = Length;
        settings.Oversold = Oversold;
        settings.Overbought = Overbought;
    }
}

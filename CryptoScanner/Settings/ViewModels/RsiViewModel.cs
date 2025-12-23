using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class RsiViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _length = 14;

    [ObservableProperty]
    private decimal _oversold = 30;

    [ObservableProperty]
    private decimal _overbought = 70;
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class BarometerFilterRangeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _caption = "";

    [ObservableProperty]
    private bool _isActive = false;

    [ObservableProperty]
    private decimal _minValue = -100;

    [ObservableProperty]
    private decimal _maxValue = 100;

    partial void OnIsActiveChanged(bool value)
    {
        // Notify that enabled state changed
        OnPropertyChanged(nameof(IsActive));
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

/// <summary>
/// The market exit of one trader side: close a position once the whole market moved this many
/// percent against it since the entry. See Barometer.MarketMove.
/// </summary>
public partial class MarketExitViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isActive = false;

    [ObservableProperty]
    private decimal _percentage = 3m;

    [ObservableProperty]
    private string _header = "Market exit";

    public void LoadConfig(SettingsTextualMarketExit settings)
    {
        IsActive = settings.Active;
        Percentage = settings.Percentage;
    }

    public void SaveConfig(SettingsTextualMarketExit settings)
    {
        settings.Active = IsActive;
        settings.Percentage = Math.Max(0m, Percentage);
    }
}

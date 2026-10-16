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
    private bool _useBtc = false;

    [ObservableProperty]
    private bool _entryLimitActive = false;

    [ObservableProperty]
    private int _entryLimitMaxPerHour = 10;

    [ObservableProperty]
    private string _header = "Market exit";

    public void LoadConfig(SettingsTextualMarketExit settings)
    {
        IsActive = settings.Active;
        Percentage = settings.Percentage;
        UseBtc = settings.UseBtc;
    }

    public void LoadEntryLimit(SettingsTextualEntryLimit settings)
    {
        EntryLimitActive = settings.Active;
        EntryLimitMaxPerHour = settings.MaxPerHour;
    }

    public void SaveEntryLimit(SettingsTextualEntryLimit settings)
    {
        settings.Active = EntryLimitActive;
        settings.MaxPerHour = Math.Max(1, EntryLimitMaxPerHour);
    }

    public void SaveConfig(SettingsTextualMarketExit settings)
    {
        settings.Active = IsActive;
        settings.Percentage = Math.Max(0m, Percentage);
        settings.UseBtc = UseBtc;
    }
}

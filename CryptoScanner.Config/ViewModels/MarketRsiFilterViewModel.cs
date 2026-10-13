using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

/// <summary>
/// The market RSI filter of one side: skip a signal while the 1h RSI of all coins, averaged, lies
/// in the band. Same layout as the symbol trend filter, but the range is the band that is SKIPPED.
/// </summary>
public partial class MarketRsiFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private BarometerFilterRangeViewModel _rsiRange;

    [ObservableProperty]
    private bool _log = false;

    [ObservableProperty]
    private string _header = "Market RSI filter (skip between)";

    public MarketRsiFilterViewModel()
    {
        _rsiRange = new BarometerFilterRangeViewModel
        {
            Caption = "RSI",
            MinValue = 45,
            MaxValue = 55,
            IsActive = false
        };
    }

    public void LoadConfig(SettingsTextualMarketRsi settings)
    {
        RsiRange.IsActive = settings.Active;
        RsiRange.MinValue = settings.Minimum;
        RsiRange.MaxValue = settings.Maximum;
        Log = settings.Log;
    }

    public void SaveConfig(SettingsTextualMarketRsi settings)
    {
        settings.Active = RsiRange.IsActive;
        settings.Minimum = Math.Min(RsiRange.MinValue, RsiRange.MaxValue);
        settings.Maximum = Math.Max(RsiRange.MinValue, RsiRange.MaxValue);
        settings.Log = Log;
    }
}

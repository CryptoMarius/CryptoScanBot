using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class IndicatorZigZagViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _usePrimaryTrend = true;

    [ObservableProperty]
    private bool _useHighLow = true;

    internal void LoadConfig(SettingsZigZag settings)
    {
        UsePrimaryTrend = settings.TrendType == TrendType.Primary;
        UseHighLow = settings.UseHighLow;
    }

    internal void SaveConfig(SettingsZigZag settings)
    {
        settings.TrendType = UsePrimaryTrend ? TrendType.Primary : TrendType.Secondary;
        settings.UseHighLow = UseHighLow;
    }
}

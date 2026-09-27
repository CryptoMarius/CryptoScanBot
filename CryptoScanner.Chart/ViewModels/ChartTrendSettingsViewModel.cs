using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.ViewModels;

public partial class ChartTrendSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private TrendType _trendType = TrendType.Primary;

    [ObservableProperty]
    private bool _showZigZag = false;

    // Support/resistance: levels, sloped lines and flips (open point 46)
    [ObservableProperty]
    private bool _showSupportResistance = false;

    public void LoadFromSession(ZoneSession session)
    {
        TrendType = session.TrendType;
        ShowZigZag = session.TrendShowZigZag;
        ShowSupportResistance = session.ShowSupportResistance;
    }

    public void SaveToSession(ZoneSession session)
    {
        session.TrendType = TrendType;
        session.TrendShowZigZag = ShowZigZag;
        session.ShowSupportResistance = ShowSupportResistance;
    }
}

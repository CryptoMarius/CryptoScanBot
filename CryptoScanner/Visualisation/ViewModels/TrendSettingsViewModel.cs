using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Zones;

namespace CryptoScanner.Visualisation.ViewModels;

public partial class TrendSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _trendType = 0; // 0=Primary, 1=Secondary

    [ObservableProperty]
    private bool _showZigZag = false;

    public void LoadFromSession(ZoneSession session)
    {
        TrendType = session.TrendIndicator;
        ShowZigZag = session.TrendShowZigZag;
    }

    public void SaveToSession(ZoneSession session)
    {
        session.TrendIndicator = TrendType;
        session.TrendShowZigZag = ShowZigZag;
    }
}

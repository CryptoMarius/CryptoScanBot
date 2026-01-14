using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.ViewModels;

public partial class ChartFibSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private TrendType _fibTrend = TrendType.Primary;

    [ObservableProperty]
    private bool _showFibRetracement = false;

    [ObservableProperty]
    private bool _showZigZag = false;

    public void LoadFromSession(ZoneSession session)
    {
        FibTrend = session.FibTrend;
        ShowFibRetracement = session.ShowFibRetracement;
        ShowZigZag = session.ShowFibZigZag;
    }

    public void SaveToSession(ZoneSession session)
    {
        session.FibTrend = FibTrend;
        session.ShowFibRetracement = ShowFibRetracement;
        session.ShowFibZigZag = ShowZigZag;
    }
}

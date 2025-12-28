using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Zones;

namespace CryptoScanner.Visualisation.ViewModels;

public partial class FibSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _fibTrend = 0; // 0=Primary, 1=Secondary

    [ObservableProperty]
    private bool _showFib = false;

    [ObservableProperty]
    private bool _showZigZag = false;

    public void LoadFromSession(ZoneSession session)
    {
        //FibTrend = session.FibIndicator;
        //ShowFib = session.FibShowFib;
        ShowZigZag = session.FibShowZigZag;
    }

    public void SaveToSession(ZoneSession session)
    {
        //session.FibIndicator = FibTrend;
        //session.FibShowFib = ShowFib;
        session.FibShowZigZag = ShowZigZag;
    }
}

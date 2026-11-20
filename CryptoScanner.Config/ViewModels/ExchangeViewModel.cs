using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class ExchangeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _windowTitle = string.Empty;

    [ObservableProperty]
    private int _activeExchange;

    [ObservableProperty]
    private int _refreshSymbols = 60;

    /// <summary>
    /// The share of HyperLiquid's per-address budget this scanner may spend. Only meaningful for the
    /// two HyperLiquid markets, but it lives here rather than on a market of its own because there is
    /// no per-exchange settings screen and because the division it describes is between PROCESSES:
    /// every scanner on this machine that talks to HyperLiquid draws from the same pool.
    /// </summary>
    [ObservableProperty]
    private int _hyperLiquidWeightPerMinute = SettingsGeneral.HyperLiquidWeightPerMinuteDefault;

    [ObservableProperty]
    private bool _signalREnabled;

    [ObservableProperty]
    private int _signalRPort = 5200;

    [ObservableProperty]
    private List<KeyValuePair<int, string>> _exchangeList = [];

    public ExchangeViewModel()
    {
        BuildExchangeList();
    }

    private void BuildExchangeList()
    {
        ExchangeList.Clear();
        foreach (var exchange in GlobalData.ExchangeListName.Values)
        {
            if (exchange.IsSupported)
            {
                ExchangeList.Add(new(exchange.Id, exchange.Name));
            }
        }
    }

    internal void LoadConfig(SettingsGeneral general)
    {
        WindowTitle = general.WindowTitle;
        if (GlobalData.ExchangeListName.TryGetValue(general.ExchangeName, out Core.Model.CryptoExchange? exchange))
            ActiveExchange = exchange.Id;
        else
            ActiveExchange = -1;

        RefreshSymbols = general.GetCandleInterval;
        HyperLiquidWeightPerMinute = general.HyperLiquidWeightPerMinute;
        SignalREnabled = general.SignalREnabled;
        SignalRPort = general.SignalRPort;
    }

    internal void SaveConfig(SettingsGeneral general)
    {
        general.WindowTitle = WindowTitle;
        if (GlobalData.ExchangeListId.TryGetValue(ActiveExchange, out Core.Model.CryptoExchange? exchange))
            general.ExchangeName = exchange.Name;
        general.GetCandleInterval = RefreshSymbols;
        general.HyperLiquidWeightPerMinute = Math.Clamp(HyperLiquidWeightPerMinute,
            SettingsGeneral.HyperLiquidWeightPerMinuteMinimum, SettingsGeneral.HyperLiquidWeightPerMinuteMaximum);
        general.SignalREnabled = SignalREnabled;
        general.SignalRPort = SignalRPort;
    }
}
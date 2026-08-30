using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class ExchangeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _extraCaption = string.Empty;

    [ObservableProperty]
    private int _activeExchange;

    [ObservableProperty]
    private int _activateExchange;

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
    private List<KeyValuePair<int, string>> _exchangeList = [];

    /// <summary>
    /// Set while LoadConfig fills the properties, so the "follow the active exchange" rule in
    /// OnActiveExchangeChanged does not overwrite the stored ActivateExchange during loading.
    /// </summary>
    private bool _loadingConfig;

    public ExchangeViewModel()
    {
        BuildExchangeList();
    }

    /// <summary>
    /// Picking another active exchange means the scanner is going to run on that exchange, so the
    /// exchange the trading app/links are opened on has to follow. Leaving it pointing at the
    /// previous exchange opened charts and orders on an exchange that is no longer being scanned.
    /// </summary>
    partial void OnActiveExchangeChanged(int value)
    {
        if (_loadingConfig)
            return;
        ActivateExchange = value;
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
        _loadingConfig = true;
        try
        {
            ExtraCaption = general.ExtraCaption;
            if (GlobalData.ExchangeListName.TryGetValue(general.ExchangeName, out Core.Model.CryptoExchange? exchange))
                ActiveExchange = exchange.Id;
            else
                ActiveExchange = -1;

            if (GlobalData.ExchangeListName.TryGetValue(general.ActivateExchangeName, out exchange))
                ActivateExchange = exchange.Id;
            else
                ActivateExchange = -1;

            RefreshSymbols = general.GetCandleInterval;
            HyperLiquidWeightPerMinute = general.HyperLiquidWeightPerMinute;
        }
        finally
        {
            _loadingConfig = false;
        }
    }

    internal void SaveConfig(SettingsGeneral general)
    {
        general.ExtraCaption = ExtraCaption;
        if (GlobalData.ExchangeListId.TryGetValue(ActiveExchange, out Core.Model.CryptoExchange? exchange))
            general.ExchangeName = exchange.Name;
        if (GlobalData.ExchangeListId.TryGetValue(ActivateExchange, out exchange))
            general.ActivateExchangeName = exchange.Name;
        general.GetCandleInterval = RefreshSymbols;
        general.HyperLiquidWeightPerMinute = Math.Clamp(HyperLiquidWeightPerMinute,
            SettingsGeneral.HyperLiquidWeightPerMinuteMinimum, SettingsGeneral.HyperLiquidWeightPerMinuteMaximum);
    }
}
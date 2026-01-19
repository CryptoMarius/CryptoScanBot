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
    }

    internal void SaveConfig(SettingsGeneral general)
    {
        general.ExtraCaption = ExtraCaption;
        if (GlobalData.ExchangeListId.TryGetValue(ActiveExchange, out Core.Model.CryptoExchange? exchange))
            general.ExchangeName = exchange.Name;
        if (GlobalData.ExchangeListId.TryGetValue(ActivateExchange, out exchange))
            general.ActivateExchangeName = exchange.Name;
        general.GetCandleInterval = RefreshSymbols;
    }
}
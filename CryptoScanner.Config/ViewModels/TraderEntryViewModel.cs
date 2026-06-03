using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderEntryViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoOrderType> _orderTypeList = new()
    {
        { "Market order", CryptoOrderType.Market },
        { "Limit order", CryptoOrderType.Limit }
    };

    private readonly Dictionary<string, CryptoEntryOrDcaStrategy> _strategyList = new()
    {
        //{ "Direct na het signaal", CryptoEntryOrDcaStrategy.Immediately },
        { "Na een signaal (sbm/stobb/enz)", CryptoEntryOrDcaStrategy.AfterNextSignal },
        //{ "Trace via de Keltner Channel en PSAR", CryptoEntryOrDcaStrategy.TrailViaKcPsar }
    };

    private readonly Dictionary<string, CryptoEntryOrDcaPricing> _pricingList = new()
    {
        { "Market order", CryptoEntryOrDcaPricing.MarketPrice },
        { "Limit order op signaal prijs", CryptoEntryOrDcaPricing.SignalPrice },
        //{ "Limit order met pullback (%)", CryptoEntryOrDcaPricing.SignalPriceWithPullback },
        //{ "Limit order op bied prijs", CryptoEntryOrDcaPricing.BidPrice },
        //{ "Limit order op vraag prijs", CryptoEntryOrDcaPricing.AskPrice }
    };

    [ObservableProperty]
    private CryptoOrderType _entryOrderType = CryptoOrderType.Market; // enum (EXACT match)

    [ObservableProperty]
    private CryptoEntryOrDcaStrategy _entryStrategy = CryptoEntryOrDcaStrategy.AfterNextSignal; // enum (EXACT match)

    [ObservableProperty]
    private CryptoEntryOrDcaPricing _entryOrderPrice = CryptoEntryOrDcaPricing.SignalPrice; // enum (EXACT match)

    [ObservableProperty]
    private int _entryRemoveTime = 5; // int (EXACT match, in minutes)

    public Dictionary<string, CryptoOrderType> OrderTypeList => _orderTypeList;
    public Dictionary<string, CryptoEntryOrDcaStrategy> StrategyList => _strategyList;
    public Dictionary<string, CryptoEntryOrDcaPricing> PricingList => _pricingList;

    public void LoadConfig(SettingsTrading settings)
    {
        EntryOrderType = settings.EntryOrderType;
        EntryStrategy = settings.EntryStrategy;
        EntryOrderPrice = settings.EntryOrderPrice;
        EntryRemoveTime = settings.EntryRemoveTime;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.EntryOrderType = EntryOrderType;
        settings.EntryStrategy = EntryStrategy;
        settings.EntryOrderPrice = EntryOrderPrice;
        settings.EntryRemoveTime = EntryRemoveTime;
    }
}

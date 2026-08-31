using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

/// <summary>One balance a paper account starts with - see SettingsTrading.PaperAssetDefaults.</summary>
public partial class PaperAssetDefaultItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private decimal _total;
}


public partial class TraderMiscSettingsViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoTradeVia> _tradeViaList = new()
    {
        { "Papertrading", CryptoTradeVia.PaperTrade },
        { "Altrady webhook", CryptoTradeVia.Altrady },
        { "Papertrading + Altrady", CryptoTradeVia.PaperTradingAndAltrady }
    };

    [ObservableProperty]
    private CryptoTradeVia _tradeVia = CryptoTradeVia.PaperTrade; // enum (EXACT match)

    [ObservableProperty]
    private bool _disableNewPositions = false; // bool (EXACT match)

    [ObservableProperty]
    private bool _useAssetManagement = true; // bool (EXACT match)

    [ObservableProperty]
    private decimal _paperAssetStartCapital = 10000m; // decimal (EXACT match)

    // The balances a paper account starts with. Filled in, this list replaces the amount above.
    [ObservableProperty]
    private ObservableCollection<PaperAssetDefaultItemViewModel> _paperAssetDefaults = [];

    [ObservableProperty]
    private bool _soundTradeNotification = false; // bool (stored in SettingsGeneral!)

    [ObservableProperty]
    private bool _logCanceledOrders = true; // bool (EXACT match)

    [ObservableProperty]
    private int _globalBuyCooldownTime = 30; // int (EXACT match, in minutes)

    [ObservableProperty]
    private int _signalCooldownAfterTradeTime = 15; // int (EXACT match, in minutes)

    [ObservableProperty]
    private int _lossCooldownTime = 0; // int (EXACT match, in minutes; 0 = off)

    [ObservableProperty]
    private decimal _maxPositionDurationDays = 0m; // decimal (EXACT match, in days; 0 = off)

    // Slot limits (all int - EXACT match)
    [ObservableProperty]
    private int _slotsMaximalLong = 1;

    [ObservableProperty]
    private int _slotsMaximalShort = 1;

    public Dictionary<string, CryptoTradeVia> TradeViaList => _tradeViaList;

    [RelayCommand]
    private void AddPaperAssetDefault()
    {
        PaperAssetDefaults.Add(new PaperAssetDefaultItemViewModel());
    }

    [RelayCommand]
    private void RemovePaperAssetDefault()
    {
        if (PaperAssetDefaults.Count > 0)
            PaperAssetDefaults.RemoveAt(PaperAssetDefaults.Count - 1);
    }

    public void LoadConfig(SettingsTrading settings, SettingsGeneral general)
    {
        TradeVia = settings.TradeVia;
        DisableNewPositions = settings.DisableNewPositions;
        UseAssetManagement = settings.UseAssetManagement;
        PaperAssetStartCapital = settings.PaperAssetStartCapital;
        LogCanceledOrders = settings.LogCanceledOrders;

        PaperAssetDefaults.Clear();
        foreach (CryptoPaperAssetDefault entry in settings.PaperAssetDefaults)
            PaperAssetDefaults.Add(new PaperAssetDefaultItemViewModel { Name = entry.Name, Total = entry.Total });
        GlobalBuyCooldownTime = settings.GlobalBuyCooldownTime;
        SignalCooldownAfterTradeTime = settings.SignalCooldownAfterTradeTime;
        LossCooldownTime = settings.LossCooldownTime;
        MaxPositionDurationDays = settings.MaxPositionDurationDays;
        SoundTradeNotification = general.SoundTradeNotification;

        SlotsMaximalLong = settings.SlotsMaximalLong;
        SlotsMaximalShort = settings.SlotsMaximalShort;
    }

    public void SaveConfig(SettingsTrading settings, SettingsGeneral general)
    {
        settings.TradeVia = TradeVia;
        settings.DisableNewPositions = DisableNewPositions;
        settings.UseAssetManagement = UseAssetManagement;
        settings.PaperAssetStartCapital = PaperAssetStartCapital;
        settings.LogCanceledOrders = LogCanceledOrders;

        // A row without a coin is an empty row somebody added and left alone, not a setting.
        settings.PaperAssetDefaults.Clear();
        foreach (PaperAssetDefaultItemViewModel item in PaperAssetDefaults)
        {
            string name = item.Name.Trim().ToUpperInvariant();
            if (name.Length > 0)
                settings.PaperAssetDefaults.Add(new CryptoPaperAssetDefault { Name = name, Total = item.Total });
        }
        settings.GlobalBuyCooldownTime = GlobalBuyCooldownTime;
        settings.SignalCooldownAfterTradeTime = SignalCooldownAfterTradeTime;
        settings.LossCooldownTime = LossCooldownTime;
        settings.MaxPositionDurationDays = MaxPositionDurationDays;
        general.SoundTradeNotification = SoundTradeNotification;

        settings.SlotsMaximalLong = SlotsMaximalLong;
        settings.SlotsMaximalShort = SlotsMaximalShort;
    }
}

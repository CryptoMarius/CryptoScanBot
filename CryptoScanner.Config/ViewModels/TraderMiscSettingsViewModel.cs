using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

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

    // The balances a paper account starts with, coin by coin - the only place a paper account reads
    // its starting balances from.
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
    private bool _cooldownInCandles = false; // bool (EXACT match): the two cooldowns in candles of the signal interval

    [ObservableProperty]
    private decimal _maxPositionDurationDays = 0m; // decimal (EXACT match, in days; 0 = off)

    // Slot limits (all int - EXACT match)
    [ObservableProperty]
    private int _slotsMaximalLong = 1;

    [ObservableProperty]
    private int _slotsMaximalShort = 1;

    // Positions on one coin, long and short together (1..n)
    [ObservableProperty]
    private int _maxPositionsPerCoin = 1;

    public Dictionary<string, CryptoTradeVia> TradeViaList => _tradeViaList;

    /// <summary>
    /// The reset button is only offered in the scanner: the emulator hands out the start capital at
    /// the start of every run itself, and shows this screen for the settings of a run, not of a
    /// running paper account.
    /// </summary>
    public bool CanResetPaperAssets => !GlobalData.IsEmulatorMode;

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

    /// <summary>
    /// Throw every paper balance away and hand out the default asset list as this screen shows it -
    /// also when it has not been saved yet. See PaperAssetsEditor.ResetToDefaults.
    /// </summary>
    public void ResetPaperAssets()
    {
        PaperAssetsEditor.ResetToDefaults(GlobalData.ActiveExchange, CollectPaperAssetDefaults());
    }

    /// <summary>The rows of the default asset list as settings; a row without a coin is left out.</summary>
    private List<CryptoPaperAssetDefault> CollectPaperAssetDefaults()
    {
        List<CryptoPaperAssetDefault> list = [];
        foreach (PaperAssetDefaultItemViewModel item in PaperAssetDefaults)
        {
            string name = item.Name.Trim().ToUpperInvariant();
            if (name.Length > 0)
                list.Add(new CryptoPaperAssetDefault { Name = name, Total = item.Total });
        }
        return list;
    }

    public void LoadConfig(SettingsTrading settings, SettingsGeneral general)
    {
        TradeVia = settings.TradeVia;
        DisableNewPositions = settings.DisableNewPositions;
        UseAssetManagement = settings.UseAssetManagement;
        LogCanceledOrders = settings.LogCanceledOrders;

        PaperAssetDefaults.Clear();
        foreach (CryptoPaperAssetDefault entry in settings.PaperAssetDefaults)
            PaperAssetDefaults.Add(new PaperAssetDefaultItemViewModel { Name = entry.Name, Total = entry.Total });
        GlobalBuyCooldownTime = settings.GlobalBuyCooldownTime;
        SignalCooldownAfterTradeTime = settings.SignalCooldownAfterTradeTime;
        LossCooldownTime = settings.LossCooldownTime;
        CooldownInCandles = settings.CooldownInCandles;
        MaxPositionDurationDays = settings.MaxPositionDurationDays;
        SoundTradeNotification = general.SoundTradeNotification;

        SlotsMaximalLong = settings.SlotsMaximalLong;
        SlotsMaximalShort = settings.SlotsMaximalShort;
        MaxPositionsPerCoin = settings.MaxPositionsPerCoin;
    }

    public void SaveConfig(SettingsTrading settings, SettingsGeneral general)
    {
        settings.TradeVia = TradeVia;
        settings.DisableNewPositions = DisableNewPositions;
        settings.UseAssetManagement = UseAssetManagement;
        settings.LogCanceledOrders = LogCanceledOrders;

        // A row without a coin is an empty row somebody added and left alone, not a setting.
        settings.PaperAssetDefaults.Clear();
        settings.PaperAssetDefaults.AddRange(CollectPaperAssetDefaults());
        settings.GlobalBuyCooldownTime = GlobalBuyCooldownTime;
        settings.SignalCooldownAfterTradeTime = SignalCooldownAfterTradeTime;
        settings.LossCooldownTime = LossCooldownTime;
        settings.CooldownInCandles = CooldownInCandles;
        settings.MaxPositionDurationDays = MaxPositionDurationDays;
        general.SoundTradeNotification = SoundTradeNotification;

        settings.SlotsMaximalLong = SlotsMaximalLong;
        settings.SlotsMaximalShort = SlotsMaximalShort;
        settings.MaxPositionsPerCoin = Math.Max(1, MaxPositionsPerCoin);
    }
}

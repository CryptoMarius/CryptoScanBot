using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

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

    public void LoadConfig(SettingsTrading settings, SettingsGeneral general)
    {
        TradeVia = settings.TradeVia;
        DisableNewPositions = settings.DisableNewPositions;
        UseAssetManagement = settings.UseAssetManagement;
        LogCanceledOrders = settings.LogCanceledOrders;

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

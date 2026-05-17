using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderMiscSettingsViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoTradeVia> _tradeViaList = new()
    {
        { "Papertrading", CryptoTradeVia.PaperTrade },
        { "Altrady webhook", CryptoTradeVia.Altrady }
    };

    [ObservableProperty]
    private CryptoTradeVia _tradeVia = CryptoTradeVia.PaperTrade; // enum (EXACT match)

    [ObservableProperty]
    private bool _disableNewPositions = false; // bool (EXACT match)

    [ObservableProperty]
    private bool _soundTradeNotification = false; // bool (stored in SettingsGeneral!)

    [ObservableProperty]
    private bool _logCanceledOrders = true; // bool (EXACT match)

    [ObservableProperty]
    private int _globalBuyCooldownTime = 30; // int (EXACT match, in minutes)

    public Dictionary<string, CryptoTradeVia> TradeViaList => _tradeViaList;

    public void LoadConfig(SettingsTrading settings)
    {
        TradeVia = settings.TradeVia;
        DisableNewPositions = settings.DisableNewPositions;
        LogCanceledOrders = settings.LogCanceledOrders;
        GlobalBuyCooldownTime = settings.GlobalBuyCooldownTime;
        SoundTradeNotification = GlobalData.Settings.General.SoundTradeNotification;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.TradeVia = TradeVia;
        settings.DisableNewPositions = DisableNewPositions;
        settings.LogCanceledOrders = LogCanceledOrders;
        settings.GlobalBuyCooldownTime = GlobalBuyCooldownTime;
        GlobalData.Settings.General.SoundTradeNotification = SoundTradeNotification;
    }
}

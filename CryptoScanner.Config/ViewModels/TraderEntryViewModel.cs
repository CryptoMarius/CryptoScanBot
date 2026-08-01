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

    [ObservableProperty]
    private CryptoOrderType _entryOrderType = CryptoOrderType.Market; // enum (EXACT match)

    [ObservableProperty]
    private int _entryRemoveTime = 5; // int (EXACT match, in minutes)

    public Dictionary<string, CryptoOrderType> OrderTypeList => _orderTypeList;

    public void LoadConfig(SettingsTrading settings)
    {
        EntryOrderType = settings.EntryOrderType;
        EntryRemoveTime = settings.EntryRemoveTime;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.EntryOrderType = EntryOrderType;
        settings.EntryRemoveTime = EntryRemoveTime;
    }
}

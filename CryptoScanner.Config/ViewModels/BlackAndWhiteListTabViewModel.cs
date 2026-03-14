using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class BlackAndWhiteListTabViewModel : ObservableObject
{
    [ObservableProperty]
    private BlackAndWhiteListViewModel _blackListLong;
    [ObservableProperty]
    private BlackAndWhiteListViewModel _blackListShort;
    [ObservableProperty]
    private BlackAndWhiteListViewModel _whiteListLong;
    [ObservableProperty]
    private BlackAndWhiteListViewModel _whiteListShort;

    public BlackAndWhiteListTabViewModel()
    {
        _blackListLong = new();
        _blackListShort = new();
        _whiteListLong = new();
        _whiteListShort = new();
    }

    internal void LoadConfig(SettingsBasic settings)
    {
        // Black and White lists
        BlackListLong.LoadConfig(settings.BlackListOversold);
        BlackListShort.LoadConfig(settings.BlackListOverbought);
        WhiteListLong.LoadConfig(settings.WhiteListOversold);
        WhiteListShort.LoadConfig(settings.WhiteListOverbought);
    }

    internal void SaveConfig(SettingsBasic settings)
    {
        BlackListLong.SaveConfig(settings.BlackListOversold);
        BlackListShort.SaveConfig(settings.BlackListOverbought);
        WhiteListLong.SaveConfig(settings.WhiteListOversold);
        WhiteListShort.SaveConfig(settings.WhiteListOverbought);
    }
}

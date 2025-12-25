using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;

using System.Collections.ObjectModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _exchanges = [];

    [ObservableProperty]
    private ExchangeViewModel _exchangeViewModel;
    [ObservableProperty]
    private CommonViewModel _commonViewModel;

    [ObservableProperty]
    private RsiViewModel _rsiViewModel;

    [ObservableProperty]
    private StochViewModel _stochViewModel;

    [ObservableProperty]
    private BollingerBandViewModel _bollingerBandViewModel;

    [ObservableProperty]
    private TrendViewModel _primaryTrend;

    [ObservableProperty]
    private TrendViewModel _secondaryTrend;

    [ObservableProperty]
    private BlackAndWhiteListViewModel _blackListLong;
    [ObservableProperty]
    private BlackAndWhiteListViewModel _blackListShort;
    [ObservableProperty]
    private BlackAndWhiteListViewModel _whiteListLong;
    [ObservableProperty]
    private BlackAndWhiteListViewModel _whiteListShort;

    [ObservableProperty]
    private QuotesViewModel _quotesViewModel;

    [ObservableProperty]
    private StrategyViewModel _strategyViewModel;

    [ObservableProperty]
    private IntervalsViewModel _intervalsViewModel;

    [ObservableProperty]
    private ColorAndSoundViewModel _longColorSound;
    [ObservableProperty]
    private ColorAndSoundViewModel _shortColorSound;

    
    public SettingsViewModel()
    {
        // Initialize child view models
        _exchangeViewModel = new();
        _commonViewModel = new();
        _rsiViewModel = new();
        _stochViewModel = new();
        _bollingerBandViewModel = new();

        _primaryTrend = new();
        _secondaryTrend = new();

        _blackListLong = new();
        _blackListShort = new();
        _whiteListLong = new();
        _whiteListShort = new();

        _quotesViewModel = new();
        _strategyViewModel = new();
        _intervalsViewModel = new();

        _longColorSound = new();
        _shortColorSound = new();

        LoadConfig();
    }


    private void LoadConfig()
    {
        ExchangeViewModel.LoadConfig(GlobalData.Settings.General);
        CommonViewModel.LoadConfig(GlobalData.Settings.General);

        RsiViewModel.LoadConfig(GlobalData.Settings.General.SettingsRsi);
        StochViewModel.LoadConfig(GlobalData.Settings.General.SettingsStoch);
        BollingerBandViewModel.LoadConfig(GlobalData.Settings.General.SettingsBb);

        PrimaryTrend.LoadConfig(GlobalData.Settings.Trend.Secondary);
        SecondaryTrend.LoadConfig(GlobalData.Settings.Trend.Secondary);

        BlackListLong.LoadConfig(GlobalData.Settings.BlackListOversold);
        BlackListShort.LoadConfig(GlobalData.Settings.BlackListOverbought);
        WhiteListLong.LoadConfig(GlobalData.Settings.WhiteListOversold);
        WhiteListShort.LoadConfig(GlobalData.Settings.WhiteListOverbought);

        //QuoteViewModel.LoadConfig(
        //StrategyViewModel.LoadConfig(
        //_intervalViewModel

        LongColorSound.LoadConfig("SBM Long", GlobalData.Settings.Signal.Stobb.ColorLong, GlobalData.Settings.Signal.Stobb.SoundFileLong);
        ShortColorSound.LoadConfig("SBM Short", GlobalData.Settings.Signal.Stobb.ColorShort, GlobalData.Settings.Signal.Stobb.SoundFileShort);
    }

    private void SaveConfig()
    {
        ExchangeViewModel.SaveConfig(GlobalData.Settings.General);
        CommonViewModel.SaveConfig(GlobalData.Settings.General);

        RsiViewModel.SaveConfig(GlobalData.Settings.General.SettingsRsi);
        StochViewModel.SaveConfig(GlobalData.Settings.General.SettingsStoch);
        BollingerBandViewModel.SaveConfig(GlobalData.Settings.General.SettingsBb);

        PrimaryTrend.SaveConfig(GlobalData.Settings.Trend.Secondary);
        SecondaryTrend.SaveConfig(GlobalData.Settings.Trend.Secondary);

        BlackListLong.SaveConfig(GlobalData.Settings.BlackListOversold);
        BlackListShort.SaveConfig(GlobalData.Settings.BlackListOverbought);
        WhiteListLong.SaveConfig(GlobalData.Settings.WhiteListOversold);
        WhiteListShort.SaveConfig(GlobalData.Settings.WhiteListOverbought);

        //QuoteViewModel.SaveConfig(
        //StrategyViewModel.SaveConfig(
        //_intervalViewModel
    }


    // todo: Reset?
    // todo: Test Speech
    // todo: Datafolder

    [RelayCommand]
    private void Okay(Window dialogWindow)
    {
        SaveConfig();
        dialogWindow.Close(true);
    }

    [RelayCommand]
    private static void Cancel(Window dialogWindow)
    {
        dialogWindow.Close(false);
    }
}

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


    // Signals
    [ObservableProperty]
    private IntervalsViewModel _intervalSignalLongViewModel;
    [ObservableProperty]
    private StrategyViewModel _strategySignalLongViewModel;
    [ObservableProperty]
    private IntervalsViewModel _intervalSignalShortViewModel;
    [ObservableProperty]
    private StrategyViewModel _strategySignalShortViewModel;


    // Trading
    [ObservableProperty]
    private IntervalsViewModel _intervalTradingLongViewModel;
    [ObservableProperty]
    private StrategyViewModel _strategyTradingLongViewModel;
    [ObservableProperty]
    private IntervalsViewModel _intervalTradingShortViewModel;
    [ObservableProperty]
    private StrategyViewModel _strategyTradingShortViewModel;

    [ObservableProperty]
    private SoundAndColorsViewModel _sbmColorAndSound;
    [ObservableProperty]
    private SoundAndColorsViewModel _stobbColorAndSound;

    
    public SettingsViewModel()
    {
        // Exchange and common
        _exchangeViewModel = new();
        _commonViewModel = new();

        // Indicators
        _rsiViewModel = new();
        _stochViewModel = new();
        _bollingerBandViewModel = new();
        _primaryTrend = new();
        _secondaryTrend = new();

        // Base coins
        _quotesViewModel = new();

        // Signals
        _intervalSignalLongViewModel = new();
        _strategySignalLongViewModel = new();
        _intervalSignalShortViewModel = new();
        _strategySignalShortViewModel = new();

        // Trading
        _intervalTradingLongViewModel = new();
        _strategyTradingLongViewModel = new();
        _intervalTradingShortViewModel = new();
        _strategyTradingShortViewModel = new();

        // Strategies
        _sbmColorAndSound = new();
        _stobbColorAndSound = new();

        // Black and White lists
        _blackListLong = new();
        _blackListShort = new();
        _whiteListLong = new();
        _whiteListShort = new();

        LoadConfig();
    }


    private void LoadConfig()
    {
        // Exchange and Common
        ExchangeViewModel.LoadConfig(GlobalData.Settings.General);
        CommonViewModel.LoadConfig(GlobalData.Settings.General);

        // Indicators
        RsiViewModel.LoadConfig(GlobalData.Settings.General.SettingsRsi);
        StochViewModel.LoadConfig(GlobalData.Settings.General.SettingsStoch);
        BollingerBandViewModel.LoadConfig(GlobalData.Settings.General.SettingsBb);
        PrimaryTrend.LoadConfig(GlobalData.Settings.Trend.Secondary);
        SecondaryTrend.LoadConfig(GlobalData.Settings.Trend.Secondary);

        // Base coins
        QuotesViewModel.LoadConfig(GlobalData.Settings.QuoteCoins);

        // Signals
        IntervalSignalLongViewModel.LoadConfig(GlobalData.Settings.Signal.Long.Strategy);
        StrategySignalLongViewModel.LoadConfig(GlobalData.Settings.Signal.Long.Interval);
        IntervalSignalShortViewModel.LoadConfig(GlobalData.Settings.Signal.Short.Strategy);
        StrategySignalShortViewModel.LoadConfig(GlobalData.Settings.Signal.Short.Interval);

        // Trading
        IntervalTradingLongViewModel.LoadConfig(GlobalData.Settings.Trading.Long.Strategy);
        StrategyTradingLongViewModel.LoadConfig(GlobalData.Settings.Trading.Long.Interval);
        IntervalTradingShortViewModel.LoadConfig(GlobalData.Settings.Trading.Short.Strategy);
        StrategyTradingShortViewModel.LoadConfig(GlobalData.Settings.Trading.Short.Interval);

        // Strategies
        SbmColorAndSound.LoadConfig("SBM", GlobalData.Settings.Signal.Sbm);
        StobbColorAndSound.LoadConfig("STOBB", GlobalData.Settings.Signal.Stobb);
        //Enzovoort..

        // Black and White lists
        BlackListLong.LoadConfig(GlobalData.Settings.BlackListOversold);
        BlackListShort.LoadConfig(GlobalData.Settings.BlackListOverbought);
        WhiteListLong.LoadConfig(GlobalData.Settings.WhiteListOversold);
        WhiteListShort.LoadConfig(GlobalData.Settings.WhiteListOverbought);

        // Debug
        // ..
    }

    private void SaveConfig()
    {
        // Exchange and Common
        ExchangeViewModel.SaveConfig(GlobalData.Settings.General);
        CommonViewModel.SaveConfig(GlobalData.Settings.General);

        // Indicators
        RsiViewModel.SaveConfig(GlobalData.Settings.General.SettingsRsi);
        StochViewModel.SaveConfig(GlobalData.Settings.General.SettingsStoch);
        BollingerBandViewModel.SaveConfig(GlobalData.Settings.General.SettingsBb);
        PrimaryTrend.SaveConfig(GlobalData.Settings.Trend.Secondary);
        SecondaryTrend.SaveConfig(GlobalData.Settings.Trend.Secondary);

        // Quotes
        QuotesViewModel.SaveConfig();

        // Signals
        IntervalSignalLongViewModel.SaveConfig(GlobalData.Settings.Signal.Long.Strategy);
        StrategySignalLongViewModel.SaveConfig(GlobalData.Settings.Signal.Long.Interval);
        IntervalSignalShortViewModel.SaveConfig(GlobalData.Settings.Signal.Short.Strategy);
        StrategySignalShortViewModel.SaveConfig(GlobalData.Settings.Signal.Short.Interval);

        // Trading
        IntervalTradingLongViewModel.SaveConfig(GlobalData.Settings.Trading.Long.Strategy);
        StrategyTradingLongViewModel.SaveConfig(GlobalData.Settings.Trading.Long.Interval);
        IntervalTradingShortViewModel.SaveConfig(GlobalData.Settings.Trading.Short.Strategy);
        StrategyTradingShortViewModel.SaveConfig(GlobalData.Settings.Trading.Short.Interval);

        // Strategies
        SbmColorAndSound.SaveConfig(GlobalData.Settings.Signal.Sbm);
        StobbColorAndSound.SaveConfig(GlobalData.Settings.Signal.Stobb);
        //Enzovoort..

        // Black and White lists
        BlackListLong.SaveConfig(GlobalData.Settings.BlackListOversold);
        BlackListShort.SaveConfig(GlobalData.Settings.BlackListOverbought);
        WhiteListLong.SaveConfig(GlobalData.Settings.WhiteListOversold);
        WhiteListShort.SaveConfig(GlobalData.Settings.WhiteListOverbought);

        // Debug
        // ..
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

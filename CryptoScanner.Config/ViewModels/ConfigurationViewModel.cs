using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<string> _exchanges = [];

    [ObservableProperty]
    private ExchangeViewModel _exchangeViewModel;
    [ObservableProperty]
    private CommonViewModel _commonViewModel;

    // Indicators
    [ObservableProperty]
    private IndicatorsTabViewModel _indicatorsTabViewModel;

    // Base coins
    [ObservableProperty]
    private QuoteTabViewModel _quoteTabViewModel;

    // Strategies
    [ObservableProperty]
    private StrategyTabViewModel _strategyTabViewModel;

    // Analyzer
    [ObservableProperty]
    private AnalyzerTabViewModel _analyzerTabViewModel;

    // Trader
    [ObservableProperty]
    private TraderTabViewModel _traderTabViewModel;

    // Rulez
    [ObservableProperty]
    private TraderRulesViewModel _traderRulesViewModel;

    // Api's
    [ObservableProperty]
    private ApiAltradyViewModel _apiAltradyViewModel;
    [ObservableProperty]
    private ApiTelegramViewModel _apiTelegramViewModel;
    [ObservableProperty]
    private ApiAlpacaViewModel _apiAlpacaViewModel;

    // Black and White lists
    [ObservableProperty]
    private BlackAndWhiteListTabViewModel _blackAndWhiteListTabViewModel;

    [ObservableProperty]
    private DebugTabViewModel _debugTabViewModel;

    public ConfigurationViewModel()
    {
        // Exchange and common
        _exchangeViewModel = new();
        _commonViewModel = new();

        // Indicators
        _indicatorsTabViewModel = new();

        // Base coins
        _quoteTabViewModel = new();

        // Strategies
        _strategyTabViewModel = new();

        // Analyzer
        _analyzerTabViewModel = new();

        // Trader
        _traderTabViewModel = new();

        // Rulez
        _traderRulesViewModel = new();

        // Api's
        _apiAltradyViewModel = new();
        _apiTelegramViewModel = new();
        _apiAlpacaViewModel = new();

        // Black and White lists
        _blackAndWhiteListTabViewModel = new();

        // Debug
        _debugTabViewModel = new();

        // Wire cross-tab strategy counterparts (Analyzer ↔ Trader)
        _analyzerTabViewModel.AnalyzerStrategyLongViewModel.CrossTabLongCounterpart = _traderTabViewModel.TraderStrategyLongViewModel;
        _analyzerTabViewModel.AnalyzerStrategyLongViewModel.CrossTabShortCounterpart = _traderTabViewModel.TraderStrategyShortViewModel;
        _analyzerTabViewModel.AnalyzerStrategyShortViewModel.CrossTabLongCounterpart = _traderTabViewModel.TraderStrategyLongViewModel;
        _analyzerTabViewModel.AnalyzerStrategyShortViewModel.CrossTabShortCounterpart = _traderTabViewModel.TraderStrategyShortViewModel;

        _traderTabViewModel.TraderStrategyLongViewModel.CrossTabLongCounterpart = _analyzerTabViewModel.AnalyzerStrategyLongViewModel;
        _traderTabViewModel.TraderStrategyLongViewModel.CrossTabShortCounterpart = _analyzerTabViewModel.AnalyzerStrategyShortViewModel;
        _traderTabViewModel.TraderStrategyLongViewModel.CrossTabLabel = "Analyzer";
        _traderTabViewModel.TraderStrategyShortViewModel.CrossTabLongCounterpart = _analyzerTabViewModel.AnalyzerStrategyLongViewModel;
        _traderTabViewModel.TraderStrategyShortViewModel.CrossTabShortCounterpart = _analyzerTabViewModel.AnalyzerStrategyShortViewModel;
        _traderTabViewModel.TraderStrategyShortViewModel.CrossTabLabel = "Analyzer";

        // Wire cross-tab interval counterparts (Analyzer ↔ Trader)
        _analyzerTabViewModel.AnalyzerIntervalLongViewModel.CrossTabLongCounterpart = _traderTabViewModel.TraderIntervalLongViewModel;
        _analyzerTabViewModel.AnalyzerIntervalLongViewModel.CrossTabShortCounterpart = _traderTabViewModel.TraderIntervalShortViewModel;
        _analyzerTabViewModel.AnalyzerIntervalShortViewModel.CrossTabLongCounterpart = _traderTabViewModel.TraderIntervalLongViewModel;
        _analyzerTabViewModel.AnalyzerIntervalShortViewModel.CrossTabShortCounterpart = _traderTabViewModel.TraderIntervalShortViewModel;

        _traderTabViewModel.TraderIntervalLongViewModel.CrossTabLongCounterpart = _analyzerTabViewModel.AnalyzerIntervalLongViewModel;
        _traderTabViewModel.TraderIntervalLongViewModel.CrossTabShortCounterpart = _analyzerTabViewModel.AnalyzerIntervalShortViewModel;
        _traderTabViewModel.TraderIntervalLongViewModel.CrossTabLabel = "Analyzer";
        _traderTabViewModel.TraderIntervalShortViewModel.CrossTabLongCounterpart = _analyzerTabViewModel.AnalyzerIntervalLongViewModel;
        _traderTabViewModel.TraderIntervalShortViewModel.CrossTabShortCounterpart = _analyzerTabViewModel.AnalyzerIntervalShortViewModel;
        _traderTabViewModel.TraderIntervalShortViewModel.CrossTabLabel = "Analyzer";


        LoadConfig(GlobalData.Settings);
    }

    private void LoadConfig(SettingsBasic settings)
    {
        // Exchange and Common
        ExchangeViewModel.LoadConfig(settings.General);
        CommonViewModel.LoadConfig(settings.General);

        // Indicators
        IndicatorsTabViewModel.LoadConfig(settings);

        // Base coins
        QuoteTabViewModel.LoadConfig(settings.QuoteCoins);

        // Strategies
        StrategyTabViewModel.LoadConfig();

        // Analyzer
        AnalyzerTabViewModel.LoadConfig(settings.Signal);

        // Trader
        TraderTabViewModel.LoadConfig(settings.Trading);

        // Rulez
        TraderRulesViewModel.LoadConfig(settings.Trading);

        // Apis
        ApiAltradyViewModel.LoadConfig(GlobalData.AltradyApi);
        ApiTelegramViewModel.LoadConfig(GlobalData.Telegram);
        ApiAlpacaViewModel.LoadConfig(GlobalData.TradingApi);

        // Black and White lists
        BlackAndWhiteListTabViewModel.LoadConfig(settings);

        // Debug
        DebugTabViewModel.LoadConfig(settings.General);
    }

    private void SaveConfig(SettingsBasic settings)
    {
        // Exchange and Common
        ExchangeViewModel.SaveConfig(settings.General);
        CommonViewModel.SaveConfig(settings.General);

        // Indicators
        IndicatorsTabViewModel.SaveConfig(settings);

        // Quotes
        QuoteTabViewModel.SaveConfig();

        // Strategies
        StrategyTabViewModel.SaveConfig();

        // Analyzer
        AnalyzerTabViewModel.SaveConfig(settings.Signal);

        // Trader
        TraderTabViewModel.SaveConfig(settings.Trading);

        // Rulez
        TraderRulesViewModel.SaveConfig(settings.Trading);

        // Apis
        ApiAltradyViewModel.SaveConfig(GlobalData.AltradyApi);
        ApiTelegramViewModel.SaveConfig(GlobalData.Telegram);
        ApiAlpacaViewModel.SaveConfig(GlobalData.TradingApi);

        // Black and White lists
        BlackAndWhiteListTabViewModel.SaveConfig(settings);

        // Debug
        DebugTabViewModel.SaveConfig(settings.General);
    }


    // todo: Reset?
    // todo: Test Speech
    // todo: Datafolder

    [RelayCommand]
    private void Okay(Window dialogWindow)
    {
        SaveConfig(GlobalData.Settings);
        dialogWindow.Close(true);
    }

    [RelayCommand]
    private static void Cancel(Window dialogWindow)
    {
        dialogWindow.Close(false);
    }
}

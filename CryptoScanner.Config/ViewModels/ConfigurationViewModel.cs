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

    // The settings this dialog edits. Normally GlobalData.Settings; a caller can pass another set
    // (e.g. those of a finished emulator run) to inspect it without swapping the global instance.
    private readonly SettingsBasic _settings;

    /// <summary>True when Okay must not write anything back — inspecting a stored settings set.</summary>
    public bool IsReadOnly { get; }

    public ConfigurationViewModel() : this(null, false)
    {
    }

    public ConfigurationViewModel(SettingsBasic? settings, bool readOnly)
    {
        _settings = settings ?? GlobalData.Settings;
        IsReadOnly = readOnly;

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


        LoadConfig(_settings);
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
        QuoteTabViewModel.LoadConfig(settings.Products, GlobalData.ActiveExchange);

        // Strategies
        // Read-only means we are inspecting a stored settings set, so the plugin tabs must come from
        // its AnalyzerSettings blocks rather than from the live plugin instances.
        StrategyTabViewModel.LoadConfig(settings.Signal, fromStoredSettings: IsReadOnly);

        // Analyzer
        AnalyzerTabViewModel.LoadConfig(settings.Signal, settings.General);

        // Trader
        TraderTabViewModel.LoadConfig(settings.Trading, settings.General);

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
        AnalyzerTabViewModel.SaveConfig(settings.Signal, settings.General);

        // Trader
        TraderTabViewModel.SaveConfig(settings.Trading, settings.General);

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
        if (!IsReadOnly)
            SaveConfig(_settings);
        dialogWindow.Close(!IsReadOnly);
    }

    [RelayCommand]
    private static void Cancel(Window dialogWindow)
    {
        dialogWindow.Close(false);
    }
}

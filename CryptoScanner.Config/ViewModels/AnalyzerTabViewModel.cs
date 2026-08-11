using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class AnalyzerTabViewModel : ObservableObject
{
    [ObservableProperty]
    private AnalyzerCommonViewModel _analyzerCommonViewModel;

    [ObservableProperty]
    private IntervalViewModel _analyzerIntervalLongViewModel;
    [ObservableProperty]
    private StrategyViewModel _analyzerStrategyLongViewModel;
    [ObservableProperty]
    private BarometerFilterViewModel _analyzerBarometerFilterLongViewModel;
    [ObservableProperty]
    private MarketTrendFilterViewModel _analyzerMarketTrendFilterLongViewModel;
    [ObservableProperty]
    private MarketTrendFilterViewModel _analyzerMarketTrendFilterSecondaryLongViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _analyzerTrendIntervalFilterLongViewModel;

    [ObservableProperty]
    private IntervalViewModel _analyzerIntervalShortViewModel;
    [ObservableProperty]
    private StrategyViewModel _analyzerStrategyShortViewModel;
    [ObservableProperty]
    private BarometerFilterViewModel _analyzerBarometerFilterShortViewModel;
    [ObservableProperty]
    private MarketTrendFilterViewModel _analyzerMarketTrendFilterShortViewModel;
    [ObservableProperty]
    private MarketTrendFilterViewModel _analyzerMarketTrendFilterSecondaryShortViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _analyzerTrendIntervalFilterShortViewModel;




    public AnalyzerTabViewModel()
    {
        _analyzerCommonViewModel = new();

        _analyzerIntervalLongViewModel = new();
        _analyzerStrategyLongViewModel = new();
        _analyzerBarometerFilterLongViewModel = new();
        _analyzerMarketTrendFilterLongViewModel = new() { Header = "Market trend filter (primary)" };
        _analyzerMarketTrendFilterSecondaryLongViewModel = new() { Header = "Market trend filter (secondary)" };
        _analyzerTrendIntervalFilterLongViewModel = new();

        _analyzerIntervalShortViewModel = new();
        _analyzerStrategyShortViewModel = new();
        _analyzerBarometerFilterShortViewModel = new();
        _analyzerMarketTrendFilterShortViewModel = new() { Header = "Market trend filter (primary)" };
        _analyzerMarketTrendFilterSecondaryShortViewModel = new() { Header = "Market trend filter (secondary)" };
        _analyzerTrendIntervalFilterShortViewModel = new();

        // Wire up the "Copy from..." popup on the strategy views so each side knows about
        // its counterpart. The popup hides the self-copy option via CanExecute.
        _analyzerStrategyLongViewModel.LongCounterpart = _analyzerStrategyLongViewModel;
        _analyzerStrategyLongViewModel.ShortCounterpart = _analyzerStrategyShortViewModel;
        _analyzerStrategyShortViewModel.LongCounterpart = _analyzerStrategyLongViewModel;
        _analyzerStrategyShortViewModel.ShortCounterpart = _analyzerStrategyShortViewModel;

        // Wire up same-tab interval counterparts
        _analyzerIntervalLongViewModel.LongCounterpart = _analyzerIntervalLongViewModel;
        _analyzerIntervalLongViewModel.ShortCounterpart = _analyzerIntervalShortViewModel;
        _analyzerIntervalShortViewModel.LongCounterpart = _analyzerIntervalLongViewModel;
        _analyzerIntervalShortViewModel.ShortCounterpart = _analyzerIntervalShortViewModel;
    }

    internal void LoadConfig(SettingsSignal settings, SettingsGeneral general)
    {
        // TODO: refactor two settings!
        AnalyzerCommonViewModel.LoadConfig(settings, general);

        AnalyzerIntervalLongViewModel.LoadConfig(settings.Long.Interval);
        AnalyzerStrategyLongViewModel.LoadConfig(settings.Long.Strategy);
        AnalyzerBarometerFilterLongViewModel.LoadConfig(settings.Long.Barometer);
        AnalyzerMarketTrendFilterLongViewModel.LoadConfig(settings.Long.MarketTrend);
        AnalyzerMarketTrendFilterSecondaryLongViewModel.LoadConfig(settings.Long.MarketTrendSecondary);
        AnalyzerTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        AnalyzerIntervalShortViewModel.LoadConfig(settings.Short.Interval);
        AnalyzerStrategyShortViewModel.LoadConfig(settings.Short.Strategy);
        AnalyzerBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        AnalyzerMarketTrendFilterShortViewModel.LoadConfig(settings.Short.MarketTrend);
        AnalyzerMarketTrendFilterSecondaryShortViewModel.LoadConfig(settings.Short.MarketTrendSecondary);
        AnalyzerTrendIntervalFilterShortViewModel.LoadConfig(settings.Short.IntervalTrend, CryptoTradeSide.Short);
    }

    internal void SaveConfig(SettingsSignal settings, SettingsGeneral general)
    {
        AnalyzerCommonViewModel.SaveConfig(settings, general);

        AnalyzerIntervalLongViewModel.SaveConfig(settings.Long.Interval);
        AnalyzerStrategyLongViewModel.SaveConfig(settings.Long.Strategy);
        AnalyzerBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        AnalyzerMarketTrendFilterLongViewModel.SaveConfig(settings.Long.MarketTrend);
        AnalyzerMarketTrendFilterSecondaryLongViewModel.SaveConfig(settings.Long.MarketTrendSecondary);
        AnalyzerTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        AnalyzerIntervalShortViewModel.SaveConfig(settings.Short.Interval);
        AnalyzerStrategyShortViewModel.SaveConfig(settings.Short.Strategy);
        AnalyzerBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        AnalyzerMarketTrendFilterShortViewModel.SaveConfig(settings.Short.MarketTrend);
        AnalyzerMarketTrendFilterSecondaryShortViewModel.SaveConfig(settings.Short.MarketTrendSecondary);
        AnalyzerTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

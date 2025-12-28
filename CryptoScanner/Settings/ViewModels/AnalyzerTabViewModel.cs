using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

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
    private TrendIntervalFilterViewModel _analyzerTrendIntervalFilterShortViewModel;
    



    public AnalyzerTabViewModel()
    {
        _analyzerCommonViewModel = new();

        _analyzerIntervalLongViewModel = new();
        _analyzerStrategyLongViewModel = new();
        _analyzerBarometerFilterLongViewModel = new();
        _analyzerMarketTrendFilterLongViewModel = new();
        _analyzerTrendIntervalFilterLongViewModel = new();

        _analyzerIntervalShortViewModel = new();
        _analyzerStrategyShortViewModel = new();
        _analyzerBarometerFilterShortViewModel = new();
        _analyzerMarketTrendFilterShortViewModel = new();
        _analyzerTrendIntervalFilterShortViewModel = new();
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        // TODO: refactor two settings!
        AnalyzerCommonViewModel.LoadConfig(GlobalData.Settings.Signal);

        AnalyzerIntervalLongViewModel.LoadConfig(settings.Long.Strategy);
        AnalyzerStrategyLongViewModel.LoadConfig(settings.Long.Interval);
        AnalyzerBarometerFilterLongViewModel.LoadConfig(settings.Long.Barometer);
        AnalyzerMarketTrendFilterLongViewModel.LoadConfig(settings.Long.MarketTrend);
        AnalyzerTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        AnalyzerIntervalShortViewModel.LoadConfig(settings.Short.Strategy);
        AnalyzerStrategyShortViewModel.LoadConfig(settings.Short.Interval);
        AnalyzerBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        AnalyzerMarketTrendFilterShortViewModel.LoadConfig(settings.Short.MarketTrend);
        AnalyzerTrendIntervalFilterShortViewModel.LoadConfig(settings.Short.IntervalTrend, CryptoTradeSide.Short);
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        AnalyzerCommonViewModel.SaveConfig(GlobalData.Settings.Signal);

        AnalyzerIntervalLongViewModel.SaveConfig(settings.Long.Strategy);
        AnalyzerStrategyLongViewModel.SaveConfig(settings.Long.Interval);
        AnalyzerBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        AnalyzerMarketTrendFilterLongViewModel.SaveConfig(settings.Long.MarketTrend);
        AnalyzerTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        AnalyzerIntervalShortViewModel.SaveConfig(settings.Short.Strategy);
        AnalyzerStrategyShortViewModel.SaveConfig(settings.Short.Interval);
        AnalyzerBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        AnalyzerMarketTrendFilterShortViewModel.SaveConfig(settings.Short.MarketTrend);
        AnalyzerTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

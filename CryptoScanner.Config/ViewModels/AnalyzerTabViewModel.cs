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
    private VolumeFilterViewModel _analyzerVolumeFilterLongViewModel;
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
    private VolumeFilterViewModel _analyzerVolumeFilterShortViewModel;
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
        _analyzerVolumeFilterLongViewModel = new();
        _analyzerMarketTrendFilterLongViewModel = new();
        _analyzerTrendIntervalFilterLongViewModel = new();

        _analyzerIntervalShortViewModel = new();
        _analyzerStrategyShortViewModel = new();
        _analyzerBarometerFilterShortViewModel = new();
        _analyzerVolumeFilterShortViewModel = new();
        _analyzerMarketTrendFilterShortViewModel = new();
        _analyzerTrendIntervalFilterShortViewModel = new();
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        // TODO: refactor two settings!
        AnalyzerCommonViewModel.LoadConfig(GlobalData.Settings.Signal);

        AnalyzerIntervalLongViewModel.LoadConfig(settings.Long.Interval);
        AnalyzerStrategyLongViewModel.LoadConfig(settings.Long.Strategy);
        AnalyzerBarometerFilterLongViewModel.LoadConfig(settings.Long.Barometer);
        AnalyzerVolumeFilterLongViewModel.LoadConfig(settings.Long.Volume);
        AnalyzerMarketTrendFilterLongViewModel.LoadConfig(settings.Long.MarketTrend);
        AnalyzerTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        AnalyzerIntervalShortViewModel.LoadConfig(settings.Short.Interval);
        AnalyzerStrategyShortViewModel.LoadConfig(settings.Short.Strategy);
        AnalyzerBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        AnalyzerVolumeFilterShortViewModel.LoadConfig(settings.Short.Volume);
        AnalyzerMarketTrendFilterShortViewModel.LoadConfig(settings.Short.MarketTrend);
        AnalyzerTrendIntervalFilterShortViewModel.LoadConfig(settings.Short.IntervalTrend, CryptoTradeSide.Short);
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        AnalyzerCommonViewModel.SaveConfig(GlobalData.Settings.Signal);

        AnalyzerIntervalLongViewModel.SaveConfig(settings.Long.Interval);
        AnalyzerStrategyLongViewModel.SaveConfig(settings.Long.Strategy);
        AnalyzerBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        AnalyzerVolumeFilterLongViewModel.SaveConfig(settings.Long.Volume);
        AnalyzerMarketTrendFilterLongViewModel.SaveConfig(settings.Long.MarketTrend);
        AnalyzerTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        AnalyzerIntervalShortViewModel.SaveConfig(settings.Short.Interval);
        AnalyzerStrategyShortViewModel.SaveConfig(settings.Short.Strategy);
        AnalyzerBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        AnalyzerVolumeFilterShortViewModel.SaveConfig(settings.Short.Volume);
        AnalyzerMarketTrendFilterShortViewModel.SaveConfig(settings.Short.MarketTrend);
        AnalyzerTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

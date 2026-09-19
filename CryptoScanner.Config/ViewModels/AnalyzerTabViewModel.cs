using CommunityToolkit.Mvvm.ComponentModel;

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
    private SymbolTrendFilterViewModel _analyzerSymbolTrendFilterLongViewModel;
    [ObservableProperty]
    private SymbolTrendFilterViewModel _analyzerSymbolTrendFilterSecondaryLongViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _analyzerTrendIntervalFilterLongViewModel;

    [ObservableProperty]
    private IntervalViewModel _analyzerIntervalShortViewModel;
    [ObservableProperty]
    private StrategyViewModel _analyzerStrategyShortViewModel;
    [ObservableProperty]
    private BarometerFilterViewModel _analyzerBarometerFilterShortViewModel;
    [ObservableProperty]
    private SymbolTrendFilterViewModel _analyzerSymbolTrendFilterShortViewModel;
    [ObservableProperty]
    private SymbolTrendFilterViewModel _analyzerSymbolTrendFilterSecondaryShortViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _analyzerTrendIntervalFilterShortViewModel;




    public AnalyzerTabViewModel()
    {
        _analyzerCommonViewModel = new();

        _analyzerIntervalLongViewModel = new();
        _analyzerStrategyLongViewModel = new();
        _analyzerBarometerFilterLongViewModel = new();
        _analyzerSymbolTrendFilterLongViewModel = new() { Header = "Symbol trend filter (primary)" };
        _analyzerSymbolTrendFilterSecondaryLongViewModel = new() { Header = "Symbol trend filter (secondary)" };
        _analyzerTrendIntervalFilterLongViewModel = new();

        _analyzerIntervalShortViewModel = new();
        _analyzerStrategyShortViewModel = new();
        _analyzerBarometerFilterShortViewModel = new();
        _analyzerSymbolTrendFilterShortViewModel = new() { Header = "Symbol trend filter (primary)" };
        _analyzerSymbolTrendFilterSecondaryShortViewModel = new() { Header = "Symbol trend filter (secondary)" };
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
        AnalyzerSymbolTrendFilterLongViewModel.LoadConfig(settings.Long.SymbolTrend);
        AnalyzerSymbolTrendFilterSecondaryLongViewModel.LoadConfig(settings.Long.SymbolTrendSecondary);
        AnalyzerTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        AnalyzerIntervalShortViewModel.LoadConfig(settings.Short.Interval);
        AnalyzerStrategyShortViewModel.LoadConfig(settings.Short.Strategy);
        AnalyzerBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        AnalyzerSymbolTrendFilterShortViewModel.LoadConfig(settings.Short.SymbolTrend);
        AnalyzerSymbolTrendFilterSecondaryShortViewModel.LoadConfig(settings.Short.SymbolTrendSecondary);
        AnalyzerTrendIntervalFilterShortViewModel.LoadConfig(settings.Short.IntervalTrend, CryptoTradeSide.Short);
    }

    internal void SaveConfig(SettingsSignal settings, SettingsGeneral general)
    {
        AnalyzerCommonViewModel.SaveConfig(settings, general);

        AnalyzerIntervalLongViewModel.SaveConfig(settings.Long.Interval);
        AnalyzerStrategyLongViewModel.SaveConfig(settings.Long.Strategy);
        AnalyzerBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        AnalyzerSymbolTrendFilterLongViewModel.SaveConfig(settings.Long.SymbolTrend);
        AnalyzerSymbolTrendFilterSecondaryLongViewModel.SaveConfig(settings.Long.SymbolTrendSecondary);
        AnalyzerTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        AnalyzerIntervalShortViewModel.SaveConfig(settings.Short.Interval);
        AnalyzerStrategyShortViewModel.SaveConfig(settings.Short.Strategy);
        AnalyzerBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        AnalyzerSymbolTrendFilterShortViewModel.SaveConfig(settings.Short.SymbolTrend);
        AnalyzerSymbolTrendFilterSecondaryShortViewModel.SaveConfig(settings.Short.SymbolTrendSecondary);
        AnalyzerTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

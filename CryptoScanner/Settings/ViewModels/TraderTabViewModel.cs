using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

public partial class TraderTabViewModel : ObservableObject
{
    [ObservableProperty]
    private TraderFuturesViewModel _traderFuturesViewModel;
    [ObservableProperty]
    TraderEntryConditionsViewModel _traderEntryConditionsViewModel;
    [ObservableProperty]
    private TraderMiscSettingsViewModel _traderMiscSettingsViewModel;
    [ObservableProperty]
    private TraderTakeProfitViewModel _traderTakeProfitViewModel;
    [ObservableProperty]
    private TraderStopLossViewModel _traderStopLossViewModel;
    [ObservableProperty]
    private TraderEntryViewModel _traderEntryViewModel;
    [ObservableProperty]
    private TraderDcaViewModel _traderDcaViewModel;

    [ObservableProperty]
    private IntervalViewModel _traderIntervalLongViewModel;
    [ObservableProperty]
    private StrategyViewModel _traderStrategyLongViewModel;
    [ObservableProperty]
    private BarometerFilterViewModel _traderBarometerFilterLongViewModel;
    [ObservableProperty]
    private MarketTrendFilterViewModel _traderMarketTrendFilterLongViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _traderTrendIntervalFilterLongViewModel;

    [ObservableProperty]
    private IntervalViewModel _traderIntervalShortViewModel;
    [ObservableProperty]
    private StrategyViewModel _traderStrategyShortViewModel;
    [ObservableProperty]
    private BarometerFilterViewModel _traderBarometerFilterShortViewModel;
    [ObservableProperty]
    private MarketTrendFilterViewModel _traderMarketTrendFilterShortViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _traderTrendIntervalFilterShortViewModel;

       

    public TraderTabViewModel()
    {
        _traderFuturesViewModel = new();
        _traderMiscSettingsViewModel = new();
        _traderEntryConditionsViewModel = new();
        _traderTakeProfitViewModel = new();
        _traderStopLossViewModel = new();
        _traderEntryViewModel = new();
        _traderDcaViewModel = new();

        _traderIntervalLongViewModel = new();
        _traderStrategyLongViewModel = new();
        _traderBarometerFilterLongViewModel = new();
        _traderMarketTrendFilterLongViewModel = new();
        _traderTrendIntervalFilterLongViewModel = new();

        _traderIntervalShortViewModel = new();
        _traderStrategyShortViewModel = new();
        _traderBarometerFilterShortViewModel = new();
        _traderMarketTrendFilterShortViewModel = new();
        _traderTrendIntervalFilterShortViewModel = new();
    }

    internal void LoadConfig(SettingsTrading settings)
    {
        TraderFuturesViewModel.LoadConfig(settings);
        TraderMiscSettingsViewModel.LoadConfig(settings);
        TraderEntryConditionsViewModel.LoadConfig(settings);
        TraderTakeProfitViewModel.LoadConfig(settings);
        TraderStopLossViewModel.LoadConfig(settings);
        TraderEntryViewModel.LoadConfig(settings);
        TraderDcaViewModel.LoadConfig(settings);

        TraderIntervalLongViewModel.LoadConfig(settings.Long.Strategy);
        TraderStrategyLongViewModel.LoadConfig(settings.Long.Interval);
        TraderBarometerFilterLongViewModel.LoadConfig(settings.Long.Barometer);
        TraderMarketTrendFilterLongViewModel.LoadConfig(settings.Long.MarketTrend);
        TraderTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        TraderIntervalShortViewModel.LoadConfig(settings.Short.Strategy);
        TraderStrategyShortViewModel.LoadConfig(settings.Short.Interval);
        TraderBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        TraderMarketTrendFilterShortViewModel.LoadConfig(settings.Short.MarketTrend);
        TraderTrendIntervalFilterShortViewModel.LoadConfig(settings.Short.IntervalTrend, CryptoTradeSide.Short);
    }

    internal void SaveConfig(SettingsTrading settings)
    {
        TraderFuturesViewModel.SaveConfig(settings);
        TraderMiscSettingsViewModel.SaveConfig(settings);
        TraderEntryConditionsViewModel.SaveConfig(settings);
        TraderTakeProfitViewModel.SaveConfig(settings);
        TraderStopLossViewModel.SaveConfig(settings);
        TraderEntryViewModel.SaveConfig(settings);
        TraderDcaViewModel.SaveConfig(settings);

        TraderIntervalLongViewModel.SaveConfig(settings.Long.Strategy);
        TraderStrategyLongViewModel.SaveConfig(settings.Long.Interval);
        TraderBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        TraderMarketTrendFilterLongViewModel.SaveConfig(settings.Long.MarketTrend);
        TraderTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        TraderIntervalShortViewModel.SaveConfig(settings.Short.Strategy);
        TraderStrategyShortViewModel.SaveConfig(settings.Short.Interval);
        TraderBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        TraderMarketTrendFilterShortViewModel.SaveConfig(settings.Short.MarketTrend);
        TraderTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

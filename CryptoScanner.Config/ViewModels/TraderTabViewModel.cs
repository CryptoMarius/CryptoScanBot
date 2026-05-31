using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

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
    private MarketTrendFilterViewModel _traderMarketTrendFilterSecondaryLongViewModel;
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
    private MarketTrendFilterViewModel _traderMarketTrendFilterSecondaryShortViewModel;
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
        _traderMarketTrendFilterLongViewModel = new() { Header = "Market trend filter (primary)" };
        _traderMarketTrendFilterSecondaryLongViewModel = new() { Header = "Market trend filter (secondary)" };
        _traderTrendIntervalFilterLongViewModel = new();

        _traderIntervalShortViewModel = new();
        _traderStrategyShortViewModel = new();
        _traderBarometerFilterShortViewModel = new();
        _traderMarketTrendFilterShortViewModel = new() { Header = "Market trend filter (primary)" };
        _traderMarketTrendFilterSecondaryShortViewModel = new() { Header = "Market trend filter (secondary)" };
        _traderTrendIntervalFilterShortViewModel = new();

        // Wire up the "Copy from..." popup on the strategy views so each side knows about
        // its counterpart. The popup hides the self-copy option via CanExecute.
        _traderStrategyLongViewModel.LongCounterpart = _traderStrategyLongViewModel;
        _traderStrategyLongViewModel.ShortCounterpart = _traderStrategyShortViewModel;
        _traderStrategyShortViewModel.LongCounterpart = _traderStrategyLongViewModel;
        _traderStrategyShortViewModel.ShortCounterpart = _traderStrategyShortViewModel;
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

        TraderIntervalLongViewModel.LoadConfig(settings.Long.Interval);
        TraderStrategyLongViewModel.LoadConfig(settings.Long.Strategy);
        TraderBarometerFilterLongViewModel.LoadConfig(settings.Long.Barometer);
        TraderMarketTrendFilterLongViewModel.LoadConfig(settings.Long.MarketTrend);
        TraderMarketTrendFilterSecondaryLongViewModel.LoadConfig(settings.Long.MarketTrendSecondary);
        TraderTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        TraderIntervalShortViewModel.LoadConfig(settings.Short.Interval);
        TraderStrategyShortViewModel.LoadConfig(settings.Short.Strategy);
        TraderBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        TraderMarketTrendFilterShortViewModel.LoadConfig(settings.Short.MarketTrend);
        TraderMarketTrendFilterSecondaryShortViewModel.LoadConfig(settings.Short.MarketTrendSecondary);
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

        TraderIntervalLongViewModel.SaveConfig(settings.Long.Interval);
        TraderStrategyLongViewModel.SaveConfig(settings.Long.Strategy);
        TraderBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        TraderMarketTrendFilterLongViewModel.SaveConfig(settings.Long.MarketTrend);
        TraderMarketTrendFilterSecondaryLongViewModel.SaveConfig(settings.Long.MarketTrendSecondary);
        TraderTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        TraderIntervalShortViewModel.SaveConfig(settings.Short.Interval);
        TraderStrategyShortViewModel.SaveConfig(settings.Short.Strategy);
        TraderBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        TraderMarketTrendFilterShortViewModel.SaveConfig(settings.Short.MarketTrend);
        TraderMarketTrendFilterSecondaryShortViewModel.SaveConfig(settings.Short.MarketTrendSecondary);
        TraderTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

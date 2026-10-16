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
    private SymbolTrendFilterViewModel _traderSymbolTrendFilterLongViewModel;
    [ObservableProperty]
    private SymbolTrendFilterViewModel _traderSymbolTrendFilterSecondaryLongViewModel;
    [ObservableProperty]
    private MarketRsiFilterViewModel _traderMarketRsiFilterLongViewModel;
    [ObservableProperty]
    private MarketStateFilterViewModel _traderMarketStateFilterLongViewModel;
    [ObservableProperty]
    private EntryFiltersViewModel _traderEntryFiltersLongViewModel;
    [ObservableProperty]
    private MarketExitViewModel _traderMarketExitLongViewModel;
    [ObservableProperty]
    private TrendIntervalFilterViewModel _traderTrendIntervalFilterLongViewModel;

    [ObservableProperty]
    private IntervalViewModel _traderIntervalShortViewModel;
    [ObservableProperty]
    private StrategyViewModel _traderStrategyShortViewModel;
    [ObservableProperty]
    private BarometerFilterViewModel _traderBarometerFilterShortViewModel;
    [ObservableProperty]
    private SymbolTrendFilterViewModel _traderSymbolTrendFilterShortViewModel;
    [ObservableProperty]
    private SymbolTrendFilterViewModel _traderSymbolTrendFilterSecondaryShortViewModel;
    [ObservableProperty]
    private MarketRsiFilterViewModel _traderMarketRsiFilterShortViewModel;
    [ObservableProperty]
    private MarketStateFilterViewModel _traderMarketStateFilterShortViewModel;
    [ObservableProperty]
    private EntryFiltersViewModel _traderEntryFiltersShortViewModel;
    [ObservableProperty]
    private MarketExitViewModel _traderMarketExitShortViewModel;
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
        _traderSymbolTrendFilterLongViewModel = new() { Header = "Symbol trend filter (primary)" };
        _traderSymbolTrendFilterSecondaryLongViewModel = new() { Header = "Symbol trend filter (secondary)" };
        _traderMarketRsiFilterLongViewModel = new();
        _traderMarketStateFilterLongViewModel = new();
        _traderEntryFiltersLongViewModel = new();
        _traderMarketExitLongViewModel = new();
        _traderTrendIntervalFilterLongViewModel = new();

        _traderIntervalShortViewModel = new();
        _traderStrategyShortViewModel = new();
        _traderBarometerFilterShortViewModel = new();
        _traderSymbolTrendFilterShortViewModel = new() { Header = "Symbol trend filter (primary)" };
        _traderSymbolTrendFilterSecondaryShortViewModel = new() { Header = "Symbol trend filter (secondary)" };
        _traderMarketRsiFilterShortViewModel = new();
        _traderMarketStateFilterShortViewModel = new();
        _traderEntryFiltersShortViewModel = new();
        _traderMarketExitShortViewModel = new();
        _traderTrendIntervalFilterShortViewModel = new();

        // Wire up the "Copy from..." popup on the strategy views so each side knows about
        // its counterpart. The popup hides the self-copy option via CanExecute.
        _traderStrategyLongViewModel.LongCounterpart = _traderStrategyLongViewModel;
        _traderStrategyLongViewModel.ShortCounterpart = _traderStrategyShortViewModel;
        _traderStrategyShortViewModel.LongCounterpart = _traderStrategyLongViewModel;
        _traderStrategyShortViewModel.ShortCounterpart = _traderStrategyShortViewModel;

        // Wire up same-tab interval counterparts
        _traderIntervalLongViewModel.LongCounterpart = _traderIntervalLongViewModel;
        _traderIntervalLongViewModel.ShortCounterpart = _traderIntervalShortViewModel;
        _traderIntervalShortViewModel.LongCounterpart = _traderIntervalLongViewModel;
        _traderIntervalShortViewModel.ShortCounterpart = _traderIntervalShortViewModel;

        // The profit lock choices depend on the Trading choice (Altrady runs only some of them)
        _traderMiscSettingsViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TraderMiscSettingsViewModel.TradeVia))
                _traderStopLossViewModel.UsesAltrady = Core.Trader.ProfitLockCalculator.UsesAltrady(_traderMiscSettingsViewModel.TradeVia);
        };
    }

    internal void LoadConfig(SettingsTrading settings, SettingsGeneral general)
    {
        TraderFuturesViewModel.LoadConfig(settings);
        TraderMiscSettingsViewModel.LoadConfig(settings, general);
        TraderEntryConditionsViewModel.LoadConfig(settings);
        TraderTakeProfitViewModel.LoadConfig(settings);
        TraderStopLossViewModel.LoadConfig(settings);
        TraderStopLossViewModel.UsesAltrady = Core.Trader.ProfitLockCalculator.UsesAltrady(TraderMiscSettingsViewModel.TradeVia);
        TraderEntryViewModel.LoadConfig(settings);
        TraderDcaViewModel.LoadConfig(settings);

        TraderIntervalLongViewModel.LoadConfig(settings.Long.Interval);
        TraderStrategyLongViewModel.LoadConfig(settings.Long.Strategy);
        TraderBarometerFilterLongViewModel.LoadConfig(settings.Long.Barometer);
        TraderSymbolTrendFilterLongViewModel.LoadConfig(settings.Long.SymbolTrend);
        TraderSymbolTrendFilterSecondaryLongViewModel.LoadConfig(settings.Long.SymbolTrendSecondary);
        TraderMarketRsiFilterLongViewModel.LoadConfig(settings.Long.MarketRsi);
        TraderMarketStateFilterLongViewModel.LoadConfig(settings.Long.MarketState);
        TraderEntryFiltersLongViewModel.LoadConfig(settings.Long);
        TraderMarketExitLongViewModel.LoadConfig(settings.Long.MarketExit);
        TraderMarketExitLongViewModel.LoadEntryLimit(settings.Long.EntryLimit);
        TraderTrendIntervalFilterLongViewModel.LoadConfig(settings.Long.IntervalTrend, CryptoTradeSide.Long);

        TraderIntervalShortViewModel.LoadConfig(settings.Short.Interval);
        TraderStrategyShortViewModel.LoadConfig(settings.Short.Strategy);
        TraderBarometerFilterShortViewModel.LoadConfig(settings.Short.Barometer);
        TraderSymbolTrendFilterShortViewModel.LoadConfig(settings.Short.SymbolTrend);
        TraderSymbolTrendFilterSecondaryShortViewModel.LoadConfig(settings.Short.SymbolTrendSecondary);
        TraderMarketRsiFilterShortViewModel.LoadConfig(settings.Short.MarketRsi);
        TraderMarketStateFilterShortViewModel.LoadConfig(settings.Short.MarketState);
        TraderEntryFiltersShortViewModel.LoadConfig(settings.Short);
        TraderMarketExitShortViewModel.LoadConfig(settings.Short.MarketExit);
        TraderMarketExitShortViewModel.LoadEntryLimit(settings.Short.EntryLimit);
        TraderTrendIntervalFilterShortViewModel.LoadConfig(settings.Short.IntervalTrend, CryptoTradeSide.Short);
    }

    internal void SaveConfig(SettingsTrading settings, SettingsGeneral general)
    {
        TraderFuturesViewModel.SaveConfig(settings);
        TraderMiscSettingsViewModel.SaveConfig(settings, general);
        TraderEntryConditionsViewModel.SaveConfig(settings);
        TraderTakeProfitViewModel.SaveConfig(settings);
        TraderStopLossViewModel.SaveConfig(settings);
        TraderEntryViewModel.SaveConfig(settings);
        TraderDcaViewModel.SaveConfig(settings);

        TraderIntervalLongViewModel.SaveConfig(settings.Long.Interval);
        TraderStrategyLongViewModel.SaveConfig(settings.Long.Strategy);
        TraderBarometerFilterLongViewModel.SaveConfig(settings.Long.Barometer);
        TraderSymbolTrendFilterLongViewModel.SaveConfig(settings.Long.SymbolTrend);
        TraderSymbolTrendFilterSecondaryLongViewModel.SaveConfig(settings.Long.SymbolTrendSecondary);
        TraderMarketRsiFilterLongViewModel.SaveConfig(settings.Long.MarketRsi);
        TraderMarketStateFilterLongViewModel.SaveConfig(settings.Long.MarketState);
        TraderEntryFiltersLongViewModel.SaveConfig(settings.Long);
        TraderMarketExitLongViewModel.SaveConfig(settings.Long.MarketExit);
        TraderMarketExitLongViewModel.SaveEntryLimit(settings.Long.EntryLimit);
        TraderTrendIntervalFilterLongViewModel.SaveConfig(settings.Long.IntervalTrend);

        TraderIntervalShortViewModel.SaveConfig(settings.Short.Interval);
        TraderStrategyShortViewModel.SaveConfig(settings.Short.Strategy);
        TraderBarometerFilterShortViewModel.SaveConfig(settings.Short.Barometer);
        TraderSymbolTrendFilterShortViewModel.SaveConfig(settings.Short.SymbolTrend);
        TraderSymbolTrendFilterSecondaryShortViewModel.SaveConfig(settings.Short.SymbolTrendSecondary);
        TraderMarketRsiFilterShortViewModel.SaveConfig(settings.Short.MarketRsi);
        TraderMarketStateFilterShortViewModel.SaveConfig(settings.Short.MarketState);
        TraderEntryFiltersShortViewModel.SaveConfig(settings.Short);
        TraderMarketExitShortViewModel.SaveConfig(settings.Short.MarketExit);
        TraderMarketExitShortViewModel.SaveEntryLimit(settings.Short.EntryLimit);
        TraderTrendIntervalFilterShortViewModel.SaveConfig(settings.Short.IntervalTrend);
    }
}

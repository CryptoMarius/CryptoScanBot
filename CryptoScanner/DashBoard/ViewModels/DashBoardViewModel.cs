using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.Helpers;

namespace CryptoScanner.DashBoard.ViewModels;

public partial class DashBoardViewModel : ObservableObject
{
    // Current values
    [ObservableProperty]
    private decimal? _marketCapTotal;

    [ObservableProperty]
    private decimal? _dollarIndex;

    [ObservableProperty]
    private decimal? _spx500;

    [ObservableProperty]
    private decimal? _bitcoinDominance;

    [ObservableProperty]
    private decimal? _fearAndGreedIndex;

    // Previous values (voor vergelijking)
    private decimal? _previousMarketCapTotal;
    private decimal? _previousDollarIndex;
    private decimal? _previousSpx500;
    private decimal? _previousBitcoinDominance;
    private decimal? _previousFearAndGreedIndex;

    // Colors (theme-aware via BrushHelper)
    [ObservableProperty]
    private IBrush _marketCapTotalColor = BrushHelper.PriceNeutral;

    [ObservableProperty]
    private IBrush _dollarIndexColor = BrushHelper.PriceNeutral;

    [ObservableProperty]
    private IBrush _spx500Color = BrushHelper.PriceNeutral;

    [ObservableProperty]
    private IBrush _bitcoinDominanceColor = BrushHelper.PriceNeutral;

    [ObservableProperty]
    private IBrush _fearAndGreedIndexColor = BrushHelper.PriceNeutral;

    private readonly ITradingViewService _tradingViewService;

    public DashBoardViewModel(ITradingViewService tradingViewService)
    {
        _tradingViewService = tradingViewService;

        // Subscribe to all events
        _tradingViewService.MarketCapTotalChanged += (s, v) => UpdateMarketCapTotal(v);
        _tradingViewService.DollarIndexChanged += (s, v) => UpdateDollarIndex(v);
        _tradingViewService.Spx500Changed += (s, v) => UpdateSpx500(v);
        _tradingViewService.BitcoinDominanceChanged += (s, v) => UpdateBitcoinDominance(v);
        _tradingViewService.FearAndGreedIndexChanged += (s, v) => UpdateFearAndGreedIndex(v);

        // Set initial values
        //MarketCapTotal = _tradingViewService.MarketCapTotalValue;
        //DollarIndex = _tradingViewService.DollarIndexValue;
        //Spx500 = _tradingViewService.Spx500Value;
        //BitcoinDominance = _tradingViewService.BitcoinDominanceValue;
        //FearAndGreedIndex = _tradingViewService.FearAndGreedIndexValue;

        System.Diagnostics.Debug.WriteLine("DashBoardViewModel constructor called");
    }

    private void UpdateMarketCapTotal(decimal newValue)
    {
        MarketCapTotalColor = GetColorForChange(_previousMarketCapTotal, newValue);
        _previousMarketCapTotal = MarketCapTotal;
        MarketCapTotal = newValue;
    }

    private void UpdateDollarIndex(decimal newValue)
    {
        DollarIndexColor = GetColorForChange(_previousDollarIndex, newValue);
        _previousDollarIndex = DollarIndex;
        DollarIndex = newValue;
    }

    private void UpdateSpx500(decimal newValue)
    {
        Spx500Color = GetColorForChange(_previousSpx500, newValue);
        _previousSpx500 = Spx500;
        Spx500 = newValue;
    }

    private void UpdateBitcoinDominance(decimal newValue)
    {
        BitcoinDominanceColor = GetColorForChange(_previousBitcoinDominance, newValue);
        _previousBitcoinDominance = BitcoinDominance;
        BitcoinDominance = newValue;
    }

    private void UpdateFearAndGreedIndex(decimal newValue)
    {
        FearAndGreedIndexColor = GetColorForChange(_previousFearAndGreedIndex, newValue);
        _previousFearAndGreedIndex = FearAndGreedIndex;
        FearAndGreedIndex = newValue;
    }

    private static IBrush GetColorForChange(decimal? previousValue, decimal newValue)
    {
        if (!previousValue.HasValue)
            return BrushHelper.PriceNeutral;

        if (newValue > previousValue.Value)
            return BrushHelper.PriceUp;

        if (newValue < previousValue.Value)
            return BrushHelper.PriceDown;

        return BrushHelper.PriceNeutral;
    }
}
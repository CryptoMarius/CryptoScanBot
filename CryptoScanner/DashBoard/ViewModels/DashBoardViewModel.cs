using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.DashBoard.Model;
using CryptoScanner.DashBoard.Services;

namespace CryptoScanner.DashBoard.ViewModels;

public partial class DashBoardViewModel : ObservableObject
{
    #region Market Indicators

    [ObservableProperty]
    private SymbolData _marketCapTotal = new();

    [ObservableProperty]
    private SymbolData _dollarIndex = new();

    [ObservableProperty]
    private SymbolData _spx500 = new();

    [ObservableProperty]
    private SymbolData _bitcoinDominance = new();

    [ObservableProperty]
    private SymbolData _fearAndGreedIndex = new();

    #endregion

    #region Crypto Symbols

    [ObservableProperty]
    private SymbolData _btcUsdt = new();

    [ObservableProperty]
    private SymbolData _ethUsdt = new();

    [ObservableProperty]
    private SymbolData _bnbUsdt = new();

    [ObservableProperty]
    private SymbolData _solUsdt = new();

    [ObservableProperty]
    private SymbolData _xrpUsdt = new();

    [ObservableProperty]
    private SymbolData _adaUsdt = new();

    #endregion

    private readonly ITradingViewService _tradingViewService;

    public DashBoardViewModel(ITradingViewService tradingViewService)
    {
        _tradingViewService = tradingViewService;

        // Subscribe to market indicator events
        _tradingViewService.MarketCapTotalChanged += (s, v) => MarketCapTotal.Update(v, null);
        _tradingViewService.DollarIndexChanged += (s, v) => DollarIndex.Update(v, null);
        _tradingViewService.Spx500Changed += (s, v) => Spx500.Update(v, null);
        _tradingViewService.BitcoinDominanceChanged += (s, v) => BitcoinDominance.Update(v, null);
        _tradingViewService.FearAndGreedIndexChanged += (s, v) => FearAndGreedIndex.Update(v, null);

        // Subscribe to crypto symbol events
        _tradingViewService.BtcUsdtChanged += (s, data) => BtcUsdt.Update(data.Price, data.Volume);
        _tradingViewService.EthUsdtChanged += (s, data) => EthUsdt.Update(data.Price, data.Volume);
        _tradingViewService.BnbUsdtChanged += (s, data) => BnbUsdt.Update(data.Price, data.Volume);
        _tradingViewService.SolUsdtChanged += (s, data) => SolUsdt.Update(data.Price, data.Volume);
        _tradingViewService.XrpUsdtChanged += (s, data) => XrpUsdt.Update(data.Price, data.Volume);
    
        System.Diagnostics.Debug.WriteLine("DashBoardViewModel constructor called");
    }
}

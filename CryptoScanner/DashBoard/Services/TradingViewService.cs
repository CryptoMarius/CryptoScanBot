using Avalonia.Threading;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.TradingView;

namespace CryptoScanner.DashBoard.Services;

public class TradingViewService : ITradingViewService
{
    private readonly List<object> _symbolInfos = new(); // object omdat FearAndGreedSymbolInfo ander type is

    // Events
    public event EventHandler<decimal>? MarketCapTotalChanged;
    public event EventHandler<decimal>? DollarIndexChanged;
    public event EventHandler<decimal>? Spx500Changed;
    public event EventHandler<decimal>? BitcoinDominanceChanged;
    public event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Current values
    public decimal? MarketCapTotal { get; private set; }
    public decimal? DollarIndex { get; private set; }
    public decimal? Spx500 { get; private set; }
    public decimal? BitcoinDominance { get; private set; }
    public decimal? FearAndGreedIndex { get; private set; }

    public void Start()
    {
        // Market Cap Total
        GlobalData.TradingViewMarketCapTotal.DataReceived += OnMarketCapTotalReceived;
        StartTradingViewSymbol("CRYPTOCAP:TOTAL3", "Market Cap total", "N2", GlobalData.TradingViewMarketCapTotal);

        // Dollar Index
        GlobalData.TradingViewDollarIndex.DataReceived += OnDollarIndexReceived;
        StartTradingViewSymbol("TVC:DXY", "US Dollar Index", "N2", GlobalData.TradingViewDollarIndex);

        // S&P 500
        GlobalData.TradingViewSpx500.DataReceived += OnSpx500Received;
        StartTradingViewSymbol("SP:SPX", "S&P 500", "N2", GlobalData.TradingViewSpx500);

        // Bitcoin Dominance
        GlobalData.TradingViewBitcoinDominance.DataReceived += OnBitcoinDominanceReceived;
        StartTradingViewSymbol("CRYPTOCAP:BTC.D", "BTC Dominance", "N2", GlobalData.TradingViewBitcoinDominance);

        // Fear and Greed Index
        GlobalData.FearAndGreedIndex.DataReceived += OnFearAndGreedIndexChangedReceived;
        StartFearAndGreedSymbol("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index", "N2", GlobalData.FearAndGreedIndex);
    }

    private void StartTradingViewSymbol(string symbol, string description, string format, SymbolValue ticker)
    {
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new TradingViewSymbolInfo();
            symbolInfo.StartAsync(symbol, description, format, ticker, 1000);
            _symbolInfos.Add(symbolInfo);
        });
    }

    private void StartFearAndGreedSymbol(string url, string description, string format, SymbolValue ticker)
    {
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new FearAndGreedSymbolInfo();
            symbolInfo.StartAsync(url, description, format, ticker, 1000);
            _symbolInfos.Add(symbolInfo);
        });
    }

    // Event handlers
    private void OnMarketCapTotalReceived(object? sender, SymbolValue data)
    {
        MarketCapTotal = data.Lp;
        Dispatcher.UIThread.Post(() => MarketCapTotalChanged?.Invoke(this, data.Lp));
    }

    private void OnDollarIndexReceived(object? sender, SymbolValue data)
    {
        DollarIndex = data.Lp;
        Dispatcher.UIThread.Post(() => DollarIndexChanged?.Invoke(this, data.Lp));
    }

    private void OnSpx500Received(object? sender, SymbolValue data)
    {
        Spx500 = data.Lp;
        Dispatcher.UIThread.Post(() => Spx500Changed?.Invoke(this, data.Lp));
    }

    private void OnBitcoinDominanceReceived(object? sender, SymbolValue data)
    {
        BitcoinDominance = data.Lp;
        Dispatcher.UIThread.Post(() => BitcoinDominanceChanged?.Invoke(this, data.Lp));
    }

    private void OnFearAndGreedIndexChangedReceived(object? sender, SymbolValue data)
    {
        FearAndGreedIndex = data.Lp;
        Dispatcher.UIThread.Post(() => FearAndGreedIndexChanged?.Invoke(this, data.Lp));
    }

    public void Stop()
    {
        GlobalData.TradingViewMarketCapTotal.DataReceived -= OnMarketCapTotalReceived;
        GlobalData.TradingViewDollarIndex.DataReceived -= OnDollarIndexReceived;
        GlobalData.TradingViewSpx500.DataReceived -= OnSpx500Received;
        GlobalData.TradingViewBitcoinDominance.DataReceived -= OnBitcoinDominanceReceived;
        GlobalData.FearAndGreedIndex.DataReceived -= OnFearAndGreedIndexChangedReceived;

        _symbolInfos.Clear();
    }
}
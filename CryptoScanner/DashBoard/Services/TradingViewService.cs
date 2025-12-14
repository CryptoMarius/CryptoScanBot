using Avalonia.Threading;

using CryptoScanner.Core.TradingView;

namespace CryptoScanner.DashBoard.Services;

public class TradingViewService : ITradingViewService
{
    // Type=object because FearAndGreedSymbolInfo is different type
    private readonly List<object> _symbolInfos = [];

    // Events
    public event EventHandler<decimal>? MarketCapTotalChanged;
    public event EventHandler<decimal>? DollarIndexChanged;
    public event EventHandler<decimal>? Spx500Changed;
    public event EventHandler<decimal>? BitcoinDominanceChanged;
    public event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Current values
    public decimal? MarketCapTotalValue { get; private set; }
    public decimal? DollarIndexValue { get; private set; }
    public decimal? Spx500Value { get; private set; }
    public decimal? BitcoinDominanceValue { get; private set; }
    public decimal? FearAndGreedIndexValue { get; private set; }

    public SymbolValue FearAndGreedIndex { get; set; } = new();
    public SymbolValue TradingViewDollarIndex { get; set; } = new();
    public SymbolValue TradingViewSpx500 { get; set; } = new();
    public SymbolValue TradingViewBitcoinDominance { get; set; } = new();
    public SymbolValue TradingViewMarketCapTotal { get; set; } = new();

    public void Start()
    {
        // Market Cap Total
        TradingViewMarketCapTotal.DataReceived += OnMarketCapTotalReceived;
        StartTradingViewSymbol("CRYPTOCAP:TOTAL3", "Market Cap total", "N2", TradingViewMarketCapTotal);

        // Dollar Index
        TradingViewDollarIndex.DataReceived += OnDollarIndexReceived;
        StartTradingViewSymbol("TVC:DXY", "US Dollar Index", "N2", TradingViewDollarIndex);

        // S&P 500
        TradingViewSpx500.DataReceived += OnSpx500Received;
        StartTradingViewSymbol("SP:SPX", "S&P 500", "N2", TradingViewSpx500);

        // Bitcoin Dominance
        TradingViewBitcoinDominance.DataReceived += OnBitcoinDominanceReceived;
        StartTradingViewSymbol("CRYPTOCAP:BTC.D", "BTC Dominance", "N2", TradingViewBitcoinDominance);

        // Fear and Greed Index
        FearAndGreedIndex.DataReceived += OnFearAndGreedIndexChangedReceived;
        StartFearAndGreedSymbol("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index", "N2", FearAndGreedIndex);
    }

    private void StartTradingViewSymbol(string symbol, string description, string format, SymbolValue ticker)
    {
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new TradingViewSymbolInfo();
            symbolInfo.StartAsync(symbol, description, format, ticker);
            _symbolInfos.Add(symbolInfo);
        });
    }

    private void StartFearAndGreedSymbol(string url, string description, string format, SymbolValue ticker)
    {
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new FearAndGreedSymbolInfo();
            symbolInfo.StartAsync(url, description, format, ticker);
            _symbolInfos.Add(symbolInfo);
        });
    }

    // Event handlers
    private void OnMarketCapTotalReceived(object? sender, SymbolValue data)
    {
        MarketCapTotalValue = data.Lp;
        Dispatcher.UIThread.Post(() => MarketCapTotalChanged?.Invoke(this, data.Lp));
    }

    private void OnDollarIndexReceived(object? sender, SymbolValue data)
    {
        DollarIndexValue = data.Lp;
        Dispatcher.UIThread.Post(() => DollarIndexChanged?.Invoke(this, data.Lp));
    }

    private void OnSpx500Received(object? sender, SymbolValue data)
    {
        Spx500Value = data.Lp;
        Dispatcher.UIThread.Post(() => Spx500Changed?.Invoke(this, data.Lp));
    }

    private void OnBitcoinDominanceReceived(object? sender, SymbolValue data)
    {
        BitcoinDominanceValue = data.Lp;
        Dispatcher.UIThread.Post(() => BitcoinDominanceChanged?.Invoke(this, data.Lp));
    }

    private void OnFearAndGreedIndexChangedReceived(object? sender, SymbolValue data)
    {
        FearAndGreedIndexValue = data.Lp;
        Dispatcher.UIThread.Post(() => FearAndGreedIndexChanged?.Invoke(this, data.Lp));
    }

    public void Stop()
    {
        TradingViewMarketCapTotal.DataReceived -= OnMarketCapTotalReceived;
        TradingViewDollarIndex.DataReceived -= OnDollarIndexReceived;
        TradingViewSpx500.DataReceived -= OnSpx500Received;
        TradingViewBitcoinDominance.DataReceived -= OnBitcoinDominanceReceived;
        FearAndGreedIndex.DataReceived -= OnFearAndGreedIndexChangedReceived;

        //foreach (var _symbolInfo in _symbolInfos)
        //{
        //    _symbolInfo.();
        //}
        _symbolInfos.Clear();
    }
}
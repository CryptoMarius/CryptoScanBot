using Avalonia.Threading;

using CryptoScanner.DashBoard.TradingView;

namespace CryptoScanner.DashBoard.Services;

public class TradingViewService : ITradingViewService, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Lock _lock = new();
    private bool _isRunning;
    private bool _disposed;

    // Events
    public event EventHandler<decimal>? MarketCapTotalChanged;
    public event EventHandler<decimal>? DollarIndexChanged;
    public event EventHandler<decimal>? Spx500Changed;
    public event EventHandler<decimal>? BitcoinDominanceChanged;
    public event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Events - crypto symbols
    public event EventHandler<decimal>? BtcUsdtChanged;
    public event EventHandler<decimal>? EthUsdtChanged;
    public event EventHandler<decimal>? BnbUsdtChanged;
    public event EventHandler<decimal>? SolUsdtChanged;
    public event EventHandler<decimal>? XrpUsdtChanged;

    // Current values - market indicators
    public decimal? MarketCapTotalValue { get; private set; }
    public decimal? DollarIndexValue { get; private set; }
    public decimal? Spx500Value { get; private set; }
    public decimal? BitcoinDominanceValue { get; private set; }
    public decimal? FearAndGreedIndexValue { get; private set; }

    // Current values - crypto symbols
    public decimal? BtcUsdtPriceValue { get; private set; }
    public decimal? EthUsdtPriceValue { get; private set; }
    public decimal? BnbUsdtPriceValue { get; private set; }
    public decimal? SolUsdtPriceValue { get; private set; }
    public decimal? XrpUsdtPriceValue { get; private set; }

    // Current values - crypto symbols
    public decimal? BtcUsdtVolumeValue { get; private set; }
    public decimal? EthUsdtVolumeValue { get; private set; }
    public decimal? BnbUsdtVolumeValue { get; private set; }
    public decimal? SolUsdtVolumeValue { get; private set; }
    public decimal? XrpUsdtVolumeValue { get; private set; }


    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                System.Diagnostics.Debug.WriteLine("TradingViewService: Already running");
                return;
            }

            System.Diagnostics.Debug.WriteLine("TradingViewService: Starting all symbols...");

            // Create single CancellationTokenSource for ALL tasks
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Register market indicators
            RegisterTradingViewSymbol("CRYPTOCAP:TOTAL3", "Market Cap total",
                v => { MarketCapTotalValue = v; MarketCapTotalChanged?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("TVC:DXY", "US Dollar Index",
                v => { DollarIndexValue = v; DollarIndexChanged?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("SP:SPX", "S&P 500",
                v => { Spx500Value = v; Spx500Changed?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("CRYPTOCAP:BTC.D", "BTC Dominance", 
                v => { BitcoinDominanceValue = v; BitcoinDominanceChanged?.Invoke(this, v); }, token);

            RegisterFearAndGreedSymbol("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index",
                v => { FearAndGreedIndexValue = v; FearAndGreedIndexChanged?.Invoke(this, v); }, token);

            // Register crypto symbols
            RegisterTradingViewSymbol("BINANCE:BTCUSDT", "Bitcoin",
                v => { BtcUsdtPriceValue = v; BtcUsdtVolumeValue = 1000*v; BtcUsdtChanged?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("BINANCE:ETHUSDT", "Ethereum",
                v => { EthUsdtPriceValue = v; EthUsdtChanged?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("BINANCE:BNBUSDT", "BNB",
                v => { BnbUsdtPriceValue = v; BnbUsdtChanged?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("BINANCE:SOLUSDT", "Solana",
                v => { SolUsdtPriceValue = v; SolUsdtChanged?.Invoke(this, v); }, token);

            RegisterTradingViewSymbol("BINANCE:XRPUSDT", "XRP",
                v => { XrpUsdtPriceValue = v; XrpUsdtVolumeValue = v; XrpUsdtChanged?.Invoke(this, v); }, token);

            _isRunning = true;
            System.Diagnostics.Debug.WriteLine("TradingViewService: Started 11 symbols (5 market indicators + 6 cryptos)");
        }
    }

    /// <summary>
    /// Registers a TradingView symbol with automatic event wiring
    /// </summary>
    private static void RegisterTradingViewSymbol(string symbol, string description, 
        Action<decimal> onValueReceived, CancellationToken token)
    {
        TickerData tickerData = new();

        // Subscribe to DataReceived event
        tickerData.DataReceived += (sender, data) =>
        {
            Dispatcher.UIThread.Post(() => onValueReceived(data.Lp));
        };

        // Start the symbol polling
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new TradingViewSymbolInfo();
            symbolInfo.StartAsync(symbol, description, tickerData, cancellationToken: token);
        }, token);
    }

    /// <summary>
    /// Registers Fear & Greed Index with automatic event wiring
    /// </summary>
    private static void RegisterFearAndGreedSymbol(string url, string description, 
        Action<decimal> onValueReceived, CancellationToken token)
    {
        TickerData tickerData = new();

        // Subscribe to DataReceived event
        tickerData.DataReceived += (sender, data) =>
        {
            Dispatcher.UIThread.Post(() => onValueReceived(data.Lp));
        };
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new FearAndGreedSymbolInfo();
            symbolInfo.StartAsync(url, description, tickerData, cancellationToken: token);
        }, token);
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning || _cancellationTokenSource == null)
            {
                System.Diagnostics.Debug.WriteLine("TradingViewService: Not running");
                return;
            }

            System.Diagnostics.Debug.WriteLine("TradingViewService: Stopping all symbols...");
            _cancellationTokenSource.Cancel();

            ////// Unsubscribe market indicators
            //TradingViewMarketCapTotal.ResetEvents();
            //TradingViewDollarIndex.ResetEvents();
            //TradingViewSpx500.ResetEvents();
            //TradingViewBitcoinDominance.ResetEvents();
            //FearAndGreedIndex.ResetEvents();

            //// Unsubscribe crypto symbols
            ////BtcUsdt.ResetEvents();
            //EthUsdt.ResetEvents();
            //BnbUsdt.ResetEvents();
            //SolUsdt.ResetEvents();
            //XrpUsdt.ResetEvents();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _isRunning = false;
            System.Diagnostics.Debug.WriteLine("TradingViewService: Stopped");
        }
    }

    #region IDisposable Pattern

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                Stop();
            }

            // Dispose unmanaged resources (if any)
            // None in this class

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

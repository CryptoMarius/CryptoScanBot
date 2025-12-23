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
    public event EventHandler<(decimal Price, decimal Volume)>? BtcUsdtChanged;
    public event EventHandler<(decimal Price, decimal Volume)>? EthUsdtChanged;
    public event EventHandler<(decimal Price, decimal Volume)>? BnbUsdtChanged;
    public event EventHandler<(decimal Price, decimal Volume)>? SolUsdtChanged;
    public event EventHandler<(decimal Price, decimal Volume)>? XrpUsdtChanged;

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
        return;
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
                (price, volume) => { MarketCapTotalValue = price; MarketCapTotalChanged?.Invoke(this, price); }, token);

            RegisterTradingViewSymbol("TVC:DXY", "US Dollar Index",
                (price, volume) => { DollarIndexValue = price; DollarIndexChanged?.Invoke(this, price); }, token);

            RegisterTradingViewSymbol("SP:SPX", "S&P 500",
                (price, volume) => { Spx500Value = price; Spx500Changed?.Invoke(this, price); }, token);

            RegisterTradingViewSymbol("CRYPTOCAP:BTC.D", "BTC Dominance",
                (price, volume) => { BitcoinDominanceValue = price; BitcoinDominanceChanged?.Invoke(this, price); }, token);

            RegisterFearAndGreedSymbol("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index",
                (price, volume) => { FearAndGreedIndexValue = price; FearAndGreedIndexChanged?.Invoke(this, price); }, token);

            // TODO: Use the right exchangecode
            // Register crypto symbols
            RegisterTradingViewSymbol("BINANCE:BTCUSDT", "Bitcoin",
                (price, volume) => { BtcUsdtPriceValue = price; BtcUsdtVolumeValue = volume; BtcUsdtChanged?.Invoke(this, (price, volume)); }, token);

            RegisterTradingViewSymbol("BINANCE:ETHUSDT", "Ethereum",
                (price, volume) => { EthUsdtPriceValue = price; EthUsdtChanged?.Invoke(this, (price, volume)); }, token);

            RegisterTradingViewSymbol("BINANCE:BNBUSDT", "BNB",
                (price, volume) => { BnbUsdtPriceValue = price; BnbUsdtChanged?.Invoke(this, (price, volume)); }, token);

            RegisterTradingViewSymbol("BINANCE:SOLUSDT", "Solana",
                (price, volume) => { SolUsdtPriceValue = price; SolUsdtChanged?.Invoke(this, (price, volume)); }, token);

            RegisterTradingViewSymbol("BINANCE:XRPUSDT", "XRP",
                (price, volume) => { XrpUsdtPriceValue = price; XrpUsdtVolumeValue = price; XrpUsdtChanged?.Invoke(this, (price, volume)); }, token);

            _isRunning = true;
            System.Diagnostics.Debug.WriteLine("TradingViewService: Started");
        }
    }

    /// <summary>
    /// Registers a TradingView symbol with automatic event wiring
    /// </summary>
    private static void RegisterTradingViewSymbol(string symbol, string displayName,
        Action<decimal, decimal> onValueReceived, CancellationToken token)
    {
        // Start the symbol polling
        Task.Factory.StartNew(() =>
        {
            var symbolInfo = new TradingViewSymbolExtractor();
            symbolInfo.StartAsync(symbol, displayName, onValueReceived, cancellationToken: token);
        }, token);
    }

    /// <summary>
    /// Registers Fear & Greed Index with automatic event wiring
    /// </summary>
    private static void RegisterFearAndGreedSymbol(string url, string displayName, 
        Action<decimal, decimal> onValueReceived, CancellationToken token)
    {
        Task.Factory.StartNew(() => { FearAndGreedIndexExtractor.StartAsync(url, 
            displayName, onValueReceived, cancellationToken: token);}, token);
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

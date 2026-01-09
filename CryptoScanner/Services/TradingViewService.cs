using CryptoScanner.TradingView;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Services;

public class TradingViewService : ITradingViewService, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _lock = new();
    private bool _isRunning;
    private bool _disposed;

    public IEnumerable<DashboardSymbolViewModel> TvSymbols { get; set; } = [];

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
            foreach (var x in TvSymbols)
            {
                switch (x.Type)
                {
                    case IndicatorType.TradingView:
                        RegisterTradingViewSymbol(x.Symbol, x.Name, (price, volume) => { x.Price = price; x.Volume = volume; x.Update(price, volume); }, token);
                        break;
                    case IndicatorType.FearAndGreed:
                        RegisterFearAndGreedSymbol(x.Symbol, x.Name, (price, volume) => { x.Price = price; x.Update(price, null); }, token);
                        break;
                }
            }

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
        Task.Factory.StartNew(() =>
        {
            FearAndGreedIndexExtractor.StartAsync(url,
            displayName, onValueReceived, cancellationToken: token);
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
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _isRunning = false;
            System.Diagnostics.Debug.WriteLine("TradingViewService: Stopped");
        }
    }

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

}

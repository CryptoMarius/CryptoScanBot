using Alpaca.Markets;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

/// <summary>
/// Real-time minute bar subscription via Alpaca data streaming.
/// Alpaca's streaming SDK is independent of CryptoExchange.Net, so we override
/// StartAsync and StopAsync entirely instead of using the Subscribe() pattern.
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private IAlpacaDataStreamingClient? _streamingClient;
    private readonly List<IAlpacaDataSubscription<IBar>> _barSubscriptions = [];


    private async Task ProcessBarAsync(IBar bar, string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return;

        // ExchangeName for Alpaca symbols is the plain ticker (e.g. "AAPL")
        if (SymbolByExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();

            var candle = await CandleTools.Process1mCandleAsync(symbol, bar.TimeUtc,
                bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
        }
    }


    /// <summary>
    /// Not used: Alpaca streaming does not follow the CryptoExchange.Net subscription pattern.
    /// StartAsync is overridden instead.
    /// </summary>
    public override Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        return Task.FromResult<WebSocketResult<UpdateSubscription>?>(null);
    }


    public override async Task StartAsync()
    {
        if (_streamingClient != null)
        {
            ScannerLog.Logger.Trace($"Alpaca kline ticker for group {GroupName} already started");
            return;
        }

        NeedsRestart = false;
        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"Alpaca kline ticker for group {GroupName} starting ({SymbolList.Count} symbols)");

        try
        {
            if (GlobalData.TradingApi.Key == "")
                throw new InvalidOperationException("Alpaca requires an API key for streaming.");

            _streamingClient = Environments.Paper.GetAlpacaDataStreamingClient(
                new SecretKey(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret));

            var authStatus = await _streamingClient.ConnectAndAuthenticateAsync(ExchangeBase.CancellationToken);
            if (authStatus != AuthStatus.Authorized)
                throw new Exception($"Alpaca streaming authentication failed: {authStatus}");

            // Subscribe to minute bars for each symbol in this group
            foreach (var symbol in SymbolList)
            {
                var subscription = _streamingClient.GetMinuteBarSubscription(symbol.ExchangeName);
                subscription.Received += bar => Task.Run(async () => await ProcessBarAsync(bar, symbol.ExchangeName));
                await _streamingClient.SubscribeAsync(subscription);
                _barSubscriptions.Add(subscription);
            }

            ScannerLog.Logger.Trace($"Alpaca kline ticker for group {GroupName} started");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {GroupName} started ({SymbolList.Count} symbols)");
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {GroupName} startup error: {ex.Message}");

            _streamingClient?.Dispose();
            _streamingClient = null;
            _barSubscriptions.Clear();

            ErrorDuringStartup = true;
            NeedsRestart = true;
        }
    }


    public override async Task StopAsync()
    {
        if (_streamingClient == null)
        {
            ScannerLog.Logger.Trace($"Alpaca kline ticker for group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"Alpaca kline ticker for group {GroupName} stopping");

        foreach (var subscription in _barSubscriptions)
        {
            try { await _streamingClient.UnsubscribeAsync(subscription); }
            catch { /* Best effort unsubscribe */ }
        }
        _barSubscriptions.Clear();

        _streamingClient.Dispose();
        _streamingClient = null;

        ScannerLog.Logger.Trace($"Alpaca kline ticker for group {GroupName} stopped");
    }
}

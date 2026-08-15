using Alpaca.Markets;
using Alpaca.Markets.Extensions;

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
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    // The one data stream this application is allowed to have. Alpaca counts connections per account
    // and not per subscription: a second connection is answered with "connection limit exceeded", and
    // the free plan allows exactly one. So every kline group borrows the same client, and the last one
    // to stop takes it down again.
    private static IAlpacaDataStreamingClient? _sharedClient;
    private static int _sharedClientUsers;
    private static readonly SemaphoreSlim SharedClientLock = new(1);

    private IAlpacaDataStreamingClient? _streamingClient;
    private readonly List<IAlpacaDataSubscription<IBar>> _barSubscriptions = [];


    /// <summary>
    /// A closed stock market delivers nothing for seventeen hours a day and the whole weekend, and
    /// that is not a defect. Without this the silence check would take a perfectly healthy stream down
    /// and build it up again every four minutes of that.
    /// </summary>
    public override bool IsExpectingData => MarketClock.IsOpen;


    private static async Task<IAlpacaDataStreamingClient> AcquireClientAsync()
    {
        await SharedClientLock.WaitAsync();
        try
        {
            if (_sharedClient == null)
            {
                if (GlobalData.TradingApi.Key == "")
                    throw new InvalidOperationException("Alpaca requires an API key for streaming.");

                var configuration = Environments.Paper.GetAlpacaDataStreamingClientConfiguration(
                    new SecretKey(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret));
                // The SDK derives the endpoint from the environment instead of from the plan, and the
                // one it picks is not always the feed we are allowed to read (see Api.DataStreamEndpoint)
                configuration.ApiEndpoint = Api.DataStreamEndpoint;

                var client = configuration.GetClient();
                var authStatus = await client.ConnectAndAuthenticateAsync(ExchangeBase.CancellationToken);
                if (authStatus != AuthStatus.Authorized)
                {
                    client.Dispose();
                    throw new Exception($"Alpaca streaming authentication failed: {authStatus}");
                }

                _sharedClient = client;
                _sharedClientUsers = 0;
            }

            _sharedClientUsers++;
            return _sharedClient;
        }
        finally
        {
            SharedClientLock.Release();
        }
    }


    private static async Task ReleaseClientAsync()
    {
        await SharedClientLock.WaitAsync();
        try
        {
            if (_sharedClient == null)
                return;

            _sharedClientUsers--;
            if (_sharedClientUsers > 0)
                return;

            try { await _sharedClient.DisconnectAsync(ExchangeBase.CancellationToken); }
            catch { /* Best effort disconnect */ }

            _sharedClient.Dispose();
            _sharedClient = null;
            _sharedClientUsers = 0;
        }
        finally
        {
            SharedClientLock.Release();
        }
    }


    private void OnBarReceived(IBar bar)
    {
        _ = ProcessBarAsync(bar);
    }


    private async Task ProcessBarAsync(IBar bar)
    {
        // The handler runs on a socket thread of the SDK: an exception that escapes here disappears
        // into an unobserved task and the ticker looks healthy while it delivers nothing.
        try
        {
            // ExchangeName for Alpaca symbols is the plain ticker (e.g. "AAPL")
            if (SymbolByExchangeName.TryGetValue(bar.Symbol, out CryptoSymbol? symbol))
            {
                IncrementTickerCount();

                var candle = await CandleTools.Process1mCandleAsync(symbol, bar.TimeUtc,
                    bar.Open, bar.High, bar.Low, bar.Close, Candle.GetQuoteVolume(bar));
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {Name} error {error.Message}");
        }
    }


    private void OnSocketClosed()
    {
        // The handlers are removed before we close the socket ourselves, so reaching this means the
        // connection was lost. Restarting is the job of SubscriptionManager.CheckSubscriptions.
        NeedsRestart = true;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {Name} stream closed");
    }


    private void OnStreamError(Exception error)
    {
        NeedsRestart = true;
        GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {Name} stream error {error.Message}");
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
            ScannerLog.Logger.Trace($"Alpaca kline subscription {Name} already started");
            return;
        }

        NeedsRestart = false;
        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"Alpaca kline subscription {Name} starting ({SymbolList.Count} symbols)");

        // Give the subscription a fresh starting point, otherwise the silence check would fire
        // immediately on a ticker that has not had the chance to receive anything yet. The base
        // StartAsync does the same; this override replaces it entirely.
        MarkActivity();

        try
        {
            _streamingClient = await AcquireClientAsync();
            _streamingClient.SocketClosed += OnSocketClosed;
            _streamingClient.OnError += OnStreamError;

            // Subscribe to the minute bars of every symbol in this group, in one go: a round trip per
            // symbol is a round trip too many, and the exchange answers the whole batch at once.
            List<IAlpacaDataSubscription> subscriptions = [];
            foreach (var symbol in SymbolList)
            {
                var subscription = _streamingClient.GetMinuteBarSubscription(symbol.ExchangeName);
                subscription.Received += OnBarReceived;
                _barSubscriptions.Add(subscription);
                subscriptions.Add(subscription);
            }
            if (subscriptions.Count > 0)
                await _streamingClient.SubscribeAsync(subscriptions, ExchangeBase.CancellationToken);

            ScannerLog.Logger.Trace($"Alpaca kline subscription {Name} started");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {Name} started ({SymbolList.Count} symbols)");
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "");
            GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {Name} startup error: {ex.Message}");

            await StopInternalAsync();

            ErrorDuringStartup = true;
            NeedsRestart = true;
        }
    }


    public override async Task StopAsync()
    {
        if (_streamingClient == null)
        {
            ScannerLog.Logger.Trace($"Alpaca kline subscription {Name} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"Alpaca kline subscription {Name} stopping");
        await StopInternalAsync();
        ScannerLog.Logger.Trace($"Alpaca kline subscription {Name} stopped");
    }


    /// <summary>
    /// Give up everything this group is holding: its own subscriptions and its share of the client
    /// the other groups are using as well.
    /// </summary>
    private async Task StopInternalAsync()
    {
        if (_streamingClient == null)
        {
            _barSubscriptions.Clear();
            return;
        }

        // Off first, so closing the socket ourselves is not reported as a lost connection
        _streamingClient.SocketClosed -= OnSocketClosed;
        _streamingClient.OnError -= OnStreamError;

        if (_barSubscriptions.Count > 0)
        {
            foreach (var subscription in _barSubscriptions)
                subscription.Received -= OnBarReceived;

            try { await _streamingClient.UnsubscribeAsync(_barSubscriptions, ExchangeBase.CancellationToken); }
            catch { /* Best effort unsubscribe */ }

            _barSubscriptions.Clear();
        }

        _streamingClient = null;
        await ReleaseClientAsync();
    }
}

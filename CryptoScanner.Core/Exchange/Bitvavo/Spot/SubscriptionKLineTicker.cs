using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

/// <summary>
/// Real-time 1-minute candle subscription via the Bitvavo WebSocket API.
/// WebSocket endpoint: wss://ws.bitvavo.com/v2/
///
/// Subscribe message:
///   {"action":"subscribe","channels":[{"name":"candles","interval":["1m"],"markets":["BTC-EUR","ETH-EUR"]}]}
///
/// Received candle message:
///   {"event":"candle","market":"BTC-EUR","interval":"1m","candle":[[timestamp_ms,"open","high","low","close","volume"]]}
///
/// No authentication required for public market data.
/// The Bitvavo SDK is not available as a JKorf/CryptoExchange.Net package, so we use
/// ClientWebSocket directly and override StartAsync/StopAsync.
///
/// Candle processing uses a cache + timer pattern (like Kucoin/Kraken):
/// WebSocket updates are cached per market; the timer processes completed candles
/// once per minute (~6 seconds after the minute boundary).
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _localCts;
    private System.Timers.Timer? _timerKline;
    private const string WsUrl = "wss://ws.bitvavo.com/v2/";

    // Cache: latest in-progress 1m candle per market, updated on every WebSocket push.
    // Protected by _cacheSemaphore; processed and cleared by the minute timer.
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
    private readonly Dictionary<string, CachedCandle> _candleCache = [];

    private record struct CachedCandle(DateTime OpenTime, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);


    static double GetNextTimer()
    {
        DateTime now = DateTime.Now;
        return 6000 + ((60 - now.Second) * 1000 - now.Millisecond);
    }


    private async Task ProcessMessageAsync(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("event", out var eventProp) || eventProp.GetString() != "candle")
                return;

            if (!root.TryGetProperty("market", out var marketProp))
                return;

            string? market = marketProp.GetString();
            if (string.IsNullOrEmpty(market))
                return;

            // "candle": [[timestamp_ms, "open", "high", "low", "close", "volume"]]
            var candleArr = root.GetProperty("candle")[0].EnumerateArray().ToArray();
            if (candleArr.Length < 6)
                return;

            DateTime openTime = DateTimeOffset.FromUnixTimeMilliseconds(candleArr[0].GetInt64()).UtcDateTime;
            decimal open   = decimal.Parse(candleArr[1].GetString()!, CultureInfo.InvariantCulture);
            decimal high   = decimal.Parse(candleArr[2].GetString()!, CultureInfo.InvariantCulture);
            decimal low    = decimal.Parse(candleArr[3].GetString()!, CultureInfo.InvariantCulture);
            decimal close  = decimal.Parse(candleArr[4].GetString()!, CultureInfo.InvariantCulture);
            decimal volume = decimal.Parse(candleArr[5].GetString()!, CultureInfo.InvariantCulture);

            // Cache the latest state of this candle; the timer will process it after the minute closes
            await _cacheSemaphore.WaitAsync();
            try
            {
                _candleCache[market] = new CachedCandle(openTime, open, high, low, close, volume);
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "Bitvavo WebSocket message processing error");
        }
    }


    private async Task ProcessCachedCandlesAsync()
    {
        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            return;

        // Only process candles whose minute has fully passed
        DateTime currentMinuteStart = DateTime.UtcNow;
        currentMinuteStart = currentMinuteStart.AddSeconds(-currentMinuteStart.Second).AddMilliseconds(-currentMinuteStart.Millisecond);

        List<(string Market, CachedCandle Candle)> toProcess = [];

        await _cacheSemaphore.WaitAsync();
        try
        {
            foreach (var kv in _candleCache)
            {
                if (kv.Value.OpenTime < currentMinuteStart)
                    toProcess.Add((kv.Key, kv.Value));
            }
            foreach (var (market, _) in toProcess)
                _candleCache.Remove(market);
        }
        finally
        {
            _cacheSemaphore.Release();
        }

        foreach (var (market, cached) in toProcess)
        {
            try
            {
                if (exchange.SymbolListExchangeName.TryGetValue(market, out CryptoSymbol? symbol))
                {
                    Interlocked.Increment(ref TickerCount);
                    if (TickerCount > 999999999)
                        Interlocked.Exchange(ref TickerCount, 0);

                    if (!GlobalData.BackTest)
                        symbol.LastPrice = cached.Close;

                    var candle = await CandleTools.Process1mCandleAsync(symbol, cached.OpenTime,
                        cached.Open, cached.High, cached.Low, cached.Close, cached.Volume);
                    GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candle);
                }
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, $"Bitvavo timer candle processing error for {market}");
            }
        }
    }


    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[65536];
        var messageBuilder = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} WebSocket closed by server for group {GroupName}");
                    NeedsRestart = true;
                    break;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    string msg = messageBuilder.ToString();
                    messageBuilder.Clear();
                    await ProcessMessageAsync(msg);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ScannerLog.Logger.Error(ex, "");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} WebSocket receive error group {GroupName}: {ex.Message}");
            NeedsRestart = true;
        }
    }


    /// <summary>
    /// Not used: Bitvavo does not use the CryptoExchange.Net subscription pattern.
    /// StartAsync is overridden instead.
    /// </summary>
    public override Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        return Task.FromResult<CallResult<UpdateSubscription>?>(null);
    }


    public override async Task StartAsync()
    {
        if (_ws != null)
        {
            ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} already started");
            return;
        }

        NeedsRestart = false;
        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} starting ({SymbolList.Count} symbols)");

        try
        {
            _localCts = CancellationTokenSource.CreateLinkedTokenSource(ExchangeBase.CancellationToken);
            _ws = new ClientWebSocket();

            await _ws.ConnectAsync(new Uri(WsUrl), _localCts.Token);

            // Subscribe to 1m candles for all symbols in this group
            var markets = SymbolList.Select(s => s.ExchangeName).ToList();
            var subscribeMsg = JsonSerializer.Serialize(new
            {
                action = "subscribe",
                channels = new[] { new { name = "candles", interval = new[] { "1m" }, markets } }
            });

            byte[] msgBytes = Encoding.UTF8.GetBytes(subscribeMsg);
            await _ws.SendAsync(new ArraySegment<byte>(msgBytes), WebSocketMessageType.Text, true, _localCts.Token);

            // Start background receive loop
            _ = Task.Run(() => ReceiveLoopAsync(_localCts.Token), _localCts.Token);

            // Timer fires ~6 seconds after each minute boundary to process completed candles
            _timerKline = new System.Timers.Timer { AutoReset = false };
            _timerKline.Elapsed += async (sender, e) =>
            {
                await ProcessCachedCandlesAsync();
                if (sender is System.Timers.Timer t)
                {
                    t.Interval = GetNextTimer();
                    t.Start();
                }
            };
            _timerKline.Interval = GetNextTimer();
            _timerKline.Start();

            ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} started");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {GroupName} started ({SymbolList.Count} symbols)");
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {GroupName} startup error: {ex.Message}");

            _timerKline?.Stop();
            _timerKline?.Dispose();
            _timerKline = null;

            _ws?.Dispose();
            _ws = null;
            _localCts?.Dispose();
            _localCts = null;

            ErrorDuringStartup = true;
            NeedsRestart = true;
        }
    }


    public override async Task StopAsync()
    {
        if (_ws == null)
        {
            ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} stopping");

        _timerKline?.Stop();
        _timerKline?.Dispose();
        _timerKline = null;

        _localCts?.Cancel();

        try
        {
            if (_ws.State == WebSocketState.Open)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None);
        }
        catch { /* Best effort */ }

        _ws.Dispose();
        _ws = null;
        _localCts?.Dispose();
        _localCts = null;

        ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} stopped");
    }
}

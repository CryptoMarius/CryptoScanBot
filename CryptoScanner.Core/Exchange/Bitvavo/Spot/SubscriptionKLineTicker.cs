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
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _localCts;
    private const string WsUrl = "wss://ws.bitvavo.com/v2/";


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

            if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                if (exchange.SymbolListExchangeName.TryGetValue(market, out CryptoSymbol? symbol))
                {
                    Interlocked.Increment(ref TickerCount);
                    var candle = await CandleTools.Process1mCandleAsync(symbol, openTime, open, high, low, close, volume);
                    GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
                }
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "Bitvavo WebSocket message processing error");
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

            ScannerLog.Logger.Trace($"Bitvavo kline ticker group {GroupName} started");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {GroupName} started ({SymbolList.Count} symbols)");
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {GroupName} startup error: {ex.Message}");

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

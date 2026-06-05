using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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
/// Candle processing uses a cache + timer pattern (same as Mexc):
/// WebSocket updates are cached per market; the timer processes completed candles
/// once per minute (~6 seconds after the minute boundary).
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _localCts;

    private const string WsUrl = "wss://ws.bitvavo.com/v2/";


    static double GetNextTimer()
    {
        DateTime now = DateTime.Now;
        return 6000 + ((60 - now.Second) * 1000 - now.Millisecond);
    }


    private async Task ProcessMessageAsync(string json, SemaphoreSlim cacheListSemaphore, SortedList<string, CryptoCandleList> symbolCandleCache)
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

            DateTime openTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(candleArr[0].GetInt64()).UtcDateTime;
            decimal open = decimal.Parse(candleArr[1].GetString()!, CultureInfo.InvariantCulture);
            decimal high = decimal.Parse(candleArr[2].GetString()!, CultureInfo.InvariantCulture);
            decimal low = decimal.Parse(candleArr[3].GetString()!, CultureInfo.InvariantCulture);
            decimal close = decimal.Parse(candleArr[4].GetString()!, CultureInfo.InvariantCulture);
            decimal volume = decimal.Parse(candleArr[5].GetString()!, CultureInfo.InvariantCulture);

            if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                if (exchange.SymbolListExchangeName.TryGetValue(market, out CryptoSymbol? symbol))
                {
                    await cacheListSemaphore.WaitAsync();
                    try
                    {
                        // Add or update the local cache
                        bool addCandle = false;
                        CandleTime candleOpenUnix = CandleTime.AlignFromDateTime(openTimeUtc, 1);
                        CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                        if (!candleCache.TryGetValue(candleOpenUnix, out CryptoCandle candle))
                        {
                            addCandle = true;
                            candle = new() { OpenTime = candleOpenUnix };
                        }
                        candle.TickDecimals = symbol.PriceDecimals;
                        candle.Open = open;
                        candle.High = high;
                        candle.Low = low;
                        candle.Close = close;
                        candle.Volume = volume;
                        if (addCandle)
                            candleCache.TryAdd(candleOpenUnix, candle);
                        else
                            candleCache[candleOpenUnix] = candle;
                    }
                    finally
                    {
                        cacheListSemaphore.Release();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "Bitvavo WebSocket message processing error");
        }
    }


    private async Task ReceiveLoopAsync(CancellationToken ct, SemaphoreSlim cacheListSemaphore, SortedList<string, CryptoCandleList> symbolCandleCache)
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
                    await ProcessMessageAsync(msg, cacheListSemaphore, symbolCandleCache);
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
            SemaphoreSlim cacheListSemaphore = new(1, 1);

            SortedList<string, CryptoCandleList> symbolCandleCache = [];

            List<string> markets = [];
            foreach (var symbol in SymbolList)
            {
                markets.Add(symbol.ExchangeName);
                symbolCandleCache.Add(symbol.ExchangeName, []);
            }

            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
                throw new Exception("Geen intervallen?");


            // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
            _localCts = CancellationTokenSource.CreateLinkedTokenSource(ExchangeBase.CancellationToken);
            _ws = new ClientWebSocket();

            await _ws.ConnectAsync(new Uri(WsUrl), _localCts.Token);

            // Subscribe to 1m candles for all symbols in this group
            var subscribeMsg = JsonSerializer.Serialize(new
            {
                action = "subscribe",
                channels = new[] { new { name = "candles", interval = new[] { "1m" }, markets } }
            });

            byte[] msgBytes = Encoding.UTF8.GetBytes(subscribeMsg);
            await _ws.SendAsync(new ArraySegment<byte>(msgBytes), WebSocketMessageType.Text, true, _localCts.Token);

            // Start background receive loop
            _ = Task.Run(() => ReceiveLoopAsync(_localCts.Token, cacheListSemaphore, symbolCandleCache), _localCts.Token);


            // Implementatie kline timer (fix)
            // Omdat er niet altijd een nieuwe candle aangeboden wordt (zoals "flut" munt TOMOUSDT)
            // kun je aanvullend een timer kunnen gebruiken die alsnog de vorige candle herhaalt.
            // De gedachte is om dat iedere minuut 10 seconden na het normale kline event te doen.

            System.Timers.Timer timerKline = new()
            {
                AutoReset = false,
            };
            timerKline.Elapsed += new System.Timers.ElapsedEventHandler(async (sender, e) =>
            {
                foreach (var symbol in SymbolList)
                {
                    try
                    {
                        await cacheListSemaphore.WaitAsync();
                        try
                        {
                            CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                            CandleTime expectedCandlesUpto = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - interval.Duration;

                            // Finally do something with the cached data
                            CryptoCandle candleLast = default;
                            foreach (CryptoCandle candle in candleCache.Values.ToList())
                            {
                                // Only the ready candles (might change the flow?)
                                if (candle.OpenTime <= expectedCandlesUpto)
                                {
                                    candleCache.Remove(candle.OpenTime);
                                    Interlocked.Increment(ref TickerCount);
                                    if (TickerCount > 999999999)
                                        Interlocked.Exchange(ref TickerCount, 0);

                                    await CandleTools.Process1mCandleAsync(symbol, candle.Date,
                                        candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
                                    candleLast = candle;
                                }
                                else break;
                            }
                            // Add the last candle in the analysis queue
                            if (candleLast.OpenTime == expectedCandlesUpto)
                            {
                                // Last known price(s)
                                if (!GlobalData.IsEmulatorMode)
                                    symbol.LastPrice = candleLast.Close;
                                GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candleLast);
                            }
                        }
                        finally
                        {
                            cacheListSemaphore.Release();
                        }
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, symbol.Name);
#if DEBUG
                        GlobalData.AddTextToLogTab($"KLine Ticker {symbol.Name} ERROR {error.Message}");
#endif
                    }
                }

                if (sender is System.Timers.Timer t)
                {
                    t.Interval = GetNextTimer();
                    t.Start();
                }
            });
            timerKline.Interval = GetNextTimer();
            timerKline.Start();

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

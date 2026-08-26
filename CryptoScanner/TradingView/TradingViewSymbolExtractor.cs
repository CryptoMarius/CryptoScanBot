using Avalonia.Threading;

using CryptoScanner.Core.Core;

using System.Text.Json;

namespace CryptoScanner.TradingView;

public class TickerData
{
    //public string? Name { get; set; }
    public string? Ticker { get; set; }
    //public string? Url { get; set; }

    //public string DisplayFormat { get; set; }
    //public DateTime? LastCheck { get; set; }
    //public decimal LastValue { get; set; }

    // Close value?
    public decimal Lp { get; set; }
    public double Volume { get; set; }

    // Onderstaand is in deze tool niet nodig, wellicht willen we er in de toekomt nog wat mee?

    //public double Ch { get; set; }
    //public double Chp { get; set; }
    //public string MarketStatus { get; set; }
    //public string CurrentSession { get; set; }
    //public double Rtc { get; set; } // pre-market value
    //public double Rch { get; set; }
    //public double Rchp { get; set; }
    //public double PrevClosePrice { get; set; }
    //public double OpenPrice { get; set; } // previous ?
    //public DateTime OpenTime { get; set; }
    //public string TimeZone { get; set; }
}


public class TradingViewSymbolExtractor
{
    private readonly TickerData _tickerData = new();
    //private Action<decimal, double> OnDataReceived = null!;

    public async void StartAsync(string tickerName, string displayName,
        Action<decimal, double> onDataReceived,
        CancellationToken cancellationToken = default,
        int startDelayMs = 250, int loopDelayMs = 1000
        )
    {
        await Task.Delay(startDelayMs, cancellationToken);
        //_tickerData.Name = displayName;
        _tickerData.Ticker = tickerName;
        //OnDataReceived = onDataReceived;

        //GlobalData.AddTextToLogTab($"TradingView {tickerName} starting");
        TradingViewSymbolWebSocket socket = new(tickerName);
        socket.DataFetched += OnValueFetched;
        socket.ConnectWebSocketAndRequestSession().Wait(cancellationToken);
        socket.RequestData().Wait(cancellationToken);

        //bool displayNext = false;
        //int reconnectCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = socket.ReceiveData().Result;
                if (result)
                {
                    Dispatcher.UIThread.Post(() => onDataReceived(_tickerData.Lp, _tickerData.Volume));
                    await Task.Delay(loopDelayMs, cancellationToken);
                    //if (displayNext)
                    //{
                    //    displayNext = false;
                    //    //System.Diagnostics.Debug.WriteLine($"{_tickerData.Name} ok");
                    //}
                }
                else
                {
                    // Failed, connect again..
                    //reconnectCount++;
                    //GlobalData.AddTextToLogTab($"TradingView {tickerName} reconnecting (attempt {reconnectCount})");
                    // Wait what the socket that just failed says is right: the short hiccup pause,
                    // or what a 429 asked for. This used to be a fixed 250 ms regardless of the
                    // answer, which is how four tickers times nineteen scanners hammered TradingView
                    // 12,194 times in four minutes on 26-08-2026 and kept the 429 alive themselves.
                    await Task.Delay(socket.RetryDelay, cancellationToken);
                    socket = new TradingViewSymbolWebSocket(tickerName);
                    socket.DataFetched += OnValueFetched;
                    socket.ConnectWebSocketAndRequestSession().Wait(cancellationToken);
                    socket.RequestData().Wait(cancellationToken);

                    //System.Diagnostics.Debug.WriteLine($"{_tickerData.Name} not succeeded");
                    //displayNext = true;
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation - exit gracefully
                break;
            }
            catch (Exception)
            {
                // Other errors - continue trying
                try
                {
                    await Task.Delay(250, cancellationToken);
                    //System.Diagnostics.Debug.WriteLine($"{_tickerData.Name} error {error.Message}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // Cleanup
        socket.DataFetched -= OnValueFetched;
    }


    private void OnValueFetched(object? sender, List<string> values)
    {
        try
        {
            //foreach (string s in values)
            //    GlobalData.AddTextToLogTab($"TradingView {_tickerData.Ticker} json: {s}");
            ApplyRates(values);
        }
        catch (Exception e)
        {
            GlobalData.AddTextToLogTab($@"Exception {e.Message}");
            ScannerLog.Logger.Error(e, "");
        }
    }

    private void ApplyRates(List<string> e)
    {
        //decimal lastValue = value.Lp;
        var flag = 0;
        foreach (var json in e)
        {
            var res = TradingViewJsonParser.TryParse(json);
            if (res == null)
                //{
                //GlobalData.AddTextToLogTab($"TradingView {_tickerData.Ticker} TryParse=null for: {json}");
                continue;
            //}
            //if (_tickerData.Name == "Bitcoin")
            //{
            //    System.Diagnostics.Debug.WriteLine($"{_tickerData.Name} error {json}");
            //}

            flag += ApplyTickerCurrentValues(res);
            //flag += ApplyMarketStatus(res);
            //flag += ApplyCurrentSession(res);
            //flag += ApplyPreMarket(res);
        }

        //if (flag > 0)
        //{
        //    _tickerData.LastCheck = DateTime.UtcNow;
        //    //if (lastValue != value.Lp)
        //    //    ValueFetched?.Invoke(this, value);
        //    //_vm.ForecastVm.CalculateNewRates(_vm.TradingViewVm.Rates);
        //    //GlobalData.AddTextToLogTab(value.Name + " value=" + value.Lp);
        //    Dispatcher.UIThread.Post(() => OnDataReceived(_tickerData.Lp, _tickerData.Volume));
        //}
    }

    //private int ApplyPreMarket(JsonDocument jDocument)
    //{
    //    if (jDocument.RootElement.TryGetProperty("rtc", out JsonElement rtcValue) && rtcValue.TryGetDouble(out double rtc))
    //        value.Rtc = rtc;

    //    if (jDocument.RootElement.TryGetProperty("rch", out JsonElement rchValue) && rchValue.TryGetDouble(out double rch))
    //        value.Rch = rch;

    //    if (jDocument.RootElement.TryGetProperty("rchp", out JsonElement rchpValue) && rchpValue.TryGetDouble(out double rchp))
    //        value.Rchp = rchp;

    //    return 1;
    //}

    //private int ApplyMarketStatus(JsonDocument jDocument)
    //{
    //    Its not really a short name...
    //    if (jObject.ContainsKey("short_description"))
    //        value.Name = jObject["short_description"].ToString();
    //    if (jDocument.RootElement.TryGetProperty("market-status", out JsonElement msValue))
    //    {
    //        var marketStatus = JsonSerializer.Deserialize<TradingViewMarketStatusObject>(msValue.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    //        if (marketStatus is not null)
    //        {
    //            value.MarketStatus = marketStatus.Phase;
    //            return 1;
    //        }
    //    }
    //    return 0;
    //}

    //private int ApplyCurrentSession(JsonDocument jDocument)
    //{
    //    if (jDocument.RootElement.TryGetProperty("current_session", out JsonElement csValue))
    //        value.CurrentSession = csValue.GetString();

    //    if (jDocument.RootElement.TryGetProperty("prev_close_price", out JsonElement pcpValue) && pcpValue.TryGetDouble(out double pcp))
    //        value.PrevClosePrice = pcp;

    //    if (jDocument.RootElement.TryGetProperty("open_price", out JsonElement opValue) && opValue.TryGetDouble(out double op))
    //        value.OpenPrice = op;

    //    if (jDocument.RootElement.TryGetProperty("open_time", out JsonElement otValue) && otValue.TryGetInt32(out int ot))
    //    {
    //        TimeSpan time = TimeSpan.FromSeconds(ot);
    //        DateTime startdate = new DateTime(1970, 1, 1) + time;
    //        value.OpenTime = startdate;
    //    }

    //    if (jDocument.RootElement.TryGetProperty("timezone", out JsonElement tzValue))
    //        value.TimeZone = tzValue.GetString();

    //    return 1;
    //}

    private int ApplyTickerCurrentValues(JsonDocument jDocument)
    {
        if (jDocument.RootElement.TryGetProperty("lp", out JsonElement lpValue) && lpValue.TryGetDecimal(out decimal lp))
            _tickerData.Lp = lp;
        //else
        //    // Log when no "lp" field is present so we can diagnose symbols like TVC:DXY that may be
        //    // closed (forex weekend) or require special permissions.
        //    ScannerLog.Logger.Info($"TradingView {_tickerData.Ticker}: no 'lp' in payload: {jDocument.RootElement}");

        if (jDocument.RootElement.TryGetProperty("volume", out JsonElement volumeValue) && volumeValue.TryGetDecimal(out decimal volume))
            _tickerData.Volume = (double)volume;
        //if (jDocument.RootElement.TryGetProperty("v", out JsonElement volumeValue2) && volumeValue2.TryGetDecimal(out decimal volume2))
        //    _tickerData.Volume = volume2;

        //if (jDocument.RootElement.TryGetProperty("ch", out JsonElement chValue) && chValue.TryGetDouble(out double ch))
        //    value.Ch = ch;

        //if (jDocument.RootElement.TryGetProperty("chp", out JsonElement chpValue) && chpValue.TryGetDouble(out double chp))
        //    value.Chp = chp;

        return 1;
    }
}
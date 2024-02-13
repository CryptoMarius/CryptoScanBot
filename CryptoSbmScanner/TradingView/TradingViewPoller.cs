using System.Text.Json;

using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner.TradingView;

public class SymbolValue
{
    public string Name { get; set; }
    public string Ticker { get; set; }
    public string Url { get; set; }

    public string DisplayFormat { get; set; }
    public DateTime? LastCheck { get; set; }
    public decimal LastValue { get; set; } // For colors

    public decimal Lp { get; set; } // Close?

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


public class TradingViewSymbolInfo
{
    private SymbolValue SymbolValue;
    private TradingViewSymbolWebSocket socket;

    public async void StartAsync(string tickerName, string displayName, string displayFormat, SymbolValue symbolValue, int startDelayMs)
    {
        await Task.Delay(startDelayMs);

        SymbolValue = symbolValue;
        SymbolValue.Name = displayName;
        SymbolValue.Ticker = tickerName;
        SymbolValue.DisplayFormat = displayFormat;
        
        socket = new TradingViewSymbolWebSocket(tickerName);
        socket.DataFetched += OnValueFetched;
        socket.ConnectWebSocketAndRequestSession().Wait();
        socket.RequestData().Wait();

        while (true)
        {
            var result = socket.ReceiveData().Result;
            if (result)
            {
                Thread.Sleep(1000);
            }
            else
            {
                // Failed, connect again..
                Thread.Sleep(100);
                socket = new TradingViewSymbolWebSocket(tickerName);
                socket.DataFetched += OnValueFetched;
                socket.ConnectWebSocketAndRequestSession().Wait();
                socket.RequestData().Wait();
            }
        }
    }

    private void OnValueFetched(object sender, List<string> values)
    {
        try
        {
            //// Debug display
            //foreach (string s in e)
            //{
            //    GlobalData.AddTextToLogTab(" ? " + s);
            //}
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
                continue;
            flag += ApplyTickerCurrentValues(res);
            //flag += ApplyMarketStatus(res);
            //flag += ApplyCurrentSession(res);
            //flag += ApplyPreMarket(res);
        }

        if (flag > 0)
        {
            SymbolValue.LastCheck = DateTime.UtcNow;
            //if (lastValue != value.Lp)
            //    ValueFetched?.Invoke(this, value);
            //_vm.ForecastVm.CalculateNewRates(_vm.TradingViewVm.Rates);
            //GlobalData.AddTextToLogTab(value.Name + " value=" + value.Lp);
        }
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
            SymbolValue.Lp = lp;

        //if (jDocument.RootElement.TryGetProperty("ch", out JsonElement chValue) && chValue.TryGetDouble(out double ch))
        //    value.Ch = ch;

        //if (jDocument.RootElement.TryGetProperty("chp", out JsonElement chpValue) && chpValue.TryGetDouble(out double chp))
        //    value.Chp = chp;

        return 1;
    }
}
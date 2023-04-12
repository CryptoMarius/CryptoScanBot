using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TradingView
{
    public class SymbolValue
    {
        public string Name { get; set; }
        public string Ticker { get; set; }
        public string Url { get; set; }
        public string DisplayFormat { get; set; }
        public DateTime? LastCheck { get; set; }
        public decimal LastValue { get; set; } // For colors

        public decimal Lp { get; set; } // Close?

        // Onderstaand is in deze tool noet nodig
        // Wellicht willen we er later wat mee?

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
        private SymbolValue value;
        private TradingViewSymbolWebSocket socket;

        public async void Start(string tickerName, string displayName, string displayFormat, SymbolValue symbolValue, int startDelayMs)
        {
            await Task.Delay(startDelayMs);

            value = symbolValue;
            value.Name = displayName;
            value.Ticker = tickerName;
            value.DisplayFormat = displayFormat;
            socket = new TradingViewSymbolWebSocket(tickerName);
            socket.DataFetched += OnValueFetched;
            socket.ConnectWebSocketAndRequestSession().Wait();
            socket.RequestData().Wait();

            while (true)
            {
                var result = socket.ReceiveData().Result;
                if (result)
                {
                    Thread.Sleep(10);
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

        private void OnValueFetched(object sender, List<string> e)
        {
            try
            {
                //// Debug display
                //foreach (string s in e)
                //{
                //    GlobalData.AddTextToLogTab(" ? " + s);
                //}
                ApplyRates(e);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
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
                value.LastCheck = DateTime.UtcNow;
                //if (lastValue != value.Lp)
                //    ValueFetched?.Invoke(this, value);
                //_vm.ForecastVm.CalculateNewRates(_vm.TradingViewVm.Rates);
                //GlobalData.AddTextToLogTab(value.Name + " value=" + value.Lp);
            }
        }

        //private int ApplyPreMarket(JObject jObject)
        //{
        //    JToken rtcToken = jObject["rtc"];
        //    if (!rtcToken.IsNullOrEmpty())
        //        value.Rtc = (double)jObject["rtc"];

        //    JToken rchToken = jObject["rch"];
        //    if (!rchToken.IsNullOrEmpty())
        //        value.Rch = (double)jObject["rch"];

        //    JToken rchpToken = jObject["rchp"];
        //    if (!rchpToken.IsNullOrEmpty())
        //        value.Rchp = (double)jObject["rchp"];

        //    return 1;
        //}

        //private int ApplyMarketStatus(JObject jObject)
        //{
        //    // Its not really a short name...
        //    //if (jObject.ContainsKey("short_description"))
        //    //    value.Name = jObject["short_description"].ToString();


        //    if (!jObject.ContainsKey("market-status"))
        //        return 0;
        //    var ms = jObject["market-status"].ToString();
        //    var marketStatus = JsonConvert.DeserializeObject<TradingViewMarketStatusObject>(ms);
        //    if (marketStatus == null) return 0;
        //    value.MarketStatus = marketStatus.Phase;
        //    return 1;
        //}

        //private int ApplyCurrentSession(JObject jObject)
        //{
        //    if (jObject.ContainsKey("current_session"))
        //        value.CurrentSession = jObject["current_session"].ToString();
        //    if (jObject.ContainsKey("prev_close_price"))
        //        value.PrevClosePrice = (double)jObject["prev_close_price"];
        //    if (jObject.ContainsKey("open_price"))
        //        value.OpenPrice = (double)jObject["open_price"];
        //    if (jObject.ContainsKey("open_time"))
        //    {
        //        var ms = (int)jObject["open_time"];
        //        TimeSpan time = TimeSpan.FromSeconds(ms);
        //        DateTime startdate = new DateTime(1970, 1, 1) + time;
        //        value.OpenTime = startdate;
        //    }
        //    if (jObject.ContainsKey("timezone"))
        //        value.TimeZone = jObject["timezone"].ToString();
        //    return 1;
        //}

        private int ApplyTickerCurrentValues(JObject jObject)
        {
            if (jObject.ContainsKey("lp"))
                value.Lp = (decimal)jObject["lp"];
            //if (jObject.ContainsKey("ch"))
            //    value.Ch = (double)jObject["ch"];
            //if (jObject.ContainsKey("chp"))
            //    value.Chp = (double)jObject["chp"];
            return 1;
        }
    }

    public static class JsonExtensions
    {
        public static bool IsNullOrEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                   (token.Type == JTokenType.Null);
        }
    }
}
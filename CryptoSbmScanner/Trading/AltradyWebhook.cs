using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using Newtonsoft.Json.Linq;
using System.Net;

namespace CryptoSbmScanner.Trading;

public class AltradyWebhook
{

    public static void Execute(CryptoSignal signal, string botKey, string botSecret, decimal stoppLoss)
    {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.altrady.com/v2/signal_bot_positions");
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        try
        {
            //"test": true,
            //"api_key": "string",
            //"api_secret": "string",
            //"side": "long",
            //"exchange": "string",
            //"symbol": "string",
            //"signal_price": 0,

            dynamic request = new JObject();
            //request.test = false;
            request.api_key = botKey;
            request.api_secret = botSecret;
            request.side = "long";
            request.exchange = "binance";
            request.symbol = signal.Symbol.Name;
            //request.signal_price = signal.Symbol.LastPrice - 2 * signal.Symbol.PriceTickSize;

            //// We zetten de buy order op de bb, want daar zakt ie toch naar terug (nouja, vaak)
            //CryptoSymbolInterval symbolPeriod = signal.Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
            //CryptoCandle candleLast = symbolPeriod.CandleList.Values.Last();
            //decimal? value = (decimal)candleLast.CandleData.BollingerBandsLowerBand + 2 * signal.Symbol.PriceTickSize;
            //if (signal.Symbol.LastPrice < (decimal)value)
            //    value = signal.Symbol.LastPrice - 2m * signal.Symbol.PriceTickSize;
            //request.signal_price = value;

            //"take_profit": [
            //{
            //"price_percentage": 0,
            //"position_percentage": 0
            //}
            //],
            dynamic take_ProfitList = new JArray(); // List<dynamic>();
            request.take_profit = take_ProfitList;

            dynamic take_Profit1 = new JObject();
            take_Profit1.price_percentage = GlobalData.Settings.Bot.ProfitPercentage;
            take_Profit1.position_percentage = 100;
            take_ProfitList.Add(take_Profit1);

            //dynamic take_Profit2 = new JObject();
            //take_Profit2.price_percentage = 0.50;
            //take_Profit2.position_percentage = 50;
            //take_ProfitList.Add(take_Profit2);

            //"stop_loss": 
            //{
            //"stop_percentage": 0,
            //"cool_down_amount": 0,
            //"cool_down_time_frame": "minute"
            //}

            if (stoppLoss > 0)
            {
                dynamic stop_loss = new JObject();
                request.stop_loss = stop_loss;
                stop_loss.stop_percentage = stoppLoss;
                stop_loss.cool_down_amount = 0;
                stop_loss.cool_down_time_frame = "minute";
            }

            string json = request.ToString();
            //Console.WriteLine(json);

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                //GlobalData.AddTextToLogTab(json);
            }
            GlobalData.AddTextToLogTab(signal.Symbol.Name + " " + signal.Interval.Name + " verzonden naar Altrady webhook");
            GlobalData.AddTextToLogTab(json);


            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using StreamReader streamReader = new(httpResponse.GetResponseStream());
            var result = streamReader.ReadToEnd();
            GlobalData.AddTextToLogTab(result);



            //?? nieuwe web
            //using HttpClient httpClient = new();
            ////HttpRequestMessage request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.altrady.com/v2/signal_bot_positions");
            //var content = new StringContent(json, Encoding.UTF8, "application/json");
            //var result = httpClient.PostAsync("https://api.altrady.com/v2/signal_bot_positions", content).Result;



            //string href = string.Format("https://api.altrady.com/v2/signal_bot_positions/");
            //webClient.UploadData(new Uri(href), downLoadFolder);
        }
        catch (Exception error)
        {
            // don care (paper account)
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("error webhook " + error.ToString()); // symbol.Text + " " + 
        }

    }
    public static void Execute1(CryptoSignal signal)
    {
        if (signal.Symbol.LastTradeDate.HasValue && signal.Symbol.LastTradeDate > DateTime.UtcNow.AddMinutes(-GlobalData.Settings.Bot.GlobalBuyCooldownTime))
        {
            GlobalData.AddTextToLogTab(signal.Symbol.Name + " " + signal.Interval.Name + " is in cooldown (geen positie)");
            return;
        }

        // in de veronderstelling dat onderstaande allemaal lukt
        signal.Symbol.LastTradeDate = DateTime.UtcNow;


        switch (signal.Strategy)
        {
            case SignalStrategy.stobbOversold:
                // Alleen SBM doet mee in de testronde
                break;
            case SignalStrategy.sbm1Oversold:
            case SignalStrategy.sbm2Oversold:
            case SignalStrategy.sbm3Oversold:
            case SignalStrategy.sbm4Oversold:
                if (signal.Symbol.Quote.Equals("USDT"))
                {
				// ja, het is nog even hardcoded
                    Execute(signal, "", "", 0.00m);
                    Execute(signal, "", "", 0.75m);
                    Execute(signal, "", "", 1.50m);
                    Execute(signal, "", "", 2.50m);
                }
                break;

            case SignalStrategy.bullishEngulfing:
            case SignalStrategy.priceCrossedEma20:
            case SignalStrategy.priceCrossedEma50:
            case SignalStrategy.priceCrossedSma20:
            case SignalStrategy.priceCrossedSma50:
            case SignalStrategy.slopeEma50TurningNegative:
            case SignalStrategy.slopeEma50TurningPositive:
            case SignalStrategy.slopeSma50TurningNegative:
            case SignalStrategy.slopeSma50TurningPositive:
            case SignalStrategy.slopeEma20TurningPositive:
            case SignalStrategy.slopeSma20TurningPositive:
                // Een andere keer, hier is een positieve trend nodig en die dwingen we (nog) niet af
				// ja, het is nog even hardcoded
                //if (signal.Symbol.Quote.Equals("BTC"))
                //    Execute(signal, "", "", 0.0m);
                //if (signal.Symbol.Quote.Equals("USDT"))
                //    Execute(signal, "", "", 0.0m);
                break;
        }
    }


}
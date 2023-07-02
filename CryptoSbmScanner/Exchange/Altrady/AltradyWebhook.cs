//using CryptoSbmScanner.Intern;
//using CryptoSbmScanner.Model;
//using Newtonsoft.Json.Linq;
//using System.Net;

namespace CryptoSbmScanner.Exchange.Altrady;

// De Altrady webhook is qua paperTesting afgekeurd, het vult netjes DCA orders op -10% 
// en sluit dan de order met winst met als gevolg een totaal vertekend beeld op het traden.
// (bijkopen gebeurd al niet vaak, bug wellicht nog zeldzamer, maar zeker te vaak om te negeren)

//public class AltradyWebhook
//{

//    private static void Execute(CryptoSignal signal, string botKey, string botSecret, decimal stoppLoss)
//    {
//        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.altrady.com/v2/signal_bot_positions");
//        httpWebRequest.ContentType = "application/json";
//        httpWebRequest.Method = "POST";

//        try
//        {
//            //"test": true,
//            //"api_key": "string",
//            //"api_secret": "string",
//            //"side": "long",
//            //"exchange": "string",
//            //"symbol": "string",
//            //"signal_price": 0,

//            dynamic request = new JObject();
//            //request.test = false;
//            request.api_key = botKey;
//            request.api_secret = botSecret;
//            request.side = "long";
//            request.exchange = "binance";
//            request.symbol = signal.Symbol.Name;
//            // Afgesterd - 27-04-2023 10:12 (na verwarrende buy's die suggeren dat er veel te hoog gekocht werd! <papertrading sucks>)
//            // vervelende is dat de LastPrice kan varieeren, je zou nog steeds best hoog kunnen inkopen (niet de schuld van deze code)
//            //request.signal_price = signal.Symbol.LastPrice + (GlobalData.Settings.Bot.GlobalBuyVarying * signal.Symbol.LastPrice) / 100m;

//            // 27-04-2023 10:12 signal_price Aangezet na verwarrende entries
//            // 28-04-2023 10:12 signal_price afgesterd na aanpassing SBM2 berekening

//            CryptoSymbolInterval symbolPeriod = signal.Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
//            CryptoCandle candleLast = symbolPeriod.CandleList.Values.Last();
//            //// We zetten de buy order op de candle.close
//            //decimal? value = candleLast.Close;
//            //// We zetten de buy order op de gemiddelde van de laatste candle
//            //decimal? value = (candleLast.Open + candleLast.Close) / 2m;
//            //// We zetten de buy order op de bb, want daar zakt ie toch naar terug (nouja, vaak)
//            //decimal? value = (decimal)candleLast.CandleData.BollingerBandsLowerBand + 2 * signal.Symbol.PriceTickSize;
//            //if (signal.Symbol.LastPrice < (decimal)value)
//            //    value = signal.Symbol.LastPrice;
//            //request.signal_price = value - signal.Symbol.PriceTickSize;
//            // En zonder de last_price is het een marketorder?

//            // wellicht is eea afhankelijk van de markt stemming,
//            // bij een positieve stemming is een ~marketorder okay
//            // bij een negatieve stemming is de bb.lower een betere keuze

//            //"take_profit": [
//            //{
//            //"price_percentage": 0,
//            //"position_percentage": 0
//            //}
//            //],
//            dynamic take_ProfitList = new JArray(); // List<dynamic>();
//            request.take_profit = take_ProfitList;

//            dynamic take_Profit1 = new JObject();
//            take_Profit1.price_percentage = GlobalData.Settings.Trading.ProfitPercentage;
//            take_Profit1.position_percentage = 100;
//            take_ProfitList.Add(take_Profit1);

//            //dynamic take_Profit2 = new JObject();
//            //take_Profit2.price_percentage = 0.50;
//            //take_Profit2.position_percentage = 50;
//            //take_ProfitList.Add(take_Profit2);

//            //"stop_loss": 
//            //{
//            //"stop_percentage": 0,
//            //"cool_down_amount": 0,
//            //"cool_down_time_frame": "minute"
//            //}

//            if (stoppLoss > 0)
//            {
//                dynamic stop_loss = new JObject();
//                request.stop_loss = stop_loss;
//                stop_loss.stop_percentage = stoppLoss;
//                stop_loss.cool_down_amount = 0;
//                stop_loss.cool_down_time_frame = "minute";
//            }

//            string json = request.ToString();
//            //Console.WriteLine(json);

//            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
//            {
//                streamWriter.Write(json);
//                //GlobalData.AddTextToLogTab(json);
//            }
//            //GlobalData.AddTextToLogTab(signal.Symbol.Name + " " + signal.Interval.Name + " verzonden naar Altrady webhook");
//            //GlobalData.AddTextToTelegram(signal.Symbol.Name + " " + signal.Interval.Name + " verzonden naar Altrady webhook");
//            //GlobalData.AddTextToLogTab(json);


//            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
//            using StreamReader streamReader = new(httpResponse.GetResponseStream());
//            var result = streamReader.ReadToEnd();
//            //GlobalData.AddTextToLogTab(result);



//            //?? nieuwe web
//            //using HttpClient httpClient = new();
//            ////HttpRequestMessage request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.altrady.com/v2/signal_bot_positions");
//            //var content = new StringContent(json, Encoding.UTF8, "application/json");
//            //var result = httpClient.PostAsync("https://api.altrady.com/v2/signal_bot_positions", content).Result;



//            //string href = string.Format("https://api.altrady.com/v2/signal_bot_positions/");
//            //webClient.UploadData(new Uri(href), downLoadFolder);
//        }
//        catch (Exception error)
//        {
//            // don care (paper account)
//            GlobalData.Logger.Error(error);
//            GlobalData.AddTextToLogTab("error webhook " + error.ToString()); // symbol.Text + " " + 
//        }

//    }
//    public static void ExecuteBuy(CryptoSignal signal)
//    {
//        if (signal.Symbol.LastTradeDate.HasValue && signal.Symbol.LastTradeDate?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > DateTime.UtcNow)
//        {
//            GlobalData.AddTextToLogTab(signal.Symbol.Name + " " + signal.Interval.Name + " heeft een openstaande trade of is in cooldown na een trade");
//            return;
//        }

//        // in de veronderstelling dat onderstaande allemaal lukt
//        signal.Symbol.LastTradeDate = DateTime.UtcNow;


//        // Alleen SBM doet mee in de testronde
//        switch (signal.Strategy)
//        {
//            case SignalStrategy.Sbm1:
//            case SignalStrategy.Sbm2:
//            case SignalStrategy.Sbm3:
//            case SignalStrategy.Sbm4:
//            case SignalStrategy.Sbm5:
//                if (signal.Symbol.Quote.Equals("USDT"))
//                {
//				    // ja, het is nog even hardcoded
//                    Execute(signal, "95315384-4bee-4e5d-8134-82d1ba372787", "a390f9b0-f32b-46b9-84b4-f0615240aca2", 02.50m);
//                    Execute(signal, "fbafb2eb-97ba-437e-9ea0-44c4e6eec7bb", "5d4c1ef7-4210-4273-804f-540522c48b27", 05.00m);
//                    Execute(signal, "a36a98f3-7968-4a5c-999d-79521fe68706", "dfdd1ecd-46f2-4ad4-9268-9c988b1afa95", 10.0m);
//                }
//                else 
//                if (signal.Symbol.Quote.Equals("BTC"))
//                {
//                    // ja, het is nog even hardcoded
//                    Execute(signal, "52128499-cee4-41c7-99fe-ab533b236629", "d8712b2e-d7db-4724-bcef-7f8c4bd99cf0", 10.00m);
//                }
//                break;



//        }
//    }


//}
﻿using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;

using System.Net;

namespace ExchangeTest.Exchange.Altrady;

public class AltradyWebhook
{

    //public static void DelegateControlToAltrady(CryptoPosition position)
    //{
    //    if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
    //    {
    //        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} unable to send to Altrady webhook, no api key's available");
    //        return;
    //    }


    //    GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook LastTradeDate={position.Symbol.LastTradeDate}");

    //    try
    //    {
    //        GlobalData.ExternalUrls.GetExternalRef(position.Symbol.Exchange, out CryptoExternalUrls? externalUrls);
    //        if (externalUrls == null || externalUrls.Altrady.Code == "")
    //        {
    //            GlobalData.AddTextToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} no exchange code available");
    //            return;
    //        }


    //        // some documentation (nicely done, thanks!)
    //        // https://support.altrady.com/en/article/webhook-signals-testing-and-errors-1pl7g40/
    //        // https://support.altrady.com/en/article/webhook-signals-open-close-increase-or-reverse-a-position-5sr46f/#4-optional-settings-for-the-open-and-reverse-signal
    //        dynamic request = new JObject();

    //        // Request body
    //        request.test = false;
    //        request.action = "open"; // ['open', 'close', 'reverse', 'increase', 'start_bot', 'start_and_open', 'stop_bot', 'stop_and_close'],
    //        if (position.Side == CryptoTradeSide.Long)
    //            request.side = "long";
    //        else
    //            request.side = "short";
    //        request.api_key = GlobalData.AltradyApi.Key;
    //        request.api_secret = GlobalData.AltradyApi.Secret;


    //        request.signal_id = $"MyPositionId{position.Id}"; // optional
    //        request.exchange = externalUrls.Altrady.Code;
    //        request.symbol = $"{externalUrls.Altrady.Code}_{position.Symbol.Quote}_{position.Symbol.Base}";
    //        request.order_type = "market"; // ['limit', 'market']
    //        request.adjust_fee = true; // Adjust the order size to ensure there is enough to pay the fee (problems when managing position from our side)
    //        request.signal_price = position.EntryPrice;
    //        //leverage (integer, optional): The leverage for a futures position ,
    //        //quote_amount(number, optional): Specifies quote amount of the entry order, if left blank, the signal bot setting will be used. ,
    //        //base_amount(number, optional): Specifies base amount of the entry order, if left blank, the signal bot setting will be used. ,

    //        // TP body (just 1)
    //        {
    //            dynamic tp_orders = new JArray();
    //            request.take_profit = tp_orders;

    //            dynamic tp = new JObject();
    //            tp_orders.Add(tp);

    //            tp.price_percentage = GlobalData.Settings.Trading.ProfitPercentage;
    //            tp.position_percentage = 100;
    //        }

    //        // DCA body (multiple)
    //        // Is going to be expensive with my 5 dca setup... ;-)
    //        if (GlobalData.Settings.Trading.DcaList.Count > 0)
    //        {
    //            dynamic dca_orders = new JArray();
    //            request.dca_orders = dca_orders;

    //            foreach (var dcaItem in GlobalData.Settings.Trading.DcaList)
    //            {
    //                dynamic dca = new JObject();
    //                dca_orders.Add(dca);

    //                dca.price_percentage = dcaItem.Percentage;
    //                dca.quantity_percentage = 100 * dcaItem.Factor;
    //            }
    //        }

    //        // SL body
    //        if (GlobalData.Settings.Trading.StopLossPercentage > 0)
    //        {
    //            //dynamic stop_loss = new JObject();
    //            //request.stop_loss = stop_loss;
    //            //stop_loss.stop_percentage = GlobalData.Settings.Trading.StopLossPercentage;
    //            //stop_loss.cool_down_amount = 0;
    //            //stop_loss.cool_down_time_frame = "minute";

    //            request.stop_loss_percentage = GlobalData.Settings.Trading.StopLossPercentage;
    //        }


    //        // send

    //        // todo, replace obsolete WebRequest with HttpClient..
    //        //HttpClient httpClient = new();
    //        //var jsonData = await httpClient.GetFromJsonAsync<request>("https://api.alternative.me/fng/");
    //        //await httpClient.SendAsync("/api/v3/exchangeInfo", HttpMethod.Post);????

    //        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.altrady.com/v2/signal_bot_positions");
    //        httpWebRequest.ContentType = "application/json";
    //        httpWebRequest.Method = "POST";

    //        string json = request.ToString();
    //        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
    //        {
    //            streamWriter.Write(json);
    //            GlobalData.AddTextToLogTab(json);
    //        }
    //        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook");
    //        ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval.Name} send to Altrady webhook {json}");

    //        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
    //        using StreamReader streamReader = new(httpResponse.GetResponseStream());
    //        var result = streamReader.ReadToEnd();

    //        // log response
    //        //string text = JsonSerializer.Serialize(result, JsonTools.JsonSerializerIndented);
    //        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval.Name} result Altrady webhook {result}");
    //        ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval.Name} result Altrady webhook {result}");
    //    }
    //    catch (Exception error)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //        GlobalData.AddTextToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} error={error}");
    //    }

    //}

    public static void DelegateAllToAltrady(CryptoPosition position, string url, string command)
    {
        if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} unable to send to Altrady webhook, no api key's available");
            return;
        }


        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook LastTradeDate={position.Symbol.LastTradeDate}");

        try
        {
            GlobalData.ExternalUrls.GetExternalRef(position.Symbol.Exchange, out CryptoExternalUrls? externalUrls);
            if (externalUrls == null || externalUrls.Altrady.Code == "")
            {
                GlobalData.AddTextToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} no exchange code available");
                return;
            }


            string filename = GlobalData.GetBaseDir() + $"{command}.json";
            if (!File.Exists(filename))
            {
                GlobalData.AddTextToLogTab($"error file does not exist {filename}");
                return;
            }
            string text = File.ReadAllText(filename);



            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            string json = text;
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                GlobalData.AddTextToLogTab(json);
            }
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook");


            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using StreamReader streamReader = new(httpResponse.GetResponseStream());
            var result = streamReader.ReadToEnd();

            // log response
            //text = JsonSerializer.Serialize(result., JsonTools.JsonSerializerIndented);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval.Name} result Altrady webhook {result}");

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} error={error}");
        }
    }

}
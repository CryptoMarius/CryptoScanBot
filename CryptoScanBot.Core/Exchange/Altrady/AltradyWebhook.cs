using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;

using Newtonsoft.Json.Linq;

using System.Net;
using System.Text.Json;

namespace CryptoScanBot.Core.Exchange.Altrady;

public class AltradyWebhook
{

    public static void DelegateControlToAltrady(CryptoPosition position)
    {
        if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} unable to send to Altrady webhook, no api key's available");
            return;
        }


        //GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook"); //  LastTradeDate={position.Symbol.LastTradeDate}

        HttpWebRequest? httpWebRequest = null;
        HttpWebResponse? httpResponse = null;
        try
        {
            GlobalData.ExternalUrls.GetExternalRef(position.Symbol.Exchange, out CryptoExternalUrls? externalUrls);
            if (externalUrls == null || externalUrls.Altrady == null || externalUrls.Altrady!.Code == "")
            {
                GlobalData.AddTextToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} no exchange code available");
                return;
            }


            // some documentation (nicely done, thanks!)
            // https://support.altrady.com/en/article/webhook-signals-testing-and-errors-1pl7g40/
            // https://support.altrady.com/en/article/webhook-signals-open-close-increase-or-reverse-a-position-5sr46f/#4-optional-settings-for-the-open-and-reverse-signal
            dynamic request = new JObject();

            //string createError = "???"; +createError

            // Request body
            request.test = false;
            request.action = "open"; // ['open', 'close', 'reverse', 'increase', 'start_bot', 'start_and_open', 'stop_bot', 'stop_and_close'],
            if (position.Side == Enums.CryptoTradeSide.Long)
                request.side = "long";
            else
                request.side = "short";
            request.api_key = GlobalData.AltradyApi.Key;
            request.api_secret = GlobalData.AltradyApi.Secret;


            request.signal_id = $"MyPositionId{position.Id}"; // optional
            request.exchange = externalUrls.Altrady.Code;
            request.symbol = $"{externalUrls.Altrady.Code}_{position.Symbol.Quote}_{position.Symbol.Base}";
            request.adjust_fee = true; // Adjust the order size to ensure there is enough to pay the fee (problems when managing position from our side)

            if (GlobalData.Settings.Trading.EntryOrderType == Enums.CryptoOrderType.Market)
                request.order_type = "market"; // ['limit', 'market']
            if (GlobalData.Settings.Trading.EntryOrderType == Enums.CryptoOrderType.Limit)
            {
                request.order_type = "limit"; // ['limit', 'market']
                request.signal_price = position.EntryPrice;
                //request.quote_amount = position.EntryAmount; // Specifies quote amount of the entry order, if left blank, the signal bot setting will be used.
                request.base_amount = position.EntryAmount; // Specifies base amount of the entry order, if left blank, the signal bot setting will be used.
            }
            //leverage (integer, optional): The leverage for a futures position ,
            //quote_amount(number, optional): Specifies quote amount of the entry order, if left blank, the signal bot setting will be used. ,
            //base_amount(number, optional): Specifies base amount of the entry order, if left blank, the signal bot setting will be used. ,

            // TP body (just 1)
            {
                dynamic tp_orders = new JArray();
                request.take_profit = tp_orders;

                dynamic tp = new JObject();
                tp_orders.Add(tp);

                tp.price_percentage = GlobalData.Settings.Trading.ProfitPercentage;
                tp.position_percentage = 100;
            }

            // DCA body (multiple)
            // Is going to be expensive with my 5 dca setup... ;-)
            if (GlobalData.Settings.Trading.DcaList.Count > 0)
            {
                dynamic dca_orders = new JArray();
                request.dca_orders = dca_orders;

                foreach (var dcaItem in GlobalData.Settings.Trading.DcaList)
                {
                    dynamic dca = new JObject();
                    dca_orders.Add(dca);

                    dca.price_percentage = dcaItem.Percentage;
                    dca.quantity_percentage = 100 * dcaItem.Factor;
                }
            }

            // SL body
            if (GlobalData.Settings.Trading.StopLossPercentage > 0)
            {
                //dynamic stop_loss = new JObject();
                //request.stop_loss = stop_loss;
                //stop_loss.stop_percentage = GlobalData.Settings.Trading.StopLossPercentage;
                //stop_loss.cool_down_amount = 0;
                //stop_loss.cool_down_time_frame = "minute";

                request.stop_loss_percentage = GlobalData.Settings.Trading.StopLossPercentage;
            }


            // send

            // todo, replace obsolete WebRequest with HttpClient..
            //HttpClient httpClient = new();
            //var jsonData = await httpClient.GetFromJsonAsync<request>("https://api.alternative.me/fng/");
            //await httpClient.SendAsync("/api/v3/exchangeInfo", HttpMethod.Post);????

            httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.altrady.com/v2/signal_bot_positions");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            string json = request.ToString();
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                //GlobalData.AddTextToLogTab(json);
            }
            ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook json {json}");

            httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using StreamReader streamReader = new(httpResponse.GetResponseStream());
            var result = streamReader.ReadToEnd();

            // log response
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval.Name} Altrady webhook result {result}");
            ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval.Name}Altrady webhook result {result}");

            //GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook");
        }
        catch (WebException error)
        {
            ScannerLog.Logger.Error(error);
            //ScannerLog.Logger.Trace(Dump("Altrady webhook request", position, httpWebRequest));
            //ScannerLog.Logger.Trace(Dump("Altrady webhook response", position, httpResponse)); null
            ScannerLog.Logger.Trace(Dump("Altrady webhook error.response", position, error.Response));

            string errorResponseBody = "";
            if (error.Response is HttpWebResponse errorResponse) // && errorResponse.StatusCode == (HttpStatusCode)422)
            {
                using StreamReader errorStreamReader = new(errorResponse.GetResponseStream());
                errorResponseBody = errorStreamReader.ReadToEnd();
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook response {error.Message} {errorResponseBody}");
                //ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval.Name} Altrady webhook response body error={error.Message} {errorResponseBody}");
            }
            else
            {
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook error error={error}");
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error);
            //ScannerLog.Logger.Trace(Dump("Altrady webhook request", position, httpWebRequest));
            //ScannerLog.Logger.Trace(Dump("Altrady webhook response", position, httpResponse)); null

            //ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval.Name} Altrady webhook error {error.Message}");
            GlobalData.AddTextToLogTab($" {position.Symbol.Name} {position.Interval!.Name} Webhook error:error={error}");
        }

    }

    private static string Dump(string caption, CryptoPosition position, object? obj)
    {
        if (obj == null)
        {
            return $"{caption} {position.Symbol.Name} {position.Interval!.Name} null";
        }
        else
        {
            return $"{caption} {position.Symbol.Name} {position.Interval!.Name} {JsonSerializer.Serialize(obj, GlobalData.JsonSerializerIndented)}";
        }
    }

}
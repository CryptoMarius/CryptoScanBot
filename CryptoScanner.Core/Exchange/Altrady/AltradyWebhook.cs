using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using Newtonsoft.Json.Linq;

using System.Text;
using System.Text.Json;

namespace CryptoScanner.Core.Exchange.Altrady;

// {
// "signalBotPositions":
//   {
//     "id":14974903,
//     "coinraySymbol":"BYBI_USDT_DMAIL",
//     "status":"new","message":null,
//     "createdAt":"2024-09-12T11:48:01.953Z",
//     "signalData":
//     {
//        "markAsTest":false,
//        "signalId":"g-ea3ffffb-fb10-4373-85c3-c324c4179ba8",
//        "marketId":1578894,
//        "side":"long",
//        "leverage":null,
//        "signalPrice":"0.2421",
//        "takeProfits":[
//         {
//             "pricePercentage":"1.2","positionPercentage":"100.0"
//         }
//         ],
//         "dcaOrders":[],
//         "stopLoss":null,
//         "quoteAmount":null,
//         "baseAmount":"413.06",
//         "adjustFee":true
//      }
//   }
// }

public class AltradyWebhookSignalData
{
    public string? SignalId { get; set; }
}

public class AltradyWebhookBotPositions
{
    public int Id { get; set; }
    public string? CoinraySymbol { get; set; }
    public AltradyWebhookSignalData? SignalData { get; set; }
}

public class AltradyWebhookPayload
{
    public AltradyWebhookBotPositions? SignalBotPositions { get; set; }
}

public class AltradyWebhook
{
    private static readonly JsonSerializerOptions AltradySerializerOptions = new() { PropertyNameCaseInsensitive = true };


    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static AltradyWebhookPayload? TryParse(string message)
    {
        //JsonDocument?
        try
        {
            if (!message.StartsWith('{'))
                return null;

            var root = JsonSerializer.Deserialize<AltradyWebhookPayload>(message, JsonTools.DeSerializerOptions);

            //return JsonDocument.Parse(branch?.V?.ToString() ?? "");
            return root;
        }
        catch (Exception e)
        {
            ScannerLog.Logger.Error(e, "");
            return null;
        }
    }


    public static async Task DelegateControlToAltradyAsync(CryptoPosition position, string url = "", string command = "open")
    {
        if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} unable to send to Altrady webhook, no api key's available");
            return;
        }

        if (url == "")
            url = "https://api.altrady.com/v2/signal_bot_positions";


        //GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook"); //  LastTradeDate={position.Symbol.LastTradeDate}

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
            request.action = command; // "open"; // ['open', 'close', 'reverse', 'increase', 'start_bot', 'start_and_open', 'stop_bot', 'stop_and_close'],
            if (position.Side == Enums.CryptoTradeSide.Long)
                request.side = "long";
            else
                request.side = "short";
            request.api_key = GlobalData.AltradyApi.Key;
            request.api_secret = GlobalData.AltradyApi.Secret;

            //request.signal_id = $"MyPositionId{position.Id}"; // optional (problem, this is not a unique id <after deleting the db for example>)

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
                //request.base_amount = position.EntryAmount; // Specifies base amount of the entry order, if left blank, the signal bot setting will be used.
            }
            //leverage (integer, optional): The leverage for a futures position ,
            //quote_amount(number, optional): Specifies quote amount of the entry order, if left blank, the signal bot setting will be used. ,
            //base_amount(number, optional): Specifies base amount of the entry order, if left blank, the signal bot setting will be used. ,

            request.quote_amount = position.EntryAmount;

            // TP body (multiple)
            if (GlobalData.Settings.Trading.TpList.Count > 0)
            {
                dynamic tp_orders = new JArray();
                request.take_profit = tp_orders;

                foreach (CryptoTpEntry entry in GlobalData.Settings.Trading.TpList)
                {
                    dynamic tp = new JObject();
                    tp_orders.Add(tp);

                    tp.position_percentage = entry.Factor;
                    tp.price_percentage = entry.Percentage;
                }
            }


            // DCA body (multiple)
            decimal stopLossPercentage = 0;
            if (GlobalData.Settings.Trading.DcaList.Count > 0)
            {
                dynamic dca_orders = new JArray();
                request.dca_orders = dca_orders;

                foreach (var dcaItem in GlobalData.Settings.Trading.DcaList)
                {
                    dynamic dca = new JObject();
                    dca_orders.Add(dca);

                    // dcaItem.Factor is already a percentage (100 = 1x, 200 = 2x, ...)
                    dca.quantity_percentage = dcaItem.Factor;
                    dca.price_percentage = dcaItem.Percentage;

                    if (dcaItem.Percentage > stopLossPercentage)
                        stopLossPercentage = dcaItem.Percentage;
                }
            }

            // SL body
            // Prefer the strategy-computed SL distance (position.SlPercentage) when present: it is already
            // the percentage Altrady expects (distance from the entry; Altrady applies the direction based
            // on the side). No price inversion needed, so this also works for market orders. Otherwise fall
            // back to the configured percentage (deepest DCA + StopLossPercentage).
            if (position.SlPercentage is decimal slPercentage)
            {
                request.stop_loss_percentage = slPercentage;
            }
            else if (GlobalData.Settings.Trading.StopLossPercentage > 0)
            {
                //dynamic stop_loss = new JObject();
                //request.stop_loss = stop_loss;
                //stop_loss.stop_percentage = GlobalData.Settings.Trading.StopLossPercentage;
                //stop_loss.cool_down_amount = 0;
                //stop_loss.cool_down_time_frame = "minute";
                request.stop_loss_percentage = stopLossPercentage + GlobalData.Settings.Trading.StopLossPercentage;
            }

            //// Expiration time in minutes
            //if (GlobalData.Settings.Trading.EntryRemoveTime > 0)
            //{
            //    request.expiry_minutes = GlobalData.Settings.Trading.EntryRemoveTime * (int)position.Interval!.Duration;
            //}

            //// Expiration price (our calculated tp)
            //if (position.ProfitPrice.HasValue)
            //    request.expiry_price = position.ProfitPrice.Value;


            // Entry expiration: cancel unfilled entry when time OR price condition is met (whichever comes first)
            // Use the first TP percentage to calculate the expiry price (ProfitPrice is not yet available at open time)
            decimal? expiryPrice = null;
            if (position.EntryPrice.HasValue && GlobalData.Settings.Trading.TpList.Count > 0)
            {
                decimal tpPercentage = GlobalData.Settings.Trading.TpList[0].Percentage;
                if (position.Side == Enums.CryptoTradeSide.Long)
                    expiryPrice = position.EntryPrice.Value * (1 + tpPercentage / 100m);
                else
                    expiryPrice = position.EntryPrice.Value * (1 - tpPercentage / 100m);
            }

            if (GlobalData.Settings.Trading.EntryRemoveTime > 0 || expiryPrice.HasValue)
            {
                dynamic entry_expiration = new JObject();
                request.entry_expiration = entry_expiration;

                if (GlobalData.Settings.Trading.EntryRemoveTime > 0)
                    entry_expiration.time = GlobalData.Settings.Trading.EntryRemoveTime * (int)position.Interval!.Duration;

                if (expiryPrice.HasValue)
                    entry_expiration.price = expiryPrice.Value;
            }



            // Send request using HttpClient
            string json = request.ToString();
            string jsonFlat = request.ToString(Newtonsoft.Json.Formatting.None);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook request {jsonFlat}");
            ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook request {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            string result = await response.Content.ReadAsStringAsync();
            //ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook response {result}");
            //GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook");

            string info = "";
            try
            {
                //string result = "{\"signalBotPositions\":{\"id\":14974903,\"coinraySymbol\":\"BYBI_USDT_DMAIL\",\"status\":\"new\",\"message\":null,\"createdAt\":\"2024-09-12T11:48:01.953Z\",\"signalData\":{\"markAsTest\":false,\"signalId\":\"g-ea3ffffb-fb10-4373-85c3-c324c4179ba8\",\"marketId\":1578894,\"side\":\"long\",\"leverage\":null,\"signalPrice\":\"0.2421\",\"takeProfits\":[{\"pricePercentage\":\"1.2\",\"positionPercentage\":\"100.0\"}],\"dcaOrders\":[],\"stopLoss\":null,\"quoteAmount\":null,\"baseAmount\":\"413.06\",\"adjustFee\":true}}}";
                var resultObject = TryParse(result);

                if (resultObject == null)
                {
                    info = "null";
                    position.AltradyPositionId = null;
                }
                else
                {
                    position.AltradyPositionId = resultObject.SignalBotPositions?.SignalData?.SignalId;
                    info = $"id={resultObject.SignalBotPositions?.Id} SignalId={resultObject.SignalBotPositions?.SignalData?.SignalId}";
                }
            }
            catch (Exception error)
            {
                info = "error " + error.Message;
            }

            // log response
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval.Name} Altrady webhook result {result} {info}");
            ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval.Name}Altrady webhook result {result} {info}");
            GlobalData.AddTextToTelegram($"{position.Symbol.Name} {position.Interval.Name} Altrady webhook {position.Side} price={position.EntryPrice}", position);
        }
        catch (HttpRequestException error)
        {
            ScannerLog.Logger.Error(error);

            string errorMessage = $"HTTP error: {error.Message}";
            if (error.StatusCode.HasValue)
            {
                errorMessage += $" (Status: {error.StatusCode})";
            }

            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook error {errorMessage}");
        }
        catch (TaskCanceledException error)
        {
            ScannerLog.Logger.Error(error);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook timeout: {error.Message}");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error);
            GlobalData.AddTextToLogTab($" {position.Symbol.Name} {position.Interval!.Name} Webhook error:error={error}");
        }
    }

    // Synchronous wrapper for backward compatibility
    public static void DelegateControlToAltrady(CryptoPosition position, string url = "", string command = "open")
    {
        // Run async method synchronously (not ideal but maintains compatibility)
        DelegateControlToAltradyAsync(position, url, command).GetAwaiter().GetResult();
    }

    private static string Dump(string caption, CryptoPosition position, object? obj)
    {
        if (obj == null)
        {
            return $"{caption} {position.Symbol.Name} {position.Interval!.Name} null";
        }
        else
        {
            return $"{caption} {position.Symbol.Name} {position.Interval!.Name} {JsonSerializer.Serialize(obj, JsonTools.JsonSerializerIndented)}";
        }
    }

}
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
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

    /// <summary>
    /// Replace the value of api_key and api_secret in a flat json string by asterisks, so the webhook
    /// can be logged without putting the credentials in the log file. Works on the serialized text
    /// rather than on the payload object, because the payload is what gets posted a moment later.
    /// A missing or empty key simply produces nothing to replace.
    /// </summary>
    internal static string MaskSecrets(string flatJson)
    {
        foreach (string secret in new[] { GlobalData.AltradyApi.Key, GlobalData.AltradyApi.Secret })
        {
            if (!string.IsNullOrEmpty(secret))
                flatJson = flatJson.Replace(secret, new string('*', 8));
        }
        return flatJson;
    }

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


    /// <summary>
    /// The base quantity that a quote entry amount buys at the entry price, put on the symbol's
    /// quantity tick grid. Rounded UP: the plain Clamp rounds a quantity down (safe for our own
    /// orders, the balance is the limit there), but here the amount in quote is what was decided
    /// on and a tick less would leave the order below it - and, for a small entry, possibly under
    /// the symbol's minimum. Returns null when the inputs cannot produce a quantity, so the caller
    /// can fall back to quote_amount.
    /// </summary>
    public static decimal? CalculateBaseAmount(CryptoSymbol symbol, decimal quoteAmount, decimal price)
    {
        if (quoteAmount <= 0 || price <= 0)
            return null;

        decimal baseAmount = quoteAmount / price;
        decimal onGrid = baseAmount.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);

        // Clamp rounds down; step up one tick when it did (a maximum, if there is one, still wins)
        if (onGrid < baseAmount && symbol.QuantityTickSize > 0)
        {
            decimal oneTickUp = onGrid + symbol.QuantityTickSize;
            if (symbol.QuantityMaximum <= 0 || oneTickUp <= symbol.QuantityMaximum)
                onGrid = oneTickUp;
        }

        if (onGrid <= 0)
            return null;
        return onGrid;
    }


    /// <summary>
    /// Serialize the payload, log it with the credentials masked, post it and return the raw answer.
    /// Shared by the open and the close signal so both end up in the log the same way.
    /// </summary>
    private static async Task<(int statusCode, string body)> PostSignalAsync(CryptoPosition position, string url, dynamic request)
    {
        string json = request.ToString();
        string jsonFlat = request.ToString(Newtonsoft.Json.Formatting.None);

        // The api key and secret are part of the payload, so the flat json that goes to the log
        // tab carries them in plain text - and that line is written at Info level, so it lands in
        // CryptoScanBot.log and in the day archive. Masked on a COPY, because `request` is the
        // object that is serialized into the body a few lines down. The Trace line below keeps the
        // full json: trace is off by default and is where you look when a webhook is rejected.
        string jsonFlatMasked = MaskSecrets(jsonFlat);
        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook request {jsonFlatMasked}");
        ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook request {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _httpClient.PostAsync(url, content);

        // The status code travels along with the body: a close is answered with an EMPTY body, so
        // the body alone cannot tell an accepted close from a failed one.
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }


    /// <summary>
    /// Did Altrady accept the close signal? Their documentation describes no answer body for a close
    /// (only for a reverse: "204 no content" on receipt), and in practice every close since 26-09-2026
    /// came back with an empty body. So the http status decides: a 2xx is accepted, unless the body
    /// carries an error member - the way the webhook refuses an open, e.g. {"error":"Too many
    /// positions opened"}.
    /// </summary>
    internal static bool IsCloseAccepted(int statusCode, string body)
    {
        if (statusCode < 200 || statusCode > 299)
            return false;
        return !body.Contains("\"error\"");
    }


    /// <summary>
    /// Give Altrady the same profit lock the trader runs itself, so their stop travels with ours
    /// instead of sitting still at the initial distance. Their webhook has no action that moves the
    /// stop of a running position, so this has to be arranged in the opening signal - and Follow
    /// Price does exactly what <see cref="Trader.ProfitLockCalculator"/> does: nothing until the
    /// price reaches the trigger, then the stop follows the best price at a fixed distance and never
    /// hands ground back. Both percentages are measured from the AVERAGE entry on their side, which
    /// moves along with a filled dca just like our own break-even price does.
    /// <para>
    /// Only the trailing method is handed over. The fixed method (break even plus a percentage) has
    /// no counterpart: their BREAK_EVEN and FOLLOW_TAKE_PROFIT move the stop when a take profit
    /// FILLS and need two or more targets, where ours moves on a profit trigger with one target. In
    /// that setting Altrady keeps the initial stop and only our own administration moves.
    /// </para>
    /// </summary>
    internal static JObject BuildStopLossBlock(decimal stopPercentage, bool moveSlToBreakEven,
        CryptoProfitLockMethod method, decimal triggerPercentage, decimal trailPercentage, int takeProfitCount = 1)
    {
        dynamic stop_loss = new JObject();
        stop_loss.stop_percentage = stopPercentage;

        if (!moveSlToBreakEven)
            return stop_loss;

        // Altrady's own protections after a take profit fill. Both need two or more take profit
        // levels; with one there is nothing left to protect once it fills, and the scanner does not
        // move its stop either (see PositionMonitor.ApplyTakeProfitLock).
        if (Trader.ProfitLockCalculator.IsArmedByTakeProfit(method))
        {
            if (takeProfitCount >= 2)
                stop_loss.protection_type = method == CryptoProfitLockMethod.FollowTakeProfit ? "FOLLOW_TAKE_PROFIT" : "BREAK_EVEN";
            return stop_loss;
        }

        if (method != CryptoProfitLockMethod.TrailingPercentage)
            return stop_loss;

        // Their trailing distance has to stay between zero and ninety-nine percent
        if (triggerPercentage <= 0 || trailPercentage <= 0 || trailPercentage >= 99)
            return stop_loss;

        stop_loss.protection_type = "PRICE";
        stop_loss.trailing_percentage = triggerPercentage;
        stop_loss.trailing_distance = trailPercentage;
        return stop_loss;
    }


    /// <summary>
    /// The take profit levels of an open signal. <paramref name="trailPercentage"/> above zero makes
    /// the LAST level trail: Altrady starts following the price at that level with trailing_distance
    /// behind it instead of selling there, which is only allowed on the last take profit. Their
    /// distance has to stay between zero and ninety-nine percent.
    /// </summary>
    internal static JArray BuildTakeProfitBlock(List<CryptoTpEntry> tpList, decimal trailPercentage)
    {
        JArray tp_orders = new();
        for (int i = 0; i < tpList.Count; i++)
        {
            dynamic tp = new JObject();
            tp.position_percentage = tpList[i].Factor;
            tp.price_percentage = tpList[i].Percentage;
            if (i == tpList.Count - 1 && trailPercentage > 0 && trailPercentage < 99)
                tp.trailing_distance = trailPercentage;
            tp_orders.Add(tp);
        }
        return tp_orders;
    }


    /// <summary>
    /// The body of a close signal, kept apart from the sending so it can be verified in a test.
    /// A price is deliberately absent: with `order_type` market Altrady refuses the combination.
    /// </summary>
    internal static JObject BuildClosePayload(CryptoPosition position, string exchangeCode, string apiKey, string apiSecret)
    {
        dynamic request = new JObject();
        request.action = "close";
        request.api_key = apiKey;
        request.api_secret = apiSecret;
        request.exchange = exchangeCode;
        request.symbol = $"{exchangeCode}_{position.Symbol.Quote}_{position.Symbol.Base}";
        if (position.Side == Enums.CryptoTradeSide.Long)
            request.side = "long";
        else
            request.side = "short";
        request.order_type = "market";
        if (!string.IsNullOrEmpty(position.AltradyPositionId))
            request.signal_id = position.AltradyPositionId;
        return request;
    }


    /// <summary>
    /// Close the position Altrady opened for us, because our own administration is finished with it.
    /// The scanner owns the exit - stop, target, trailing stop, timeout - and Altrady only executes.
    /// Their webhook has no action that moves a stop of a running position, so a close signal is the
    /// only way to keep both sides in step while our own stop travels.
    /// <para>
    /// `order_type` is required on a close. Market is a pseudo market order at Altrady (a limit ten
    /// percent away from the price) and must NOT be combined with a price. The signal id is the id
    /// Altrady generated for the opening signal, which is exactly what is stored in
    /// <see cref="CryptoPosition.AltradyPositionId"/>; with one position per market it is not
    /// strictly needed, but it makes the signal address one position instead of every position for
    /// this market and side.
    /// </para>
    /// </summary>
    private static async Task<bool> CloseAtAltradyAsync(CryptoPosition position, string url, CryptoExternalUrls externalUrls)
    {
        dynamic request = BuildClosePayload(position, externalUrls.Altrady!.Code!,
            GlobalData.AltradyApi.Key, GlobalData.AltradyApi.Secret);

        (int statusCode, string result) = await PostSignalAsync(position, url, (JObject)request);

        // A close answers with the position it closed; a refusal answers with an error member. There
        // is no signal id to check against, because a close does not create a new signal.
        //bool accepted = TryParse(result) != null && !result.Contains("\"error\"");
        // Measured 27-09-2026: every close came back with an empty body, which the line above counted
        // as a refusal. The http status is what tells them apart (see IsCloseAccepted).
        bool accepted = IsCloseAccepted(statusCode, result);
        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook close result status={statusCode} {result}");
        ScannerLog.Logger.Trace($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook close result status={statusCode} {result}");
        // No telegram message on a refused close, unlike a refused open. Altrady runs the same stop
        // and the same target as we do, so in the normal case their position is already gone by the
        // time our administration closes and the close signal finds nothing left to do. That is not
        // worth a message per position; the error tab keeps it for when it IS something else.
        if (!accepted)
            GlobalData.AddErrorToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady refused the close: status={statusCode} {result}");
        return accepted;
    }


    /// <summary>
    /// Send the position to the Altrady webhook. Returns true only when Altrady answered with a signal id,
    /// which is the single proof that the position was really opened on their side. Anything else (no api
    /// keys, no exchange code, a product the webhook cannot express, an http error, or an answer without a
    /// signal id such as {"error":"Too many positions opened"}) returns false, so the caller can undo its
    /// own administration instead of running a position without a counterpart.
    /// </summary>
    public static async Task<bool> DelegateControlToAltradyAsync(CryptoPosition position, string url = "", string command = "open")
    {
        if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} unable to send to Altrady webhook, no api key's available");
            return false;
        }

        if (url == "")
            url = "https://api.altrady.com/v2/signal_bot_positions";


        //GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.Interval!.Name} send to Altrady webhook"); //  LastTradeDate={position.Symbol.LastTradeDate}

        try
        {
            GlobalData.ExternalUrls.GetExternalRef(position.Symbol.Exchange, out CryptoExternalUrls? externalUrls);
            if (externalUrls == null || externalUrls.Altrady == null || externalUrls.Altrady!.Code == "")
            {
                GlobalData.AddErrorToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} no exchange code available");
                return false;
            }

            // A close is a handful of fields and none of the entry settings below apply to it. The
            // product check further down is skipped on purpose: a position can only carry an Altrady
            // id when the opening signal already passed that check.
            if (command == "close")
                return await CloseAtAltradyAsync(position, url, externalUrls);


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

            // The webhook symbol format below is Code_Quote_Base, which can only express a spot or
            // regular perpetual market. An X-Perp (dated contract) or a deployed market shares its
            // base+quote with another instrument, so sending Code_Quote_Base would open the WRONG
            // contract at Altrady. Refuse loudly instead of trading the wrong market.
            string product = position.Symbol.Product;
            if (product != "" && product != CryptoProduct.Spot && product != CryptoProduct.Perpetual)
            {
                GlobalData.AddErrorToLogTab($"error webhook {position.Symbol.Name} {position.Interval!.Name} " +
                    $"product {product} cannot be expressed in the Altrady webhook symbol format, position not delegated");
                return false;
            }

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

            // A market entry is sized in BASE. On HyperLiquid Perpetual (05-09-2026) every long sent
            // with quote_amount was refused with "Market order doesn't support quote currency", while
            // the shorts of the same day went through. Altrady's own help page on webhook errors says
            // the exchange then accepts only one of the two size fields and the other one has to be
            // used. A limit entry keeps quote_amount, which has never been refused.
            decimal? baseAmount = null;
            if (GlobalData.Settings.Trading.EntryOrderType == Enums.CryptoOrderType.Market &&
                position.EntryAmount.HasValue && position.EntryPrice.HasValue)
            {
                baseAmount = CalculateBaseAmount(position.Symbol, position.EntryAmount.Value, position.EntryPrice.Value);
            }

            if (baseAmount.HasValue)
                request.base_amount = baseAmount.Value;
            else
                request.quote_amount = position.EntryAmount;

            // TP body (multiple). A per-signal TP override collapses this to a single TP; see EffectiveTpList.
            var tpList = Trader.TradeTools.EffectiveTpList(position);
            if (tpList.Count > 0)
                request.take_profit = BuildTakeProfitBlock(tpList, GlobalData.Settings.Trading.TakeProfitTrailPercentage);


            // DCA body (multiple)
            // When the strategy provides a signal SL, skip DCA levels that fall beyond it — those
            // would never fill because the SL triggers first.
            decimal stopLossPercentage = 0;
            if (GlobalData.Settings.Trading.DcaList.Count > 0)
            {
                dynamic dca_orders = new JArray();
                request.dca_orders = dca_orders;

                foreach (var dcaItem in GlobalData.Settings.Trading.DcaList)
                {
                    if (position.SlPercentage.HasValue && dcaItem.Percentage >= position.SlPercentage.Value)
                        continue;

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
            // When the strategy provides its own SL percentage, use it directly (measured from entry).
            // DCAs beyond this SL are already filtered out of the dca_orders array above, so the SL
            // is always on the correct side of all placed DCAs.
            decimal? initialStopPercentage = null;
            if (position.SlPercentage is decimal slPercentage)
            {
                initialStopPercentage = slPercentage;
            }
            else if (GlobalData.Settings.Trading.StopLossPercentage > 0)
            {
                //dynamic stop_loss = new JObject();
                //request.stop_loss = stop_loss;
                //stop_loss.stop_percentage = GlobalData.Settings.Trading.StopLossPercentage;
                //stop_loss.cool_down_amount = 0;
                //stop_loss.cool_down_time_frame = "minute";
                initialStopPercentage = stopLossPercentage + GlobalData.Settings.Trading.StopLossPercentage;
            }

            if (initialStopPercentage.HasValue)
            {
                request.stop_loss = BuildStopLossBlock(initialStopPercentage.Value,
                    GlobalData.Settings.Trading.MoveSlToBreakEven,
                    GlobalData.Settings.Trading.MoveSlToBreakEvenMethod,
                    GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage,
                    GlobalData.Settings.Trading.MoveSlToBreakEvenTrailPercentage,
                    tpList.Count);
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
            var expiryTpList = Trader.TradeTools.EffectiveTpList(position);
            if (position.EntryPrice.HasValue && expiryTpList.Count > 0)
            {
                decimal tpPercentage = expiryTpList[0].Percentage;
                if (position.Side == Enums.CryptoTradeSide.Long)
                    expiryPrice = position.EntryPrice.Value * (1 + tpPercentage / 100m);
                else
                    expiryPrice = position.EntryPrice.Value * (1 - tpPercentage / 100m);

                // Put it on the tick grid the same way every other price we calculate is put there,
                // so the level in Altrady is the level in our own administration. Measured 25-09-2026:
                // without this the webhook sent 0.585015 for a symbol with a tick of 0.0001 and
                // 133.07425 for one with a tick of 0.01. Altrady never places this as an order - it
                // compares it to the price itself - so this changes the number, not the behaviour.
                expiryPrice = expiryPrice.Value.ClampPrice(position.Side, position.Symbol.PriceMinimum,
                    position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
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
            (_, string result) = await PostSignalAsync(position, url, (JObject)request);
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
            GlobalData.AddTextToTelegram($"{position.Symbol.Name} {position.Interval.Name} Altrady webhook {position.Side} price={position.EntryPrice}", position, CryptoTelegramCategory.OrderPlaced);

            // The signal id is the only proof that Altrady really opened the position. An answer like
            // {"error":"Too many positions opened"} parses into an empty object, so AltradyPositionId stays
            // null. Until now that was only the INFO line above and the paper position lived on without a
            // counterpart at Altrady.
            bool accepted = !string.IsNullOrEmpty(position.AltradyPositionId);
            if (!accepted)
            {
                GlobalData.AddErrorToLogTab($"{position.Symbol.Name} {position.Interval.Name} Altrady refused the position: {result}");
                GlobalData.AddTextToTelegram($"{position.Symbol.Name} {position.Interval.Name} Altrady refused the position: {result}",
                    position, CryptoTelegramCategory.OrderPlaced);
            }
            return accepted;
        }
        catch (HttpRequestException error)
        {
            ScannerLog.Logger.Error(error);

            string errorMessage = $"HTTP error: {error.Message}";
            if (error.StatusCode.HasValue)
            {
                errorMessage += $" (Status: {error.StatusCode})";
            }

            GlobalData.AddErrorToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook error {errorMessage}");
            return false;
        }
        catch (TaskCanceledException error)
        {
            ScannerLog.Logger.Error(error);
            GlobalData.AddErrorToLogTab($"{position.Symbol.Name} {position.Interval!.Name} Altrady webhook timeout: {error.Message}");
            return false;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error);
            GlobalData.AddErrorToLogTab($" {position.Symbol.Name} {position.Interval!.Name} Webhook error:error={error}");
            return false;
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
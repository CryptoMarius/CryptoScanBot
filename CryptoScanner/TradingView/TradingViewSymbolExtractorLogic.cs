using CryptoScanner.Core.Core;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.TradingView;

// Think this can be simplified further, but for now this works

public class TradingViewJsonRootObject
{
    public required string M { get; set; }
    public required List<object> P { get; set; }
}

public class TradingViewJsonPayloadObject
{
    public required string N { get; set; }
    public required string S { get; set; }
    public required object V { get; set; }
}

public class TradingViewMarketStatusObject
{
    public required string Phase { get; set; }
    public required string Tradingday { get; set; }
}

public static class TradingViewJsonParser
{
    public static JsonDocument? TryParse(string message)
    {
        try
        {
            if (!message.StartsWith("{\"m"))
                return null;

            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<TradingViewJsonRootObject>(message, options);
            var p = root?.P[1].ToString() ?? "";
            if (root?.M != "qsd")
                return null;

            var branch = JsonSerializer.Deserialize<TradingViewJsonPayloadObject>(p, options);

            return JsonDocument.Parse(branch?.V?.ToString() ?? "");
        }
        catch (Exception e)
        {
            ScannerLog.Logger.Error(e, "");
            return null;
        }
    }

}


public class TradingViewSymbolWebSocket(string tickerName)
{
    public delegate void DataFetchedEvent(object? sender, List<string> e);

    // Based on https://github.com/mli2805/WebListener/tree/master/BalisStandard/Pollers/TradingView
    // https://github.com/mli2805/WebListener/blob/master/BalisStandard/Pollers/TradingView/TikerExt.cs
    // informatief: https://github.com/Hattorius/Tradingview-ticker/blob/25d952a3b9c309cb8cc4c914a5e62cec2d8b53af/ticker.py
    // authentication ea: https://github.com/0xrushi/tradingview-scraper
    // Meer commando's: https://github.com/0xrushi/tradingview-scraper/issues/1
    // https://stackoverflow.com/questions/65741117/protocol-error-when-connecting-to-websocket-in-nodejs
    // https://stackoverflow.com/questions/63624043/web-scraping-an-interactive-chart

    private readonly string TickerName = tickerName;
    private readonly ClientWebSocket ClientWebSocket = new();
    private readonly CancellationTokenSource CancellationTokenSource = new();
    public event DataFetchedEvent? DataFetched;

    private static string ConstructRequest(string method, List<string> parameters, List<string> flags)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append('{');
        {
            // method
            stringBuilder.Append('"');
            stringBuilder.Append('m');
            stringBuilder.Append('"');
            stringBuilder.Append(':');
            stringBuilder.Append('"');
            stringBuilder.Append(method);
            stringBuilder.Append('"');


            // parameters
            stringBuilder.Append(',');
            stringBuilder.Append('"');
            stringBuilder.Append('p');
            stringBuilder.Append('"');
            stringBuilder.Append(':');
            {
                stringBuilder.Append('[');
                {
                    int count = 0;
                    foreach (string parameter in parameters)
                    {
                        if (count > 0)
                            stringBuilder.Append(',');
                        count++;

                        stringBuilder.Append('"');
                        stringBuilder.Append(parameter);
                        stringBuilder.Append('"');
                    }
                }

                // Hier is iets te optimaliseren (als het eerst maar werkt)
                // "quote_add_symbols",[session, "NASDAQ:AAPL", {"flags":["force_permission"]}]
                if (flags.Count > 0)
                {
                    stringBuilder.Append(',');
                    stringBuilder.Append('{');
                    {
                        stringBuilder.Append('"');
                        stringBuilder.Append("flags");
                        stringBuilder.Append('"');
                        stringBuilder.Append(':');

                        stringBuilder.Append('[');
                        {
                            int count = 0;
                            foreach (string flag in flags)
                            {
                                if (count > 0)
                                    stringBuilder.Append(',');
                                count++;

                                stringBuilder.Append('"');
                                stringBuilder.Append(flag);
                                stringBuilder.Append('"');
                            }
                        }
                        stringBuilder.Append(']');
                    }
                    stringBuilder.Append('}');
                }
                stringBuilder.Append(']');
            }
        }
        stringBuilder.Append('}');

        return stringBuilder.ToString();
    }


    public async Task SendData(string request)
    {
        request = $"~m~{request.Length}~m~{request}";
        //GlobalData.AddTextToLogTab(request);
        //GlobalData.AddTextToLogTab($"TradingView {TickerName} send: {request}");
        var bytes = Encoding.UTF8.GetBytes(request);
        ArraySegment<byte> data = new(bytes, 0, bytes.Length);

        // Guard: the underlying ClientWebSocket may already be Aborted / Closed / disposed —
        // happens when the connection dropped, ConnectAsync timed out, or the extractor
        // abandoned this instance for a reconnect. Sending on it throws ObjectDisposedException
        // and pollutes the log. State == Open is the only state in which SendAsync is valid.
        if (ClientWebSocket.State != WebSocketState.Open)
            return;

        try
        {
            await ClientWebSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationTokenSource.Token);
        }
        catch (ObjectDisposedException)
        {
            // Race: socket transitioned to disposed between the State check above and SendAsync.
            // Not worth logging as ERROR — the next reconnect cycle will pick it up.
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path — token signaled while SendAsync was in flight.
        }
        catch (Exception e)
        {
            GlobalData.AddTextToLogTab($@"Exception {e.Message}");
            ScannerLog.Logger.Error(e, e.Message);
        }
    }

    public async Task ConnectWebSocketAndRequestSession()
    {
        ClientWebSocket.Options.UseDefaultCredentials = true;
        ClientWebSocket.Options.SetRequestHeader("Origin", "https://www.tradingview.com");
        try
        {
            Uri uri = new("wss://data.tradingview.com/socket.io/websocket");
            //https://www.tradingview.com/chart/C0G0Mzob/?symbol=TVC%3ADXY&interval=60
            //GlobalData.AddTextToLogTab($"TradingView {TickerName} connecting...");
            await ClientWebSocket.ConnectAsync(uri, CancellationTokenSource.Token);
            //GlobalData.AddTextToLogTab($"TradingView {TickerName} connected, state={ClientWebSocket.State}");

            //string request = ConstructRequest("chart_create_session", ["my_chartsession", ""], []);
            //await SendData(request);

            // Reverted to the 2.1.8 working setup: quote_create_session with trailing empty string,
            // no set_auth_token, no quote_set_fields.
            // set_auth_token with "unauthorized_user_token" blocks TVC: and SP: symbols.
            string request = ConstructRequest("quote_create_session", ["my_session", ""], []);
            await SendData(request);

            //request = ConstructRequest("set_auth_token", ["unauthorized_user_token"], []);
            //await SendData(request);

            //request = ConstructRequest("set_data_quality", ["low"], []);
            //await SendData(request);
        }
        catch (Exception e)
        {
            GlobalData.AddTextToLogTab($"TradingView {TickerName} connect exception: {e.Message}");
            ScannerLog.Logger.Error(e, e.Message);
        }
    }


    public async Task RequestData()
    {
        // TVC:DXY and similar public index symbols do not need any flags.
        // Passing {"flags":["force_permission"]} causes a critical_error: invalid_parameters.
        string request = ConstructRequest("quote_add_symbols", ["my_session", TickerName], []);
        await SendData(request);
    }


    private string _remainsOfMessage = "";

    public async Task<bool> ReceiveData()
    {
        try
        {
            var receiveBuffer = new byte[30000];
            var arraySegment = new ArraySegment<byte>(receiveBuffer);
            WebSocketReceiveResult result = await ClientWebSocket.ReceiveAsync(arraySegment, CancellationTokenSource.Token);

            if (arraySegment.Array != null && (result.Count != 0 || result.CloseStatus == WebSocketCloseStatus.Empty))
            {
                string message = Encoding.ASCII.GetString(arraySegment.Array, arraySegment.Offset, result.Count);
                //GlobalData.AddTextToLogTab($"TradingView {TickerName} received ({result.Count} bytes): {message}");
                _remainsOfMessage = ParseSocketData(_remainsOfMessage + message, out List<string> jsonList);
                //GlobalData.AddTextToLogTab($"TradingView {TickerName} parsed {jsonList.Count} parts, remains={_remainsOfMessage.Length} chars");
                OnCrossRateFetched(jsonList);
            }
            else
            {
                //GlobalData.AddTextToLogTab($"TradingView {TickerName} receive: count={result.Count} closeStatus={result.CloseStatus} state={ClientWebSocket.State}");
            }

            //if (ClientWebSocket.State == WebSocketState.CloseReceived)
            //    GlobalData.AddTextToLogTab($"TradingView {TickerName} websocket closed by server: {result.CloseStatusDescription}");

            return ClientWebSocket.State != WebSocketState.CloseReceived;
        }
        catch (Exception)
        {
            //GlobalData.AddTextToLogTab($@"Exception {e.Message}");
            //ScannerLog.Logger.Error(e, "");
            //GlobalData.AddTextToLogTab($"TradingView {TickerName} receive exception: {e.Message}");
            //ScannerLog.Logger.Error(e, "");
            return false;
        }
    }


    protected virtual void OnCrossRateFetched(List<string> e)
    {
        DataFetched?.Invoke(this, e);
    }

    /// <summary>
    /// splits row data on portions preceded be ~m~ 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="jsonList"></param>
    /// <returns></returns>
    private static string ParseSocketData(string message, out List<string> jsonList)
    {
        jsonList = [];
        try
        {
            //GlobalData.AddTextToLogTab(message);
            while (message.Length > 3)
            {
                var str = message[3..];
                var pos = str.IndexOf("~m~", StringComparison.InvariantCulture);
                var lengthStr = str[..pos];
                var length = int.Parse(lengthStr);

                if (message.Length >= length + 3 + 3 + lengthStr.Length)
                {
                    var jsonStr = str.Substring(3 + lengthStr.Length, length);
                    jsonList.Add(jsonStr);
                    message = str[(length + 3 + lengthStr.Length)..];
                    if (message == "")
                        return "";
                }
                else
                {
                    return message;
                }
            }
            return message;
        }
        catch (Exception e)
        {
            ScannerLog.Logger.Error(e, e.Message);
            return "";
        }
    }
}
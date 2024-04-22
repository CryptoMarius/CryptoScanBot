using System.Net.WebSockets;
using System.Text;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.TradingView;

public delegate void DataFetchedEvent(object sender, List<string> e);

// Gebaseerd op https://github.com/mli2805/WebListener/tree/master/BalisStandard/Pollers/TradingView
// https://github.com/mli2805/WebListener/blob/master/BalisStandard/Pollers/TradingView/TikerExt.cs
// informatief: https://github.com/Hattorius/Tradingview-ticker/blob/25d952a3b9c309cb8cc4c914a5e62cec2d8b53af/ticker.py
// authentication ea: https://github.com/0xrushi/tradingview-scraper
// Meer commando's: https://github.com/0xrushi/tradingview-scraper/issues/1
// https://stackoverflow.com/questions/65741117/protocol-error-when-connecting-to-websocket-in-nodejs
// https://stackoverflow.com/questions/63624043/web-scraping-an-interactive-chart

public class TradingViewSymbolWebSocket(string tickerName)
{
    private readonly string TickerName = tickerName;
    private readonly ClientWebSocket ClientWebSocket = new();
    private readonly CancellationTokenSource CancellationTokenSource = new();
    public event DataFetchedEvent DataFetched;

    private static string ConstructRequest(string method, List<string> parameters, List<string> flags)
    {
        // bulky, maar beter te tarceren dan die stomme encoded strings van c# (nouja, voorkeur)

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
        var bytes = Encoding.UTF8.GetBytes(request);
        ArraySegment<byte> data = new(bytes, 0, bytes.Length);
        try
        {
            await ClientWebSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationTokenSource.Token);
        }
        catch (Exception)
        {
            //GlobalData.AddTextToLogTab($@"Exception {e.Message}");
            //ScannerLog.Logger.Error(e, "");
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
            await ClientWebSocket.ConnectAsync(uri, CancellationTokenSource.Token);

            //string request = ConstructRequest("chart_create_session", ["my_chartsession", ""], []);
            //await SendData(request);

            string request = ConstructRequest("quote_create_session", ["my_session", ""], []);
            await SendData(request);

            //request = ConstructRequest("set_auth_token", ["unauthorized_user_token"], []);
            //await SendData(request);

            //request = ConstructRequest("set_data_quality", ["low"], []);
            //await SendData(request);
        }
        catch (Exception)
        {
            //GlobalData.AddTextToLogTab($@"Exception {e.Message}");
            //ScannerLog.Logger.Error(e, "");
        }
    }


    public async Task RequestData()
    {
        string request = ConstructRequest("quote_add_symbols", ["my_session", TickerName], []); // "force_permission"
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
                _remainsOfMessage = ParseSocketData(_remainsOfMessage + message, out List<string> jsonList);
                OnCrossRateFetched(jsonList);
            }

            return ClientWebSocket.State != WebSocketState.CloseReceived;
        }
        catch (Exception)
        {
            //GlobalData.AddTextToLogTab($@"Exception {e.Message}");
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
        catch (Exception)
        {
            //ScannerLog.Logger.Error(e, "");
            return "";
        }
    }
}
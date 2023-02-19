using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TradingView
{
    public delegate void DataFetchedEvent(object sender, List<string> e);

    public class TradingViewSymbolWebSocket
    {
        private string TickerName;
        private ClientWebSocket ClientWebSocket;
        private CancellationTokenSource CancellationTokenSource;
        private const string TradingViewAddress = "wss://data.tradingview.com/socket.io/websocket";
        private const string SessionRequest = "~m~50~m~{\"p\":[\"my_session\",\"\"],\"m\":\"quote_create_session\"}";
        private readonly Uri _tradingViewUri;
        private readonly ArraySegment<byte> _buffer;
        public event DataFetchedEvent DataFetched;

        public TradingViewSymbolWebSocket(string tickerName)
        {
            TickerName = tickerName;

            _tradingViewUri = new Uri(TradingViewAddress);
            var encoded = Encoding.UTF8.GetBytes(SessionRequest);
            _buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);

            ClientWebSocket = new ClientWebSocket();
            ClientWebSocket.Options.UseDefaultCredentials = true;
            ClientWebSocket.Options.SetRequestHeader("Origin", "https://www.tradingview.com");
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ConnectWebSocketAndRequestSession()
        {
            try
            {
                await ClientWebSocket.ConnectAsync(_tradingViewUri, CancellationTokenSource.Token);
                await ClientWebSocket.SendAsync(_buffer, WebSocketMessageType.Text, true, CancellationTokenSource.Token);

            }
            catch (Exception e)
            {
                Console.WriteLine($@"Exception {e.Message}");
            }
        }

      
        public async Task RequestData()
        {
            string request = "{\"p\":[\"my_session\",\"" + TickerName + "\",{\"flags\":[\"force_permission\"]}],\"m\":\"quote_add_symbols\"}";
            var framed = $"~m~{request.Length}~m~{request}";
            var bytes = Encoding.UTF8.GetBytes(framed);
            ArraySegment<byte> data = new ArraySegment<byte>(bytes, 0, bytes.Length);
            
            try
            {
                await ClientWebSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine($@"Exception {e.Message}");
            }
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
            catch (Exception e)
            {
                Console.WriteLine($"Exception {e.Message}");
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
        private string ParseSocketData(string message, out List<string> jsonList)
        {
            jsonList = new List<string>();
            try
            {
                while (message.Length > 3)
                {
                    var str = message.Substring(3);
                    var pos = str.IndexOf("~m~", StringComparison.InvariantCulture);
                    var lengthStr = str.Substring(0, pos);
                    var length = int.Parse(lengthStr);

                    if (message.Length >= length + 3 + 3 + lengthStr.Length)
                    {
                        var jsonStr = str.Substring(3 + lengthStr.Length, length);
                        jsonList.Add(jsonStr);
                        message = str.Substring(length + 3 + lengthStr.Length);
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
                return "";
            }
        }
    }
}
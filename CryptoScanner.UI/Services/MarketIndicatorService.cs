using CryptoScanner.UI.ViewModels;

using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.UI.Services;

public class MarketIndicatorService : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly List<Task> _runningTasks = [];

    public List<MarketIndicatorInfo> Indicators { get; } =
    [
        new() { Name = "Market Cap Total", TvSymbol = "CRYPTOCAP:TOTAL3", DisplayFormat = "N2", BigPrice = true },
        new() { Name = "US Dollar Index", TvSymbol = "TVC:DXY", DisplayFormat = "N2", BigPrice = true },
        new() { Name = "S&P 500", TvSymbol = "SP:SPX", DisplayFormat = "N2", BigPrice = true },
        new() { Name = "BTC Dominance", TvSymbol = "CRYPTOCAP:BTC.D", DisplayFormat = "N2" },
        new() { Name = "Fear and Greed index", IsFearAndGreed = true, DisplayFormat = "N2" },
    ];

    public event Action? IndicatorsChanged;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        int delay = 250;
        _runningTasks.Clear();
        foreach (var indicator in Indicators)
        {
            int startDelay = delay;
            if (indicator.IsFearAndGreed)
                _runningTasks.Add(Task.Run(() => PollFearAndGreedAsync(indicator, token), token));
            else
                _runningTasks.Add(Task.Run(() => RunTradingViewAsync(indicator, startDelay, token), token));
            delay += 500;
        }
    }

    private async Task RunTradingViewAsync(MarketIndicatorInfo indicator, int startDelay, CancellationToken ct)
    {
        await Task.Delay(startDelay, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Origin", "https://www.tradingview.com");
                await ws.ConnectAsync(new Uri("wss://data.tradingview.com/socket.io/websocket"), ct);

                await SendTvMessage(ws, BuildRequest("quote_create_session", ["my_session", ""]), ct);
                await SendTvMessage(ws, BuildRequest("quote_add_symbols", ["my_session", indicator.TvSymbol!]), ct);

                string remains = "";
                while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    var buffer = new byte[16384];
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.Count == 0 || result.CloseStatus.HasValue)
                        break;

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    remains = ParseMessages(remains + message, indicator);

                    IndicatorsChanged?.Invoke();
                    await Task.Delay(1000, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch
            {
            }

            if (!ct.IsCancellationRequested)
                await Task.Delay(5000, ct);
        }
    }

    private async Task PollFearAndGreedAsync(MarketIndicatorInfo indicator, CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        using var http = new HttpClient();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var data = await http.GetFromJsonAsync<FGResponse>("https://api.alternative.me/fng/", ct);

                // Read into a local: the null test and the parse were two separate lookups into
                // the array, so the compiler could not tie them together. Invariant culture
                // because the value comes from an api, not from the user's regional settings.
                string? value = data?.Data?.Length > 0 ? data.Data[0].Value : null;
                if (!string.IsNullOrEmpty(value)
                    && decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal val))
                {
                    UpdateIndicator(indicator, val, 0);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }

            await Task.Delay(120_000, ct);
        }
    }

    private void UpdateIndicator(MarketIndicatorInfo indicator, decimal price, double volume)
    {
        string text = indicator.BigPrice
            ? ColorHelper.GetLargeVolumeText(price)
            : price.ToString(indicator.DisplayFormat);
        if (text != indicator.PriceText)
        {
            string css = price > indicator.LastPrice ? "text-green"
                       : price < indicator.LastPrice ? "text-red"
                       : indicator.ColorClass;
            indicator.LastPrice = price;
            indicator.PriceText = text;
            indicator.ColorClass = css;
            IndicatorsChanged?.Invoke();
        }
    }

    private string ParseMessages(string data, MarketIndicatorInfo indicator)
    {
        try
        {
            while (data.Length > 3)
            {
                var str = data[3..];
                int pos = str.IndexOf("~m~", StringComparison.Ordinal);
                if (pos < 0) return data;

                string lengthStr = str[..pos];
                if (!int.TryParse(lengthStr, out int length))
                    return "";

                if (data.Length < length + 3 + 3 + lengthStr.Length)
                    return data;

                string json = str.Substring(3 + lengthStr.Length, length);
                TryExtractPrice(json, indicator);
                data = str[(length + 3 + lengthStr.Length)..];
                if (data == "") return "";
            }
            return data;
        }
        catch { return ""; }
    }

    private void TryExtractPrice(string json, MarketIndicatorInfo indicator)
    {
        try
        {
            if (!json.StartsWith("{\"m")) return;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("m", out var m) || m.GetString() != "qsd")
                return;

            if (!root.TryGetProperty("p", out var p) || p.GetArrayLength() < 2)
                return;

            using var vDoc = JsonDocument.Parse(p[1].GetProperty("v").GetRawText());
            var v = vDoc.RootElement;

            if (v.TryGetProperty("lp", out var lp) && lp.TryGetDecimal(out decimal price))
            {
                double volume = 0;
                if (v.TryGetProperty("volume", out var vol) && vol.TryGetDouble(out double volVal))
                    volume = volVal;
                UpdateIndicator(indicator, price, volume);
            }
        }
        catch { }
    }

    private static string BuildRequest(string method, List<string> parameters)
    {
        var sb = new StringBuilder();
        sb.Append("{\"m\":\"").Append(method).Append("\",\"p\":[");
        for (int i = 0; i < parameters.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(parameters[i]).Append('"');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static async Task SendTvMessage(ClientWebSocket ws, string request, CancellationToken ct)
    {
        string framed = $"~m~{request.Length}~m~{request}";
        var bytes = Encoding.UTF8.GetBytes(framed);
        if (ws.State == WebSocketState.Open)
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public void Dispose()
    {
        _cts?.Cancel();

        // Give the websocket loops a moment to observe the cancellation and close cleanly
        try
        {
            Task.WhenAll(_runningTasks).Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        _runningTasks.Clear();

        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class FGResponse
    {
        public FGData[]? Data { get; set; }
    }

    private class FGData
    {
        public string? Value { get; set; }
    }
}

public class MarketIndicatorInfo
{
    public string Name { get; init; } = "";
    public string? TvSymbol { get; init; }
    public bool IsFearAndGreed { get; init; }
    public string DisplayFormat { get; init; } = "N2";
    public bool BigPrice { get; init; }
    public string PriceText { get; set; } = "-";
    public string ColorClass { get; set; } = "";
    public decimal LastPrice { get; set; }

    /// <summary>
    /// The page opened when the row is clicked, same targets as the Avalonia dashboard
    /// (DashboardSymbolViewModel.GetUrl).
    /// </summary>
    public string GetUrl()
    {
        if (IsFearAndGreed)
            return "https://alternative.me/crypto/fear-and-greed-index/";
        if (!string.IsNullOrEmpty(TvSymbol))
            return $"https://www.tradingview.com/chart/?symbol={TvSymbol}&interval=60";
        return "";
    }
}

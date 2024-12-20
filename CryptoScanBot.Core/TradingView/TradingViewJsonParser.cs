using CryptoScanBot.Core.Core;

using System.Text.Json;

namespace CryptoScanBot.Core.TradingView;

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

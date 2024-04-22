using System.Text.Json;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.TradingView;

public static class TradingViewJsonParser
{
    public static JsonDocument TryParse(string message)
    {
        try
        {
            if (!message.StartsWith("{\"m"))
                return null;

            var root = JsonSerializer.Deserialize<TradingViewJsonRootObject>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var p = root.P[1].ToString();
            if (root.M != "qsd")
                return null;

            var branch = JsonSerializer.Deserialize<TradingViewJsonPayloadObject>(p, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return JsonDocument.Parse(branch.V.ToString());
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
    public string M { get; set; }
    public List<object> P { get; set; }
}

public class TradingViewJsonPayloadObject
{
    public string N { get; set; }
    public string S { get; set; }
    public object V { get; set; }

}

public class TradingViewMarketStatusObject
{
    public string Phase { get; set; }
    public string Tradingday { get; set; }

}

using System.Net.Http.Json;


namespace CryptoScanner.DashBoard.TradingView;

public class FGIndex
{
    public FGIndexData[] Data { get; set; } = [];
}

public class FGIndexData
{
    public string? Value { get; set; }
}

public class FearAndGreedSymbolInfo
{
    private TickerData? SymbolValue;
    private readonly HttpClient httpClient = new();

    public async void StartAsync(string url, string displayName, string displayFormat,
        TickerData symbolValue, int startDelayMs = 250, int loopDelayMs = 6000,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(startDelayMs, cancellationToken);
        SymbolValue = symbolValue;
        SymbolValue.Url = url;
        SymbolValue = symbolValue;
        SymbolValue.Name = displayName;
        SymbolValue.DisplayFormat = displayFormat;

        // Okay, dit kan allemaal veel mooier, maar zo is voorlopig ook wel even goed
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                //// De Fear and Greed index (elke 24 uur een nieuwe waarde)
                ///{
                //           "name": "Fear and Greed Index",
                //"data": [

                //    {
                //      "value": "53",
                //		"value_classification": "Neutral",
                //		"timestamp": "1674345600",
                //		"time_until_update": "29260"

                //    }
                //    ],
                //    "metadata": {
                //    "error": null

                //    }
                //}

                if (SymbolValue.LastCheck == null || DateTime.UtcNow >= SymbolValue.LastCheck)
                {
                    var jsonData = await httpClient.GetFromJsonAsync<FGIndex>("https://api.alternative.me/fng/", cancellationToken);
                    string value = jsonData?.Data[0].Value ?? "";
                    //FearAndGreedIndex = jsonData["data"][0]["value"].Value<string>();
                    SymbolValue.Lp = decimal.Parse(value);
                    SymbolValue.LastCheck = DateTime.UtcNow.AddHours(1); // = Next check
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation - exit gracefully
                break;
            }
            catch
            {
                //FearAndGreedIndex = "Connection-Error"; // jammer..
                //GlobalData.FearAndGreedIndex.LastValue = decimal.Parse(FearAndGreedIndex);
            }

            try
            {
                await Task.Delay(loopDelayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation - exit gracefully
                break;
            }
        }
    }
}

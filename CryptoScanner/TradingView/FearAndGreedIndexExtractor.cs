using Avalonia.Threading;

using System.Net.Http.Json;

namespace CryptoScanner.TradingView;


public class FearAndGreedIndexExtractor
{
    internal class FGIndex
    {
        public FGIndexData[] Data { get; set; } = [];
    }

    internal class FGIndexData
    {
        public string? Value { get; set; }
    }


    public static async void StartAsync(string url, string displayName,
        Action<decimal, decimal> onDataReceived,
        int startDelayMs = 250, int loopDelayMs = 6000,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(startDelayMs, cancellationToken);

        DateTime? _lastCheck = null;


        using HttpClient httpClient = new();
        {
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

                    if (_lastCheck == null || DateTime.UtcNow >= _lastCheck)
                    {
                        var jsonData = await httpClient.GetFromJsonAsync<FGIndex>("https://api.alternative.me/fng/", cancellationToken);
                        string value = jsonData?.Data[0].Value ?? "";
                        if (!string.IsNullOrEmpty(value))
                        {
                            //FearAndGreedIndex = jsonData["data"][0]["value"].Value<string>();
                            decimal lp = decimal.Parse(value);
                            _lastCheck = DateTime.UtcNow.AddMinutes(2); // = Next check
                                                                        //onDataReceived(_tickerData.Lp);
                            Dispatcher.UIThread.Post(() => onDataReceived(lp, 0));
                        }
                        else await Task.Delay(250, cancellationToken);
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
}
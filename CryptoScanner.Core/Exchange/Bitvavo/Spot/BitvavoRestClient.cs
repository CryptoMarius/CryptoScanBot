using CryptoScanner.Core.Core;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

/// <summary>
/// Minimal HTTP client for the Bitvavo REST API v2.
/// No official JKorf/.NET SDK exists for Bitvavo, so we call the REST API directly.
/// Docs: https://docs.bitvavo.com/
/// </summary>
public class BitvavoRestClient : IDisposable
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.bitvavo.com/v2";

    public BitvavoRestClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }


    /// <summary>
    /// Fetches OHLCV candles for a given market and interval.
    /// Returns candles sorted ascending (oldest first), or null on error.
    /// </summary>
    public async Task<List<BitvavoCandle>?> GetCandlesAsync(
        string market, string interval, DateTime startTime, DateTime endTime, int limit)
    {
        long startMs = new DateTimeOffset(startTime, TimeSpan.Zero).ToUnixTimeMilliseconds();
        long endMs = new DateTimeOffset(endTime, TimeSpan.Zero).ToUnixTimeMilliseconds();

        string url = $"{BaseUrl}/{market}/candles?interval={interval}&limit={limit}&start={startMs}&end={endMs}";

        string json;
        try
        {
            json = await _http.GetStringAsync(url);
        }
        catch (HttpRequestException ex)
        {
            throw new ExchangeException($"Bitvavo HTTP error fetching candles for {market}: {ex.Message}");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // API returns an error object on failure: {"errorCode":..., "error":"..."}
        if (root.ValueKind == JsonValueKind.Object)
        {
            var errorMsg = root.TryGetProperty("error", out var e) ? e.GetString() : json;
            throw new ExchangeException($"Bitvavo API error for {market}: {errorMsg}");
        }

        var candles = new List<BitvavoCandle>();
        foreach (var element in root.EnumerateArray())
        {
            // Each element is [timestamp_ms, "open", "high", "low", "close", "volume"]
            var arr = element.EnumerateArray().ToArray();
            if (arr.Length < 6)
                continue;

            candles.Add(new BitvavoCandle
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(arr[0].GetInt64()).UtcDateTime,
                Open = decimal.Parse(arr[1].GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
                High = decimal.Parse(arr[2].GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
                Low = decimal.Parse(arr[3].GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
                Close = decimal.Parse(arr[4].GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
                Volume = decimal.Parse(arr[5].GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
            });
        }
        return candles;
    }


    /// <summary>
    /// Fetches all available markets from Bitvavo.
    /// </summary>
    public async Task<List<BitvavoMarket>?> GetMarketsAsync()
    {
        string json;
        try
        {
            json = await _http.GetStringAsync($"{BaseUrl}/markets");
        }
        catch (HttpRequestException ex)
        {
            throw new ExchangeException($"Bitvavo HTTP error fetching markets: {ex.Message}");
        }

        return JsonSerializer.Deserialize<List<BitvavoMarket>>(json);
    }


    /// <summary>
    /// Fetches 24h ticker data for all markets from Bitvavo.
    /// </summary>
    public async Task<List<BitvavoTicker>?> GetTickersAsync()
    {
        string json;
        try
        {
            json = await _http.GetStringAsync($"{BaseUrl}/ticker/24h");
        }
        catch (HttpRequestException ex)
        {
            throw new ExchangeException($"Bitvavo HTTP error fetching tickers: {ex.Message}");
        }

        return JsonSerializer.Deserialize<List<BitvavoTicker>>(json);
    }


    public void Dispose() => _http.Dispose();
}


public class BitvavoCandle
{
    public DateTime OpenTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}


public class BitvavoTicker
{
    [JsonPropertyName("market")]
    public string Market { get; set; } = "";

    [JsonPropertyName("volumeQuote")]
    public string VolumeQuote { get; set; } = "0";
}


public class BitvavoMarket
{
    [JsonPropertyName("market")]
    public string Market { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("base")]
    public string Base { get; set; } = "";

    [JsonPropertyName("quote")]
    public string Quote { get; set; } = "";

    [JsonPropertyName("pricePrecision")]
    public int? PricePrecision { get; set; }

    [JsonPropertyName("minOrderInQuoteAsset")]
    public string MinOrderInQuoteAsset { get; set; } = "0";

    [JsonPropertyName("minOrderInBaseAsset")]
    public string MinOrderInBaseAsset { get; set; } = "0";
}

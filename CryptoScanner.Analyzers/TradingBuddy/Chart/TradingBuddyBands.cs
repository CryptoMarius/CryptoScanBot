using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.Analyzers.TradingBuddy.Chart;

/// <summary>
/// Read-only client for TradingBuddy's served "BABA" bands (scanner3.tradingbuddy.io
/// /api/v1/baba/bands). Used by the chart to overlay the vendor's own bands next to the scanner's
/// reimplementation. The bearer token is read automatically from the installed TradingBuddy desktop
/// app's localStorage (the user is logged in there); it is never entered manually.
///
/// The band width TradingBuddy serves is only ~58% reproducible offline (the midline VWMA(hlc3,50) is
/// exact, but a market-volatility driven widening is not), which is exactly why showing the vendor's
/// own values on the chart is useful. See E:\baba\FINDINGS.md.
///
/// Draw() is synchronous, so <see cref="GetCached"/> returns whatever is cached and kicks off a
/// background refresh when the entry is missing or stale; <see cref="BandsUpdated"/> then lets the
/// chart redraw once new data has arrived.
/// </summary>
public static class TradingBuddyBands
{
    private const string Host = "https://scanner3.tradingbuddy.io";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>Raised (on a background thread) after a fetch has updated the cache, so the chart can redraw.</summary>
    public static event Action? BandsUpdated;

    /// <summary>One symbol/interval's bands, keyed by candle open time in unix milliseconds.</summary>
    public sealed class BandSeries
    {
        public required Dictionary<long, double> Upper { get; init; }
        public required Dictionary<long, double> Lower { get; init; }
        public required Dictionary<long, double> Basis { get; init; }
    }

    private sealed record CacheEntry(DateTime FetchedUtc, BandSeries? Series);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();
    private static readonly ConcurrentDictionary<string, bool> InFlight = new();

    // Cached token so we do not re-read the leveldb files on every draw.
    private static string? _token;
    private static DateTime _tokenReadUtc = DateTime.MinValue;

    /// <summary>
    /// Returns the cached band series for the given symbol/timeframe, or null when nothing is cached yet.
    /// Triggers a background refresh when the cache is empty or older than <see cref="CacheTtl"/>.
    /// </summary>
    public static BandSeries? GetCached(CryptoSymbol symbol, CryptoInterval interval)
    {
        string? exchangeCode = ExchangeCode(symbol.Exchange);
        if (exchangeCode == null)
            return null;

        string tbSymbol = symbol.Name;               // e.g. BTCUSDT
        string tf = interval.Name;                   // e.g. 1h, 4h
        string key = $"{exchangeCode}/{tbSymbol}/{tf}";

        Cache.TryGetValue(key, out CacheEntry? entry);
        bool stale = entry == null || (DateTime.UtcNow - entry.FetchedUtc) > CacheTtl;
        if (stale && InFlight.TryAdd(key, true))
            _ = Task.Run(() => FetchAsync(key, exchangeCode, tbSymbol, tf));

        return entry?.Series;
    }

    private static async Task FetchAsync(string key, string exchangeCode, string symbol, string tf)
    {
        try
        {
            string? token = ReadToken();
            if (token == null)
            {
                Cache[key] = new CacheEntry(DateTime.UtcNow, null);
                return;
            }

            string url = $"{Host}/api/v1/baba/bands/{exchangeCode}/{symbol}/{tf}?limit=500";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new("Bearer", token);
            using HttpResponseMessage resp = await Http.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                Cache[key] = new CacheEntry(DateTime.UtcNow, null);
                return;
            }

            string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            BandSeries? series = Parse(json);
            Cache[key] = new CacheEntry(DateTime.UtcNow, series);
            if (series != null)
                BandsUpdated?.Invoke();
        }
        catch (Exception e)
        {
            GlobalData.AddTextToLogTab($"TradingBuddy bands fetch failed ({key}): {e.Message}");
            Cache[key] = new CacheEntry(DateTime.UtcNow, null);
        }
        finally
        {
            InFlight.TryRemove(key, out _);
        }
    }

    private static BandSeries? Parse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out JsonElement data))
            return null;

        static Dictionary<long, double> Read(JsonElement arr)
        {
            var d = new Dictionary<long, double>();
            foreach (JsonElement p in arr.EnumerateArray())
                d[p.GetProperty("timestamp").GetInt64()] = p.GetProperty("value").GetDouble();
            return d;
        }

        if (!data.TryGetProperty("upperBand", out JsonElement u) ||
            !data.TryGetProperty("lowerBand", out JsonElement l) ||
            !data.TryGetProperty("babaLine", out JsonElement b))
            return null;

        return new BandSeries { Upper = Read(u), Lower = Read(l), Basis = Read(b) };
    }

    /// <summary>
    /// Maps a scanner exchange to the TradingBuddy exchange code (e.g. Binance futures → "binance_futures").
    /// Returns null for exchanges TradingBuddy does not serve.
    /// </summary>
    private static string? ExchangeCode(ExchangeModel? exchange)
    {
        if (exchange == null)
            return null;

        // TradingBuddy only serves the spot variant for Bitvavo and HyperLiquid.
        bool futures = exchange.TradingType == CryptoTradingType.Futures;
        return exchange.ExchangeType switch
        {
            CryptoExchangeType.Binance => futures ? "binance_futures" : "binance_spot",
            CryptoExchangeType.Bybit => futures ? "bybit_futures" : "bybit_spot",
            CryptoExchangeType.Mexc => futures ? "mexc_futures" : "mexc_spot",
            CryptoExchangeType.Bitvavo => "bitvavo_spot",
            CryptoExchangeType.HyperLiquid => "hyperliquid_spot",
            _ => null,
        };
    }

    /// <summary>
    /// Reads the current bearer token from the TradingBuddy desktop app's localStorage. The token lives
    /// (as a JWT inside a JSON blob under the "tradingbuddy-auth" key) in the app's Chromium leveldb
    /// files. We scan the .log/.ldb bytes for JWTs and return the one with the latest, still-valid exp.
    /// Windows-only (the app is an Electron desktop app); returns null elsewhere or when not logged in.
    /// </summary>
    private static string? ReadToken()
    {
        if (_token != null && (DateTime.UtcNow - _tokenReadUtc) < TimeSpan.FromMinutes(5))
            return _token;

        try
        {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(roaming))
                return null;

            string dir = Path.Combine(roaming, "tradingbuddy-app", "Partitions", "tradingbuddy",
                                      "Local Storage", "leveldb");
            if (!Directory.Exists(dir))
                return null;

            var jwtRegex = new Regex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+");
            string? best = null;
            long bestExp = 0;
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (string file in Directory.EnumerateFiles(dir)
                         .Where(f => f.EndsWith(".log") || f.EndsWith(".ldb")))
            {
                byte[] bytes;
                try
                {
                    // The app keeps these files open; read with a permissive share mode.
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var ms = new MemoryStream();
                    fs.CopyTo(ms);
                    bytes = ms.ToArray();
                }
                catch
                {
                    continue;
                }

                // Treat each byte as a char so JWT substrings survive the binary framing.
                string text = string.Create(bytes.Length, bytes, static (span, src) =>
                {
                    for (int i = 0; i < src.Length; i++)
                        span[i] = (char)src[i];
                });

                foreach (Match m in jwtRegex.Matches(text))
                {
                    long exp = JwtExp(m.Value);
                    if (exp > nowUnix && exp > bestExp)
                    {
                        bestExp = exp;
                        best = m.Value;
                    }
                }
            }

            _token = best;
            _tokenReadUtc = DateTime.UtcNow;
            return best;
        }
        catch (Exception e)
        {
            GlobalData.AddTextToLogTab($"TradingBuddy token read failed: {e.Message}");
            return null;
        }
    }

    /// <summary>Decodes a JWT's "exp" (unix seconds) from its payload, or 0 when it cannot be read.</summary>
    private static long JwtExp(string jwt)
    {
        try
        {
            string[] parts = jwt.Split('.');
            if (parts.Length < 2)
                return 0;
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            byte[] raw = Convert.FromBase64String(payload);
            using JsonDocument doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty("exp", out JsonElement exp) ? exp.GetInt64() : 0;
        }
        catch
        {
            return 0;
        }
    }
}

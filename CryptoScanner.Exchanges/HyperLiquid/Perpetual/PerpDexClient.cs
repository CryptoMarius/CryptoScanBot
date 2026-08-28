using CryptoScanner.Core.Core;

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Perpetual;

/// <summary>
/// One market of a perpetual market that an outside party deployed on HyperLiquid, with everything
/// the symbol administration needs about it.
/// </summary>
/// <param name="Name">The full market name as HyperLiquid writes it, "xyz:GOLD".</param>
/// <param name="QuantityDecimals">szDecimals, a NUMBER of decimals and not a tick size.</param>
/// <param name="MarkPrice">Used to derive the price tick, the same way the own market does it.</param>
/// <param name="DayVolume">Notional volume over 24 hours, so already in the settlement currency.</param>
/// <param name="IsDelisted">Delisted markets are deactivated rather than stored.</param>
internal sealed record PerpDexMarket(
    string Name,
    int QuantityDecimals,
    decimal MarkPrice,
    decimal DayVolume,
    bool IsDelisted);


/// <summary>
/// The one call the HyperLiquid.Net package does not cover: the state of the markets that outside
/// parties deployed on HyperLiquid.
/// <para>
/// The package has GetPerpDexesAsync for the list of those markets and GetExchangeInfoAsync(dex) for
/// their instruments, but GetExchangeInfoAndTickersAsync - the call that carries the mark price and
/// the volume - takes no dex and always answers for HyperLiquid's own market. Without a volume every
/// one of these symbols would be stored at zero, drop below the volume boundary and have its candles
/// and subscriptions released; without a mark price there is nothing to derive the price tick from.
/// </para>
/// <para>
/// It is the same public endpoint the package uses, with the dex added to the body. No key and no
/// signature: POST /info is open. Should a later version of the package take a dex there, this whole
/// class can go and the caller can use the package instead.
/// </para>
/// </summary>
internal static class PerpDexClient
{
    // One HttpClient for the whole application, the same reasoning as in BitvavoRestClient: a client
    // per call leaves a socket in TIME_WAIT behind for every request. PooledConnectionLifetime keeps
    // a static client from holding a connection forever, so a changed address behind the host is
    // still picked up during a run of several days.
    private static readonly HttpClient _http = new(
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    private const string InfoUrl = "https://api.hyperliquid.xyz/info";


    /// <summary>
    /// Every market of one deployed perpetual market.
    /// <para>
    /// The answer of metaAndAssetCtxs is a pair: the first element holds the instruments, the second
    /// the state of each of them in the same order.
    /// </para>
    /// <para>
    /// An empty list on any failure, which is not fatal for the fetch as a whole: the caller keeps
    /// the markets it already has and this one deployed market delivers nothing this cycle.
    /// </para>
    /// </summary>
    /// <returns>
    /// The markets, and the untouched answer so the caller can store it beside the other exchange
    /// dumps. Without that raw text these markets appear nowhere on disk - symbols.json only holds
    /// HyperLiquid's own market - and a symbol like HYNA1000PEPEUSDC cannot be traced back to the
    /// instrument it came from.
    /// </returns>
    public static async Task<(List<PerpDexMarket> Markets, string? RawJson)> GetMarketsAsync(string dex)
    {
        List<PerpDexMarket> result = [];

        string json;
        try
        {
            // Same endpoint and the same budget as everything the package sends, so it is booked
            // there instead of riding on top of it. An ordinary info request weighs 20.
            // metaAndAssetCtxs is one of those - it is candleSnapshot that carries a surcharge per 60
            // items, and this call returns one object and not a list of candles.
            await LibraryRateLimit.SpendAsync(HyperLiquidLimits.GateName, HyperLiquidLimits.BaseAddress,
                HyperLiquidLimits.InfoPath, HyperLiquidLimits.InfoRequestWeight, ExchangeBase.CancellationToken);

            string body = JsonSerializer.Serialize(new { type = "metaAndAssetCtxs", dex });
            using StringContent content = new(body, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(InfoUrl, content, ExchangeBase.CancellationToken);
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync(ExchangeBase.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The session was stopped (exchange switch, standby, shutdown), that is not an error
            return (result, null);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"HyperLiquid: no data for the '{dex}' market, {error.Message}");
            return (result, null);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2)
            return (result, json);

        var universe = root[0].TryGetProperty("universe", out var u) ? u : default;
        var contexts = root[1];
        if (universe.ValueKind != JsonValueKind.Array || contexts.ValueKind != JsonValueKind.Array)
            return (result, json);

        int count = Math.Min(universe.GetArrayLength(), contexts.GetArrayLength());
        for (int i = 0; i < count; i++)
        {
            var market = universe[i];
            var state = contexts[i];

            if (!market.TryGetProperty("name", out var name) || name.GetString() is not string marketName)
                continue;

            bool delisted = market.TryGetProperty("isDelisted", out var d) && d.ValueKind == JsonValueKind.True;
            int decimals = market.TryGetProperty("szDecimals", out var sz) && sz.TryGetInt32(out int value) ? value : 0;

            result.Add(new PerpDexMarket(
                marketName,
                decimals,
                ReadDecimal(state, "markPx"),
                ReadDecimal(state, "dayNtlVlm"),
                delisted));
        }

        return (result, json);
    }


    /// <summary>
    /// HyperLiquid writes every number as a string, so the numbers have to be parsed rather than
    /// read. Invariant culture on purpose: the answer uses a point, whatever the machine does.
    /// </summary>
    private static decimal ReadDecimal(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) &&
            decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            return result;
        return 0m;
    }
}

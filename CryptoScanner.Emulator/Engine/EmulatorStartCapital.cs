using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Text.Json;

namespace CryptoScanner.Emulator.Engine;


/// <summary>
/// The money an emulator run starts with. Since 26-10-2026 that is the Default column of Paper
/// assets (Settings.Trading.PaperAssetDefaults) and nothing else: the run configuration and the queue
/// no longer carry a start capital or an asset-management switch of their own, because two places
/// for one setting is what put the HyperLiquid runs of Session1 on 2.000 USDC while they said 100.000.
/// <para>
/// What is left of the old fields is read here only, to translate them: a run configuration file, a
/// queue entry or a stored run from before that date said "StartCapital: X", which handed out X to
/// every traded quote coin when the default list was empty, and the default list itself when it was
/// filled. <see cref="FromLegacy"/> turns that into the default list it amounts to, so an old queue
/// entry still measures the same thing and an old run is still recognised as the same measurement.
/// </para>
/// </summary>
public static class EmulatorStartCapital
{
    /// <summary>
    /// What a run configuration or queue entry with the old "StartCapital" handed out: the amount for
    /// every traded quote coin (FetchCandles), the way PaperAssets.SeedStartBalances did with an empty
    /// default list.
    /// </summary>
    public static List<CryptoPaperAssetDefault> FromLegacy(decimal startCapital, IEnumerable<CryptoQuoteData> quotes)
    {
        if (startCapital <= 0)
            return [];
        return Normalise(quotes
            .Where(q => q.FetchCandles && !string.IsNullOrWhiteSpace(q.Name))
            .Select(q => new CryptoPaperAssetDefault { Name = q.Name, Total = startCapital }));
    }


    /// <summary>
    /// The default list the way the reset reads it: coins in capitals, rows without a coin or without
    /// an amount left out (they hand out nothing), one row per coin, sorted - so two lists that hand
    /// out the same money also look the same.
    /// </summary>
    public static List<CryptoPaperAssetDefault> Normalise(IEnumerable<CryptoPaperAssetDefault> defaults)
    {
        Dictionary<string, decimal> byName = new(StringComparer.Ordinal);
        foreach (CryptoPaperAssetDefault entry in defaults)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Total <= 0)
                continue;
            byName[entry.Name.Trim().ToUpperInvariant()] = entry.Total;
        }
        return [.. byName.OrderBy(e => e.Key, StringComparer.Ordinal)
            .Select(e => new CryptoPaperAssetDefault { Name = e.Key, Total = e.Value })];
    }


    /// <summary>
    /// The old "StartCapital" of a stored run configuration, or null when it has none (every run
    /// from 26-10-2026 on).
    /// </summary>
    public static decimal? LegacyStartCapital(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(configJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("StartCapital", out JsonElement value)
                && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal amount) && amount > 0)
                return amount;
        }
        catch (JsonException)
        {
        }
        return null;
    }


    /// <summary>
    /// What a stored run started with per quote coin, from its two blobs: the default list of its
    /// settings snapshot when that is filled, else the old StartCapital of its configuration for every
    /// quote coin, else nothing. Used by the capital check at the end of a run.
    /// </summary>
    public static Func<string, decimal> PerQuote(string? configJson, SettingsBasic? settings)
    {
        List<CryptoPaperAssetDefault> defaults = Normalise(settings?.Trading.PaperAssetDefaults ?? []);
        if (defaults.Count > 0)
        {
            Dictionary<string, decimal> byName = defaults.ToDictionary(d => d.Name, d => d.Total, StringComparer.OrdinalIgnoreCase);
            return quote => byName.TryGetValue(quote, out decimal amount) ? amount : 0m;
        }

        decimal legacy = LegacyStartCapital(configJson) ?? 0m;
        return _ => legacy;
    }
}

namespace CryptoScanner.Analyzers.Nwe;

/// <summary>
/// NWE envelope values for one candle, in both variants: the repainting one (used by SignalNwe and
/// SignalNweBb) and the non-repainting one (used by SignalNweNp).
/// <para>
/// Replaces six entries in the old string-keyed <c>CryptoData.Custom</c> dictionary. Typed fields
/// mean a typo is a compile error instead of a silently missing value, and the plugin can add
/// fields without touching Core.
/// </para>
/// </summary>
public sealed class NweCandleData
{
    public double? Center { get; set; }
    public double? Upper { get; set; }
    public double? Lower { get; set; }

    /// <summary>Non-repainting variant.</summary>
    public double? NpCenter { get; set; }
    public double? NpUpper { get; set; }
    public double? NpLower { get; set; }
}

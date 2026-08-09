namespace CryptoScanner.Analyzers.Vbs;

/// <summary>
/// VBS band values for one candle, computed once by <see cref="Indicators.VbsIndicatorExtension"/>
/// and shared by the long and short signal, the chart overlay and the stop-loss calculation — so a
/// candle with both sides active pays for the VWMA/ATR once, not twice.
/// <para>
/// Lives in the plugin, not in CryptoData: the engine attaches it through
/// <c>CryptoData.SetPluginData</c> without knowing what VBS is.
/// </para>
/// </summary>
public sealed class VbsCandleData
{
    /// <summary>VWMA(hlc3, Length) — the band centre.</summary>
    public double? Basis { get; set; }

    /// <summary>Basis + Mult * vwStdev.</summary>
    public double? Upper { get; set; }

    /// <summary>Basis - Mult * vwStdev.</summary>
    public double? Lower { get; set; }

    /// <summary>Volume-weighted stdev of hlc3, so a stop-loss can be expressed in vwStdev units.</summary>
    public double? VwStdev { get; set; }

    /// <summary>
    /// ACS (Average Candle Size) as a percentage: AcsFactor * SMA((high-low)/close, AcsLength) * 100.
    /// Drives the stop-loss (SL = entry -/+ Acs%), reverse-engineered from TradingBuddy.
    /// </summary>
    public double? Acs { get; set; }

    /// <summary>The slow ATR(Length) used for the older ATR-based stop-loss percentage.</summary>
    public double? AtrSl { get; set; }
}

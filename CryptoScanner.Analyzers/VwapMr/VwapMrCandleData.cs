namespace CryptoScanner.Analyzers.VwapMr;

/// <summary>
/// The VWAP band of one candle, computed once by <see cref="Indicators.VwapMrIndicatorExtension"/>
/// and shared by the long and short signal and the exit. Only attached when the day VWAP is
/// complete, so a missing slot means "no value", never "zero".
/// </summary>
public sealed class VwapMrCandleData
{
    /// <summary>The day VWAP of hlc3, from 00:00 UTC.</summary>
    public double Vwap { get; set; }

    /// <summary>Vwap + Mult * stdev(close, StdevLength).</summary>
    public double Upper { get; set; }

    /// <summary>Vwap - Mult * stdev(close, StdevLength).</summary>
    public double Lower { get; set; }
}

namespace CryptoScanner.Core.Settings.Strategy;

// "atrrb" — AtrRb Bands & Ribbon: a Keltner-style EMA basis with ATR-based bands.
// A long alert fires when price breaks the macro LOWER band; a short on the macro UPPER band.
// These parameters drive BOTH the chart drawer (AtrRbBands) and the atrrb signal (AtrRbBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class SettingsSignalStrategyAtrRb : SettingsSignalStrategyBase
{
    // EMA/ATR length used for the band basis (Pine default 20).
    public int Length { get; set; } = 20;

    // Macro outer-band multiplier: basis +/- ATR * OuterMult (the wide cloud; Pine tuned to 4.2).
    public double OuterMult { get; set; } = 4.2;

    // Inner ribbon multiplier: basis +/- ATR * InnerMult (chart ribbon only; default 1.0).
    public double InnerMult { get; set; } = 1.0;

    // The break must be the lowest Low (long) / highest High (short) within this trailing window
    // of bars (mirrors the Pine ta.lowest / ta.highest filter).
    public int BreakLookback { get; set; } = 5;

    public SettingsSignalStrategyAtrRb() : base()
    {
    }
}

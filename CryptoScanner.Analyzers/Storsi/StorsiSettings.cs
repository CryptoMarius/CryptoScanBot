using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Storsi;

// "bre" — Buddy Reversion Engine (BRE): Donchian-based outer bands (the gray "plateaus") with an
// EMA+ATR middle cloud (DIDO). A long alert fires when the Low breaks the macro LOWER band; a short
// when the High breaks the macro UPPER band, with optional HMA-trend / RSI / Stochastic-RSI filters.
// These parameters drive BOTH the chart drawer (BreBands) and the bre signal (BreBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class StorsiSettings : SettingsSignalStrategyBase
{
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 100.0;

    public int AddRsiAmount { get; set; } = 0;

    public bool CheckBollingerBandsCondition { get; set; } = false;
    public bool CheckMacdRecovery { get; set; } = false;
    public bool OnlyIfLux5m { get; set; } = false;
    // Lux 5m threshold (percentage 1..100). Long needs Lux5mValue <= -Lux5mPercentage,
    // short needs Lux5mValue >= +Lux5mPercentage. Default 50 matches the previous
    // hard-coded behavior; raise toward 100 for stricter (rarer) signals.
    public int Lux5mPercentage { get; set; } = 50;
    public bool SkipFirstSignal { get; set; } = false;

    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    // Zone confirmations — when any of these is enabled, at least one of the enabled
    // zone rejections must match (OR). All disabled = no zone filter.
    public bool UseDlzZone { get; set; } = false;
    public bool UseFvgZone { get; set; } = false;
    public bool UseSmcZone { get; set; } = false;

    public StorsiSettings() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}
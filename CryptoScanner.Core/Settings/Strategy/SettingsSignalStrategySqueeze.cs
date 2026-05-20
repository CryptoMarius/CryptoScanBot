namespace CryptoScanner.Core.Settings.Strategy;

// TTM Squeeze family (squeeze.fade + squeeze.brk).
// A "squeeze" is when the Bollinger Bands sit fully inside the Keltner Channel
// (BB.upper <= KC.upper && BB.lower >= KC.lower) for one or more candles.
[Serializable]
public class SettingsSignalStrategySqueeze : SettingsSignalStrategyBase
{
    // BB width filter — same idea as Stobb (avoid completely flat bands)
    public double BBMinPercentage { get; set; } = 0.50;
    public double BBMaxPercentage { get; set; } = 10.0;

    // squeeze.fade — counter-trend: price wicks beyond BB at a Stoch extreme,
    // after a recent squeeze had been building energy. Stoch must cross back.
    public int FadeSqueezeLookback { get; set; } = 10; // how far back to look for a recent squeeze
    public bool UseLowHigh { get; set; } = false;       // BB-break test: candle Low/High vs Open/Close

    // squeeze.brk — classic TTM breakout: squeeze JUST released and momentum kicks in.
    public int BrkReleaseLookback { get; set; } = 6;    // must have been in squeeze somewhere in this window
    public int BrkReleaseMinCandles { get; set; } = 2;  // and at least N of those candles must have been a squeeze

    // Trend filters (cheap to disable, expensive to evaluate — keep them last in IsSignal)
    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    public SettingsSignalStrategySqueeze() : base()
    {
        SoundFileLong = "sound-stobb-oversold.wav";
        SoundFileShort = "sound-stobb-overbought.wav";
    }
}

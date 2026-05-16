namespace CryptoScanner.Core.Settings.Strategy;

// Stoch + MACD crossover strategy
// Source: https://www.youtube.com/watch?v=vLbLZWi_Ypc
//
// Long  : close > SMA200, Stoch oversold, MACD histogram crosses up through zero.
// Short : close < SMA200, Stoch overbought, MACD histogram crosses down through zero.
// SL anchor: most recent confirmed swing low (long) / swing high (short).
// TP : entry +/- RRR * (entry - SL).
[Serializable]
public class SettingsSignalStrategyStochMacd : SettingsSignalStrategyBase
{
    // Require close above SMA200 (long) / below SMA200 (short) before evaluating the entry trigger.
    public bool RequireTrendFilter { get; set; } = true;

    // Lookback (in candles) for swing pivot search.
    public int SwingLookback { get; set; } = 30;

    // Bars required on each side of a pivot to confirm it (classic fractal-style).
    public int SwingPivotBars { get; set; } = 2;

    // Risk:Reward ratio used to compute the proposed take-profit price.
    public decimal RiskRewardRatio { get; set; } = 2.0m;

    public SettingsSignalStrategyStochMacd() : base()
    {
    }
}

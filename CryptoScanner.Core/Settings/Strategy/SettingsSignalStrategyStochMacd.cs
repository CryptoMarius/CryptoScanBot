namespace CryptoScanner.Core.Settings.Strategy;

// Stoch + MACD crossover strategy
// Source: https://www.youtube.com/watch?v=vLbLZWi_Ypc
//
// Long  : TrendPrimary on the active interval is Bullish, Stoch oversold, MACD histogram crosses up through zero.
// Short : TrendPrimary on the active interval is Bearish, Stoch overbought, MACD histogram crosses down through zero.
// SL anchor: most recent confirmed swing low (long) / swing high (short).
// TP : entry +/- RRR * (entry - SL), optionally fee-adjusted.
[Serializable]
public class SettingsSignalStrategyStochMacd : SettingsSignalStrategyBase
{
    // Require TrendPrimary Bullish (long) / Bearish (short) on the active interval before evaluating
    // the entry trigger. Uses the same Dow-theory ZigZag source as SignalTrend / SignalTrendHtf.
    public bool RequireTrendFilter { get; set; } = true;

    // Lookback (in candles) for swing pivot search.
    public int SwingLookback { get; set; } = 30;

    // Bars required on each side of a pivot to confirm it (classic fractal-style).
    public int SwingPivotBars { get; set; } = 2;

    // Risk:Reward ratio used to compute the proposed take-profit price.
    public decimal RiskRewardRatio { get; set; } = 2.0m;

    // When true, shift the take-profit price so that the desired RRR is achieved *after* paying
    // both the entry and exit fees (using Symbol.Exchange.FeeRate, which is in percent — e.g.
    // 0.1 means 0.1%). At a typical 0.1% spot fee on a 2R target this widens the TP by ~0.2%.
    public bool IncludeFeesInTp { get; set; } = true;

    public SettingsSignalStrategyStochMacd() : base()
    {
    }
}

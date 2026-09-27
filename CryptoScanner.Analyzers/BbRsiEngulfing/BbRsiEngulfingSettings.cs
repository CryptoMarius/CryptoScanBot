using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbRsiEngulfing;

// Settings of the bbrsiengulfing strategy (Bollinger Bands, RSI and an engulfing candle). The
// header used to describe dbr, copied along with the file (open point 112).
[Serializable]
public class BbRsiEngulfingSettings : SettingsSignalStrategyBase
{
    // Own BB range since 26-09-2026 (open point 109): this strategy used to read stobb's pair, so
    // whoever tuned stobb silently retuned this one too. The defaults are what it effectively ran
    // with (stobb's 1,5 and no upper bound).
    [SettingCaption("Filter on BB%")]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("", SameRowAs = nameof(BBMinPercentage))]
    public double BBMaxPercentage { get; set; } = 0.0;

    // The fourth check of this strategy is called "engulfing" but tests whether the candle closes
    // above the HIGH of the previous one (below the LOW for a short). That is a breakout condition,
    // not an engulfing: it says nothing about where this candle OPENED, nor about the colour or the
    // size of either body. Measured on three symbols and 75 700 candles of 15m on 29-08-2026: of the
    // 16 296 candles the rule fires on, 12 514 (77%) are not an engulfing at all, while 2 937 (44%)
    // of the real engulfings are missed because they close between the previous open and its high.
    //
    // Left as it was, because that is what all the runs so far measured. Switching this on uses the
    // classic definition instead - body covers body, opposite colours - through
    // CandlePatternHelper, the same code the candlepattern strategy uses.
    [SettingCaption("Use a strict engulfing candle",
        Tooltip = "Off is the rule every run so far measured: the candle only has to close above the "
            + "high of the previous one (below its low for a short). On uses the classic engulfing - "
            + "body covers body, opposite colours - through the same code the candlepattern strategy uses.")]
    public bool UseStrictEngulfing { get; set; } = false;

    public BbRsiEngulfingSettings() : base()
    {
    }
}

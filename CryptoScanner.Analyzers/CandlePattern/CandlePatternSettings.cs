using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.CandlePattern;

/// <summary>
/// One strategy for all the classic reversal patterns, with the pattern itself as a setting rather
/// than as a strategy of its own. That is what makes them comparable: a run varies
/// <see cref="Pattern"/> and nothing else, so the difference in the result is the pattern and not a
/// second implementation that happens to filter differently.
/// </summary>
[Serializable]
public class CandlePatternStrategySettings : SettingsSignalStrategyBase
{
    /// <summary>Which shape to fire on. Long reads it bullish, short reads it bearish.</summary>
    public CryptoCandlePattern Pattern { get; set; } = CryptoCandlePattern.Engulfing;

    /// <summary>
    /// The thresholds the shape is measured against, all as a percentage of the candle's own range.
    /// Reachable per run through the dotted path "Shape.MinWickPercentage" and so on.
    /// </summary>
    public CandlePatternSettings Shape { get; set; } = new();

    /// <summary>
    /// How many candles of movement AGAINST the trade have to precede the pattern. A reversal needs
    /// something to reverse: without this a hammer in a rising market counts as a buy signal, and
    /// hammer and hanging man become the same thing. Zero switches the requirement off, which is
    /// worth measuring separately - it is the difference between "the shape says something" and "the
    /// shape says something at the right moment".
    /// </summary>
    public int PrecedingCandles { get; set; } = 3;

    /// <summary>
    /// How far price has to have moved over those candles, as a percentage. Zero means any move in
    /// the right direction counts.
    /// </summary>
    public decimal PrecedingPercentage { get; set; } = 0m;

    public CandlePatternStrategySettings() : base()
    {
    }
}

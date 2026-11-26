namespace CryptoScanner.Core.Enums;

/// <summary>
/// The classic reversal patterns, as shapes. Each one has a bullish and a bearish reading; which of
/// the two applies follows from the trade side, not from the shape - a hammer and a hanging man are
/// the same candle, and only what came before it tells them apart.
/// </summary>
public enum CryptoCandlePattern
{
    /// <summary>Small body at one end, one long wick, the other wick short. Hammer / hanging man.</summary>
    Hammer,

    /// <summary>The same shape upside down. Inverted hammer / shooting star.</summary>
    InvertedHammer,

    /// <summary>The body of this candle covers the whole body of the previous one, opposite colour.</summary>
    Engulfing,

    /// <summary>The reverse: the previous body covers this one entirely.</summary>
    Harami,

    /// <summary>Opens beyond the previous close and closes back past the middle of the previous body.</summary>
    PiercingLine,

    /// <summary>Three candles: a long one, a small one, and a long one back the other way.</summary>
    MorningStar,

    /// <summary>Two candles that stop at the same price - equal lows, or equal highs.</summary>
    Tweezer,

    /// <summary>
    /// A continuation, not a reversal: a decisive candle, two to four small candles inside its range,
    /// and a decisive candle the same way that closes beyond the first one. Rising / falling three
    /// methods. It reads up to six candles, and the preceding move against the trade does not apply.
    /// </summary>
    ThreeMethods,
}

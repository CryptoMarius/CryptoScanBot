namespace CryptoScanner.Core.Signal.Indicators;

/// <summary>Indicator families the registry can build on a shared QuoteHub.</summary>
public enum IndicatorKind
{
    BollingerBands,
    Sma,
    Ema,
    Rsi,
    Macd,
    Stoch,
    ParabolicSar,
    Atr,
    WmaLow,
    WmaHigh,
    SuperTrend,
}

/// <summary>
/// Identifies one indicator instance by family plus its parameters, so two consumers asking for
/// the same thing (say Atr(50) from the VBS plugin and from a strategy) share a single hub instead
/// of each running their own copy on every candle.
/// <para>
/// Three doubles cover every indicator in use; unused slots stay 0. Using a record struct means
/// equality and hashing come for free, which is what makes it a dictionary key.
/// </para>
/// </summary>
public readonly record struct IndicatorKey(IndicatorKind Kind, double P1, double P2, double P3)
{
    public static IndicatorKey BollingerBands(int length, double deviation) => new(IndicatorKind.BollingerBands, length, deviation, 0);
    public static IndicatorKey Sma(int length) => new(IndicatorKind.Sma, length, 0, 0);
    public static IndicatorKey Ema(int length) => new(IndicatorKind.Ema, length, 0, 0);
    public static IndicatorKey Rsi(int length) => new(IndicatorKind.Rsi, length, 0, 0);
    public static IndicatorKey Macd(int fast, int slow, int signal) => new(IndicatorKind.Macd, fast, slow, signal);
    public static IndicatorKey Stoch(int length, int smoothD, int smoothK) => new(IndicatorKind.Stoch, length, smoothD, smoothK);
    public static IndicatorKey ParabolicSar(double step, double max) => new(IndicatorKind.ParabolicSar, step, max, 0);
    public static IndicatorKey Atr(int length) => new(IndicatorKind.Atr, length, 0, 0);
    public static IndicatorKey WmaLow(int length) => new(IndicatorKind.WmaLow, length, 0, 0);
    public static IndicatorKey WmaHigh(int length) => new(IndicatorKind.WmaHigh, length, 0, 0);
    public static IndicatorKey SuperTrend(int lookback, double multiplier) => new(IndicatorKind.SuperTrend, lookback, multiplier, 0);

    public override string ToString() => Kind switch
    {
        IndicatorKind.BollingerBands => $"Bb({P1:0.##},{P2:0.##})",
        IndicatorKind.Macd => $"Macd({P1:0.##},{P2:0.##},{P3:0.##})",
        IndicatorKind.Stoch => $"Stoch({P1:0.##},{P2:0.##},{P3:0.##})",
        IndicatorKind.ParabolicSar => $"PSar({P1:0.##},{P2:0.##})",
        IndicatorKind.SuperTrend => $"SuperTrend({P1:0.##},{P2:0.##})",
        _ => $"{Kind}({P1:0.##})",
    };
}

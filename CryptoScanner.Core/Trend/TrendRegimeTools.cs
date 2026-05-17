namespace CryptoScanner.Core.Trend;

// Classification of the *market regime* — separate from direction.
// Built on ADX (Wilder, 1978). The two questions a trend strategy must answer
// before entering are: "which direction?" (handled by TrendBiasTools / Dow / BOS)
// and "is the market trending at all?" — answered here.
public enum TrendRegime
{
    Unknown,        // ADX not (yet) computable
    Ranging,        // ADX below the lower threshold — mean-reversion territory
    Transitioning,  // ADX between lower and upper threshold — direction unclear
    Trending,       // ADX above the upper threshold — directional move in progress
}


public static class TrendRegimeTools
{
    // Wilder's classic thresholds: <20 ranging, 20-25 unclear, >25 trending.
    // Crypto often runs hotter; callers can override via settings.
    public const double DefaultRangingMax = 20.0;
    public const double DefaultTrendingMin = 25.0;


    public static TrendRegime Classify(double? adx, double rangingMax = DefaultRangingMax, double trendingMin = DefaultTrendingMin)
    {
        if (adx == null)
            return TrendRegime.Unknown;

        double value = adx.Value;
        if (value < rangingMax)
            return TrendRegime.Ranging;
        if (value >= trendingMin)
            return TrendRegime.Trending;
        return TrendRegime.Transitioning;
    }


    public static bool IsTrending(double? adx, double trendingMin = DefaultTrendingMin)
        => adx != null && adx.Value >= trendingMin;
}

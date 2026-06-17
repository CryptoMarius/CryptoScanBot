using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Stand-alone, side-effect-free detector for a "glijbaan" (slide): a coin in a sustained, ORDERLY,
/// still-ongoing decline. Pure and additive — it does NOT touch GlobalData/DB or any existing strategy;
/// run it next to the current logic and judge it before wiring it in.
///
/// Why the earlier versions kept lighting up in a calm range: they measured steepness as slope / ATR.
/// In a quiet market the ATR is tiny, so dividing by it INFLATED the score — exactly the wrong way
/// round (it rewarded low volatility). That whole approach is dropped here.
///
/// Instead the core is the Kaufman EFFICIENCY RATIO, the classic "trend vs. range" measure:
///     efficiency = |close[i] - close[i-N]| / sum(|close[k] - close[k-1]|)   over the window
/// It is the net displacement divided by the total path travelled, so:
///   - a clean one-way slide  -> path ≈ displacement -> efficiency near 1   (flagged),
///   - a choppy range          -> displacement ≈ 0   -> efficiency near 0   (rejected),
/// and it is scale-free / volatility-independent (no ATR inversion).
///
/// A candle is "sliding" when, over the window, the move is DOWN, the efficiency is high enough
/// (directional, not choppy), the net drop is a real percentage (not noise), AND the most recent
/// stretch is still heading down (so it lets go shortly after a bottom instead of lingering).
/// </summary>
public static class SlideDetector
{
    // --- Tunable behaviour (adjust here in source) ---

    /// <summary>Window over which the slide is measured (bars). The trend context.</summary>
    public const int DefaultLength = 40;

    /// <summary>Recent stretch (bars, &lt; <see cref="DefaultLength"/>) that must ALSO still be down.
    /// Lower = the flag releases faster after a bottom; higher = more naloop but steadier.</summary>
    public const int DefaultRecencyLength = 10;

    /// <summary>Minimum efficiency ratio (0..1). Higher = stricter / only very straight declines count.
    /// A choppy range sits near 0; a clean one-way slide sits near 1. ~0.35 separates the two well.</summary>
    public const double DefaultMinEfficiency = 0.35;

    /// <summary>Minimum net drop over the window, in percent. Rejects tiny drifts that happen to be
    /// "efficient" but are economically meaningless.</summary>
    public const double DefaultMinDropPercent = 1.0;

    /// <summary>
    /// Per-candle slide state. <see cref="Efficiency"/> (0..1) = net displacement / total path travelled
    /// over the window (1 = perfectly straight); <see cref="DropPercent"/> = net drop over the window in
    /// percent (positive = price fell).
    /// </summary>
    public readonly record struct SlideResult(
        bool Ready, bool IsSliding, double Efficiency, double DropPercent);

    /// <param name="length">Window over which the slide is measured (the trend context).</param>
    /// <param name="recencyLength">Recent stretch that must also still be down (cuts the tail after a bottom).</param>
    /// <param name="minEfficiency">Minimum efficiency ratio (0..1) for the decline to count as orderly/directional.</param>
    /// <param name="minDropPercent">Minimum net drop over the window, in percent.</param>
    public static List<SlideResult> Detect(IReadOnlyList<CryptoCandle> candles,
        int length = DefaultLength, int recencyLength = DefaultRecencyLength,
        double minEfficiency = DefaultMinEfficiency, double minDropPercent = DefaultMinDropPercent)
    {
        var result = new List<SlideResult>(candles?.Count ?? 0);
        if (candles == null || candles.Count == 0)
            return result;

        if (recencyLength >= length)
            recencyLength = Math.Max(1, length / 2);

        for (int i = 0; i < candles.Count; i++)
        {
            if (i < length)
            {
                result.Add(new SlideResult(false, false, 0, 0));
                continue;
            }

            double closeNow = (double)candles[i].Close;
            double closeThen = (double)candles[i - length].Close;
            double change = closeNow - closeThen;           // negative = down over the window

            // Total path travelled over the window (sum of absolute bar-to-bar moves).
            double path = 0;
            for (int k = i - length + 1; k <= i; k++)
                path += Math.Abs((double)candles[k].Close - (double)candles[k - 1].Close);

            // Kaufman efficiency ratio: net displacement vs. total path. 1 = straight, ~0 = choppy.
            double efficiency = path > 0 ? Math.Abs(change) / path : 0;
            double dropPercent = closeThen > 0 ? -change / closeThen * 100.0 : 0;   // positive = dropped

            // The recent stretch must still be heading down, so the flag is released shortly after a
            // bottom (the window-wide change stays negative for ~length bars, but this short check flips
            // as soon as price stops making lower closes).
            double closeRecent = (double)candles[i - recencyLength].Close;
            bool recentDown = closeNow < closeRecent;

            bool isSliding = change < 0                      // net direction is down
                && recentDown                               // and still going down right now
                && efficiency >= minEfficiency              // orderly / directional, not a choppy range
                && dropPercent >= minDropPercent;           // and a real drop, not noise

            result.Add(new SlideResult(true, isSliding, efficiency, dropPercent));
        }

        return result;
    }
}

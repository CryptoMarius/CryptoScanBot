using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// Helper for the relative volume filter.
///
/// Relative volume (RelVol) measures how active the current candle is compared to the recent
/// average. It is calculated as:
///
///   RelVol = current_candle_volume / SMA(volume, lookback)
///
/// This is a direction-neutral quality filter: it does not predict which way the price moves,
/// but ensures there is sufficient market participation before a signal is accepted.
///
/// Why this matters:
///   - A stochastic cross or Bollinger Band touch on very low volume is statistically weaker.
///   - False breakouts (price briefly exits a band then snaps back) tend to occur on thin volume.
///   - Filtering out below-average-volume candles reduces noise without biasing direction.
///
/// Typical thresholds:
///   - MinRelVol = 0.8  → accept candles with at least 80% of average volume
///   - MinRelVol = 1.0  → only accept candles with above-average volume (stricter)
///   - MaxRelVol = 999  → no upper cap (very high volume is still a valid signal)
///   - Lookback  = 20   → rolling 20-candle SMA baseline
///
/// Note: this filter is skipped for strategies that bypass the barometer gate (e.g. DLZ/FVG zones),
/// because those strategies are triggered by price-structure events rather than momentum conditions.
/// </summary>
public static class VolumeHelper
{
    /// <summary>
    /// Checks whether the current candle's relative volume falls within the configured range.
    /// Returns true (pass) when:
    ///   - The filter is disabled (settings.Active = false)
    ///   - There are not enough candles to calculate the baseline (graceful skip)
    ///   - RelVol is between settings.MinRelative and settings.MaxRelative
    /// Returns false (block) when RelVol is outside the configured range.
    /// </summary>
    /// <param name="indicatorData">Indicator data for the current interval, used to access the candle list.</param>
    /// <param name="settings">Compiled volume filter settings.</param>
    /// <param name="reaction">Human-readable reason when the check fails; empty string when it passes.</param>
    public static bool CheckRelativeVolume(CryptoIndicatorData indicatorData,
        SettingsCompiledVolume settings, out string reaction)
    {
        reaction = "";

        if (!settings.Active)
            return true;

        // Current candle volume
        decimal currentVolume = indicatorData.LastCandle.Volume;
        if (currentVolume == 0)
        {
            // Zero-volume candle (e.g. weekend / holiday gap filler) - skip rather than block
            return true;
        }

        // Need at least 'Lookback' candles in the list to compute a meaningful average
        var candleList = indicatorData.CandleList;
        if (candleList.Count < settings.Lookback)
            return true; // Not enough history - skip check gracefully

        // Calculate the rolling volume SMA over the last 'Lookback' candles
        decimal sumVolume = 0m;
        int count = 0;
        foreach (var candle in candleList.Values.TakeLast(settings.Lookback))
        {
            sumVolume += candle.Volume;
            count++;
        }

        if (count == 0 || sumVolume == 0)
            return true; // No usable data - skip

        decimal avgVolume = sumVolume / count;
        decimal relVol = currentVolume / avgVolume;

        if (relVol < settings.MinRelative || relVol > settings.MaxRelative)
        {
            reaction = $"Relative volume {relVol:N2} not between {settings.MinRelative:N2} and {settings.MaxRelative:N2}";
            return false;
        }

        return true;
    }
}

using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbDlz;

/// <summary>
/// Combined signal: STOBB (oversold Bollinger Band tag with Stoch) firing while price is
/// approaching or inside a long DLZ zone.
///
/// Inherits the full STOBB pipeline (BB width / BB position / Stoch / trend / AdditionalChecks)
/// from <see cref="SignalStobbLong"/> so every existing STOBB setting (SoftSbm, MA percentages,
/// MA crossings, RSI filter, OnlyIfPreviousStobb, Lux 5m, trend filters …) keeps applying.
/// Only the zone-proximity gate is added; it runs first because it short-circuits cheaply
/// when no zone is nearby.
///
/// Zones themselves are owned by <see cref="Dlz.SignalDominantLevelNearLong"/> — this class
/// only reads the precomputed state, it never closes or alarm-flags a zone.
/// </summary>
public class SignalStobbDlzLong : SignalStobbLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // Cheap gate first: most candles are nowhere near a zone, so this eliminates them
        // before we run the indicator-heavy STOBB checks.
        if (!this.IsNearDlzZone(out string zoneInfo))
        {
            ExtraText = "no nearby dlz zone";
            return false;
        }

        // Full STOBB pipeline (BB width, below BB, Stoch oversold, optional trend filters).
        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}

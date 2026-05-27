using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbDlz;

/// <summary>
/// Combined signal: STOBB (oversold Bollinger Band tag with Stoch) firing while price is
/// inside a long DLZ zone. "Inside" means the candle's low has entered the zone and the
/// close has not broken below the zone's floor.
///
/// Inherits the full STOBB pipeline (BB width / BB position / Stoch / trend / AdditionalChecks)
/// from <see cref="SignalStobbLong"/> so every existing STOBB setting (SoftSbm, MA percentages,
/// MA crossings, RSI filter, OnlyIfPreviousStobb, Lux 5m, trend filters …) keeps applying.
/// The zone gate runs first because it short-circuits cheaply when price is not inside any zone.
///
/// Zones themselves are owned by <see cref="Dlz.SignalDominantLevelNearLong"/> — this class
/// only reads the precomputed state, it never closes or alarm-flags a zone.
/// </summary>
public class SignalStobbDlzLong : SignalStobbLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // Cheap gate first: skip candles that did not produce a rejection wick on any DLZ zone.
        // See ZoneProximityHelper.WasRejectedAtDlzZone for the test+close-back-outside criteria.
        if (!this.WasRejectedAtDlzZone(out string zoneInfo))
        {
            ExtraText = "no dlz rejection";
            return false;
        }

        // Full STOBB pipeline (BB width, below BB, Stoch oversold, optional trend filters).
        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}

using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbSmc;

/// <summary>
/// Combined signal: STOBB (oversold Bollinger Band tag with Stoch) firing while price shows
/// a confirmed rejection off a long SMC Order Block (demand zone). Inherits the full STOBB
/// pipeline (BB width / BB position / Stoch / trend / AdditionalChecks) from
/// <see cref="SignalStobbLong"/> so every existing STOBB setting (SoftSbm, MA percentages,
/// MA crossings, RSI filter, OnlyIfPreviousStobb, Lux 5m, trend filters …) keeps applying.
/// The zone gate runs first because it short-circuits cheaply when no SMC rejection is in play.
///
/// SMC zones are produced by <see cref="Zones.ZoneSmc"/> — this class only reads the
/// precomputed state via <see cref="ZoneProximityHelper.WasRejectedAtSmcZone"/>.
/// </summary>
public class SignalStobbSmcLong : SignalStobbLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.WasRejectedAtSmcZone(out string zoneInfo))
        {
            ExtraText = "no smc rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}

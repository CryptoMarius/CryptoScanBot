using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiSmc;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI oversold) firing while price shows a confirmed
/// rejection off a long SMC Order Block (demand zone). Inherits the full STORSI pipeline
/// (BB width / Stoch / RSI / trend / AdditionalChecks) from <see cref="SignalStoRsiLong"/>
/// so every existing STORSI setting keeps applying. The zone gate runs first because it
/// short-circuits cheaply when no SMC rejection is in play.
///
/// SMC zones are produced by <see cref="Zones.ZoneSmc"/> — this class only reads the
/// precomputed state via <see cref="ZoneProximityHelper.WasRejectedAtSmcZone"/>.
/// </summary>
public class SignalStoRsiSmcLong : SignalStoRsiLong
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

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}

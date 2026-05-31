using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiZone;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI oversold) firing while price shows a confirmed
/// rejection off ANY active zone type — DLZ, FVG or SMC. Acts as a one-stop confluence
/// strategy so the user does not need separate <c>storsi.dlz</c>, <c>storsi.fvg</c>,
/// <c>storsi.smc</c> entries in their strategy list. The first zone type that matches wins;
/// the per-zone settings (intervals, MaxTouches, NearZonePercentage…) remain in effect via
/// the respective helpers in <see cref="ZoneProximityHelper"/>.
///
/// The base storsi pipeline runs first because it is the cheapest gate; the zone-rejection
/// check only runs once the momentum side is confirmed.
/// </summary>
public class SignalStoRsiZoneLong : SignalStoRsiLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!base.IsSignal())
            return false;

        if (this.WasRejectedAtDlzZone(out string zoneInfo)
            || this.WasRejectedAtFvgZone(out zoneInfo)
            || this.WasRejectedAtSmcZone(out zoneInfo))
        {
            ExtraText = $"storsi+{zoneInfo}";
            return true;
        }

        ExtraText = "no zone rejection";
        return false;
    }
}

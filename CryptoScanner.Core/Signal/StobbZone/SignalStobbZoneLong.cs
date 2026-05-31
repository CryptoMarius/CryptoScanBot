using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbZone;

/// <summary>
/// Combined signal: STOBB (oversold Bollinger Band tag with Stoch) firing while price shows
/// a confirmed rejection off ANY active zone type — DLZ, FVG or SMC. Acts as a one-stop
/// confluence strategy so the user does not need separate <c>stobb.dlz</c>, <c>stobb.fvg</c>,
/// <c>stobb.smc</c> entries in their strategy list.
///
/// Order: the cheap zone-rejection check runs first (matching the existing <c>stobb.dlz</c>
/// pattern), then the full STOBB pipeline only when a rejection was found.
/// </summary>
public class SignalStobbZoneLong : SignalStobbLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!(this.WasRejectedAtDlzZone(out string zoneInfo)
            || this.WasRejectedAtFvgZone(out zoneInfo)
            || this.WasRejectedAtSmcZone(out zoneInfo)))
        {
            ExtraText = "no zone rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}

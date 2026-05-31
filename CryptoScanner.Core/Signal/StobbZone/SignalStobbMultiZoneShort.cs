using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbZone;

/// <summary>
/// Combined signal: STOBB.MULTI (overbought Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off ANY active zone type — DLZ, FVG or SMC.
/// See <see cref="SignalStobbZoneLong"/> for the design notes.
/// </summary>
public class SignalStobbMultiZoneShort : SignalStobbMultiShort
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

        ExtraText = $"stobb.multi+{zoneInfo}";
        return true;
    }
}

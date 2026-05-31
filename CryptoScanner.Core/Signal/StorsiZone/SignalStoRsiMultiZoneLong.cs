using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiZone;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI oversold with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off ANY active zone type — DLZ, FVG or SMC.
/// See <see cref="SignalStoRsiZoneLong"/> for the design notes.
/// </summary>
public class SignalStoRsiMultiZoneLong : SignalStoRsiMultiLong
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
            ExtraText = $"storsi.multi+{zoneInfo}";
            return true;
        }

        ExtraText = "no zone rejection";
        return false;
    }
}

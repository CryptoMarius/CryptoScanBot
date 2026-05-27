using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbFvg;

/// <summary>
/// Combined signal: STOBB (overbought Bollinger Band tag with Stoch) firing while price is
/// inside a short FVG zone. See <see cref="StobbDlz.SignalStobbDlzLong"/> for the design notes.
/// </summary>
public class SignalStobbFvgShort : SignalStobbShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!base.IsSignal())
            return false;

        // Require a rejection wick off the FVG zone, not just "price inside". See
        // ZoneProximityHelper.WasRejectedAtFvgZone for the test+close-back-outside criteria.
        if (!this.WasRejectedAtFvgZone(out string zoneInfo))
        {
            ExtraText = "no fvg rejection";
            return false;
        }

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}

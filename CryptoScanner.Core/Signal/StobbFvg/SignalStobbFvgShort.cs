using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbFvg;

/// <summary>
/// Combined signal: STOBB (overbought Bollinger Band tag with Stoch) firing while price is
/// approaching or inside a short FVG zone. See <see cref="StobbDlz.SignalStobbDlzLong"/> for
/// the design notes.
/// </summary>
public class SignalStobbFvgShort : SignalStobbShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.IsNearFvgZone(out string zoneInfo))
        {
            ExtraText = "no nearby fvg zone";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}

using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiFvg;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI overbought) firing while price is approaching
/// or inside a short FVG zone. See <see cref="StorsiDlz.SignalStoRsiDlzLong"/> for the design notes.
/// </summary>
public class SignalStoRsiFvgShort : SignalStoRsiShort
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

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}

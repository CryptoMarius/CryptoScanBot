using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbFvg;

/// <summary>
/// Combined signal: STOBB.MULTI (overbought Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a short FVG zone. See
/// <see cref="SignalStobbMultiFvgLong"/> for the design notes.
/// </summary>
public class SignalStobbMultiFvgShort : SignalStobbMultiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.WasRejectedAtFvgZone(out string zoneInfo))
        {
            ExtraText = "no fvg rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb.multi+{zoneInfo}";
        return true;
    }
}

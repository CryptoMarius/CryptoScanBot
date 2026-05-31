using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiFvg;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI overbought with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a short FVG zone. See
/// <see cref="SignalStoRsiMultiFvgLong"/> for the design notes.
/// </summary>
public class SignalStoRsiMultiFvgShort : SignalStoRsiMultiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!base.IsSignal())
            return false;

        if (!this.WasRejectedAtFvgZone(out string zoneInfo))
        {
            ExtraText = "no fvg rejection";
            return false;
        }

        ExtraText = $"storsi.multi+{zoneInfo}";
        return true;
    }
}

using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiFvg;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI oversold with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a long FVG zone. Inherits the full
/// STORSI multi-timeframe pipeline from <see cref="SignalStoRsiMultiLong"/>. See
/// <see cref="SignalStoRsiFvgLong"/> for the single-TF version.
/// </summary>
public class SignalStoRsiMultiFvgLong : SignalStoRsiMultiLong
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

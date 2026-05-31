using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiSmc;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI overbought with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a short SMC Order Block (supply zone).
/// See <see cref="SignalStoRsiMultiSmcLong"/> for the design notes.
/// </summary>
public class SignalStoRsiMultiSmcShort : SignalStoRsiMultiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.WasRejectedAtSmcZone(out string zoneInfo))
        {
            ExtraText = "no smc rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"storsi.multi+{zoneInfo}";
        return true;
    }
}

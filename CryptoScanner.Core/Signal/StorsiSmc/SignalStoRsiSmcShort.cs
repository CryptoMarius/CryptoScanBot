using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiSmc;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI overbought) firing while price shows a confirmed
/// rejection off a short SMC Order Block (supply zone). See <see cref="SignalStoRsiSmcLong"/>
/// for the design notes.
/// </summary>
public class SignalStoRsiSmcShort : SignalStoRsiShort
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

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}

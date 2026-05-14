using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Storsi;

// WGHM - Wave Generation High Momentum
// Shared base for all StoRsi variants (single + multi, long + short).
//
// Inherits directly from SignalCreateBase, not from SignalSbmBase, because StoRsi does not
// share any of the SBM-specific pipeline checks (MACD recovery, MA-percentage filters,
// MA-crossings). Going through SbmBase would silently re-introduce those checks via
// inherited AdditionalChecks/GiveUp.
public class SignalStoRsiBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Rsi == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }
}

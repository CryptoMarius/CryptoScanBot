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

    public override bool GiveUp(CryptoSignal signal)
    {
        if (!base.GiveUp(signal))
            return false;

        switch (SignalSide)
        {
            case Enums.CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close > (decimal?)CandleLast?.CandleData?.Sma20)
                {
                    ExtraText = "Close above sma20";
                    return true;
                }
                break;
            case Enums.CryptoTradeSide.Short:
                if (CandleLast!.Candle.Close < (decimal?)CandleLast!.CandleData?.Sma20)
                {
                    ExtraText = "Close below sma20";
                    return true;
                }
                break;
        }

        ExtraText = "";
        return false;
    }
}

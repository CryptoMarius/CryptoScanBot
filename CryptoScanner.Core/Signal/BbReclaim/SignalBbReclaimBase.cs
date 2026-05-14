using CryptoScanner.Core.Model;

#if DEBUG
namespace CryptoScanner.Core.Signal.BbReclaim;

/// <summary>
/// Shared base for the BB-extreme + MA-reclaim strategy. Indicator requirements are the same
/// for both directions: Bollinger Bands, SMA20 (= BB middle), and EMA9.
/// </summary>
public class SignalBbReclaimBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.Ema9 == null
           || data.CandleData.BollingerBandsUpperBand == null
           || data.CandleData.BollingerBandsLowerBand == null
           || data.CandleData.BollingerBandsDeviation == null)
            return false;

        return true;
    }
}
#endif

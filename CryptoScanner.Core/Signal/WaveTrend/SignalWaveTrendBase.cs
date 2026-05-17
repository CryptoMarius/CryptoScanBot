#if DEBUG
namespace CryptoScanner.Core.Signal.WaveTrend;

public class SignalWaveTrendBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.Sma200 == null
           || data.CandleData.BollingerBandsDeviation == null)
            return false;

        return true;
    }
}
#endif

using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Gaussian;

/// <summary>
/// Gaussian Pullback Short — fires during a confirmed downtrend when price wicks up
/// to touch the Gaussian filter line and closes back below it (Variant B: wick touch + close below).
///
/// Entry conditions:
///   - contswLast == -1  : Gaussian confirmed downtrend
///   - filteredLast < filteredPrev : filter still falling
///   - candle.High >= filteredLast : wick reached the filter line
///   - candle.Close <  filteredLast : closed back below (rejection confirmed)
/// </summary>
#if DEBUG
public class SignalGaussianPullbackShort : SignalGaussianScalpBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if ((data == null)
            || data.Candle.OpenTime == 0
            || data.CandleData == null)
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        if (!ComputeGaussianState(out double filteredLast, out double filteredPrev, out int contswLast))
        {
            ExtraText = "insufficient history for Gaussian filter";
            return false;
        }

        if (contswLast != -1)
        {
            ExtraText = "no confirmed downtrend";
            return false;
        }

        if (filteredLast >= filteredPrev)
        {
            ExtraText = "Gaussian not falling";
            return false;
        }

        double high = (double)CandleLast.Candle.High;
        double close = (double)CandleLast.Candle.Close;

        if (high < filteredLast)
        {
            ExtraText = $"high {high:N6} did not reach Gaussian {filteredLast:N6}";
            return false;
        }

        if (close >= filteredLast)
        {
            ExtraText = $"close {close:N6} did not close below Gaussian {filteredLast:N6}";
            return false;
        }

        ExtraText = $"G pullback ↓ g={filteredLast:N6}";
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + 2 * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = "give up after 2 candles";
            return true;
        }

        return false;
    }
}
#endif

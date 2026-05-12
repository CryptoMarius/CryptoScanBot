using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// Gaussian Pullback Long — fires during a confirmed uptrend when price wicks down
/// to touch the Gaussian filter line and closes back above it (Variant B: wick touch + close above).
///
/// Entry conditions:
///   - contswLast == 1  : Gaussian confirmed uptrend
///   - filteredLast > filteredPrev : filter still rising
///   - candle.Low  <= filteredLast : wick reached the filter line
///   - candle.Close >  filteredLast : closed back above (bounce confirmed)
/// </summary>
#if DEBUG
public class SignalGaussianPullbackLong : SignalGaussianScalpBase
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

        if (contswLast != 1)
        {
            ExtraText = "no confirmed uptrend";
            return false;
        }

        if (filteredLast <= filteredPrev)
        {
            ExtraText = "Gaussian not rising";
            return false;
        }

        double low = (double)CandleLast.Candle.Low;
        double close = (double)CandleLast.Candle.Close;

        if (low > filteredLast)
        {
            ExtraText = $"low {low:N6} did not reach Gaussian {filteredLast:N6}";
            return false;
        }

        if (close <= filteredLast)
        {
            ExtraText = $"close {close:N6} did not bounce above Gaussian {filteredLast:N6}";
            return false;
        }

        ExtraText = $"G pullback ↑ g={filteredLast:N6}";
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

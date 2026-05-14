using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

#if DEBUG
namespace CryptoScanner.Core.Signal.BbReclaim;

/// <summary>
/// Short variant — bearish BB-extreme reclaim. Mirror of <see cref="SignalBbReclaimLong"/>.
///
/// Setup:
///   1. Within the last N candles, a "washout" candle: high pierced BB.upper (wholly or partially)
///      AND its close was above both EMA9 and SMA20.
///   2. Current candle: close below EMA9, AND EMA9 below SMA20 (bearish stack).
/// </summary>
public class SignalBbReclaimShort : SignalBbReclaimBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // BB width must be at least 1.5% (reusing the Stobb threshold like SignalBbmaLong does)
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        decimal close = CandleLast.Candle.Close;
        decimal ema9  = (decimal)CandleLast.CandleData!.Ema9!.Value;
        decimal sma20 = (decimal)CandleLast.CandleData!.Sma20!.Value;

        if (ema9 >= sma20)
        {
            ExtraText = "ema9 not below sma20";
            return false;
        }
        if (close >= ema9)
        {
            ExtraText = "close not below ema9";
            return false;
        }

        // Fire only on the crossing candle: the previous candle must have had EMA9 at or above SMA20.
        if (!GetPrevCandle(CandleLast, out MyData? prevForCross) || prevForCross == null)
        {
            ExtraText = "no prev candle for ema9/sma20 cross check";
            return false;
        }
        decimal prevEma9  = (decimal)prevForCross.CandleData!.Ema9!.Value;
        decimal prevSma20 = (decimal)prevForCross.CandleData!.Sma20!.Value;
        if (prevEma9 < prevSma20)
        {
            ExtraText = "ema9 already below sma20 — no fresh cross";
            return false;
        }

        int lookback = GlobalData.Settings.Signal.BbReclaim.Lookback;
        MyData cursor = CandleLast;

        for (int i = 0; i < lookback; i++)
        {
            if (!GetPrevCandle(cursor, out MyData? prev) || prev == null)
                break;
            cursor = prev;

            decimal highK   = cursor.Candle.High;
            decimal closeK  = cursor.Candle.Close;
            decimal bbUpper = (decimal)cursor.CandleData!.BollingerBandsUpperBand!.Value;
            decimal ema9K   = (decimal)cursor.CandleData!.Ema9!.Value;
            decimal sma20K  = (decimal)cursor.CandleData!.Sma20!.Value;

            if (highK > bbUpper && closeK > ema9K && closeK > sma20K)
            {
                ExtraText = $"bb washout {i + 1} bars back";
                return true;
            }
        }

        ExtraText = "no recent bb washout found";
        return false;
    }
}
#endif

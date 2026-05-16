using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

#if DEBUG
namespace CryptoScanner.Core.Signal.StochMacd;

public class SignalStochMacdBase : SignalCreateBase
{
    /// <summary>
    /// Adjust the raw take-profit so that the desired RRR is achieved *after* paying both the
    /// entry fee and the exit fee. Pass-through when fees are disabled in settings or when the
    /// exchange has FeeRate == 0.
    ///
    /// FeeRate convention (matches PaperTrading.cs / TradeTools.cs): stored as percent —
    /// 0.1 = 0.1%, so the fractional rate is FeeRate / 100.
    ///
    /// Derivation:
    ///   Long  net  = TP - entry - (entry + TP) * fee = (TP_raw - entry)
    ///              ⇒ TP = (TP_raw + entry * fee) / (1 - fee)
    ///   Short net  = entry - TP - (entry + TP) * fee = (entry - TP_raw)
    ///              ⇒ TP = (TP_raw - entry * fee) / (1 + fee)
    ///
    /// Uses SignalSide from the base class so subclasses don't need to pass the side explicitly.
    /// </summary>
    protected decimal AdjustTpForFees(decimal entry, decimal rawTp)
    {
        if (!GlobalData.Settings.Signal.StochMacd.IncludeFeesInTp)
            return rawTp;

        decimal fee = Symbol.Exchange.FeeRate / 100m;
        if (fee <= 0m)
            return rawTp;

        return SignalSide == CryptoTradeSide.Long
            ? (rawTp + entry * fee) / (1m - fee)
            : (rawTp - entry * fee) / (1m + fee);
    }

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.StochSignal == null
           || data.CandleData.MacdHistogram == null
           || data.CandleData.MacdValue == null
           || data.CandleData.MacdSignal == null)
            return false;

        return true;
    }


    // Finds the most recent confirmed swing low within the lookback window.
    // A swing low at index k requires `pivotBars` bars on each side with strictly higher lows.
    protected bool TryFindSwingLow(int lookback, int pivotBars, out decimal swingLow)
    {
        swingLow = 0;
        int total = lookback + 2 * pivotBars;
        var values = SymbolInterval.CandleList.GetLastNValues(total);
        if (values.Count < 2 * pivotBars + 1)
            return false;

        int n = values.Count;
        for (int k = n - 1 - pivotBars; k >= pivotBars; k--)
        {
            decimal candidate = values[k].Low;
            bool isPivot = true;
            for (int j = 1; j <= pivotBars; j++)
            {
                if (values[k - j].Low <= candidate || values[k + j].Low <= candidate)
                {
                    isPivot = false;
                    break;
                }
            }
            if (isPivot)
            {
                swingLow = candidate;
                return true;
            }
        }
        return false;
    }


    // Mirror — finds the most recent confirmed swing high.
    protected bool TryFindSwingHigh(int lookback, int pivotBars, out decimal swingHigh)
    {
        swingHigh = 0;
        int total = lookback + 2 * pivotBars;
        var values = SymbolInterval.CandleList.GetLastNValues(total);
        if (values.Count < 2 * pivotBars + 1)
            return false;

        int n = values.Count;
        for (int k = n - 1 - pivotBars; k >= pivotBars; k--)
        {
            decimal candidate = values[k].High;
            bool isPivot = true;
            for (int j = 1; j <= pivotBars; j++)
            {
                if (values[k - j].High >= candidate || values[k + j].High >= candidate)
                {
                    isPivot = false;
                    break;
                }
            }
            if (isPivot)
            {
                swingHigh = candidate;
                return true;
            }
        }
        return false;
    }
}
#endif

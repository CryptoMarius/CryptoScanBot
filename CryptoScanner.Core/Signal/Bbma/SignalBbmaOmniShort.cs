using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

#if DEBUG
namespace CryptoScanner.Core.Signal.Bbma;

/// <summary>
/// Short variant of the BBMA Omni strategy. Mirror of <see cref="SignalBbmaOmniLong"/> —
/// state classifiers are 1-on-1 ports of the "sell" code paths from
/// "BBMA Oma Ally OmniView.mq5". Line numbers in the comments refer to that source.
///
/// Note on WMA orientation (matches OmniView):
///   - CSD (CSAK Sell)     uses Wma05Low  / Wma10Low    (close must fall below recent lows)
///   - Extreme (Sell)      uses Wma05High at UpperBand  (MA poked above band)
///   - Reentry (Sell)      uses Wma05High / Wma10High   (pullback into the highs-MA zone)
/// </summary>
public class SignalBbmaOmniShort : SignalBbmaOmniBase
{
    public OmniState GetOmniState(MyData data)
    {
        if (IsExtreme(data)) return OmniState.Extreme;
        if (IsCsm(data))     return OmniState.Csm;
        if (IsCsd(data))     return OmniState.Csd;
        if (IsMlv(data))     return OmniState.Mlv;
        if (IsReentry(data)) return OmniState.Reentry;
        return OmniState.None;
    }


    /// <summary>
    /// CSAK Sell — OmniView lines 776-786 (sell variant).
    ///   single-bar :  open[i] &gt; mid AND close[i] &lt; mid AND close[i] &lt; malo5 AND close[i] &lt; malo10
    ///   two-bar    :  open[i-1] &gt; mid[i-1] AND close[i-1] &gt; mid[i-1]
    ///              AND close[i] &lt; mid AND open[i] &lt; mid AND close[i] &lt; malo5 AND close[i] &lt; malo10
    /// </summary>
    private bool IsCsd(MyData data)
    {
        decimal open    = data.Candle.Open;
        decimal close   = data.Candle.Close;
        decimal mid     = (decimal)data.CandleData!.Sma20!.Value;
        decimal malo5   = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10  = (decimal)data.CandleData!.Wma10Low!.Value;

        // single-bar form
        if (open > mid && close < mid && close < malo5 && close < malo10)
            return true;

        // two-bar form
        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        decimal openPrev  = prev.Candle.Open;
        decimal closePrev = prev.Candle.Close;
        decimal midPrev   = (decimal)prev.CandleData!.Sma20!.Value;
        return openPrev > midPrev && closePrev > midPrev
            && close < mid && open < mid && close < malo5 && close < malo10;
    }


    /// <summary>
    /// Extreme Sell — OmniView lines 811-815.
    ///   (mahi5 ≥ UB recent[0..2])
    /// AND (current OR prev candle is bearish)
    /// AND (wick rejection of UB current, or prev-wick + current-close-below-UB, or gap-down open-below-UB after prev-close-above-UB)
    /// </summary>
    private bool IsExtreme(MyData data)
    {
        decimal open    = data.Candle.Open;
        decimal close   = data.Candle.Close;
        decimal high    = data.Candle.High;
        decimal upperB  = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;
        decimal mahi5   = (decimal)data.CandleData!.Wma05High!.Value;

        if (!GetPrevCandle(data, out MyData? prev) || prev == null) return false;
        if (!GetPrevCandle(prev, out MyData? prev2) || prev2 == null) return false;

        decimal closePrev   = prev.Candle.Close;
        decimal openPrev    = prev.Candle.Open;
        decimal highPrev    = prev.Candle.High;
        decimal upperBPrev  = (decimal)prev.CandleData!.BollingerBandsUpperBand!.Value;
        decimal upperBPrev2 = (decimal)prev2.CandleData!.BollingerBandsUpperBand!.Value;
        decimal mahi5Prev   = (decimal)prev.CandleData!.Wma05High!.Value;
        decimal mahi5Prev2  = (decimal)prev2.CandleData!.Wma05High!.Value;

        bool maPoked = mahi5 >= upperB || mahi5Prev >= upperBPrev || mahi5Prev2 >= upperBPrev2;
        if (!maPoked) return false;

        bool bearishCandle = close < open || closePrev < openPrev;
        if (!bearishCandle) return false;

        bool wickRejection =
              (high >= upperB && close < upperB)
           || (highPrev >= upperBPrev && close < upperB)
           || (open <= upperB && closePrev >= upperBPrev);

        return wickRejection;
    }


    /// <summary>
    /// Momentum Sell / CSM — OmniView lines 909-913.
    ///   close[i] ≤ LowerBand[i]
    /// </summary>
    private bool IsCsm(MyData data)
    {
        decimal close   = data.Candle.Close;
        decimal lowerB  = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;
        return close <= lowerB;
    }


    /// <summary>
    /// MLV / MHV — stateless approximation (see SignalBbmaOmniLong for the rationale).
    /// Wick rejection of the upper band while WmaHigh5 is still inside the band.
    /// </summary>
    private bool IsMlv(MyData data)
    {
        decimal close   = data.Candle.Close;
        decimal high    = data.Candle.High;
        decimal upperB  = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;
        decimal mahi5   = (decimal)data.CandleData!.Wma05High!.Value;

        return high >= upperB && close < upperB && mahi5 < upperB;
    }


    /// <summary>
    /// Reentry Sell — OmniView lines 920-923.
    ///   (high ≥ mahi5 OR high ≥ mahi10)
    /// AND (close ≤ mahi5 OR close ≤ mahi10)
    /// AND close ≤ mid (MiddleBuffer)
    /// </summary>
    private bool IsReentry(MyData data)
    {
        decimal close   = data.Candle.Close;
        decimal high    = data.Candle.High;
        decimal mid     = (decimal)data.CandleData!.Sma20!.Value;
        decimal mahi5   = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10  = (decimal)data.CandleData!.Wma10High!.Value;

        bool touchedMa  = high >= mahi5  || high >= mahi10;
        bool closedBack = close <= mahi5 || close <= mahi10;
        return touchedMa && closedBack && close <= mid;
    }


    /// <summary>
    /// HTF validation — mirror of <see cref="SignalBbmaOmniLong.CheckHtf"/>.
    /// </summary>
    private bool CheckHtf(CryptoInterval interval, MyData current, out string htfSetup)
    {
        htfSetup = "";

        const int CsmLookback = 20;
        const int CsdLookback = 20;
        const int MlvLookback = 10;
        const int MinGap = 3;

        int csmIndex = -1;
        int csdIndex = -1;
        int mlvIndex = -1;

        MyData? cursor = current;
        int max = Math.Max(CsmLookback, Math.Max(CsdLookback, MlvLookback));
        for (int i = 0; i < max; i++)
        {
            if (!GetPrevCandle(interval, cursor, out cursor) || cursor == null)
                break;

            OmniState state = GetOmniState(cursor);
            if (csmIndex < 0 && i < CsmLookback && state == OmniState.Csm) csmIndex = i;
            if (csdIndex < 0 && i < CsdLookback && state == OmniState.Csd) csdIndex = i;
            if (mlvIndex < 0 && i < MlvLookback && state == OmniState.Mlv) mlvIndex = i;
        }

        if (mlvIndex >= 0 && csmIndex > mlvIndex && csmIndex - mlvIndex >= MinGap)
        {
            htfSetup = "MHV";
            return true;
        }

        if (csmIndex >= 0)
        {
            htfSetup = "CSM";
            return true;
        }

        if (csdIndex >= 0)
        {
            htfSetup = "CSD";
            return true;
        }

        return false;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        return GetOmniState(CandleLast) == OmniState.Extreme;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        MyData? candleLtf = CandleLast;
        OmniState stateLtfNow = GetOmniState(candleLtf);
        if (stateLtfNow != OmniState.Reentry)
        {
            ExtraText = $"LTF not in Reentry ({stateLtfNow})";
            return false;
        }

        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
            return false;

        OmniState stateLtfBack = OmniState.None;
        for (int i = 0; i < 30; i++)
        {
            if (!GetPrevCandle(candleLtf, out candleLtf) || candleLtf == null)
            {
                ExtraText = $"insufficient LTF history for lookback ({i} candles checked)";
                return false;
            }

            stateLtfBack = GetOmniState(candleLtf);
            if (stateLtfBack == OmniState.Extreme || stateLtfBack == OmniState.Mlv
                || stateLtfBack == OmniState.Csm || stateLtfBack == OmniState.Csd)
                break;
        }

        if (!(stateLtfBack == OmniState.Extreme || stateLtfBack == OmniState.Mlv
              || stateLtfBack == OmniState.Csm  || stateLtfBack == OmniState.Csd))
        {
            ExtraText = $"LTF no preceding Extreme/MLV/CSM/CSD found";
            return false;
        }

        var resultMtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, mtf);
        if (!resultMtf.success || resultMtf.candle == null || !IndicatorsOkay(resultMtf.candle))
        {
            ExtraText = $"no data for MTF ({resultMtf.higherInterval.Interval.Name})";
            return false;
        }
        OmniState stateMtf = GetOmniState(resultMtf.candle);

        var resultHtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, htf);
        if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
        {
            ExtraText = $"no data for HTF";
            return false;
        }
        OmniState stateHtf = GetOmniState(resultHtf.candle);

        // HTF trend filter: EMA50 above mid-BB AND Wma05High above mid-BB → bearish bias
        double ema50Htf      = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf      = resultHtf.candle.CandleData!.Sma20!.Value;
        double wma05HighHtf  = resultHtf.candle.CandleData!.Wma05High!.Value;
        if (ema50Htf <= midBbHtf || wma05HighHtf <= midBbHtf)
        {
            ExtraText = $"HTF ema50 not above mid-BB — bullish bias";
            return false;
        }

        if (stateHtf != OmniState.Reentry)
        {
            ExtraText = $"HTF not in Reentry ({stateHtf})";
            return false;
        }

        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle, out string htfSetup))
        {
            ExtraText = $"HTF no CSM/CSD/MHV setup";
            return false;
        }

        string code = OmniStateCode(stateHtf) + OmniStateCode(stateMtf) + OmniStateCode(stateLtfBack);
        if (code == "RRE" || code == "REM" || code == "REE" || code == "RME")
        {
            ExtraText = $"{code} [{htfSetup}] {resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name}";
            return true;
        }

        ExtraText = $"code {code} not valid ({resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name})";
        return false;
    }
}
#endif

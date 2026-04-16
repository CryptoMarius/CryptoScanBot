using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Bbma;

public class SignalBbmaBase : SignalCreateBase
{
    public enum BbmaState
    {
        None,
        Extreme,
        MagicExtreme,
        Mlv, // Mhv
        Csm,
        //CSD
        Reentry
    }



    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.Ema50 == null
           || data.CandleData.Wma05Low == null
           || data.CandleData.Wma10Low == null
           || data.CandleData.Wma05High == null
           || data.CandleData.Wma10High == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.BollingerBandsPercentage == null
           )
            return false;

        return true;
    }


    internal static string TfStateCode(BbmaState state) => state switch
    {
        BbmaState.MagicExtreme => "EE",
        BbmaState.Extreme => "E",
        BbmaState.Mlv => "M",
        BbmaState.Reentry => "R",
        _ => "-"
    };


    /// <summary>
    /// Returns the fixed BBMA higher timeframe pair for the signal interval.
    /// These pairs are fixed (not consecutive steps) and define the 3-TF BBMA system.
    /// </summary>
    internal bool GetIntervals(out CryptoIntervalPeriod interval2, out CryptoIntervalPeriod interval3)
    {
        // For BBMA codes
        switch (Interval.IntervalPeriod)
        {
            case CryptoIntervalPeriod.interval1m:
                interval2 = CryptoIntervalPeriod.interval5m;
                interval3 = CryptoIntervalPeriod.interval15m;
                break;
            case CryptoIntervalPeriod.interval2m:
                interval2 = CryptoIntervalPeriod.interval10m;
                interval3 = CryptoIntervalPeriod.interval30m;
                break;
            case CryptoIntervalPeriod.interval3m:
                interval2 = CryptoIntervalPeriod.interval15m;
                interval3 = CryptoIntervalPeriod.interval1h;
                break;
            case CryptoIntervalPeriod.interval5m:
                interval2 = CryptoIntervalPeriod.interval15m;
                interval3 = CryptoIntervalPeriod.interval1h;
                break;
            case CryptoIntervalPeriod.interval10m:
                interval2 = CryptoIntervalPeriod.interval30m;
                interval3 = CryptoIntervalPeriod.interval2h;
                break;
            case CryptoIntervalPeriod.interval15m:
                interval2 = CryptoIntervalPeriod.interval1h;
                interval3 = CryptoIntervalPeriod.interval4h;
                break;
            case CryptoIntervalPeriod.interval30m:
                interval2 = CryptoIntervalPeriod.interval2h;
                interval3 = CryptoIntervalPeriod.interval8h;
                break;
            case CryptoIntervalPeriod.interval1h:
                interval2 = CryptoIntervalPeriod.interval4h;
                interval3 = CryptoIntervalPeriod.interval1d;
                break;
            case CryptoIntervalPeriod.interval2h:
                interval2 = CryptoIntervalPeriod.interval6h;
                interval3 = CryptoIntervalPeriod.interval1d;
                break;
            case CryptoIntervalPeriod.interval3h:
                interval2 = CryptoIntervalPeriod.interval8h;
                interval3 = CryptoIntervalPeriod.interval1d;
                break;
            case CryptoIntervalPeriod.interval4h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            case CryptoIntervalPeriod.interval6h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            case CryptoIntervalPeriod.interval8h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            case CryptoIntervalPeriod.interval12h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            default:
                ExtraText = $"not accepted interval {Interval.Name}";
                //GlobalData.AddTextToLogTab($"{Symbol.Name} {Interval.IntervalPeriod} {CryptoTradeSide.Long} failed PrepareHigherInterval (1)");
                interval2 = Interval.IntervalPeriod;
                interval3 = Interval.IntervalPeriod;
                return false;
        }
        return true;
    }


    // Is there a previous CSM
    internal bool CheckCsmLong(CryptoInterval interval, MyData? candle, int lookback = 15)
    {
        for (int i = 0; i < lookback; i++)
        {
            if (!GetPrevCandle(interval, candle, out candle))
                return false;

            decimal band = (decimal)candle!.CandleData!.BollingerBandsUpperBand!.Value;

            // Price still reaching BB.Lower → not a genuine MLV phase per PDF.
            if (candle.Candle.Close > band && candle.Candle.Open < band)
                return true;
        }
        return false;
    }

    // Is there a previous CSM
    internal bool CheckCsmShort(CryptoInterval interval, MyData? candle, int lookback = 15)
    {
        for (int i = 0; i < lookback; i++)
        {
            if (!GetPrevCandle(interval, candle, out candle))
                return false;

            decimal band = (decimal)candle!.CandleData!.BollingerBandsLowerBand!.Value;

            // Price still reaching BB.Lower → not a genuine MLV phase per PDF.
            if (candle.Candle.Close < band && candle.Candle.Open > band)
                return true;
        }
        return false;
    }

    // internal enum BbmaStateX { None, FoundExtreme, FoundTPW, ValidMLV }
    //// We use MLV for the abbreviation MHV internally
    //internal BbmaStateX DetectMlv(CryptoInterval interval, MyData candle)
    //{
    //    int lookback = 25;
    //    // Binnen 25 candles zowel een Extreme, TPW en een MLV

    //    // stap 1: Find the most recent extreme
    //    MyData loop = candle;
    //    MyData? extreme = null;
    //    bool isExtremeLow = false;
    //    bool isExtremeHigh = false;
    //    for (int i = 0; i < lookback; i++)
    //    {
    //        // Een Extreme is een CLOSE buiten de BB
    //        decimal lBand = (decimal)loop!.CandleData!.BollingerBandsLowerBand!.Value;
    //        decimal uBand = (decimal)loop!.CandleData!.BollingerBandsUpperBand!.Value;

    //        if (loop.Candle.Close > uBand)
    //        {
    //            extreme = loop;
    //            isExtremeHigh = true;
    //            break;
    //        }
    //        if (loop.Candle.Close < lBand)
    //        {
    //            extreme = loop;
    //            isExtremeLow = true;
    //            break;
    //        }

    //        if (!GetPrevCandle(interval, loop, out loop!))
    //            return BbmaStateX.None;
    //    }

    //    // KRITISCHE CHECK: Geen Extreme gevonden in de lookback? Dan geen MLV.
    //    if (extreme == null)
    //        return BbmaStateX.None;


    //    // stap 2: Is er een TPW (Touch Mid BB) geweest NA de Extreme?
    //    loop = extreme;
    //    bool hasTPW = false;
    //    for (int i = 0; i < lookback; i++)
    //    {
    //        // Price needs to overlap the middle bollingerbands
    //        decimal mBand = (decimal)loop!.CandleData!.Sma20!.Value;
    //        if (loop.Candle.Low <= mBand && loop.Candle.High >= mBand)
    //        {
    //            hasTPW = true;
    //            break;
    //        }
    //        if (!GetNextCandle(interval, loop, out loop!))
    //            return BbmaStateX.None;
    //    }
    //    if (!hasTPW)
    //        return BbmaState.FoundExtreme;


    //    // stap 3: De huidige candle testen op MLV(Rejection)
    //    if (isExtremeHigh)
    //    {
    //        decimal upperBand = (decimal)candle!.CandleData!.BollingerBandsUpperBand!.Value;

    //        // KRITISCH: Als de huidige candle BUITEN de BB sluit, is het Momentum (CSM),
    //        // en dat heft de MLV onmiddellijk op!
    //        if (candle.Candle.Close > upperBand)
    //            return BbmaState.None;

    //        // Detectie: De prijs probeert de uiterste BB te testen maar faalt (Close blijft binnen)
    //        // We kijken of de wick (High/Low) dichtbij de BB komt voor de 'rejection' look
    //        bool isUpperMLV = candle.Candle.High >= upperBand * 0.998m && candle.Candle.Close < upperBand;
    //        return (isUpperMLV) ? BbmaStateX.ValidMLV : BbmaStateX.FoundTPW;
    //    }
    //    else if (isExtremeLow)
    //    {
    //        decimal bbLower = (decimal)candle!.CandleData!.BollingerBandsLowerBand!.Value;

    //        // KRITISCH: Als de huidige candle BUITEN de BB sluit, is het Momentum (CSM),
    //        // en dat heft de MLV onmiddellijk op!
    //        if (candle.Candle.Close < bbLower)
    //            return BbmaStateX.None;

    //        // Detectie: De prijs probeert de uiterste BB te testen maar faalt (Close blijft binnen)
    //        // We kijken of de wick (High/Low) dichtbij de BB komt voor de 'rejection' look
    //        bool isLowerMLV = candle.Candle.Low <= bbLower * 1.002m && candle.Candle.Close > bbLower;
    //        return (isLowerMLV) ? BbmaStateX.ValidMLV : BbmaStateX.FoundTPW;
    //    }

    //    return BbmaStateX.None;
    //}



    /// <summary>
    /// Classifies the BBMA state of a candle for Long setups (uses WMA5/10 on lows).
    /// Priority: MagicExtreme → Extreme → Extreme(Advance) → MHV → Reentry → None
    ///
    /// Extreme (Pine-aligned): MA5(low) below BB.Lower AND wick rejection —
    /// low pierced the band, close recovered inside.
    /// MHV (Pine-aligned): same wick condition as Extreme, but MA5 is inside the band —
    /// a failed second breakout attempt after a previous Extreme.
    /// </summary>
    public static BbmaState BbmaStateLong(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal low = data.Candle.Low;
        decimal close = data.Candle.Close;
        decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
        decimal wma5Low = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal wma10Low = (decimal)data.CandleData!.Wma10Low!.Value;
        decimal middleBand = (decimal)data.CandleData!.Sma20!.Value;
        decimal bbLower = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;

        if (wma5Low < bbLower)
        {
            // MagicExtreme (EE): both MAs below BB.Lower
            if (wma10Low < bbLower)
                return BbmaState.MagicExtreme;

            // Extreme (Pine-aligned): MA5(low) below BB.Lower AND wick rejection
            if (low < bbLower && close > bbLower)
                return BbmaState.Extreme;
        }

        // Extreme type B wick rejection of upper bb
        if (low < bbLower && close > bbLower)
            return BbmaState.Extreme;

        // Extreme (Advance): wick rejection of EMA50 (not in Pine, but valid extension)
        if (low < ema50 && close > ema50 && open > ema50)
            return BbmaState.Extreme;

        // MHV (Market Has No Volume): wick pierced lower band, close recovered, MA5 still inside band
        // Priority above Reentry per Pine: EXT > MHV > RE
        if (low < bbLower && close > bbLower)
            return BbmaState.Mlv;

        if (open > bbLower && close < bbLower)
            return BbmaState.Csm;

        // Reentry: local uptrend, close above mid, low touched the MA5/10 zone
        var upTrend = close > ema50;
        if (upTrend && close >= middleBand && low <= Math.Max(wma5Low, wma10Low))
            return BbmaState.Reentry;

        return BbmaState.None;
    }

    /// <summary>
    /// Classifies the BBMA state of a candle for Short setups (uses WMA5/10 on highs).
    /// Priority: MagicExtreme → Extreme → Extreme(Advance) → MHV → Reentry → None
    ///
    /// Extreme (Pine-aligned): MA5(high) above BB.Upper AND wick rejection —
    /// high pierced the band, close recovered inside.
    /// MHV (Pine-aligned): same wick condition as Extreme, but MA5 is inside the band —
    /// a failed second breakout attempt after a previous Extreme.
    /// </summary>
    public static BbmaState BbmaStateShort(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal high = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
        decimal wma5High = (decimal)data.CandleData!.Wma05High!.Value;
        decimal wma10High = (decimal)data.CandleData!.Wma10High!.Value;
        decimal middleBand = (decimal)data.CandleData!.Sma20!.Value;
        decimal bbUpper = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;

        if (wma5High > bbUpper)
        {
            // MagicExtreme (EE): both MAs above BB.Upper
            if (wma10High > bbUpper)
                return BbmaState.MagicExtreme;

            // Extreme (Pine-aligned): MA5(high) above BB.Upper AND wick rejection
            if (high > bbUpper && close < bbUpper)
                return BbmaState.Extreme;
        }

        // Extreme type B wick rejection of upper bb
        if (high > bbUpper && close < bbUpper)
            return BbmaState.Extreme;

        // Extreme (Advance): wick rejection of EMA50 (not in Pine, but valid extension)
        if (high > ema50 && close < ema50 && open < ema50)
            return BbmaState.Extreme;

        // MHV (Market Has No Volume): wick pierced upper band, close recovered, MA5 still inside band
        // Priority above Reentry per Pine: EXT > MHV > RE
        if (high > bbUpper && close < bbUpper)
            return BbmaState.Mlv;

        if (open < bbUpper && close > bbUpper)
            return BbmaState.Csm;

        // Reentry: local downtrend, close below mid, high touched the MA5/10 zone
        var downTrend = close < ema50;
        if (downTrend && close <= middleBand && high >= Math.Min(wma5High, wma10High))
            return BbmaState.Reentry;

        return BbmaState.None;
    }



}

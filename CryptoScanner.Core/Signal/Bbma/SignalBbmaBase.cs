using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Bbma;

#if DEBUG
public class SignalBbmaBase : SignalCreateBase
{
    public enum BbmaState
    {
        None,
        Extreme,
        MagicExtreme,
        Mlv,
        //Csm,
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
    //        decimal lowerBand = (decimal)candle!.CandleData!.BollingerBandsLowerBand!.Value;

    //        // KRITISCH: Als de huidige candle BUITEN de BB sluit, is het Momentum (CSM),
    //        // en dat heft de MLV onmiddellijk op!
    //        if (candle.Candle.Close < lowerBand)
    //            return BbmaStateX.None;

    //        // Detectie: De prijs probeert de uiterste BB te testen maar faalt (Close blijft binnen)
    //        // We kijken of de wick (High/Low) dichtbij de BB komt voor de 'rejection' look
    //        bool isLowerMLV = candle.Candle.Low <= lowerBand * 1.002m && candle.Candle.Close > lowerBand;
    //        return (isLowerMLV) ? BbmaStateX.ValidMLV : BbmaStateX.FoundTPW;
    //    }

    //    return BbmaStateX.None;
    //}



    /// <summary>
    /// Classifies the BBMA state of a candle for Long setups (uses WMA5/10 on lows).
    /// Priority: MagicExtreme → Extreme(TypeA) → Extreme(TypeB) → Extreme(Advance) → Reentry → Mlv → None
    ///
    /// allowWickDetection: disable for TF2/TF3 because their candles are still forming —
    /// wick levels are not yet final, but MA positions are reliable.
    /// </summary>
    public static BbmaState BbmaStateLong(MyData data, bool allowWickDetection = true)
    {
        decimal wma5Low = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal wma10Low = (decimal)data.CandleData!.Wma10Low!.Value;
        decimal lowerBand = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;

        if (wma5Low < lowerBand)
        {
            // MagicExtreme (EE): both MAs below BB.Lower
            if (wma10Low < lowerBand)
                return BbmaState.MagicExtreme;

            // Extreme (Type A): WMA5(low) below BB.Lower
            return BbmaState.Extreme;
        }


        decimal low = data.Candle.Low;
        if (allowWickDetection)
        {
            decimal close = data.Candle.Close;
            decimal open = data.Candle.Open;

            // Extreme (Type B): wick rejection of BB.Lower
            if (low < lowerBand && close > lowerBand && open > lowerBand)
                return BbmaState.Extreme;

            // Extreme (Advance): wick rejection of EMA50
            decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
            if (low < ema50 && close > ema50 && open > ema50)
                return BbmaState.Extreme;
        }

        // Reentry: bullish CSD active + price reached the 510 buy zone
        //   Standard : close within the zone — between WMA10(low) and WMA5(low)
        //   MA Retest: wick dipped below WMA5(low), close recovered above WMA10(low)
        //if (wma5High > wma10High)
        {
            //decimal wma5High = (decimal)data.CandleData!.Wma05High!.Value;
            //decimal wma10High = (decimal)data.CandleData!.Wma10High!.Value;
            bool priceInZone = low <= wma5Low || low <= wma10Low;
            //bool maRetest = allowWickDetection && low < wma5Low && close > wma10Low;
            //bool maRetest = allowWickDetection && low < wma5Low;
            if (priceInZone) //|| maRetest
                return BbmaState.Reentry;
        }

        // TODO: could be correct, but imho more an assumption?
        // Mlv (Market Loss Volume): WMA5(low) above BB.Lower but below WMA10(low) — pre-CSD
        if (wma5Low >= lowerBand && wma5Low < wma10Low)
            return BbmaState.Mlv;

        return BbmaState.None;
    }

    /// <summary>
    /// Classifies the BBMA state of a candle for Short setups (uses WMA5/10 on highs).
    /// Priority: MagicExtreme → Extreme(TypeA) → Extreme(TypeB) → Extreme(Advance) → Reentry → Mlv → None
    ///
    /// allowWickDetection: disable for TF2/TF3 because their candles are still forming —
    /// wick levels are not yet final, but MA positions are reliable.
    /// </summary>
    public static BbmaState BbmaStateShort(MyData data, bool allowWickDetection = true)
    {
        decimal wma5High = (decimal)data.CandleData!.Wma05High!.Value;
        decimal wma10High = (decimal)data.CandleData!.Wma10High!.Value;
        decimal bbUpper = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;

        if (wma5High > bbUpper)
        {
            // MagicExtreme (EE): both MAs above BB.Upper
            if (wma10High > bbUpper)
                return BbmaState.MagicExtreme;

            // Extreme (Type A): WMA5(high) above BB.Upper
            return BbmaState.Extreme;
        }

        decimal high = data.Candle.High;
        if (allowWickDetection)
        {
            decimal close = data.Candle.Close;
            decimal open = data.Candle.Open;

            // Extreme (Type B): wick rejection of BB.Upper
            if (high > bbUpper && close < bbUpper && open < bbUpper)
                return BbmaState.Extreme;

            // Extreme (Advance): wick rejection of EMA50
            decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
            if (high > ema50 && close < ema50 && open < ema50)
                return BbmaState.Extreme;
        }

        // Reentry: bearish CSD active + price reached the 510 sell zone
        //   Standard : close within the zone — between WMA5(high) and WMA10(high)
        //   MA Retest: wick spiked above WMA5(high), close recovered below WMA10(high)
        //if (wma5High < wma10High)
        {
            bool priceInZone = high >= wma5High || high >= wma10High;
            //bool maRetest = allowWickDetection && high > wma5High && close < wma10High;
            //bool maRetest = allowWickDetection && high > wma5High;
            if (priceInZone) //|| maRetest
                return BbmaState.Reentry;
        }

        // Mlv (MHV phase): WMA5(high) below BB.Upper but above WMA10(high) — pre-CSD
        if (wma5High <= bbUpper && wma5High > wma10High)
            return BbmaState.Mlv;

        return BbmaState.None;
    }



}
#endif

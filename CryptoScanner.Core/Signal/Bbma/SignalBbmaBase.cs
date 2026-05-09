using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

#if DEBUG
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


    //// Is there a previous CSM
    //internal bool CheckCsmLong(CryptoInterval interval, MyData? candle, int lookback = 15)
    //{
    //    for (int i = 0; i < lookback; i++)
    //    {
    //        if (!GetPrevCandle(interval, candle, out candle))
    //            return false;

    //        decimal band = (decimal)candle!.CandleData!.BollingerBandsUpperBand!.Value;

    //        // Price still reaching BB.Lower → not a genuine MLV phase per PDF.
    //        if (candle.Candle.Close > band && candle.Candle.Open < band)
    //            return true;
    //    }
    //    return false;
    //}

    //// Is there a previous CSM
    //internal bool CheckCsmShort(CryptoInterval interval, MyData? candle, int lookback = 15)
    //{
    //    for (int i = 0; i < lookback; i++)
    //    {
    //        if (!GetPrevCandle(interval, candle, out candle))
    //            return false;

    //        decimal band = (decimal)candle!.CandleData!.BollingerBandsLowerBand!.Value;

    //        // Price still reaching BB.Lower → not a genuine MLV phase per PDF.
    //        if (candle.Candle.Close < band && candle.Candle.Open > band)
    //            return true;
    //    }
    //    return false;
    //}

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

}
#endif

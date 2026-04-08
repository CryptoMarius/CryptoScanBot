using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

#if DEBUG
public class SignalBbMaShort : SignalBbmaBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           //|| data.CandleData.Ema50 == null
           //|| data.CandleData.Wma05High == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    //private bool IsExtreme(CryptoCandle data, int backward)
    //{
    //    // go back x extra data(s)?
    //    while (backward-- > 0)
    //    {
    //        decimal wma05High = (decimal)data.CandleData!.Wma05High!;
    //        decimal wma10High = (decimal)data.CandleData!.Wma10High!;
    //        decimal bbUpper = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;

    //        // Extreme Type A: LWMA 5 high/low closes above/below BB
    //        bool extremeTypeA = wma05High > bbUpper;

    //        // Extreme Type B: Bullish/bearish data rejects BB
    //        bool extremeTypeB = data.High > bbUpper && data.Open < bbUpper && data.Close < bbUpper;

    //        // Magic Extreme: LWMA 5 + LWMA 10 outside BB
    //        bool magicExtreme = extremeTypeA && wma10High > bbUpper; // && data.Close < data.Open;

    //        // Advance Extreme: Price rejects EMA 50 (wick rejection)
    //        decimal ema50 = (decimal)data.CandleData!.Ema50!;
    //        bool advanceExtreme = data.High > ema50 && data.Close < ema50 && data.Open < ema50;

    //        if (extremeTypeA || extremeTypeB || advanceExtreme || magicExtreme)
    //            return true;

    //        if (!GetPrevCandle(data, out CryptoCandle? prev))
    //            return false;
    //        data = prev!;
    //    }

    //    return false;
    //}

    //internal bool IsReentry(CryptoCandle data, int backward)
    //{
    //    // go back x extra data(s)?
    //    while (backward-- > 0)
    //    {
    //        if (!GetPrevCandle(data, out CryptoCandle? prev))
    //            return false;

    //        decimal wma05High = (decimal)data.CandleData!.Wma05High!;
    //        decimal wma10High = (decimal)data.CandleData!.Wma10High!;

    //        //decimal wma05HighPrev = (decimal)data.CandleData!.Wma05High!;
    //        //decimal wma10HighPrev = (decimal)data.CandleData!.Wma10High!;

    //        // CSD (CSAK): LWMA5/WMA10 crossover (use lows for buy, highs for sell)
    //        //bool csd = wma05Low > wma10Low && wma05LowPrev <= wma10LowPrev;

    //        //// Early CSD: CSD zonder volledige MLV (hoog risico)
    //        //bool earlyCsdBull = csd && (!signals[tf].ContainsKey("MLV") || !signals[tf]["MLV"].Active);

    //        //// CSM: Strong data after CSD
    //        //double bodySize = Math.Abs(candles[i].Close - candles[i].Open);
    //        //bool strongCandle = bodySize > 0.01 * candles[i].Close;
    //        //bool csmBull = csdBull && strongCandle && candles[i].Close > candles[i].Open;
    //        //bool csmBear = csdBear && strongCandle && candles[i].Close < candles[i].Open;

    //        //// Early CSM: CSM zonder volledige CSD (hoog risico)
    //        //bool earlyCsmBull = csmBull && (!signals[tf].ContainsKey("CSDBull") || !signals[tf]["CSDBull"].Active);
    //        //bool earlyCsmBear = csmBear && (!signals[tf].ContainsKey("CSDBear") || !signals[tf]["CSDBear"].Active);

    //        //// Re-entry Zones (na CSD/CSM)
    //        //bool reentryBuyZone = (csdBull || csmBull || earlyCsdBull || earlyCsmBull) && candles[i].Close >= lwma5_low[i] && candles[i].Close <= lwma10_low[i];
    //        //bool reentrySellZone = (csdBear || csmBear || earlyCsdBear || earlyCsmBear) && candles[i].Close <= lwma5_high[i] && candles[i].Close >= lwma10_high[i];

    //        bool possibleReentry = data.Close >= wma10High && data.Close <= wma05High;
    //        if (possibleReentry)
    //            return true;

    //        data = prev!;
    //    }
    //    return false;
    //}


    public bool Calculate(CryptoIntervalPeriod ltf, CryptoIntervalPeriod mtf, CryptoIntervalPeriod htf)
    {
        CryptoInterval interval1 = GlobalData.IntervalListPeriod[ltf];
        CryptoCandleList candlesTf1 = Symbol.GetSymbolInterval(ltf).CandleList;
        if (candlesTf1.Count == 0)
            return false;

        CryptoInterval interval2 = GlobalData.IntervalListPeriod[mtf];
        CryptoCandleList candlesTf2 = Symbol.GetSymbolInterval(mtf).CandleList;
        if (candlesTf2.Count == 0)
            return false;

        CryptoInterval interval3 = GlobalData.IntervalListPeriod[htf];
        CryptoCandleList candlesTf3 = Symbol.GetSymbolInterval(htf).CandleList;
        if (candlesTf3.Count == 0)
            return false;

        bool result = false;
        var bbma = new BbmaStrategyGrok2();
        bbma.SignalTriggered += (sender, args) =>
        {
            if (args.Event == BbmaStrategyGrok2.BbmaEvent.ReEntry && args.Side == SignalSide)
            {
                result = true;
                ExtraText = $"{interval1.Name}/{interval2.Name}/{interval3.Name}";
                //GlobalData.AddTextToLogTab($"{symbol.Name} ({interval1.Name}/{mtf.Name}/{htf.Name}) {args.Side} {args.Event} {args.Message}");
            }
        };
        bbma.Compute(candlesTf1, candlesTf2, candlesTf3);
        return result;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval5m)
            return false;

        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
            return false;

        return Calculate(Interval.IntervalPeriod, mtf, htf);

        //// For now just focus on the 2 extremes (the second situation), REE

        //// BBMA codes
        //// REE,   1h Reentry 15m Extreme, 5m Extreme
        //// REM,   1h Reentry 15m Extreme, 5m Momentum?

        //if (!PrepareHigherInterval(mtf, out CryptoSymbolInterval interval2_, out CryptoCandle? candle2))
        //{
        //    GlobalData.AddTextToLogTab($"{Symbol.Name} {mtf} {CryptoTradeSide.Long} failed PrepareHigherInterval (2)");
        //    //PrepareHigherInterval(mtf, out interval2_, out candle2);
        //    return false;
        //}
        //if (!IsExtreme(candle2!, 3))
        //    return false;
        //ExtraText += $"{Interval.Name} {CandleLast.DateLocal:dd-MM HH:mm}, {interval2_.Interval.Name} {candle2!.DateLocal}";


        //if (!PrepareHigherInterval(htf, out CryptoSymbolInterval interval3_, out CryptoCandle? candle3))
        //{
        //    //GlobalData.AddTextToLogTab($"{Symbol.Name} {htf} {CryptoTradeSide.Long} failed PrepareHigherInterval (3)");
        //    ExtraText += $", {interval3_.Interval.Name} {candle3?.DateLocal:dd-MM HH:mm} FAILED";
        //    //    PrepareHigherInterval(htf, out interval3_, out candle3); // debug
        //    return false;
        //}
        //if (!IsReentry(candle3!, 3))
        //    return false;
        //ExtraText += $", {interval3_.Interval.Name} {candle3!.DateLocal:dd-MM HH:mm}";


        //return true;
    }

}
#endif
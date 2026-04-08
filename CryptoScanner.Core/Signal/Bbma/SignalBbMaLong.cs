using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

#if DEBUG
public class SignalBbMaLong : SignalBbmaBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           //|| data.CandleData.Ema50 == null
           //|| data.CandleData.Wma05Low == null
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
    //        decimal wma05Low = (decimal)data.CandleData!.Wma05Low!;
    //        decimal wma10Low = (decimal)data.CandleData!.Wma10Low!;
    //        decimal bbLower = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;

    //        // Extreme Type A: LWMA 5 high/low closes above/below BB
    //        bool extremeTypeA = wma05Low < bbLower;

    //        // Extreme Type B: Bullish/bearish data rejects BB
    //        bool extremeTypeB = data.Low < bbLower && data.Close > bbLower && data.Open > bbLower;

    //        // Magic Extreme: LWMA 5 + LWMA 10 outside BB
    //        bool magicExtreme = extremeTypeA && wma10Low < bbLower;

    //        // Advance Extreme: Price rejects EMA 50 (wick rejection)
    //        decimal ema50 = (decimal)data.CandleData!.Ema50!;
    //        bool advanceExtreme = data.Low < ema50 && data.Open > ema50 && data.Close > ema50;

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

    //        decimal wma05Low = (decimal)data.CandleData!.Wma05Low!;
    //        decimal wma10Low = (decimal)data.CandleData!.Wma10Low!;

    //        //decimal wma05LowPrev = (decimal)data.CandleData!.Wma05Low!;
    //        //decimal wma10LowPrev = (decimal)data.CandleData!.Wma10Low!;

    //        // CSD (CSAK): LWMA5/WMA10 crossover (use lows for buy, highs for sell)
    //        //bool csd = wma05Low > wma10Low && wma05LowPrev <= wma10LowPrev;

    //        //// Early CSD: CSD zonder volledige MLV (hoog risico)
    //        //bool earlyCsdBull = csd && (!signals[tf].ContainsKey("MLV") || !signals[tf]["MLV"].Active);

    //        //// CSM: Strong data after CSD
    //        //double bodySize = Math.Abs(candles[i].Close - candles[i].Open);
    //        //bool strongCandle = bodySize > 0.01 * candles[i].Close;
    //        //bool csmBull = csd && strongCandle && candles[i].Close > candles[i].Open;

    //        //// Early CSM: CSM zonder volledige CSD (hoog risico)
    //        //bool earlyCsmBull = csmBull && (!signals[tf].ContainsKey("CSDBull") || !signals[tf]["CSDBull"].Active);

    //        //// Re-entry Zones (na CSD/CSM)
    //        //bool reentryBuyZone = (csd || csmBull || earlyCsdBull || earlyCsmBull) && candles[i].Close >= wma05Low && candles[i].Close <= wma10Low;
    //        //return reentryBuyZone;

    //        bool possibleReentry = data.Close >= wma05Low && data.Close <= wma10Low;
    //        if (possibleReentry)
    //            return true;

    //        data = prev!;
    //    }
    //    return false;
    //}


    public bool Calculate(CryptoIntervalPeriod tf1, CryptoIntervalPeriod tf2, CryptoIntervalPeriod tf3)
    {
        CryptoInterval interval1 = GlobalData.IntervalListPeriod[tf1];
        //LoadSymbolCandles(symbol, interval1);
        CryptoCandleList candlesTf1 = Symbol.GetSymbolInterval(tf1).CandleList;
        if (candlesTf1.Count == 0)
            return false;

        CryptoInterval interval2 = GlobalData.IntervalListPeriod[tf2];
        //LoadSymbolCandles(symbol, mtf);
        CryptoCandleList candlesTf2 = Symbol.GetSymbolInterval(tf2).CandleList;
        if (candlesTf2.Count == 0)
            return false;

        CryptoInterval interval3 = GlobalData.IntervalListPeriod[tf3];
        //LoadSymbolCandles(symbol, htf);
        CryptoCandleList candlesTf3 = Symbol.GetSymbolInterval(tf3).CandleList;
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
        //else ExtraText += $", {interval3_.Interval.Name} {candle3!.DateLocal:dd-MM HH:mm}";


        //return true;
    }

}
#endif
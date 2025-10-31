using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

#if DEBUG
public class SignalBbMaLong : SignalCreateBase
{
    public SignalBbMaLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    private bool GetIntervals(out CryptoIntervalPeriod interval2, out CryptoIntervalPeriod interval3)
    {
        // For BBMA codes
        switch (Interval.IntervalPeriod)
        {
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
            default:
                ExtraText = $"not accepted interval {Interval.Name}";
                //GlobalData.AddTextToLogTab($"{Symbol.Name} {Interval.IntervalPeriod} {CryptoTradeSide.Long} failed PrepareHigherInterval (1)");
                interval2 = Interval.IntervalPeriod;
                interval3 = Interval.IntervalPeriod;
                return false;
        }
        return true;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           //|| candle.CandleData.Ema50 == null
           //|| candle.CandleData.Wma05Low == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    private bool PrepareHigherInterval(CryptoIntervalPeriod higher, out CryptoSymbolInterval higherInterval, out CryptoCandle? candle)
    {
        higherInterval = Symbol.GetSymbolInterval(higher);
        long candleOpenTime = IntervalTools.StartOfIntervalCandle2(CandleLast.OpenTime, Interval.Duration, higherInterval.Interval.Duration);
        if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out candle))
        {
            ExtraText += $"nocandle:{candleOpenTime}";
            return false;
        }

        if (candle.CandleData == null)
        {
            List<CryptoCandle>? history = CandleIndicatorData.CollectCandles(Symbol, higherInterval.Interval, candleOpenTime, out string reason);
            if (history == null)
            {
                DateTime x = CandleTools.GetUnixDate(candleOpenTime);
                ExtraText += $"hist:null {x.ToLocalTime()} {reason}";
                return false;
            }
            CandleIndicatorData.CalculateIndicators(Symbol, higherInterval.Interval, history);
        }

        return true;
    }


    //private bool IsExtreme(CryptoCandle candle, int backward)
    //{
    //    // go back x extra candle(s)?
    //    while (backward-- > 0)
    //    {
    //        decimal wma05Low = (decimal)candle.CandleData!.Wma05Low!;
    //        decimal wma10Low = (decimal)candle.CandleData!.Wma10Low!;
    //        decimal bbLower = (decimal)candle.CandleData!.BollingerBandsLowerBand!.Value;

    //        // Extreme Type A: LWMA 5 high/low closes above/below BB
    //        bool extremeTypeA = wma05Low < bbLower;

    //        // Extreme Type B: Bullish/bearish candle rejects BB
    //        bool extremeTypeB = candle.Low < bbLower && candle.Close > bbLower && candle.Open > bbLower;

    //        // Magic Extreme: LWMA 5 + LWMA 10 outside BB
    //        bool magicExtreme = extremeTypeA && wma10Low < bbLower;

    //        // Advance Extreme: Price rejects EMA 50 (wick rejection)
    //        decimal ema50 = (decimal)candle.CandleData!.Ema50!;
    //        bool advanceExtreme = candle.Low < ema50 && candle.Open > ema50 && candle.Close > ema50;

    //        if (extremeTypeA || extremeTypeB || advanceExtreme || magicExtreme)
    //            return true;

    //        if (!GetPrevCandle(candle, out CryptoCandle? prev))
    //            return false;
    //        candle = prev!;
    //    }

    //    return false;
    //}


    //internal bool IsReentry(CryptoCandle candle, int backward)
    //{
    //    // go back x extra candle(s)?
    //    while (backward-- > 0)
    //    {
    //        if (!GetPrevCandle(candle, out CryptoCandle? prev))
    //            return false;

    //        decimal wma05Low = (decimal)candle.CandleData!.Wma05Low!;
    //        decimal wma10Low = (decimal)candle.CandleData!.Wma10Low!;

    //        //decimal wma05LowPrev = (decimal)candle.CandleData!.Wma05Low!;
    //        //decimal wma10LowPrev = (decimal)candle.CandleData!.Wma10Low!;

    //        // CSD (CSAK): LWMA5/WMA10 crossover (use lows for buy, highs for sell)
    //        //bool csd = wma05Low > wma10Low && wma05LowPrev <= wma10LowPrev;

    //        //// Early CSD: CSD zonder volledige MLV (hoog risico)
    //        //bool earlyCsdBull = csd && (!signals[tf].ContainsKey("MLV") || !signals[tf]["MLV"].Active);

    //        //// CSM: Strong candle after CSD
    //        //double bodySize = Math.Abs(candles[i].Close - candles[i].Open);
    //        //bool strongCandle = bodySize > 0.01 * candles[i].Close;
    //        //bool csmBull = csd && strongCandle && candles[i].Close > candles[i].Open;

    //        //// Early CSM: CSM zonder volledige CSD (hoog risico)
    //        //bool earlyCsmBull = csmBull && (!signals[tf].ContainsKey("CSDBull") || !signals[tf]["CSDBull"].Active);

    //        //// Re-entry Zones (na CSD/CSM)
    //        //bool reentryBuyZone = (csd || csmBull || earlyCsdBull || earlyCsmBull) && candles[i].Close >= wma05Low && candles[i].Close <= wma10Low;
    //        //return reentryBuyZone;

    //        bool possibleReentry = candle.Close >= wma05Low && candle.Close <= wma10Low;
    //        if (possibleReentry)
    //            return true;

    //        candle = prev!;
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
        //LoadSymbolCandles(symbol, interval2);
        CryptoCandleList candlesTf2 = Symbol.GetSymbolInterval(tf2).CandleList;
        if (candlesTf2.Count == 0)
            return false;

        CryptoInterval interval3 = GlobalData.IntervalListPeriod[tf3];
        //LoadSymbolCandles(symbol, interval3);
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
                //GlobalData.AddTextToLogTab($"{symbol.Name} ({interval1.Name}/{interval2.Name}/{interval3.Name}) {args.Side} {args.Event} {args.Message}");
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

        //if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        //{
        //    ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
        //    return false;
        //}

        //if (!IsExtreme(CandleLast, 2))
        //    return false;



        if (!GetIntervals(out CryptoIntervalPeriod interval2, out CryptoIntervalPeriod interval3))
            return false;

        return Calculate(Interval.IntervalPeriod, interval2, interval3);

        //// For now just focus on the 2 extremes (the second situation), REE

        //// BBMA codes
        //// REE,   1h Reentry 15m Extreme, 5m Extreme
        //// REM,   1h Reentry 15m Extreme, 5m Momentum?

        //if (!PrepareHigherInterval(interval2, out CryptoSymbolInterval interval2_, out CryptoCandle? candle2))
        //{
        //    GlobalData.AddTextToLogTab($"{Symbol.Name} {interval2} {CryptoTradeSide.Long} failed PrepareHigherInterval (2)");
        //    //PrepareHigherInterval(interval2, out interval2_, out candle2);
        //    return false;
        //}
        //if (!IsExtreme(candle2!, 3))
        //    return false;
        //ExtraText += $"{Interval.Name} {CandleLast.DateLocal:dd-MM HH:mm}, {interval2_.Interval.Name} {candle2!.DateLocal}";


        //if (!PrepareHigherInterval(interval3, out CryptoSymbolInterval interval3_, out CryptoCandle? candle3))
        //{
        //    //GlobalData.AddTextToLogTab($"{Symbol.Name} {interval3} {CryptoTradeSide.Long} failed PrepareHigherInterval (3)");
        //    ExtraText += $", {interval3_.Interval.Name} {candle3?.DateLocal:dd-MM HH:mm} FAILED";
        //    //    PrepareHigherInterval(interval3, out interval3_, out candle3); // debug
        //    return false;
        //}
        //if (!IsReentry(candle3!, 3))
        //    return false;
        //else ExtraText += $", {interval3_.Interval.Name} {candle3!.DateLocal:dd-MM HH:mm}";


        return true;
    }

}
#endif
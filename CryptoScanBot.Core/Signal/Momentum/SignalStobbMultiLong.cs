using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalStobbMultiLong : SignalSbmBaseLong
{
    public SignalStobbMultiLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.StobbMulti;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Sma20 == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
            CandleLast!.CandleData.StochOscillator,
            CandleLast!.CandleData.StochSignal
        );
    }



    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        {
            if (!CandleLast!.IsSbmConditionsOversold(false))
            {
                response = "geen sbm condities";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        {
            if (!candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (!candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (!candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.IsRsiOversold())
        {
            response = "rsi niet oversold";
            return false;
        }

        if (GlobalData.Settings.Signal.Stobb.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60) == null)
        {
            response = "geen voorgaande stobb gevonden";
            return false;
        }

        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        //// Er een candle onder de bb opent of sluit
        //if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        //{
        //    ExtraText = "niet beneden de bb.lower";
        //    return false;
        //}

        //// Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        //if (!CandleLast.IsStochOversold())
        //{
        //    ExtraText = "stoch niet oversold";
        //    return false;
        //}

        long unixDate = CandleLast.OpenTime;

        // Is it a signal valid over 4 intervals (multistorsi)
        int okay = 4;
        ExtraText = "";
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = 6; count > 0; count--)
        {
            CryptoSymbolInterval higherInterval = Symbol.GetSymbolInterval(intervalPeriod);
            long candleOpenTime = IntervalTools.StartOfIntervalCandle2(unixDate, Interval.Duration, higherInterval.Interval.Duration);
            // todo, not working for emulator!
            CryptoCandle candle = higherInterval.CandleList.Values[^1];

            if (candle.CandleData == null)
            {
                List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
                if (history == null)
                    return false;
                CandleIndicatorData.CalculateIndicators(history);
            }

            if (IndicatorsOkay(candle!) && candle.IsStochOversold() && candle.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
            {
                if (ExtraText != "")
                    ExtraText += ',';
                ExtraText += higherInterval.Interval.Name;

                okay--;
                if (okay == 0)
                    return true;
            }
            else
            {
                // first interval needs to be a signal
                if (count == 6)
                    return false;
            }

            //if (okay < count) return false;

            if (intervalPeriod == CryptoIntervalPeriod.interval1d)
                return false;
            intervalPeriod++;
        }


        //// close date shouw be in the lower part of the bb
        //if (!IsInLowerPartOfBollingerBands(1, 10.0m))
        //    return false;

        ExtraText = "";
        return false;
    }


}


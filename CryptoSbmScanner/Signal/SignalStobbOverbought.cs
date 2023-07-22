using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalStobbOverbought : SignalSbmBaseOverbought
{
    public SignalStobbOverbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        ReplaceSignal = true;
        SignalMode = CryptoOrderSide.Sell;
        SignalStrategy = CryptoSignalStrategy.Stobb;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Sma20 == null)
           || (candle.CandleData.StochSignal == null)
           || (candle.CandleData.StochOscillator == null)
           || (candle.CandleData.BollingerBandsDeviation == null)
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
            CandleLast.CandleData.StochOscillator,
            CandleLast.CandleData.StochSignal
        );
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.StobIncludeSoftSbm)
        {
            if (!CandleLast.IsSbmConditionsOverbought(false))
            {
                response = "geen sbm condities";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.StobIncludeSbmPercAndCrossing)
        {
            if (!candle.IsSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out response))
                return false;
            if (!candle.IsSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out response))
                return false;
            if (!candle.IsSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.StobIncludeRsi && !CandleLast.IsRsiOversold())
        {
            response = "rsi niet overbought";
            return false;
        }


        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.StobbUseLowHigh))
        {
            ExtraText = "niet boven de bb";
            return false;
        }

        // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.IsStochOverbought())
        {
            ExtraText = "stoch niet overbought";
            return false;
        }


        // TODO: Wellicht toch weer activeren?
        // Platte candles of candles zonder volume
        // EXTRA (anders krijg je toch vreemde momenten met platte candles)
        //CryptoSignal Signal = new CryptoSignal();
        //SignalCreate.CalculateAdditionalAlarmProperties(Signal, Candles.Values.ToList(), 30, CandleLast.OpenTime);
        //if (GlobalData.Settings.Signal.CandlesWithZeroVolume > 0 && Signal.CandlesWithZeroVolume >= GlobalData.Settings.Signal.CandlesWithZeroVolume)
        //    return false;
        //if (GlobalData.Settings.Signal.CandlesWithFlatPrice > 0 && Signal.CandlesWithFlatPrice >= GlobalData.Settings.Signal.CandlesWithFlatPrice)
        //    return false;
        //if (GlobalData.Settings.Signal.AboveBollingerBandsSma > 0 && Signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma)
        //    return false;
        //if (GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0 && Signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper)
        //    return false;


        return true;
    }


}


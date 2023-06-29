using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalStobbOverbought : SignalSbmBaseOversold
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
        if (GlobalData.Settings.Signal.StobIncludeSoftSbm)
        {
            if (!CandleLast.IsSbmConditionsOversold(false))
            {
                response = "geen sbm condities";
                return false;
            }
        }

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


    public override bool AllowStepIn(CryptoSignal signal)
    {
        return true;
    }



    public override bool GiveUp(CryptoSignal signal)
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return true;
        }


        if (Math.Min(CandleLast.Open, CandleLast.Close) <= (decimal)CandleLast.CandleData.Sma20)
        {
            //reason = string.Format("{0} give up (pricewise.body < bb) {1}", text, dcaPrice.ToString0());
            ExtraText = "give up (pricewise.body > bb)";
            return true;
        }


        if (CandleLast.CandleData.StochOscillator <= 20)
        {
            ExtraText = "give up(stoch.osc > 80)";
            //AppendLine(string.Format("{0} give up (stoch.osc < 20) {1}", text, dcaPrice.ToString0());
            return true;
        }


        // Langer dan x candles willen we niet wachten
        if ((CandleLast.OpenTime - signal.EventTime) > GlobalData.Settings.Trading.GlobalBuyRemoveTime * Interval.Duration)
        {
            ExtraText = string.Format("Ophouden na {0} candles", GlobalData.Settings.Trading.GlobalBuyRemoveTime);
            return true;
        }

        if (CandleLast.CandleData.PSar < CandleLast.CandleData.Sma20)
        {
            ExtraText = string.Format("De PSAR staat onder de sma20 {0:N8}", CandleLast.CandleData.PSar);
            return true;
        }

        if (Symbol.LastPrice < (decimal)CandleLast.CandleData.Sma20)
        {
            ExtraText = string.Format("De prijs staat onder de sma20 {0:N8}", Symbol.LastPrice);
            return true;
        }


        // Als de barometer alsnog daalt dan stoppen
        BarometerData barometerData = Symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
        if (barometerData.PriceBarometer >= GlobalData.Settings.Trading.Barometer01hBotMinimal)
        {
            ExtraText = string.Format("Barometer 1h {0} below {1}", barometerData.PriceBarometer?.ToString0("N2"), GlobalData.Settings.Trading.Barometer01hBotMinimal.ToString0("N2"));
            return true;
        }

        ExtraText = "";
        return false;
    }

}


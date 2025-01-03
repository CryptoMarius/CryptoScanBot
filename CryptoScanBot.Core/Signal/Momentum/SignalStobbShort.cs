﻿using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalStobbShort : SignalSbmBaseShort
{
    public SignalStobbShort(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Stobb;
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
            CandleLast!.CandleData!.StochOscillator,
            CandleLast!.CandleData!.StochSignal
        );
    }



    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        {
            if (!CandleLast.IsSbmConditionsOverbought(false))
            {
                response = "no sbm conditions";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        {
            if (!candle.IsSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (!candle.IsSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (!candle.IsSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.IsRsiOverbought())
        {
            response = "rsi not overbought";
            return false;
        }

        if (GlobalData.Settings.Signal.Stobb.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60) == null)
        {
            response = "no previous stobb found";
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
            ExtraText = "bb.width te klein " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        {
            ExtraText = "not above bb.upper";
            return false;
        }

        // Sprake van een overbought situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.IsStochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        return true;
    }


}


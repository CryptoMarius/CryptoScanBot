using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelope: SignalCreateBase
{
    public SignalLuxNadarayaWatsonEnvelope(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing, implements both long and short
    }


    private static bool EnoughMomentum(List<CryptoCandle> nwe, int max, out decimal perc)
    {
        // We noticed weak turn's
        int count = 15;
        var o = nwe[max - 1];
        decimal value = o.Close;
        decimal diff = 0;
        for (int i = max - 2; i > 0; i--)
        {
            var o2 = nwe[i];
            decimal d = Math.Abs(o2.Close - value);
            if (d > diff)
                diff = d;

            count--;
            if (count == 0)
                break;
        }

        // less than x% change is not enough
        perc = 100 * diff / value;
        if (perc < 0.25m)
            return true; // false

        return true;
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // TODO implement short version...

        if (GlobalData.Settings.Signal.Nwe.OnlyIfLux5m && SignalSide == CryptoTradeSide.Long)
        {
            if (CandleLast.CandleData!.Lux5mValue > -50)
            {
                response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%)";
                return false;
            }
        }
        if (GlobalData.Settings.Signal.Nwe.OnlyIfLux5m && SignalSide == CryptoTradeSide.Short)
        {
            if (CandleLast.CandleData!.Lux5mValue < 50)
            {
                response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%)";
                return false;
            }
        }

        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Nwe.IncludeSoftSbm && SignalSide == CryptoTradeSide.Long)
        {
            if (!CandleLast!.SbmConditionsOversold(false))
            {
                response = "no sbm conditions";
                return false;
            }
        }
        if (GlobalData.Settings.Signal.Nwe.IncludeSoftSbm && SignalSide == CryptoTradeSide.Short)
        {
            if (!CandleLast.IsSbmConditionsOverbought(false))
            {
                response = "no sbm conditions";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Nwe.IncludeSbmPercAndCrossing && SignalSide == CryptoTradeSide.Long)
        {
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                !candle.Sma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                !candle.Sma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                !candle.Sma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }
        if (GlobalData.Settings.Signal.Nwe.IncludeSbmPercAndCrossing && SignalSide == CryptoTradeSide.Short)
        {
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                !candle.IsSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                !candle.IsSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                !candle.IsSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Nwe.IncludeRsi && SignalSide == CryptoTradeSide.Long && !CandleLast.RsiOversold())
        {
            response = "rsi not oversold";
            return false;
        }
        if (GlobalData.Settings.Signal.Nwe.IncludeRsi && SignalSide == CryptoTradeSide.Short && !CandleLast.RsiOverbought())
        {
            response = "rsi not overbought";
            return false;
        }


        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }
        
		
	
		
		
        // configuration:
        decimal h = GlobalData.Settings.Signal.Nwe.BandWidth;
        decimal mult = GlobalData.Settings.Signal.Nwe.Multiplication;

        // Iterate the last 500 candles
        int maxlen = 500;
        int n = SymbolInterval.CandleList.Count;
        int max = Math.Min(maxlen, n - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) candle in de chart context(dus de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige candle.
        long offsett = CandleLast.OpenTime; // - max * Interval.Duration;

        List<CryptoCandle> nwe = [];
        decimal sae = 0;

        // Compute and set NWE points 
        for (int i = 0; i < max; i++)
        {
            // Compute weighted mean 
            decimal sum = 0;
            decimal sumw = 0;
            for (int j = 0; j < max; j++)
            {
                // Gaussian window
                decimal w = (decimal)Math.Exp(-(Math.Pow(i - j, 2)) / (double)(h * h * 2));
                if (SymbolInterval.CandleList.TryGetValue(offsett - j * Interval.Duration, out CryptoCandle? candlej))
                    sum += candlej.Close * w;
                sumw += w;
            }
            decimal y2 = sum / sumw;

            long openTime = offsett - i * Interval.Duration;
            if (SymbolInterval.CandleList.TryGetValue(openTime, out CryptoCandle? candlei))
            {
                sae += Math.Abs(candlei.Close - y2);

                nwe.Add(new CryptoCandle
                {
                    Close = y2,
                    Open = y2,
                    High = y2,
                    Low = y2,
                    Volume = 0,
                    OpenTime = candlei!.OpenTime,
                    CandleData = candlei?.CandleData,
                });
            }
            else
            {
                nwe.Add(new CryptoCandle
                {
                    Close = y2,
                    Open = y2,
                    High = y2,
                    Low = y2,
                    Volume = 0,
                    OpenTime = openTime,
                    CandleData = null,
                });
            }
        }
        sae = sae / max * mult;


        ExtraText = "";
        nwe.Reverse();
        //List<SlopeResult> slopeNweList = (List<SlopeResult>)nwe.GetSlope(2);
        //var slopeNweLast = slopeNweList[max - 1];
        // Calculate the angle in degrees
        //double angle_radians = Math.Atan(slopeNweLast.Slope ?? 0);
        //double angle_degrees = angle_radians * (180 / Math.PI);
        //double angle_degrees2 = (180 / Math.PI) * (slopeNweLast.Slope ?? 0);

        var candleLast = nwe[max - 1];
        var candlePrev = nwe[max - 2];

        if (candleLast.CandleData == null || candlePrev.CandleData == null)
            return false;


        decimal nwevalue = nwe[max - 1].Close;
        decimal upperband = nwevalue + sae;
        decimal lowerband = nwevalue - sae;

        // buy alert
        if (SignalSide == CryptoTradeSide.Long)
        {
            // Candle outside the band
            if (CandleLast!.Open <= lowerband && CandleLast.Close <= lowerband && EnoughMomentum(nwe, max, out decimal _))
            {
                //ExtraText = $"{angle_degrees2:N2}°, {perc1:N2}%";
                return true;
            }
            // Candle sticking pearsing trough the band
            if (candlePrev!.Close > lowerband && CandleLast.Close <= lowerband && EnoughMomentum(nwe, max, out decimal _))
            {
               //ExtraText = $"{angle_degrees2:N2}°, {perc2:N2}%";
                return true;
            }
        }

        // sell alert
        if (SignalSide == CryptoTradeSide.Short) 
        {
            // Candle outside the band
            if (CandleLast!.Open >= upperband && CandleLast.Close >= upperband && EnoughMomentum(nwe, max, out decimal _))
            {
                //ExtraText = $"{angle_degrees2:N2}°, {perc3:N2}%";
                return true;
            }
            // Candle sticking pearsing trough the band
            if (candlePrev!.Close < upperband && CandleLast.Close >= upperband && EnoughMomentum(nwe, max, out decimal _))
            {
                //ExtraText = $"{angle_degrees2:N2}°, {perc4:N2}%";
                return true;
            }
        }

        return false;
    }

}

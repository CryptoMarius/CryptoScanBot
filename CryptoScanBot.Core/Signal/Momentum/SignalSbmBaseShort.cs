using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

namespace CryptoScanBot.Core.Signal.Momentum;


public static class SignalSbmBaseOverboughtHelper
{
    public static bool IsSbmConditionsOverbought(this CryptoCandle candle, bool includePsarCheck = true)
    {
        // Optimalisatie, zou naar de SignalSbmBaseOverbought kunnen (maar laten staan vanwege overbought)

        // Overbought (denk groen-geel-rood) - short
        // psar
        // 20 (green)
        // 50 (orange)
        // 200 (red)

        // Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        if (candle.CandleData?.Sma200 >= candle.CandleData?.Sma50)
            return false;
        if (candle.CandleData?.Sma200 >= candle.CandleData?.Sma20)
            return false;
        if (candle.CandleData?.Sma50 >= candle.CandleData?.Sma20)
            return false;


        if (includePsarCheck)
        {
            if (candle.CandleData?.PSar < candle.CandleData?.Sma20)
                return false;

            // Dan is de psar omgeslagen (hoort hier niet?)
            if ((decimal)candle.CandleData?.PSar! >= candle.Close)
                return false;
        }

        return true;
    }

    public static bool IsSma200AndSma50OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData?.Sma50! - (decimal)candle.CandleData?.Sma200!;
        decimal value2 = ((decimal)candle.CandleData?.Sma50! + (decimal)candle.CandleData?.Sma200!) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma50AndSma20OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        decimal value = (decimal)candle.CandleData?.Sma20! - (decimal)candle.CandleData?.Sma50!;
        decimal value2 = ((decimal)candle.CandleData?.Sma20! + (decimal)candle.CandleData?.Sma50!) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma200AndSma20OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle!.CandleData?.Sma20! - (decimal)candle.CandleData?.Sma200!;
        decimal value2 = ((decimal)candle!.CandleData?.Sma20! + (decimal)candle.CandleData?.Sma200!) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }
}


public class SignalSbmBaseShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : SignalSbmBase(symbol, interval, candle)
{
    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (!candle.IsSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
            return false;
        if (!candle.IsSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
            return false;
        if (!candle.IsSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
            return false;

        if (!CheckMaCrossings(out response))
            return false;

        return true;
    }

    public bool IsMacdRecoveryOverbought(int candleCount)
    {
        CryptoCandle? last = CandleLast;

        // Hoe positief wil je het hebben?
        //if (last!.CandleData?.MacdHistogram < 0)
        //{
        //    //ExtraText = string.Format("De MACD.Hist is rood {0:N8}", last.CandleData?.MacdHistogram);
        //    return true;
        //}

        // Is er "herstel" ten opzichte van de vorige macd histogram candle?
        int iterator = 0;
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle? prev))
                return false;

            if (last.CandleData?.MacdHistogram >= prev!.CandleData?.MacdHistogram)
                return false;

            //if (last.CandleData?.MacdHistogram <= prev!.CandleData?.MacdHistogram || last.CandleData?.MacdHistogram >= 0)
            //{
            //    // vermeld ik de juiste kleur? ;-)
            //    if (last.CandleData?.MacdHistogram <= 0)
            //    {
            //        // Hoe positief wil je het hebben?
            //        //ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtrood {1:N8} {2:N8} (last)", iterator, prev.CandleData?.MacdHistogram, last.CandleData?.MacdHistogram);
            //    }
            //    else
            //    {
            //        //ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtgroen {1:N8} {2:N8} (last)", iterator, prev.CandleData?.MacdHistogram, last.CandleData?.MacdHistogram);
            //        return false;
            //    }
            //}

            iterator--;
            last = prev;
            candleCount--;
        }

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // De ma lijnen en psar goed staan
        if (!CandleLast!.IsSbmConditionsOverbought(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // Er recovery is via de macd
        if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Deze routine is een beetje back to the basics, gewoon een nette SBM, vervolgens
        // 2 MACD herstel candles, wat rsi en stoch condities om glijbanen te voorkomen

        if (!GetPrevCandle(CandleLast, out CryptoCandle? candlePrev))
            return false;


        // ********************************************************************
        if (GlobalData.Settings.Trading.CheckFurtherPriceMove)
        {
            if (CandleLast.Close >= candlePrev!.Close)
            {
                ExtraText = $"Price {candlePrev!.Close:N8} goes up even more {CandleLast.Close:N8}";
                return false;
            }

            //if (CheckPriceGoingDown(signal))
            //{
            //    ExtraText = $"Price going down";
            //    return false;
            //}
        }




        // ********************************************************************
        // MACD
        if (GlobalData.Settings.Trading.CheckIncreasingMacd)
        {
            if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
            {
                // ExtraText is al ingevuld
                return false;
            }
        }


        // ********************************************************************
        // RSI decreasing
        if (GlobalData.Settings.Trading.CheckIncreasingRsi)
        {
            //// At least x which is kind of a minimum (normally 30-70), hardcoded because we can change it
            //double? boundary = 85;
            //if (CandleLast?.CandleData!.Rsi > boundary)
            //{
            //    ExtraText = $"RSI {CandleLast?.CandleData!.Rsi:N8} not below {boundary:N0}";
            //    return false;
            //}

            // RSI should recover
            if (CandleLast?.CandleData?.Rsi >= candlePrev?.CandleData?.Rsi)
            {
                ExtraText = $"Rsi {candlePrev.CandleData.Rsi:N8} not recovering >= {CandleLast.CandleData.Rsi:N8}";
                return false;
            }

            //if (!RsiDecreasingInTheLast(3, 1))
            //{
            //    ExtraText = string.Format("RSI not descreasing in the last 3,1");
            //    return false;
            //}
        }

        // ********************************************************************
        // PSAR
        //if ((decimal)CandleLast.CandleData?.PSar > CandleLast.Close)
        //{
        //    ExtraText = string.Format("De PSAR staat niet onder de prijs {0:N8}", CandleLast.CandleData?.PSar);
        //    return false;
        //}


        // ********************************************************************
        // STOCH
        // Stochastic:
        // Red %D = signal, average from the last 3 %K values
        // Blue %K = Oscilator calculated from the last 14 candles
        if (GlobalData.Settings.Trading.CheckIncreasingStoch)
        {
            // Stochastic: Omdat ik ze door elkaar haal
            // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
            // Blauw %K = Oscilator berekend over een lookback periode van 14 candles

            //// At least 80 which is kind of a minimum (normally 20-80), hardcoded because we can change it
            //double? boundary = 88;
            //if (CandleLast?.CandleData!.StochOscillator > boundary)
            //{
            //    ExtraText = $"Stoch.%K {CandleLast?.CandleData!.StochOscillator:N8} not below {boundary:N0}";
            //    return false;
            //}

            // %K should recover
            if (CandleLast?.CandleData!.StochOscillator >= candlePrev?.CandleData?.StochOscillator)
            {
                ExtraText = $"Stoch.K {candlePrev.CandleData.StochOscillator:N8} not recovering > {CandleLast.CandleData?.StochOscillator:N8}";
                return false;
            }

            // De %D en %K should moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
            if (CandleLast?.CandleData?.StochSignal >= CandleLast?.CandleData?.StochOscillator)
            {
                ExtraText = $"Stoch.%D {candlePrev?.CandleData?.StochSignal:N8} not below %K {candlePrev?.CandleData?.StochOscillator:N8}";
                return false;
            }
        }


        // Koop als de close vlak bij de bb.upper is (c.q. niet te ver naar boven zit)
        // Werkt goed!!! (toch even experimenteren) - maar negeert hierdoor ook veel signalen die wel bruikbaar waren
        //double? value = CandleLast.CandleData?.BollingerBandsUpperBand - 0.25 * CandleLast.CandleData?.BollingerBandsDeviation;
        //if (Symbol.LastPrice < (decimal)value)
        //{
        //    ExtraText = string.Format("Symbol.Lastprice {0:N8} > BB.Upper + 0.25 * StdDev {1:N8}", Symbol.LastPrice, value);
        //    signal.LastPrice = Symbol.LastPrice;
        //    return false;
        //}

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        //// ********************************************************************
        //// Als BTC snel gedaald is dan stoppen (NB: houdt geen rekening met closedate!)
        //if (GlobalData.PauseTrading.Until >= CandleLast.OpenTime)
        //{
        //    ExtraText = string.Format("De bot is gepauseerd omdat {0}", GlobalData.PauseTrading.Text);
        //    return true;
        //}


        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return true;
        }



        // ********************************************************************
        // Instaptijd verstreken (oneindig wachten is geen optie)
        if (CandleLast?.OpenTime - signal.EventTime > GlobalData.Settings.Trading.EntryRemoveTime * Interval.Duration)
        {
            ExtraText = $"Stop after {GlobalData.Settings.Trading.EntryRemoveTime} candles";
            return true;
        }


        // ********************************************************************
        // PSAR
        //if ((decimal)CandleLast.CandleData?.PSar < CandleLast.Close)
        //{
        //    ExtraText = string.Format("De PSAR staat onder de prijs {0:N8}", CandleLast.CandleData?.PSar);
        //    return true;
        //}

        // alsnog een neerwaardse richting gekozen (wel een rare conditie)
        //if (CandleLast.CandleData?.PSar > CandleLast.CandleData?.Sma20)
        //{
        //    ExtraText = string.Format("De PSAR staat boven de sma20 {0:N8}", CandleLast.CandleData?.PSar);
        //    return true;
        //}


        // ********************************************************************
        // BB - buiten de grenzen
        // okay, ff wachten, er komt vast nog een melding
        // Er een candle onder de bb opent of sluit (eigenlijk overbodig icm macd)
        //if (CandleLast.Close < (decimal)CandleLast.CandleData?.BollingerBandsLowerBand || Symbol.LastPrice < (decimal)CandleLast.CandleData?.BollingerBandsLowerBand)
        //{
        //    ExtraText = "Close of LastPrice beneden de bb.lower";
        //    return true;
        //}

        if (CandleLast!.Close < (decimal)CandleLast!.CandleData?.BollingerBandsLowerBand! || Symbol.LastPrice < (decimal)CandleLast.CandleData?.BollingerBandsLowerBand!)
        {
            ExtraText = "Close of LastPrice below bb.lower";
            return true;
        }




        // ********************************************************************
        // RSI
        // okay, ff wachten - slope van de laatste 5 candles
        // Die slope werkt niet lekker vindt ik, nog eens nazoeken
        // Er een candle onder de bb opent of sluit (eigenlijk overbodig icm macd)
        //if (CandleLast.CandleData?.SlopeRsi < 0) 
        //{
        //    ExtraText = "Slope RSI < 0";
        //    return true;
        //}

        // 2023-04-29 12:15 toegevoegd: Neergaande rsi meldingen vermijden.
        //if (!RsiDecreasingInTheLast(3, 1))
        //{
        //    ExtraText = string.Format("RSI aflopend in de laatste 3,1, laat maar");
        //    return true;
        //}



        // ********************************************************************
        // Barometer(s)
        if (!BarometerHelper.ValidBarometerConditions(GlobalData.ActiveExchange!, Symbol.Quote, TradingConfig.Trading[CryptoTradeSide.Short].Barometer, out ExtraText))
            return true;


        ExtraText = "";
        return false;
    }

}

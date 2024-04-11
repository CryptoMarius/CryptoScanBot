using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Trader;

namespace CryptoScanBot.Signal;


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
        if (candle.CandleData.Sma200 >= candle.CandleData.Sma50)
            return false;
        if (candle.CandleData.Sma200 >= candle.CandleData.Sma20)
            return false;
        if (candle.CandleData.Sma50 >= candle.CandleData.Sma20)
            return false;


        if (includePsarCheck)
        {
            if (candle.CandleData.PSar < candle.CandleData.Sma20)
                return false;

            // Dan is de psar omgeslagen (hoort hier niet?)
            if ((decimal)candle.CandleData.PSar >= candle.Close)
                return false;
        }

        return true;
    }

    public static bool IsSma200AndSma50OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma50 - (decimal)candle.CandleData.Sma200;
        decimal value2 = ((decimal)candle.CandleData.Sma50 + (decimal)candle.CandleData.Sma200) / 2;
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
        decimal value = (decimal)candle.CandleData.Sma20 - (decimal)candle.CandleData.Sma50;
        decimal value2 = ((decimal)candle.CandleData.Sma20 + (decimal)candle.CandleData.Sma50) / 2;
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
        decimal value = (decimal)candle.CandleData.Sma20 - (decimal)candle.CandleData.Sma200;
        decimal value2 = ((decimal)candle.CandleData.Sma20 + (decimal)candle.CandleData.Sma200) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsStochOverbought(this CryptoCandle candle)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten groter zijn dan 80% (overbought)
        if (candle.CandleData.StochSignal < GlobalData.Settings.General.StochValueOverbought)
            return false;
        if (candle.CandleData.StochOscillator < GlobalData.Settings.General.StochValueOverbought)
            return false;
        return true;
    }


    public static bool IsRsiOverbought(this CryptoCandle candle)
    {
        if (candle.CandleData.Rsi < GlobalData.Settings.General.RsiValueOverbought)
            return false;
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
        CryptoCandle last = CandleLast;

        // Hoe positief wil je het hebben?
        if (last.CandleData.MacdHistogram < 0)
        {
            //ExtraText = string.Format("De MACD.Hist is rood {0:N8}", last.CandleData.MacdHistogram);
            return true;
        }

        // Is er "herstel" ten opzichte van de vorige macd histogram candle?
        int iterator = 0;
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
                return false;


            if (last.CandleData.MacdHistogram >= prev.CandleData.MacdHistogram || last.CandleData.MacdHistogram < 0)
            {
                // vermeld ik de juiste kleur? ;-)
                if (last.CandleData.MacdHistogram <= 0)
                {
                    // Hoe positief wil je het hebben?
                    //ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtrood {1:N8} {2:N8} (last)", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                }
                else
                {
                    //ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtgroen {1:N8} {2:N8} (last)", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                    return false;
                }
            }

            iterator--;
            last = prev;
            candleCount--;
        }

        return true;
    }


    protected bool IsInUpperPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Is de prijs onlangs dicht bij de bovenste bb geweest?

        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            decimal value = (decimal)last.CandleData.BollingerBandsUpperBand;
            value -= (decimal)last.CandleData.BollingerBandsDeviation * percentage / 100m;

            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
            {
                if (last.High >= value)
                    return true;
            }
            else
            {
                if (last.Open >= value || last.Close >= value)
                    return true;
            }

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }

    //protected bool HadIncreasingVolume(out string reaction)
    //{
    //    // Het gemiddelde volume uitrekenen, als de vorige candle ~4* die avg-volume heeft en 
    //    // de volgende candle heeft een flink lagere volume dan is er grote kans op een correctie

    //    // Het probleem is alleen hoe we te lage volumes detecteren?

    //    reaction = "";
    //    decimal sumVolume = 0;

    //    int count = 20;
    //    for (int i = Candles.Count - 1; i > 0; i--)
    //    {
    //        CryptoCandle candle = Candles.Values[i];
    //        sumVolume += candle.Volume;
    //        count--;
    //        if (count < 0)
    //            break;
    //    }
    //    decimal avgVolume = sumVolume / 20;

    //    // De volume van die laatste x candles moet boven de 1% volume van de munt zitten
    //    if (sumVolume < 0.01m * Symbol.Volume)
    //    {
    //        reaction = string.Format("Geen volume {0:N8} volume(20)={0:N8} < 0.01m*Symbol.Volume {1:N8}", sumVolume, 0.01m * Symbol.Volume);
    //        return false;
    //    }


    //    if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
    //    {
    //        reaction = ExtraText;
    //        return false;
    //    }

    //    if (candlePrev.Volume > 3 * avgVolume && CandleLast.Volume < avgVolume && CandleLast.Volume < candlePrev.Volume) 
    //    {
    //        reaction = string.Format("Volume spike prev={0:N8} last={1:N8}", candlePrev.Volume, CandleLast.Volume);
    //        return true;
    //    }


    //    return false;
    //}


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // De ma lijnen en psar goed staan
        if (!CandleLast.IsSbmConditionsOverbought(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // Er recovery is via de macd
        if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
            return false;

        return true;
    }


    /// <summary>
    /// Is de RSI oplopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public bool IsRsiIncreasingInTheLast(int candleCount, int allowedDown)
    {
        // We gaan van rechts naar links (van de nieuwste candle richting verleden)

        int down = 0;
        bool first = true;
        //StringBuilder log = new();
        CryptoCandle last = CandleLast;


        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
                return false;

            if (last.CandleData.Rsi <= prev.CandleData.Rsi)
            {
                down++;
                if (first)
                {
                    //log.AppendLine(string.Format("RSI controle count={0} prev={1:N8} last={2:N8} down={3} (first)", candleCount, prev.CandleData.Rsi, last.CandleData.Rsi, down));
                    //GlobalData.AddTextToLogTab(log.ToString());
                    return false;
                }

                if (down > allowedDown)
                    return false;
            }

            //log.AppendLine(string.Format("RSI controle count={0} prev={1:N8} last={2:N8} down={3}", candleCount, prev.CandleData.Rsi, last.CandleData.Rsi, down));
            last = prev;
            candleCount--;
            first = false;
        }

        //GlobalData.AddTextToLogTab(log.ToString());
        //if (down > allowedDown)
        //    return false;
        return true;
    }

    /// <summary>
    /// Is de RSI aflopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public bool IsRsiDecreasingInTheLast(int candleCount, int allowedDown)
    {
        // We gaan van rechts naar links (van de nieuwste candle richting verleden)
        int down = 0;
        bool first = true;
        CryptoCandle last = CandleLast;


        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
                return false;

            if (last.CandleData.Rsi >= prev.CandleData.Rsi)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            last = prev;
            candleCount--;
            first = false;
        }

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Deze routine is een beetje back to the basics, gewoon een nette SBM, vervolgens
        // 2 MACD herstel candles, wat rsi en stoch condities om glijbanen te voorkomen

        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
            return false;

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
        // RSI
        if (GlobalData.Settings.Trading.CheckIncreasingRsi)
        {
            // Is there any RSI recovery visible (a bit weak)
            if (CandleLast.CandleData.Rsi > GlobalData.Settings.General.RsiValueOverbought)
            {
                ExtraText = $"RSI {CandleLast.CandleData.Rsi:N8} niet boven de {GlobalData.Settings.General.RsiValueOverbought}";
                return false;
            }

            // 2023-04-28 15:11 Afgesterd, hierdoor stappen we te laat in?
            // 2023-04-29 12:15 Weer geactiveerd: Het vermijden van glijbanen.
            // Dus we stappen nu later in, maar met een beetje meer zekerheid?
            if (!IsRsiDecreasingInTheLast(3, 1))
            {
                ExtraText = string.Format("RSI niet aflopend in de laatste 3,1");
                return false;
            }
        }

        // ********************************************************************
        // PSAR
        //if ((decimal)CandleLast.CandleData.PSar > CandleLast.Close)
        //{
        //    ExtraText = string.Format("De PSAR staat niet onder de prijs {0:N8}", CandleLast.CandleData.PSar);
        //    return false;
        //}


        // ********************************************************************
        // STOCH
        // Stochastic: Omdat ik ze door elkaar haal
        // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
        // Blauw %K = Oscilator berekend over een lookback periode van 14 candles
        if (GlobalData.Settings.Trading.CheckIncreasingStoch)
        {
            // Stochastic: Omdat ik ze door elkaar haal
            // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
            // Blauw %K = Oscilator berekend over een lookback periode van 14 candles

            // Afgesterd - 27-04-2023 10:12
            // Met name de %K..
            if (CandleLast.CandleData.StochOscillator > candlePrev.CandleData.StochOscillator)
            {
                ExtraText = string.Format("Stoch.K {0:N8} hersteld niet > {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
                return false;
            }

            // Afgesterd - 27-04-2023 10:12
            //double? minimumStoch = 84;
            //if (CandleLast.CandleData.StochOscillator > minimumStoch)
            //{
            //    ExtraText = string.Format("Stoch.K {0:N8} niet onder de {1:N0}", candlePrev.CandleData.StochOscillator, minimumStoch);
            //    return false;
            //}

            // De %D en %K moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
            if (CandleLast.CandleData.StochSignal < CandleLast.CandleData.StochOscillator)
            {
                ExtraText = string.Format("Stoch.%D {0:N8} heeft de %K {1:N8} niet gekruist", candlePrev.CandleData.StochSignal, candlePrev.CandleData.StochOscillator);
                return false;
            }
        }


        // ********************************************************************
        // Extra?

        // Profiteren van een nog hogere prijs?
        if (GlobalData.Settings.Trading.CheckFurtherPriceMove)
        {
            if (Symbol.LastPrice > signal.LastPrice)
            {
                if (Symbol.LastPrice != signal.LastPrice)
                {
                    ExtraText = string.Format("Symbol.LastPrice {0:N8} gaat verder naar boven {1:N8}", Symbol.LastPrice, signal.LastPrice);
                }
                return false;
            }
        }
        signal.LastPrice = (decimal)Symbol.LastPrice;

        // Koop als de close vlak bij de bb.upper is (c.q. niet te ver naar boven zit)
        // Werkt goed!!! (toch even experimenteren) - maar negeert hierdoor ook veel signalen die wel bruikbaar waren
        //double? value = CandleLast.CandleData.BollingerBandsUpperBand - 0.25 * CandleLast.CandleData.BollingerBandsDeviation;
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
        //if (GlobalData.PauseTrading.Until >= CandleLast.Date)
        //{
        //    ExtraText = string.Format("De bot is gepauseerd omdat {0}", GlobalData.PauseTrading.Text);
        //    return true;
        //}


        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return true;
        }
		

		
		// ********************************************************************
        // Instaptijd verstreken (oneindig wachten is geen optie)
        if ((CandleLast.OpenTime - signal.EventTime) > GlobalData.Settings.Trading.GlobalBuyRemoveTime * Interval.Duration)
        {
            ExtraText = $"Ophouden na {GlobalData.Settings.Trading.GlobalBuyRemoveTime} candles";
            return true;
        }


        // ********************************************************************
        // PSAR
        //if ((decimal)CandleLast.CandleData.PSar < CandleLast.Close)
        //{
        //    ExtraText = string.Format("De PSAR staat onder de prijs {0:N8}", CandleLast.CandleData.PSar);
        //    return true;
        //}

        // alsnog een neerwaardse richting gekozen (wel een rare conditie)
        //if (CandleLast.CandleData.PSar > CandleLast.CandleData.Sma20)
        //{
        //    ExtraText = string.Format("De PSAR staat boven de sma20 {0:N8}", CandleLast.CandleData.PSar);
        //    return true;
        //}


        // ********************************************************************
        // BB - buiten de grenzen
        // okay, ff wachten, er komt vast nog een melding
        // Er een candle onder de bb opent of sluit (eigenlijk overbodig icm macd)
        //if (CandleLast.Close < (decimal)CandleLast.CandleData.BollingerBandsLowerBand || Symbol.LastPrice < (decimal)CandleLast.CandleData.BollingerBandsLowerBand)
        //{
        //    ExtraText = "Close of LastPrice beneden de bb.lower";
        //    return true;
        //}

        if (CandleLast.Close < (decimal)CandleLast.CandleData.BollingerBandsLowerBand || Symbol.LastPrice < (decimal)CandleLast.CandleData.BollingerBandsLowerBand)
        {
            ExtraText = "Close of LastPrice boven de bb.lower";
            return true;
        }




        // ********************************************************************
        // RSI
        // okay, ff wachten - slope van de laatste 5 candles
        // Die slope werkt niet lekker vindt ik, nog eens nazoeken
        // Er een candle onder de bb opent of sluit (eigenlijk overbodig icm macd)
        //if (CandleLast.CandleData.SlopeRsi < 0) 
        //{
        //    ExtraText = "Slope RSI < 0";
        //    return true;
        //}

        // 2023-04-29 12:15 toegevoegd: Neergaande rsi meldingen vermijden.
        //if (!IsRsiDecreasingInTheLast(3, 1))
        //{
        //    ExtraText = string.Format("RSI aflopend in de laatste 3,1, laat maar");
        //    return true;
        //}



        // ********************************************************************
        // Barometer(s)
        // Als de barometer alsnog daalt/stijgt dan stoppen
        //foreach (KeyValuePair<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> entry in TradingConfig.Trading[CryptoTradeSide.Short].Barometer)
        //{
        //    if (!SymbolTools.CheckValidBarometer(Symbol.QuoteData, entry.Key, entry.Value, out ExtraText))
        //        return true;
        //}
        if (!BarometerHelper.ValidBarometerConditions(Symbol.QuoteData, TradingConfig.Trading[CryptoTradeSide.Short].Barometer, out ExtraText))
            return true;


        ExtraText = "";
        return false;
    }

}

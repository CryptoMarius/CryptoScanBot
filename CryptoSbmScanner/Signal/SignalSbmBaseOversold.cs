using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

namespace CryptoSbmScanner.Signal;


public static class SignalSbmBaseOversoldHelper
{
    public static bool IsSbmConditionsOversold(this CryptoCandle candle, bool includePsarCheck)
    {
        // Optimalisatie, zou naar de SignalSbmBaseOversold kunnen, maar de stobb gebruikt deze routine ook

        // Oversold (denk groen-geel-rood) - long
        // 200 (red)
        // 50 (orange)
        // 20 (green)
        // psar

        // Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        if (candle.CandleData.Sma50 >= candle.CandleData.Sma200)
            return false;
        if (candle.CandleData.Sma20 >= candle.CandleData.Sma200)
            return false;
        if (candle.CandleData.Sma20 >= candle.CandleData.Sma50)
            return false;


        if (includePsarCheck)
        {
            if (candle.CandleData.PSar > candle.CandleData.Sma20)
                return false;

            // Dan is de psar omgeslagen (hoort hier niet)
            if ((decimal)candle.CandleData.PSar <= candle.Close)
                return false;
        }

        return true;
    }

    public static bool IsSma200AndSma50OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma200 - (decimal)candle.CandleData.Sma50;
        decimal value2 = ((decimal)candle.CandleData.Sma200 + (decimal)candle.CandleData.Sma50) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma50AndSma20OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        decimal value = (decimal)candle.CandleData.Sma50 - (decimal)candle.CandleData.Sma20;
        decimal value2 = ((decimal)candle.CandleData.Sma50 + (decimal)candle.CandleData.Sma20) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma200AndSma20OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma200 - (decimal)candle.CandleData.Sma20;
        decimal value2 = ((decimal)candle.CandleData.Sma200 + (decimal)candle.CandleData.Sma20) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }

    public static bool IsStochOversold(this CryptoCandle candle)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData.StochSignal > GlobalData.Settings.General.StochValueOversold)
            return false;
        if (candle.CandleData.StochOscillator > GlobalData.Settings.General.StochValueOversold)
            return false;
        return true;
    }

    public static bool IsRsiOversold(this CryptoCandle candle)
    {
        if (candle.CandleData.Rsi > GlobalData.Settings.General.RsiValueOversold)
            return false;
        return true;
    }

}


public class SignalSbmBaseOversold : SignalSbmBase
{
    public SignalSbmBaseOversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (!candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out response))
            return false;
        if (!candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out response))
            return false;
        if (!candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out response))
            return false;

        if (!CheckMaCrossings(out response))
            return false;

        return true;
    }

    public bool IsMacdRecoveryOversold(int candleCount)
    {
        CryptoCandle last = CandleLast;

        // Hoe positief wil je het hebben?
        if (last.CandleData.MacdHistogram > 0)
        {
            //ExtraText = string.Format("De MACD.Hist is groen {0:N8}", last.CandleData.MacdHistogram);
            return true; // niet echt een juiste controle
        }

        // Is er "herstel" ten opzichte van de vorige macd histogram candle?
        int iterator = 0;
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
                return false;


            if (last.CandleData.MacdHistogram <= prev.CandleData.MacdHistogram || last.CandleData.MacdHistogram >= 0)
            {
                // TODO: Vermeld ik wel de juiste kleur(en)? 
                if (last.CandleData.MacdHistogram >= 0)
                {
                    // Hoe positief wil je het hebben?
                    //ExtraText = string.Format("De MACD[{0:N0}].Hist is groen {1:N8} {2:N8}", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                }
                else
                {
                    //ExtraText = string.Format("De MACD[{0:N0}].Hist is niet roze {1:N8} {2:N8}", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                    return false;
                }
            }

            iterator--;
            last = prev;
            candleCount--;
        }

        return true;
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
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.SbmBBMinPercentage, GlobalData.Settings.Signal.SbmBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // De ma lijnen en psar goed staan
        if (!CandleLast.IsSbmConditionsOversold(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // Er recovery is via de macd
        if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
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
        CryptoCandle last = CandleLast;

        // De laatste candle MOET altijd verbeteren
        if (!GetPrevCandle(last, out CryptoCandle prev))
            return false;
        if (last.CandleData.Rsi > prev.CandleData.Rsi)
            return false;


        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount-- >= 0)
        {
            if (!GetPrevCandle(last, out prev))
                return false;

            if (last.CandleData.Rsi > prev.CandleData.Rsi)
                down++;

            last = prev;
        }

        if (down > allowedDown)
            return false;
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
            if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
            {
                // ExtraText is al ingevuld
                return false;
            }
        }


        // ********************************************************************
        // RSI
        if (GlobalData.Settings.Trading.CheckIncreasingRsi)
        {
            if (CandleLast.CandleData.Rsi < GlobalData.Settings.General.RsiValueOversold) // was 25 
            {
                ExtraText = $"RSI {CandleLast.CandleData.Rsi:N8} niet boven de {GlobalData.Settings.General.RsiValueOversold}";
                return false;
            }

            // 2023-04-28 15:11 Afgesterd, hierdoor stappen we te laat in?
            // 2023-04-29 12:15 Weer geactiveerd: Het vermijden van glijbanen.
            // Dus we stappen nu later in, maar met een beetje meer zekerheid?
            if (!IsRsiIncreasingInTheLast(3, 1))
            {
                ExtraText = string.Format("RSI niet oplopend in de laatste 3,1");
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
        if (GlobalData.Settings.Trading.CheckIncreasingStoch)
        {
            // Stochastic: Omdat ik ze door elkaar haal
            // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
            // Blauw %K = Oscilator berekend over een lookback periode van 14 candles

            // Afgesterd - 27-04-2023 10:12
            // Met name de %K moet herstellen
            if (candlePrev.CandleData.StochOscillator >= CandleLast.CandleData.StochOscillator)
            {
                ExtraText = string.Format("Stoch.K {0:N8} hersteld niet < {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
                return false;
            }

            // Afgesterd - 27-04-2023 10:12
            //double? minimumStoch = 25;
            //if (CandleLast.CandleData.StochOscillator < minimumStoch)
            //{
            //    ExtraText = string.Format("Stoch.K {0:N8} niet boven de {1:N0}", candlePrev.CandleData.StochOscillator, minimumStoch);
            //    return false;
            //}

            // De %D en %K moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
            if (CandleLast.CandleData.StochSignal > CandleLast.CandleData.StochOscillator)
            {
                ExtraText = string.Format("Stoch.%D {0:N8} heeft de %K {1:N8} niet gekruist", candlePrev.CandleData.StochSignal, candlePrev.CandleData.StochOscillator);
                return false;
            }
        }



        //// Ter debug de laatste 4 RSI waarden verzamelen (daar moet iets fout gaan, maar wat?)
        //int count = 4;
        //string debug = "";
        //CryptoCandle oldCandle = signal.Candle;
        //CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
        //while (count > 0)
        //{
        //    if (debug == "")
        //        debug = oldCandle.CandleData.Rsi?.ToString("N3");
        //    else
        //        debug = oldCandle.CandleData.Rsi?.ToString("N3") + ", " + debug;

        //    if (!symbolInterval.CandleList.TryGetValue(oldCandle.OpenTime - Interval.Duration, out oldCandle))
        //        break;

        //    count--;
        //}
        //debug = "(" + debug + ")";
        //ExtraText += debug;

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

        // ********************************************************************
        // Instaptijd verstreken (oneindig wachten is geen optie)
        if ((CandleLast.OpenTime - signal.EventTime) > GlobalData.Settings.Trading.GlobalBuyRemoveTime * Interval.Duration)
        {
            ExtraText = string.Format("Ophouden na {0} candles", GlobalData.Settings.Trading.GlobalBuyRemoveTime);
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

        if (CandleLast.Close > (decimal)CandleLast.CandleData.BollingerBandsUpperBand || Symbol.LastPrice > (decimal)CandleLast.CandleData.BollingerBandsUpperBand)
        {
            ExtraText = "Close of LastPrice boven de bb.upper";
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
        if (!SymbolTools.CheckValidBarometer(Symbol.QuoteData, CryptoIntervalPeriod.interval1h, GlobalData.Settings.Trading.Barometer01hBotMinimal, out ExtraText))
            return true;

        if (!SymbolTools.CheckValidBarometer(Symbol.QuoteData, CryptoIntervalPeriod.interval4h, GlobalData.Settings.Trading.Barometer04hBotMinimal, out ExtraText))
            return true;

        if (!SymbolTools.CheckValidBarometer(Symbol.QuoteData, CryptoIntervalPeriod.interval1d, GlobalData.Settings.Trading.Barometer24hBotMinimal, out ExtraText))
            return true;


        ExtraText = "";
        return false;
    }

}

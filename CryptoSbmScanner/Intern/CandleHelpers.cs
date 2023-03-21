using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

public static class Helper
{
    public static decimal ConvertRadiansToDegrees(this decimal radians)
    {
        double degrees = (double)radians * (180 / Math.PI);
        return (decimal)degrees;
    }
    public static decimal ConvertDegreesToRadians(this decimal degrees)
    {
        double radians = (double)degrees * (Math.PI / 180);
        return (decimal)radians;
    }

    /// <summary>Remove trailing zeroes on the decimal.</summary>
    /// <param name="value">The value to normalize.</param>
    /// From the CryptoAdvisor sources, thanks!
    /// <returns>1.230000 becomes 1.23</returns>
    public static decimal Normalize(this decimal value)
    {
        return value / 1.000000000000000000000000000000000m;
    }


    public static bool IsBetween<T>(this T item, T start, T end)
    {
        return Comparer<T>.Default.Compare(item, start) >= 0
            && Comparer<T>.Default.Compare(item, end) <= 0;
    }


    public static string OhlcText(this CryptoCandle candle, string fmt, bool includeSymbol = false, bool includeInterval = false, bool includeVolume = false)
    {
        // Include the next time so it is clear what candle has focus (it saves a lot of questions)
        DateTime date = CandleTools.GetUnixDate(candle.OpenTime);
        string s = date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + "-" + date.AddSeconds(candle.Interval.Duration).ToLocalTime().ToString("HH:mm");

        if (includeSymbol)
            s += " " + candle.Symbol.Name;

        if (includeInterval)
            s = s + " interval=" + candle.Interval.Name;

        s = s + " open=" + candle.Open.ToString(fmt);
        s = s + " high=" + candle.High.ToString(fmt);
        s = s + " low=" + candle.Low.ToString(fmt);
        s = s + " close=" + candle.Close.ToString(fmt);
        if (includeVolume)
            s = s + " volume=" + candle.Volume.ToString();
        return s;
    }

    /// <summary>
    /// Remove any trailing 0's 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fmt"></param>
    /// <returns></returns>
    public static string ToString0(this decimal? value, string fmt = "N8")
    {
        // Een alternatief hievoor is de Normalize() functie herboven
        // (maar dat zal qua performance niet veel uitmaken denk ik)

        string text = value.HasValue ? ((decimal)value).ToString(fmt) : "0"; //Get the stock string

        //If there is a decimal point present
        if (text.Contains('.'))
        {
            //Remove all trailing zeros
            text = text.TrimEnd('0');

            //If all we are left with is a decimal point
            if (text.EndsWith(".")) //then remove it
                text = text.TrimEnd('.');
        }

        return text;
    }

    /// <summary>
    /// Remove any trailing 0's 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fmt"></param>
    /// <returns></returns>
    public static string ToString0(this decimal value, string fmt = "N8")
    {
        string text = value.ToString(fmt); //Get the stock string

        //If there is a decimal point present
        if (text.Contains('.'))
        {
            //Remove all trailing zeros
            text = text.TrimEnd('0');

            //If all we are left with is a decimal point
            if (text.EndsWith(".")) //then remove it
                text = text.TrimEnd('.');
        }

        return text;
    }


    /// <summary>
    /// Clamp a decimal to a min and max value
    /// </summary>
    /// <param name="minValue">Min value</param>
    /// <param name="maxValue">Max value</param>
    /// <param name="stepSize">Smallest unit value should be evenly divisible by</param>
    /// <param name="value">Value to clamp</param>
    /// Uit de CryptoAdvisor sources, thanks!
    /// <returns>Clamped value</returns>
    public static decimal Clamp(this decimal value, decimal minValue, decimal maxValue, decimal? stepSize)
    {
        if (minValue < 0)
            throw new ArgumentOutOfRangeException(nameof(minValue));
        else if (maxValue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue));
        else if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        else if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(minValue));

        if (stepSize.HasValue)
        {
            if (stepSize < 0)
                throw new ArgumentOutOfRangeException(nameof(stepSize));
            decimal mod = value % stepSize.Value;
            value -= mod;
        }

        if (maxValue > 0)
            value = Math.Min(maxValue, value);

        value = Math.Max(minValue, value);

        return value.Normalize();
    }



    public static bool IsBarometerSymbol(this CryptoSymbol symbol)
    {
        return symbol.Base.StartsWith("$"); // de $BMV (Volume) of $BMP (Price)
        //return ((symbol.Base.Equals(Constants.SymbolNameBarometerPrice)) || (symbol.Base.Equals(Constants.SymbolNameBarometerVolume));
    }


    public static bool CheckValidMinimalVolume(this CryptoSymbol symbol, out string text)
    {
        if (symbol.QuoteData.MinimalVolume > 0)
        {
            // Controleer of de munt actief is (beetje raar)
            if (!symbol.QuoteData.FetchCandles)
            {
                text = string.Format("{0} Er worden geen candles opgehaald", symbol.Name);
                return false;
            }

            // Controleer of er genoeg volume is (van de afgelopen 24 uur)
            if (symbol.Volume < symbol.QuoteData.MinimalVolume)
            {
                text = string.Format("{0} 24 uur volume {1} onder het minimum {2}", symbol.Name, symbol.Volume.ToString0(), symbol.QuoteData.MinimalVolume.ToString0());
                return false;
            }
        }

        text = "";
        return true;
    }

    public static bool CheckValidMinimalPrice(this CryptoSymbol symbol, out string text)
    {
        // Controleer of de munt actief is (beetje raar)
        if (!symbol.QuoteData.FetchCandles)
        {
            text = string.Format("{0} Er worden geen candles opgehaald", symbol.Name);
            return false;
        }

        // Controleer de prijs van de munt
        if (symbol.QuoteData.MinimalPrice > 0 && symbol.LastPrice < symbol.QuoteData.MinimalPrice)
        {
            text = string.Format("{0} Prijs {1} onder het minimum {2}", symbol.Name, symbol.LastPrice.ToString0(), symbol.QuoteData.MinimalPrice.ToString0());
            return false;
        }

        text = "";
        return true;
    }




    public static bool CheckBollingerBandsWidth(this CryptoCandle candle, decimal minValue, decimal maxValue)
    {
        decimal boundary = minValue;
        if (boundary > 0 && (decimal)candle.CandleData.BollingerBandsPercentage <= boundary)
            return false;

        boundary = maxValue;
        if (boundary > 0 && (decimal)candle.CandleData.BollingerBandsPercentage >= boundary)
            return false;

        return true;
    }



    public static bool IsBelowBollingerBands(this CryptoCandle candle, bool useLowHigh)
    {
        // Geopend of gesloten onder de bollinger band
        decimal value;
        if (useLowHigh)
            value = candle.Low;
        else
            value = Math.Min(candle.Open, candle.Close);
        decimal band = (decimal)candle.CandleData.BollingerBands.LowerBand.Value;
        //band = band.Clamp(candle.Symbol.PriceMinimum, candle.Symbol.PriceMaximum, candle.Symbol.PriceTickSize);
        if (value <= band)
            return true;
        return false;
    }



    public static bool IsAboveBollingerBands(this CryptoCandle candle, bool useLowHigh)
    {
        // Geopend of gesloten boven de bollinger band
        decimal value;
        if (useLowHigh)
            value = candle.High;
        else
            value = Math.Max(candle.Open, candle.Close);
        decimal band = (decimal)candle.CandleData.BollingerBands.UpperBand.Value;
        //band = band.Clamp(candle.Symbol.PriceMinimum, candle.Symbol.PriceMaximum, candle.Symbol.PriceTickSize);
        if (value >= band)
            return true;
        return false;
    }



    public static bool IsStochOverbought(this CryptoCandle candle)
    {
        if (candle.CandleData.Stoch.Signal.Value < 80)
            return false;
        if (candle.CandleData.Stoch.Oscillator.Value < 80)
            return false;
        return true;
    }



    public static bool IsStochOversold(this CryptoCandle candle)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData.Stoch.Signal.Value > 20)
            return false;
        if (candle.CandleData.Stoch.Oscillator.Value > 20)
            return false;
        return true;
    }


    public static bool IsRsiOverbought(this CryptoCandle candle)
    {
        if (candle.CandleData.Rsi.Rsi.Value < 70)
            return false;
        return true;
    }



    public static bool IsRsiOversold(this CryptoCandle candle)
    {
        // Rsiastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData.Rsi.Rsi.Value > 30)
            return false;
        return true;
    }


    public static bool IsSbmConditionsOversold(this CryptoCandle candle, bool includePsarCheck = true)
    {
        // Oversold (denk groen-geel-rood) - long
        // 200 (red)
        // 50 (orange)
        // 20 (green)
        // psar

        // Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        if (candle.CandleData.Sma50.Sma.Value >= candle.CandleData.Sma200.Sma.Value)
            return false;
        if (candle.CandleData.BollingerBands.Sma.Value >= candle.CandleData.Sma200.Sma.Value)
            return false;
        if (candle.CandleData.BollingerBands.Sma.Value >= candle.CandleData.Sma50.Sma.Value)
            return false;

        if (includePsarCheck)
        {
            if (candle.CandleData.PSar > candle.CandleData.BollingerBands.Sma.Value)
                return false;
            // Dan is de psar omgeslagen
            if ((decimal)candle.CandleData.PSar <= candle.Close)
                return false;
        }

        if (!candle.IsMa200AndMa50OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage))
            return false;
        if (!candle.IsMa200AndMa20OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage))
            return false;
        if (!candle.IsMa50AndMa20OkayOversold(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage))
            return false;

        return true;
    }


    public static bool IsSbmConditionsOverbought(this CryptoCandle candle, bool includePsarCheck = true)
    {
        // Overbought (denk groen-geel-rood) - short
        // psar
        // 20 (green)
        // 50 (orange)
        // 200 (red)

        // Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        if (candle.CandleData.Sma200.Sma.Value >= candle.CandleData.Sma50.Sma.Value)
            return false;
        if (candle.CandleData.Sma200.Sma.Value >= candle.CandleData.BollingerBands.Sma.Value)
            return false;
        if (candle.CandleData.Sma50.Sma.Value >= candle.CandleData.BollingerBands.Sma.Value)
            return false;

        if (includePsarCheck)
        {
            if (candle.CandleData.PSar < candle.CandleData.BollingerBands.Sma.Value)
                return false;
            // Dan is de psar omgeslagen
            if ((decimal)candle.CandleData.PSar >= candle.Close)
                return false;
        }

        return true;
    }



    public static bool IsMa200AndMa50OkayOversold(this CryptoCandle candle, decimal percentage = 0.3m)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma200.Sma.Value - (decimal)candle.CandleData.Sma50.Sma.Value;
        decimal value2 = ((decimal)candle.CandleData.Sma200.Sma.Value + (decimal)candle.CandleData.Sma50.Sma.Value) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            //ExtraText = string.Format("sma200 en sma50 < {0}%", percentage);
            return false;
        }
        return true;
    }


    public static bool IsMa50AndMa20OkayOversold(this CryptoCandle candle, decimal percentage = 0.5m)
    {
        decimal value = (decimal)candle.CandleData.Sma50.Sma.Value - (decimal)candle.CandleData.BollingerBands.Sma.Value;
        decimal value2 = ((decimal)candle.CandleData.Sma50.Sma.Value + (decimal)candle.CandleData.BollingerBands.Sma.Value) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            //ExtraText = string.Format("sma50 en sma20  < {0}%", percentage);
            return false;
        }
        return true;
    }


    public static bool IsMa200AndMa20OkayOversold(this CryptoCandle candle, decimal percentage = 0.7m)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma200.Sma.Value - (decimal)candle.CandleData.BollingerBands.Sma.Value;
        decimal value2 = ((decimal)candle.CandleData.Sma200.Sma.Value + (decimal)candle.CandleData.BollingerBands.Sma.Value) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            //ExtraText = string.Format("sma200 en sma20  < {0}%", percentage);
            return false;
        }
        return true;
    }

}

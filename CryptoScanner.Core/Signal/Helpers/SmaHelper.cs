using CryptoScanner.Core.Model;

public static class SmaHelper
{

    public static bool SbmConditionsOversold(this CryptoCandle candle, bool includePsarCheck)
    {
        // Line levels:
        // -sma 200 (red)
        // -sma 50 (orange)
        // -sma 20 (green)
        // -psar

        if (candle.CandleData?.Sma200 > candle.CandleData?.Sma50 && candle.CandleData?.Sma50 > candle.CandleData?.Sma20)
        {
            // Wait until psar is below the sma20
            if (includePsarCheck)
            {
                if (candle.CandleData?.PSar < candle.CandleData?.Sma20)
                    return true;
                else
                    return false;
            }
            else return true;
        }
        return false;

        //// Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        //if (candle.CandleData?.Sma50 >= candle.CandleData?.Sma200)
        //    return false;
        //if (candle.CandleData?.Sma20 >= candle.CandleData?.Sma200)
        //    return false;
        //if (candle.CandleData?.Sma20 >= candle.CandleData?.Sma50)
        //    return false;


        //if (includePsarCheck)
        //{
        //    // wait at least until it is below the sma20
        //    if (candle.CandleData?.PSar > candle.CandleData?.Sma20)
        //        return false;

        //    // psar switched to the opposite side
        //    if ((decimal?)candle.CandleData?.PSar <= candle.Close)
        //        return false;
        //}

        //return true;
    }

    public static bool IsPercentageSma200AndSma50OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal? value = (decimal?)candle.CandleData?.Sma200 - (decimal?)candle.CandleData?.Sma50;
        decimal? value2 = ((decimal?)candle.CandleData?.Sma200 + (decimal?)candle.CandleData?.Sma50) / 2;
        decimal? perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsPercentageSma50AndSma20OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        decimal? value = (decimal?)candle.CandleData?.Sma50 - (decimal?)candle.CandleData?.Sma20;
        decimal? value2 = ((decimal?)candle.CandleData?.Sma50 + (decimal?)candle.CandleData?.Sma20) / 2;
        decimal? perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsPercentageSma200AndSma20OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal? value = (decimal?)candle.CandleData?.Sma200 - (decimal?)candle.CandleData?.Sma20;
        decimal? value2 = ((decimal?)candle.CandleData?.Sma200 + (decimal?)candle.CandleData?.Sma20) / 2;
        decimal? perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }

    public static bool IsSbmConditionsOverbought(this CryptoCandle candle, bool includePsarCheck = true)
    {
        // Line levels:
        // -psar
        // -sma 20 (green)
        // -sma 50 (orange)
        // -sma 200 (red)

        if (candle.CandleData?.Sma200 < candle.CandleData?.Sma50 && candle.CandleData?.Sma50 < candle.CandleData?.Sma20)
        {
            // Wait until psar is above the sma20
            if (includePsarCheck)
            {
                if (candle.CandleData?.PSar > candle.CandleData?.Sma20)
                    return true;
                else
                    return false;
            }
            else return true;
        }
        return false;


        //// Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        //if (candle.CandleData?.Sma200 >= candle.CandleData?.Sma50)
        //    return false;
        //if (candle.CandleData?.Sma200 >= candle.CandleData?.Sma20)
        //    return false;
        //if (candle.CandleData?.Sma50 >= candle.CandleData?.Sma20)
        //    return false;


        //if (includePsarCheck)
        //{
        //    // wait at least until it is above the sma20
        //    if (candle.CandleData?.PSar < candle.CandleData?.Sma20)
        //        return false;

        //    // psar switched to the opposite side
        //    if ((decimal)candle.CandleData?.PSar! >= candle.Close)
        //        return false;
        //}

        //return true;
    }

    public static bool IsPercentageSma200AndSma50OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
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


    public static bool IsPercentageSma50AndSma20OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
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


    public static bool IsPercentageSma200AndSma20OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
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
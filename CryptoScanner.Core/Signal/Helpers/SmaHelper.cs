using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

public static class SmaHelper
{

    public static bool IsSbmConditionsOversold(this MyData data)
    {
        // Line levels:
        // -sma 200 (red)
        // -sma 50 (orange)
        // -sma 20 (green)
        return data.CandleData?.Sma200 > data.CandleData?.Sma50 && data.CandleData?.Sma50 > data.CandleData?.Sma20;
    }


    public static bool IsSbmConditionsPSarOversold(this MyData data)
    {
        // Line levels:
        // -sma 20 (green)
        // -psar

        // Wait until psar is below the sma20
        if (data.CandleData?.PSar > data.CandleData?.Sma20)
            return false;

        // psar switched to the opposite side
        if ((decimal?)data.CandleData?.PSar <= data.Candle.Close)
            return false;
        return true;

    }

    public static bool IsPercentageSma200AndSma50OkayOversold(this MyData data, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal? value = (decimal?)data.CandleData?.Sma200 - (decimal?)data.CandleData?.Sma50;
        decimal? value2 = ((decimal?)data.CandleData?.Sma200 + (decimal?)data.CandleData?.Sma50) / 2;
        decimal? perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsPercentageSma50AndSma20OkayOversold(this MyData data, decimal percentage, out string response)
    {
        decimal? value = (decimal?)data.CandleData?.Sma50 - (decimal?)data.CandleData?.Sma20;
        decimal? value2 = ((decimal?)data.CandleData?.Sma50 + (decimal?)data.CandleData?.Sma20) / 2;
        decimal? perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsPercentageSma200AndSma20OkayOversold(this MyData data, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal? value = (decimal?)data.CandleData?.Sma200 - (decimal?)data.CandleData?.Sma20;
        decimal? value2 = ((decimal?)data.CandleData?.Sma200 + (decimal?)data.CandleData?.Sma20) / 2;
        decimal? perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }

    public static bool IsSbmConditionsOverbought(this MyData data)
    {
        // Line levels:
        // -sma 20 (green)
        // -sma 50 (orange)
        // -sma 200 (red)
        return data.CandleData?.Sma200 < data.CandleData?.Sma50 && data.CandleData?.Sma50 < data.CandleData?.Sma20;
    }

    public static bool IsSbmConditionsPSarOverbought(this MyData data)
    {
        // Line levels:
        // -psar
        // -sma 20 (green)

        // wait at least until it is above the sma20
        if (data.CandleData?.PSar < data.CandleData?.Sma20)
            return false;

        // psar switched to the opposite side
        if ((decimal)data.CandleData?.PSar! >= data.Candle.Close)
            return false;
        return true;
    }

    public static bool IsPercentageSma200AndSma50OkayOverbought(this MyData data, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)data.CandleData?.Sma50! - (decimal)data.CandleData?.Sma200!;
        decimal value2 = ((decimal)data.CandleData?.Sma50! + (decimal)data.CandleData?.Sma200!) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsPercentageSma50AndSma20OkayOverbought(this MyData data, decimal percentage, out string response)
    {
        decimal value = (decimal)data.CandleData?.Sma20! - (decimal)data.CandleData?.Sma50!;
        decimal value2 = ((decimal)data.CandleData?.Sma20! + (decimal)data.CandleData?.Sma50!) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsPercentageSma200AndSma20OkayOverbought(this MyData data, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)data!.CandleData?.Sma20! - (decimal)data.CandleData?.Sma200!;
        decimal value2 = ((decimal)data!.CandleData?.Sma20! + (decimal)data.CandleData?.Sma200!) / 2;
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
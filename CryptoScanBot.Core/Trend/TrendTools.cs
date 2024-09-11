using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Trend;

public class TrendTools
{
    public static string TrendIndicatorText(CryptoTrendIndicator? trend)
    {
        if (!trend.HasValue)
            return "";

        return trend switch
        {
            CryptoTrendIndicator.Bullish => "up",
            CryptoTrendIndicator.Bearish => "down",
            _ => "",
        };
    }
}

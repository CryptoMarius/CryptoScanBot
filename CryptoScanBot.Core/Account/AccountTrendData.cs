using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Account;

public class AccountTrendData
{
    // Trend: Last calculated trend & date
    public long? TrendInfoUnix { get; set; }
    public DateTime? TrendInfoDate { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }


    public void ResetTrendData()
    {
        TrendInfoUnix = null;
        TrendInfoDate = null;
        TrendIndicator = CryptoTrendIndicator.Sideways;
    }
}
using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Account;

public class AccountQuoteData
{
    public required string QuoteName { get; set; }

    // The pausing values for each side
    public Dictionary<CryptoTradeSide, PauseBarometer> PauseBarometerList { get; set; } = [];

    // The barometer values for each interval 
    public Dictionary<CryptoIntervalPeriod, BarometerData> BarometerDataList { get; set; } = [];



    public AccountQuoteData()
    {
        // Initialize sides
        PauseBarometerList = new()
        {
            { CryptoTradeSide.Long, new PauseBarometer() },
            { CryptoTradeSide.Short, new PauseBarometer() }
        };

        // Initialize intervals
        for (CryptoIntervalPeriod interval = CryptoIntervalPeriod.interval1m; interval <= CryptoIntervalPeriod.interval1d; interval++)
            BarometerDataList[interval] = new BarometerData();
    }
}

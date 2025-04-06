using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;
using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Json;

namespace CryptoScanBot.Core.Model;

public class CryptoQuoteData
{
    public required string Name { get; set; }

    public string DisplayFormat { get; set; } = "N8";

    // Basecoin data
    public bool FetchCandles { get; set; }
    public decimal MinimalVolume { get; set; }
    public decimal MinimalPrice { get; set; }
    // Trading: The initial entry amount
    public decimal EntryAmount { get; set; }
    // Trading: The initial entry percentage of PF
    public decimal EntryPercentage { get; set; }
    // Color of the base coin in signal grid
    [JsonConverter(typeof(ColorConverter))]
    public System.Drawing.Color DisplayColor { get; set; } = System.Drawing.Color.White;

    // List of symbols (for this quote)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = [];

    // The pausing values for each side
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoTradeSide, PauseBarometer> PauseBarometerList { get; set; } = [];

    // The barometer values for each interval 
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoIntervalPeriod, BarometerData> BarometerDataList { get; set; } = [];



    public CryptoQuoteData()
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

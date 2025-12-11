using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;
using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using Avalonia.Media;

namespace CryptoScanner.Core.Model;

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
    public Color DisplayColor { get; set; } = Colors.White;

    // List of symbols (for this quote)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = [];

    // The pausing values for each side
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoTradeSide, CryptoPauseBarometer> PauseBarometerList { get; set; } = [];

    // The barometer values for each interval
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoIntervalPeriod, CryptoBarometerData> BarometerDataList { get; set; } = [];



    public CryptoQuoteData()
    {
        // Initialize sides
        PauseBarometerList = new()
        {
            { CryptoTradeSide.Long, new CryptoPauseBarometer() },
            { CryptoTradeSide.Short, new CryptoPauseBarometer() }
        };

        // Initialize intervals
        for (CryptoIntervalPeriod interval = CryptoIntervalPeriod.interval1m; interval <= CryptoIntervalPeriod.interval1w; interval++)
            BarometerDataList[interval] = new CryptoBarometerData();
    }
}

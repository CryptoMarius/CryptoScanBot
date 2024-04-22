using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using Dapper.Contrib.Extensions;
using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Model;

[Serializable]
public class CryptoQuoteData
{
    // BTCUSDT, Base=BTC, Quote=USDT
    public string Name { get; set; }
    public bool FetchCandles { get; set; }
    public bool CreateSignals { get; set; }
    public decimal MinimalVolume { get; set; }
    public decimal MinimalPrice { get; set; }

    // Het initiele aankoopbedrag
    public decimal EntryAmount { get; set; }

    // Het initiele aankooppercentage
    public decimal EntryPercentage { get; set; }

    [JsonConverter(typeof(ColorConverter))]
    public System.Drawing.Color DisplayColor { get; set; } = System.Drawing.Color.White;

    // De laatst berekende barometer standen
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoIntervalPeriod, BarometerData> BarometerList { get; set; } = [];

    // Gecachte lijst met symbolen (de zoveelste), met name voor de barometer(s)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = [];

    public string DisplayFormat { get; set; } = "N8";

    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoTradeSide, PauseRule> PauseBarometer { get; set; } = [];

    public CryptoQuoteData()
    {
        SymbolList = [];
        PauseBarometer = new()
        {
            { CryptoTradeSide.Long, new PauseRule() },
            { CryptoTradeSide.Short, new PauseRule() }
        };

        // Dan maar op de moeilijke manier? (kan wellicht gewoon via de symbols BMP$*** en BMV$***?)
        for (CryptoIntervalPeriod interval = CryptoIntervalPeriod.interval1m; interval <= CryptoIntervalPeriod.interval1d; interval++)
        {
            BarometerList[interval] = new BarometerData();
        }
    }
}



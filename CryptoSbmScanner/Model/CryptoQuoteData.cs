using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;

using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;

namespace CryptoSbmScanner.Model;

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

    // Maximaal aantal slots op de exchange
    //public int SlotsMaximal { get; set; }

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color DisplayColor { get; set; } = Color.White;

    // De laatst berekende barometer standen
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoIntervalPeriod, BarometerData> BarometerList { get; set; } = [];

    // Gecachte lijst met symbolen (de zoveelste), met name voor de barometer(s)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = [];

    //[Computed]
    //[JsonIgnore]
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



public static class CryptoQuoteDataHelper
{
    public static decimal GetEntryAmount(this CryptoQuoteData quoteData, decimal currentAssetQuantity, CryptoTradeAccountType tradeAccountType)
    {
        // Bepaal het entry bedrag 
        // TODO Er is geen percentage bij papertrading mogelijk (of we moeten een werkende papertrade asset management implementeren)

        // Heeft de gebruiker een percentage of een aantal ingegeven?
        if (tradeAccountType ==CryptoTradeAccountType.RealTrading && quoteData.EntryPercentage > 0m)
            return quoteData.EntryPercentage * currentAssetQuantity / 100;
        else
            return quoteData.EntryAmount;
    }
}

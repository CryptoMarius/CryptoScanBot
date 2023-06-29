using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange.Binance;
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
    public decimal BuyAmount { get; set; }

    // Het initiele aankooppercentage
    public decimal BuyPercentage { get; set; }

    // Maximaal aantal slots op de exchange
    public int SlotsMaximal { get; set; }

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color DisplayColor { get; set; } = Color.White;

    // TODO: Uitfaseren uit deze class en naar een specifiek Exchange-achtig object doorzetten???
    [Computed]
    [JsonIgnore]
    public List<BinanceStream1mCandles> BinanceStream1mCandles { get; set; } = new List<BinanceStream1mCandles>();

    // De laatst berekende barometer standen
    [Computed]
    [JsonIgnore]
    public BarometerData[] BarometerList { get; } = new BarometerData[Enum.GetValues(typeof(CryptoIntervalPeriod)).Length];

    // Gecachte lijst met symbolen (de zoveelste), met name voor de barometer(s)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = new List<CryptoSymbol>();

    [Computed]
    public string DisplayFormat { get; set; } = "N8";

    public CryptoQuoteData()
    {
        SymbolList = new List<CryptoSymbol>();

        // Dan maar op de moeilijke manier? (kan wellicht gewoon via de symbols BMP$*** en BMV$***?)
        for (CryptoIntervalPeriod interval = CryptoIntervalPeriod.interval1m; interval <= CryptoIntervalPeriod.interval1d; interval++)
        {
            BarometerList[(long)interval] = new BarometerData();
        }
    }
}

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Intern;

using System.Drawing;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Dapper.Contrib.Extensions;

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

    [Computed]
    public List<BinanceStream1mCandles> BinanceStream1mCandles { get; set; } = new List<BinanceStream1mCandles>();

    // De laatst berekende barometer standen
    [Computed]
    public BarometerData[] BarometerList { get; } = new BarometerData[Enum.GetValues(typeof(CryptoIntervalPeriod)).Length];

    // Gecachte lijst met symbolen (de zoveelste), met name voor de barometer(s)
    [Computed]
    public List<CryptoSymbol> SymbolList { get; } = new List<CryptoSymbol>();

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

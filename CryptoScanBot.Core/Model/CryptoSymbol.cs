using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

static public class Constants
{
    public const string SymbolNameBarometerPrice = "$BMP";
    //public const string SymbolNameBarometerVolume = "$BMV"; // an experiment, needs to be continued someday
}

[Table("Symbol")]
public class CryptoSymbol
{
    [Key]
    public int Id { get; set; }
    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public string Name { get; set; }
    public string Base { get; set; } //De munt zelf (NKN, THETA, AION enzovoort)
    public string Quote { get; set; } //De basismunt (BTC, ETH, USDT enzovoort)
    public int Status { get; set; } //0 voor inactief, 1 voor actief

    // Ongebruikt, weg ermee
    //The precision of the quote asset (maar voor wat dan?)
    //public int QuoteAssetPrecision { get; set; }

    // Ongebruikt, weg ermee
    //The precision of the base asset
    //public int BaseAssetPrecision { get; set; }

    // Ongebruikt, weg ermee
    //BinanceSymbolMinNotionalFilter
    //The minimal total size of an order (calculated by Price * Quantity).
    // Deze is voor bybit niet aanwezig en bij Binance slechts voor ~20 gevuld, weg ermee!
    //public decimal MinNotional { get; set; }

    // The minimal quantity of an order
    public decimal QuantityMinimum { get; set; }
    //The maximum quantity of an order
    public decimal QuantityMaximum { get; set; }
    // The tick size of the quantity. The quantity can not have more precision as this and can only be incremented in steps of this.
    public decimal QuantityTickSize { get; set; }
    [Computed]
    public string QuantityDisplayFormat { get; set; } = "N8";


    // The minimal value of an order
    public decimal QuoteValueMinimum { get; set; }
    // The maximum value of an order
    public decimal QuoteValueMaximum { get; set; }


    //BinanceSymbolPriceFilter
    //The minimal price the order can be for
    public decimal PriceMinimum { get; set; }
    //The max price the order can be for
    public decimal PriceMaximum { get; set; }
    //The tick size of the price. The price can not have more precision as this and
    //can only be incremented in steps of this.
    public decimal PriceTickSize { get; set; }
    [Computed]
    public string PriceDisplayFormat { get; set; } = "N8";

    public bool IsSpotTradingAllowed { get; set; }
    public bool IsMarginTradingAllowed { get; set; }

    public decimal Volume { get; set; }


    // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
    // Wat het inhoud weet ik niet maar toegevoegde waarde is er voor het traden wel.
    // https://bybit-exchange.github.io/docs/v5/market/history-fund-rate
    public decimal FundingRate { get; set; }
    // (minute)
    public decimal FundingInterval { get; set; }



    // Gevuld door de Binance Ticker, ALLEMAAL gebaseerd op de 24h prijs
    //[Computed]
    // Laatste waarde volgens de ticker
    //public decimal? OpenPrice { get; set; }
    //[Computed]
    // Laatste waarde volgens de ticker
    //public decimal? HighPrice { get; set; }
    //[Computed]
    // Laatste waarde volgens de ticker
    //public decimal? LowPrice { get; set; }


    // Last value from the symbol ticker or candle.Close
    public decimal? LastPrice { get; set; }
    [Computed]
    // Last value from the symbol ticker or candle.Close
    public decimal? BidPrice { get; set; }
    [Computed]
    // Last value from the symbol ticker or candle.Close
    public decimal? AskPrice { get; set; }


    /// <summary>
    /// For fetching the trades
    /// </summary>
    public DateTime? LastTradeFetched { get; set; }
    public long? LastTradeIdFetched { get; set; }


    /// <summary>
    /// Last time we traded on this symbol (cooldown)
    /// </summary>
    public DateTime? LastTradeDate { get; set; }

    [Computed]
    // Quote: display format
    public virtual CryptoQuoteData? QuoteData { get; set; }


    [Computed]
    // Interval related data like candles and last candle fetched
    public List<CryptoSymbolInterval> IntervalPeriodList { get; set; } = [];

    [Computed]
    // Quick reference to the first intervalSymbol
    public SortedList<long, CryptoCandle> CandleList { get { return IntervalPeriodList[0].CandleList; } }


    //// Quick en dirty voor het testen van de performance van balanceren
    //// Waarom kies ik ervoor om het altijd quick en dirty te doen??????
    //[Computed]
    //public Decimal QuantityTest { get; set; }
    //[Computed]
    //public Decimal QuoteQuantityTest { get; set; }


    public CryptoSymbol()
    {
        IntervalPeriodList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = new()
            {
                Interval = interval,
                IntervalPeriod = interval.IntervalPeriod,
            };
            IntervalPeriodList.Add(symbolInterval);
        }
    }

    public CryptoSymbolInterval GetSymbolInterval(CryptoIntervalPeriod intervalPeriod)
    {
        return IntervalPeriodList[(int)intervalPeriod];
    }

    public void ClearSignals()
    {
        foreach (CryptoSymbolInterval symbolInterval in IntervalPeriodList)
            symbolInterval.Signal = null;
    }
}

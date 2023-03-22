using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner.Model;

public class CryptoSymbol
{
    public virtual CryptoExchange Exchange { get; set; }

    public string Name { get; set; }
    public string Base { get; set; } //De munt zelf (NKN, THETA, AION enzovoort)
    public string Quote { get; set; } //De basismunt (BTC, ETH, USDT, BUSD enzovoort)
    public int Status { get; set; } //0 voor inactief, 1 voor actief

    //The precision of the quote asset (maar voor wat dan?)
    public int QuoteAssetPrecision { get; set; }
    //The precision of the base asset
    public int BaseAssetPrecision { get; set; }

    //BinanceSymbolMinNotionalFilter
    //The minimal total size of an order (calculated by Price * Quantity).
    public decimal MinNotional { get; set; }

    //BinanceSymbolLotSizeFilter;
    //The minimal quantity of an order
    public decimal QuantityMinimum { get; set; }
    //The maximum quantity of an order
    public decimal QuantityMaximum { get; set; }
    //The tick size of the quantity. The quantity can not have more precision as this
    //and can only be incremented in steps of this.
    public decimal QuantityTickSize { get; set; }

    //BinanceSymbolPriceFilter
    //The minimal price the order can be for
    public decimal PriceMinimum { get; set; }
    //The max price the order can be for
    public decimal PriceMaximum { get; set; }
    //The tick size of the price. The price can not have more precision as this and
    //can only be incremented in steps of this.
    public decimal PriceTickSize { get; set; }

    public bool IsSpotTradingAllowed { get; set; }
    public bool IsMarginTradingAllowed { get; set; }

    //Gevuld door de MiniTicker, ALLEMAAL gebaseerd op de 24h prijs
    public decimal? OpenPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? LowPrice { get; set; }

    //Laatste waarde volgens de miniticker
    public decimal? LastPrice { get; set; }
    //Laatste waarde volgens de miniticker
    public decimal? BidPrice { get; set; }
    //Laatste waarde volgens de miniticker
    public decimal? AskPrice { get; set; }

    public decimal Volume { get; set; }

    // De trend percentage (berekend uit de candlefetched.TrendIndicator)
    // Handig om hier een datum van berekening bij te zetten? (voor wie?)
    public float? TrendPercentage { get; set; }

    public CryptoQuoteData QuoteData { get; set; }

    // Interval related data like candles, candlefetched etc.
    public List<CryptoSymbolInterval> IntervalPeriodList { get; set; } = new List<CryptoSymbolInterval>();

    // NB: Verwijst nu naar de IntervalPeriodList<1m>.CandleList
    public SortedList<long, CryptoCandle> CandleList { get { return IntervalPeriodList[0].CandleList; } }

    //[Computed]
    public string DisplayFormat { get; set; } = "N8";


    public void InitializePeriods() 
	    => IntervalPeriodList = GlobalData.IntervalList.Select(interval 
        => new CryptoSymbolInterval { Interval = interval }).ToList();

    public CryptoSymbol() => InitializePeriods();

    public CryptoSymbolInterval GetSymbolInterval(CryptoIntervalPeriod intervalPeriod) => IntervalPeriodList[(int)intervalPeriod];

}

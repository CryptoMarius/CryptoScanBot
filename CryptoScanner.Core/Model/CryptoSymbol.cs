using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Symbol")]
public class CryptoSymbol
{
    [Key]
    public int Id { get; set; }
    public int ExchangeId { get; set; }
    [Computed]
    public virtual required CryptoExchange Exchange { get; set; }

    public required string Name { get; set; }
    public required string Base { get; set; } // (ADA, NKN, LTC, etc)
    public required string Quote { get; set; } // (BTC, ETH, EUR, USDT etc)
    public required string ExchangeName { get; set; } 
    public int Status { get; set; } // 0 for inactive, 1 voor active

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


    //The minimal price the order can be for
    public decimal PriceMinimum { get; set; }
    //The max price the order can be for
    public decimal PriceMaximum { get; set; }
    //The tick size of the price. The price can not have more precision as this and
    //can only be incremented in steps of this.
    public decimal PriceTickSize { get; set; }
    [Computed]
    public string PriceDisplayFormat { get; set; } = "N8";

    // TODO: never used in this scanner sofar
    //public bool IsSpotTradingAllowed { get; set; }
    //public bool IsMarginTradingAllowed { get; set; }


    // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
    // Wat het inhoud weet ik niet maar toegevoegde waarde is er voor het traden wel.
    // https://bybit-exchange.github.io/docs/v5/market/History-fund-rate
    public decimal FundingRate { get; set; }
    // (minute)
    public decimal FundingInterval { get; set; }


    // Last value from the symbol ticker or candle.Close
    [Computed]
    public decimal? LastPrice { get; set; } = null;

    // Volume in the last 24 hour
    public decimal Volume { get; set; }

    /// <summary>
    /// For fetching the trades
    /// </summary>
    public long? LastTradeIdFetched { get; set; }
    public DateTime? LastTradeFetched { get; set; }


    /// <summary>
    /// Last time we traded on this symbol (cooldown)
    /// </summary>
    public DateTime? LastTradeDate { get; set; }

    [Computed]
    // Quote: display format
    public required virtual CryptoQuoteData QuoteData { get; set; }

    [Computed]
    // Interval related data like candles and last candle fetched
    public CryptoSymbolData Data { get; set; } = new();


    public CryptoSymbolInterval GetSymbolInterval(CryptoIntervalPeriod intervalPeriod)
    {
        return Data.SymbolIntervalList[(int)intervalPeriod];
    }

    public bool EnoughVolume()
    {
        if (QuoteData == null || QuoteData.MinimalVolume == 0)
            return true;
        if (Volume > 0.9m * QuoteData.MinimalVolume)
            return true;
        return false;
    }

    public void ClearCandles()
    {
        foreach (var symbolInterval in Data.SymbolIntervalList)
        {
            symbolInterval.CandleList.Clear();
        }
    }
}

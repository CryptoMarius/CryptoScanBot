﻿using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Model;

static public class Constants
{
    public const string SymbolNameBarometerPrice = "$BMP";
    //public const string SymbolNameBarometerVolume = "$BMV";
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


    // Laatste waarde volgens de ticker
    public decimal? LastPrice { get; set; }
    [Computed]
    // Laatste waarde volgens de ticker
    public decimal? BidPrice { get; set; }
    [Computed]
    // Laatste waarde volgens de ticker
    public decimal? AskPrice { get; set; }

    
    /// <summary>
    /// Voor het ophalen van de trades
    /// </summary>
    public DateTime? LastTradeFetched { get; set; }
    public long ? LastTradeIdFetched { get; set; }
    public DateTime? LastOrderFetched { get; set; }
    [Computed]
    // Laatste waarde volgens de ticker
    public bool HasOrdersAndTradesLoaded { get; set; } = false;

    // De trend percentage (berekend uit de candlefetched.TrendIndicator)
    public float? TrendPercentage { get; set; }
    public DateTime? TrendInfoDate { get; set; }

    /// <summary>
    /// Datum dat we de laatste keer hebben gekocht
    /// </summary>
    public DateTime? LastTradeDate { get; set; }

    //[Computed]
    //[JsonIgnore]
    //public bool IsBalancing { get; set; }

    [Computed]
    // gegevens quote, display format, barometers etc
    public virtual CryptoQuoteData QuoteData { get; set; }


    [Computed]
    // Interval related data like candles, last candle fetched, trend information etc.
    public List<CryptoSymbolInterval> IntervalPeriodList { get; set; } = [];

    [Computed]
    // NB: Verwijst nu naar de IntervalPeriodList<1m>.CandleList
    public SortedList<long, CryptoCandle> CandleList { get { return IntervalPeriodList[0].CandleList; } }


    [Computed]
    public SortedList<string, CryptoTrade> TradeList { get; } = [];

    [Computed]
    public SortedList<string, CryptoOrder> OrderList { get; } = [];
    


    //// Quick en dirty voor het testen van de performance van balanceren
    //// Waarom kies ik ervoor om het altijd quick en dirty te doen??????
    //[Computed]
    //public Decimal QuantityTest { get; set; }
    //[Computed]
    //public Decimal QuoteQuantityTest { get; set; }


    public CryptoSymbol()
    //???
    //    => IntervalPeriodList = GlobalData.IntervalList.Select(interval 
    //    => new CryptoSymbolInterval { Interval = interval }).ToList();
    {
        IntervalPeriodList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = new()
            {
                // Dit is een constructor, exchange en symbol zijn er nog niet
                //ExchangeId = this.ExchangeId,
                //SymbolId = this.Id,
                Interval = interval,
                IntervalId = interval.Id,
                IntervalPeriod = interval.IntervalPeriod,
            };
            IntervalPeriodList.Add(symbolInterval);
        }
    }

    public CryptoSymbolInterval GetSymbolInterval(CryptoIntervalPeriod intervalPeriod) 
        => IntervalPeriodList[(int)intervalPeriod];

    public void ClearSignals()
    {
        foreach (CryptoSymbolInterval symbolInterval in IntervalPeriodList)
            symbolInterval.Signal = null;
    }
}

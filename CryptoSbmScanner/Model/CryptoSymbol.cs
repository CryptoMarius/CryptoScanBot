﻿using CryptoSbmScanner.Intern;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

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
    [Computed]
    public decimal? OpenPrice { get; set; }
    [Computed]
    public decimal? HighPrice { get; set; }
    [Computed]
    public decimal? LowPrice { get; set; }

    //Laatste waarde volgens de miniticker
    public decimal? LastPrice { get; set; }
    [Computed]
    //Laatste waarde volgens de miniticker
    public decimal? BidPrice { get; set; }
    [Computed]
    //Laatste waarde volgens de miniticker
    public decimal? AskPrice { get; set; }

    public decimal Volume { get; set; }

    /// <summary>
    /// Laatste order id die we hebben opgehaald
    /// </summary>
    public long? LastOrderfetched { get; set; }
    /// <summary>
    /// Laatste trade id die we hebben opgehaald
    /// </summary>
    public long? LastTradefetched { get; set; }

    // De trend percentage (berekend uit de candlefetched.TrendIndicator)
    public float? TrendPercentage { get; set; }
    public DateTime? TrendInfoDate { get; set; }


    [Computed]
    public DateTime? LastTradeDate { get; set; }

    [Computed]
    //[JsonIgnore]
    //[Jil.JilDirective(Ignore = true)]
    public bool IsBalancing { get; set; }

    [Computed]
    public virtual CryptoQuoteData QuoteData { get; set; }


    [Computed]
    // Interval related data like candles, candlefetched etc.
    public List<CryptoSymbolInterval> IntervalPeriodList { get; set; } = new();

    [Computed]
    // NB: Verwijst nu naar de IntervalPeriodList<1m>.CandleList
    public SortedList<long, CryptoCandle> CandleList { get { return IntervalPeriodList[0].CandleList; } }

    //[Computed]
    //Besloten om de orders niet meer op te halen, ze zijn niet echt nodig.
    //De trades bevat ook alle informatie die we feitelijk nodig hebben.
    //public SortedList<long, Order> OrderList { get; } = new SortedList<long, Order>();

    [Computed]
    public SortedList<long, CryptoTrade> TradeList { get; } = new();


    [Computed]
    public string DisplayFormat { get; set; } = "N8";

    [Computed]
    public int SignalCount
    {
        get
        {
            int count = 0;
            foreach (CryptoSymbolInterval symbolInterval in IntervalPeriodList)
            {
                if (symbolInterval.Signal != null)
                    count++;
            }
            return count;
        }
    }

    [Computed]
    public int PositionCount
    {
        get
        {
            int count = 0;
            if (Exchange.PositionList.TryGetValue(Name, out var positionList))
                count += positionList.Count;
            return count;
        }
    }

    public CryptoSymbol()
    //???
    //    => IntervalPeriodList = GlobalData.IntervalList.Select(interval 
    //    => new CryptoSymbolInterval { Interval = interval }).ToList();
    {
        IntervalPeriodList = new();
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolPeriod = new()
            {
                ExchangeId = this.ExchangeId,
                SymbolId = this.Id,
                Interval = interval,
                IntervalId = interval.Id,
                IntervalPeriod = interval.IntervalPeriod,
            };
            IntervalPeriodList.Add(symbolPeriod);
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

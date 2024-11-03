using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;


[Table("Signal")]
public class CryptoSignal: CryptoData2
{
    [Key]
    public int Id { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual required CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual required CryptoSymbol Symbol { get; set; }

    public int IntervalId { get; set; }
    [Computed]
    public virtual required CryptoInterval Interval { get; set; }

    //Hmmmm, de EventTime bevat de candle.OpenTime, maar niet gegarandeerd dat deze nog aanwezig is
    [Computed]
    public virtual required CryptoCandle? Candle { get; set; }

    public bool BackTest { get; set; }

    // Melden en tevens bewaren
    public bool IsInvalid { get; set; }

    /// <summary>
    /// Bevat de candle.OpenTime (maar het signaal is pas bij CloseTime gedetecteerd)
    /// </summary>
    public long EventTime { get; set; }
    public DateTime OpenDate { get; set; }

    // Einde van de candle (voor sorteren in web)
    public DateTime CloseDate { get; set; }

    // Tot dit tijdstip is het signaal geldig (nodig voor de query)
    public DateTime ExpirationDate { get; set; }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " signal=" + OpenDate.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    public string EventText { get; set; }


    [Computed]
    public decimal? LastPrice { get; set; }
    [Computed]
    public double? PriceDiff { get { if (Symbol.LastPrice.HasValue) return (double)(100 * (Symbol.LastPrice / SignalPrice - 1)); else return 0; } }

    // Display only, in het grid om em om een grijze regel laten zien
    [Computed]
    public int ItemIndex { get; set; }

    public double AvgBB { get; set; }

    [Computed]
    public decimal MinEntry
    {
        get
        {
            decimal minEntryValue = 0;
            if (Symbol.LastPrice.HasValue)
                minEntryValue = Symbol.QuantityMinimum * (decimal)Symbol.LastPrice;

            if (Symbol.QuoteValueMinimum > 0 && Symbol.QuoteValueMinimum > minEntryValue)
                minEntryValue = Symbol.QuoteValueMinimum;

            return minEntryValue; 
        }
    }
}
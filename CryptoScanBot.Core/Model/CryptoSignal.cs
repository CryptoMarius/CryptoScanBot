using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;


[Table("Signal")]
public class CryptoSignal
{
    [Key]
    public int Id { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual CryptoSymbol Symbol { get; set; }

    public int IntervalId { get; set; }
    [Computed]
    public virtual CryptoInterval Interval { get; set; }

    //Hmmmm, de EventTime bevat de candle.OpenTime, maar niet gegarandeerd dat deze nog aanwezig is
    [Computed]
    public virtual CryptoCandle? Candle { get; set; }

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

    public CryptoTradeSide Side { get; set; }

    [Computed]
    public string SideText
    {
        get
        {
            return Side switch
            {
                CryptoTradeSide.Long => "long",
                CryptoTradeSide.Short => "short",
                _ => "?",
            };
        }
    }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " signal=" + OpenDate.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return SignalHelper.GetAlgorithm(Strategy); } }


    public double Last24HoursChange { get; set; }
    public double Last24HoursEffective { get; set; }
    public double Last10DaysEffective { get; set; }

    public string EventText { get; set; }
    public decimal Price { get; set; }

#if DEBUG
    // Statistics, the min and max differences against the signalprice
    public decimal PriceMin { get; set; }
    public double PriceMinPerc { get; set; }
    public decimal PriceMax { get; set; }
    public double PriceMaxPerc { get; set; }
#endif

    [Computed]
    public decimal? LastPrice { get; set; }
    [Computed]
    public double? PriceDiff { get { if (Symbol.LastPrice.HasValue) return (double)(100 * (Symbol.LastPrice / Price - 1)); else return 0; } }
    public decimal Volume { get; set; }

    public float TrendPercentage { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }

    // Stochastic waarden
    public double? StochOscillator { get; set; } // Stochastic oscillator %K
    public double? StochSignal { get; set; }  // Stochastic oscillator %D

    // Bollinger Bands
    public double? BollingerBandsDeviation { get; set; }
    [Computed]
    public double? BollingerBandsUpperBand { get { return Sma20 + BollingerBandsDeviation; } }
    [Computed]
    public double? BollingerBandsLowerBand { get { return Sma20 - BollingerBandsDeviation; } }
    public double? BollingerBandsPercentage { get; set; }

    // PSAR waarden
    public double? PSar { get; set; }

    //public double? KeltnerUpperBand { get; set; }
    //public double? KeltnerLowerBand { get; set; }

    // RSI waarden
    public double? Rsi { get; set; }
    //public double? SlopeRsi { get; set; }

    public int LuxIndicator5m { get; set; }

    //uitgezet
    ////public double? Ema8 { get; set; }
    //public double? Ema20 { get; set; }
    //public double? Ema50 { get; set; }
    ////public double? Ema100 { get; set; }
    //public double? Ema200 { get; set; }
    //public double? SlopeEma20 { get; set; }
    //public double? SlopeEma50 { get; set; }

    // SMA waarden
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    public double? Sma50 { get; set; }
    //public double? Sma100 { get; set; }
    public double? Sma200 { get; set; }
    //public double? SlopeSma20 { get; set; } uitgezet
    //public double? SlopeSma50 { get; set; } uitgezet


    // Wellicht introduceren en weghalen uit de "Alarm"?
    public int CandlesWithZeroVolume { get; set; } // Candles zonder volume
    public int CandlesWithFlatPrice { get; set; } // De zogenaamde platte candles
    public int AboveBollingerBandsSma { get; set; } // Aantal candles die boven de BB.Sma uitkomen
    public int AboveBollingerBandsUpper { get; set; } // Aantal candles die boven de BB.Upper uitkomen

    // Display only, in het grid om em om een grijze regel laten zien
    [Computed]
    public int ItemIndex { get; set; }


    // Barometers
    public decimal? Barometer15m { get; set; }
    public decimal? Barometer30m { get; set; }
    public decimal? Barometer1h { get; set; }
    public decimal? Barometer4h { get; set; }
    public decimal? Barometer1d { get; set; }

    // Trend
    public CryptoTrendIndicator? Trend15m { get; set; }
    public CryptoTrendIndicator? Trend30m { get; set; }
    public CryptoTrendIndicator? Trend1h { get; set; }
    public CryptoTrendIndicator? Trend4h { get; set; }
    public CryptoTrendIndicator? Trend1d { get; set; }


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
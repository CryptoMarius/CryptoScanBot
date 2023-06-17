using CryptoSbmScanner.Signal;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;


public enum CryptoTradeDirection
{
    Long,
    Short
}

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
    public virtual CryptoCandle Candle { get; set; }


    [Computed]
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

    // Tot dit tijdstip is dit signaal ongeveer geldig (~x candles, tegenwoordig instelbaar)
    public DateTime ExpirationDate { get; set; }

    public CryptoTradeDirection Mode { get; set; }

    [Computed]
    public string ModeText
    {
        get
        {
            if (IsInvalid)
                return "info";

            return Mode switch
            {
                CryptoTradeDirection.Long => "long",
                CryptoTradeDirection.Short => "short",
                _ => "?",
            };
        }
    }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " signal=" + OpenDate.ToLocalTime() + " " + ModeText + " " + StrategyText; } }

    public SignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return SignalHelper.GetSignalAlgorithmText(Strategy); } }


    public double Last24HoursChange { get; set; }
    public double Last24HoursEffective { get; set; }
    //public double Last48HoursChange { get; set; }
    //public double Last48HoursEffective { get; set; }

    public string EventText { get; set; }
    public decimal Price { get; set; }
    [Computed]
    public decimal? LastPrice { get; set; }
    [Computed]
    public double? PriceDiff { get { return (double)(100 * ((Symbol.LastPrice / Price) - 1)); } }
    public decimal Volume { get; set; }

    //[Computed]
    //public string Reaction { get; set; }

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

    public double? KeltnerUpperBand { get; set; }
    public double? KeltnerLowerBand { get; set; }

    // RSI waarden
    public double? Rsi { get; set; }
    public double? SlopeRsi { get; set; }

    public int FluxIndicator5m { get; set; }

    //public double? Ema8 { get; set; }
    public double? Ema20 { get; set; }
    public double? Ema50 { get; set; }
    //public double? Ema100 { get; set; }
    public double? Ema200 { get; set; }
    public double? SlopeEma20 { get; set; }
    public double? SlopeEma50 { get; set; }

    // SMA waarden
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    public double? Sma50 { get; set; }
    //public double? Sma100 { get; set; }
    public double? Sma200 { get; set; }
    public double? SlopeSma20 { get; set; }
    public double? SlopeSma50 { get; set; }


    // Wellicht introduceren en weghalen uit de "Alarm"?
    public int CandlesWithZeroVolume { get; set; } // Candles zonder volume
    public int CandlesWithFlatPrice { get; set; } // De zogenaamde platte candles
    public int AboveBollingerBandsSma { get; set; } // Aantal candles die boven de BB.Sma uitkomen
    public int AboveBollingerBandsUpper { get; set; } // Aantal candles die boven de BB.Upper uitkomen
}
using CryptoSbmScanner.Signal;

namespace CryptoSbmScanner.Model;

public enum SignalStrategy
{
    candlesJumpUp, // 0
    candlesJumpDown, // 1

    sbm1Overbought, // 2
    sbm1Oversold, // 3

    stobbOverbought, // 4
    stobbOversold, // 5

    sbm2Overbought, // 6
    sbm2Oversold, // 7

    sbm3Overbought, // 8
    sbm3Oversold, // 9

    // Experimental
    sbm4Oversold, // 10
    sbm4Overbought, // 11

    sbm5Oversold,
    sbm5Overbought,

    // Experimental
    bullishEngulfing,
    bearischEngulfing,

    // Experimental
    priceCrossedSma20,
    priceCrossedSma50,

    // Experimental
    priceCrossedEma20,
    priceCrossedEma50,

    // Experimental
    slopeSma50TurningNegative,
    slopeSma50TurningPositive,

    slopeEma50TurningNegative,
    slopeEma50TurningPositive,

    slopeSma20TurningPositive,
    slopeEma20TurningPositive
}


public enum SignalMode
{
    modeInfo,
    modeLong,
    modeShort,
    modeInfo2 // not relevant signals
}

public class CryptoSignal
{
    public bool BackTest { get; set; }
    public virtual CryptoExchange Exchange { get; set; }
    public virtual CryptoSymbol Symbol { get; set; }

    public virtual CryptoInterval Interval { get; set; }
    public virtual CryptoCandle Candle { get; set; }
    public long EventTime { get; set; }
    public DateTime OpenDate { get; set; }

    // Einde van de candle (voor sorteren in web)
    public DateTime CloseDate { get; set; }

    public SignalMode Mode { get; set; }

    public string ModeText
    {
        get
        {
            return Mode switch
            {
                SignalMode.modeLong => "long",
                SignalMode.modeShort => "short",
                SignalMode.modeInfo => "info",
                SignalMode.modeInfo2 => "info",
                _ => "?",
            };
        }
    }

    public string DisplayText
    {
        get
        {
            return Symbol.Name + " " + Interval.Name + " " + OpenDate.ToLocalTime() + " " + ModeText + " " + StrategyText;
        }
    }

    public SignalStrategy Strategy { get; set; }


    public string StrategyText
    {
        get
        {
            return SignalHelper.GetSignalAlgorithmText(Strategy);
        }
    }


    public double Last24HoursChange { get; set; }
    public double Last24HoursEffective { get; set; }
    //public double Last48HoursChange { get; set; }
    //public double Last48HoursEffective { get; set; }

    public string EventText { get; set; }
    public decimal Price { get; set; }
    public decimal Volume { get; set; }

    public float TrendPercentage { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }

    // Stochastic waarden
    public double? StochOscillator { get; set; } // Stochastic oscillator %K
    public double? StochSignal { get; set; }  // Stochastic oscillator %D

    // Bollinger Bands
    //public double? BollingerBandsLowerBand { get; set; }
    //public double? BollingerBandsUpperBand { get; set; }
    public double? BollingerBandsDeviation { get; set; }
    public double? BollingerBandsPercentage { get; set; }

    // PSAR waarden
    public double? PSar { get; set; }

    public double? Tema { get; set; }

    // RSI waarden
    public double? Rsi { get; set; }
    public double? SlopeRsi { get; set; }

    public int FluxIndicator5m { get; set; }
    //#if DEBUG
    //public double? PSarDave { get; set; }
    //public double? PSarJason { get; set; }
    //public double? PSarTulip { get; set; }
    //#endif        

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

    public decimal? LastPrice;
}
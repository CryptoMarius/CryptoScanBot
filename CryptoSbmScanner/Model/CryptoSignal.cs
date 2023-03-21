namespace CryptoSbmScanner.Model;

public enum SignalStrategy
{
    strategyCandlesJumpUp, // 0
    strategyCandlesJumpDown, // 1

    strategySbm1Overbought, // 2
    strategySbm1Oversold, // 3

    strategyStobbOverbought, // 4
    strategyStobbOversold, // 5

    strategySbm2Overbought, // 6
    strategySbm2Oversold, // 7

    strategySbm3Overbought, // 8
    strategySbm3Oversold, // 9

    // Experimental (need to go)
    strategyPriceCrossedMa20,
    strategyPriceCrossedMa50
}


public enum SignalMode
{
    modeInfo,
    modeLong,
    modeShort
}

public class CryptoSignal
{
    public bool BackTest { get; set; }
    public CryptoCandle Candle { get; set; }
    public virtual CryptoExchange Exchange { get; set; }
    public virtual CryptoSymbol Symbol { get; set; }

    public long EventTime { get; set; }
    public DateTime OpenDate { get; set; }

    // Einde van de candle (voor sorteren in web)
    public DateTime CloseDate { get; set; }

    public SignalMode Mode { get; set; }

    public string ModeText
    {
        get
        {
            string text = Mode switch
            {
                SignalMode.modeLong => "long",
                SignalMode.modeShort => "short",
                SignalMode.modeInfo => "info",
                _ => "?",
            };
            return text;
        }
    }

    public SignalStrategy Strategy { get; set; }
    public string StrategyText
    {
        get
        {
            string text = Strategy switch
            {
                SignalStrategy.strategyCandlesJumpUp => "jump+",
                SignalStrategy.strategyCandlesJumpDown => "jump-",
                SignalStrategy.strategyStobbOverbought => "stobb+",
                SignalStrategy.strategyStobbOversold => "stobb-",
                SignalStrategy.strategySbm1Overbought => "sbm+",
                SignalStrategy.strategySbm1Oversold => "sbm-",
                SignalStrategy.strategySbm2Overbought => "sbm2+",
                SignalStrategy.strategySbm2Oversold => "sbm2-",
                SignalStrategy.strategySbm3Overbought => "sbm3+",
                SignalStrategy.strategySbm3Oversold => "sbm3-",
                SignalStrategy.strategyPriceCrossedMa20 => "close>ma20",
                SignalStrategy.strategyPriceCrossedMa50 => "close>ma50",
                _ => "?",
            };
            return text;
        }
    }

    public decimal Last24Hours { get; set; }

    public string EventText { get; set; }
    public virtual CryptoInterval Interval { get; set; }

    public decimal Price { get; set; }
    public decimal Volume { get; set; }

    public float TrendPercentage { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }

    // Stochastic waarden
    public float StochOscillator { get; set; } // Stochastic oscillator %K
    public float StochSignal { get; set; }  // Stochastic oscillator %D

    // Bollinger Bands
    public float BollingerBandsLowerBand { get; set; }
    public float BollingerBandsUpperBand { get; set; }
    public float BollingerBandsPercentage { get; set; }

    // PSAR waarden
    public float PSar { get; set; }
#if DEBUG
    public float PSarDave { get; set; }
    public float PSarJason { get; set; }
    public float PSarTulip { get; set; }
#endif        

    // RSI waarden
    public float Rsi { get; set; }

    // SMA waarden
    public float Sma200 { get; set; }
    public float Sma50 { get; set; }
    public float Sma20 { get; set; }


    // Wellicht introduceren en weghalen uit de "Alarm"?
    public int CandlesWithZeroVolume { get; set; } // Candles zonder volume
    public int CandlesWithFlatPrice { get; set; } // De zogenaamde platte candles
    public decimal AboveBollingerBandsSma { get; set; } // Aantal candles die boven de BB.Sma uitkomen
    public decimal AboveBollingerBandsUpper { get; set; } // Aantal candles die boven de BB.Upper uitkomen
}
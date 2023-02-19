using System;

namespace CryptoSbmScanner
{
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
        //[JsonIgnore]
        public bool BackTest { get; set; }

        //[JsonIgnore]
        public CryptoCandle Candle { get; set; }

        //[JsonIgnore]
        public virtual CryptoExchange Exchange { get; set; }

        //[JsonIgnore]
        public virtual CryptoSymbol Symbol { get; set; }

        public long EventTime { get; set; }
        public DateTime OpenDate { get; set; }

        // Einde van de candle (voor sorteren in web)
        public DateTime CloseDate { get; set; }

        // Tot dit tijdstip is dit signaal ongeveer geldig (~15 candles)
        //public DateTime ExpirationDate { get; set; }
        
        public SignalMode Mode { get; set; }
        //[JsonIgnore]
        public string ModeText
        {
            get
            {
                string text = "";
                switch (Mode)
                {
                    case SignalMode.modeLong:
                        text = "long";
                        break;
                    case SignalMode.modeShort:
                        text = "short";
                        break;
                    case SignalMode.modeInfo:
                        text = "info";
                        break;
                    default:
                        text = "?";
                        break;
                }
                return text;
            }
        }

        public SignalStrategy Strategy { get; set; }
        //[JsonIgnore]
        public string StrategyText
        {
            get
            {
                string text = "";
                switch (Strategy)
                {
                    case SignalStrategy.strategyCandlesJumpUp:
                        text = "jump+";
                        break;
                    case SignalStrategy.strategyCandlesJumpDown:
                        text = "jump-";
                        break;
                    case SignalStrategy.strategyStobbOverbought:
                        text = "stobb+";
                        break;
                    case SignalStrategy.strategyStobbOversold:
                        text = "stobb-";
                        break;
                    case SignalStrategy.strategySbm1Overbought:
                        text = "sbm+";
                        break;
                    case SignalStrategy.strategySbm1Oversold:
                        text = "sbm-";
                        break;
                    case SignalStrategy.strategySbm2Overbought:
                        text = "sbm2+";
                        break;
                    case SignalStrategy.strategySbm2Oversold:
                        text = "sbm2-";
                        break;
                    case SignalStrategy.strategySbm3Overbought:
                        text = "sbm3+";
                        break;
                    case SignalStrategy.strategySbm3Oversold:
                        text = "sbm3-";
                        break;
                    case SignalStrategy.strategyPriceCrossedMa20:
                        text = "close>ma20";
                        break;
                    case SignalStrategy.strategyPriceCrossedMa50:
                        text = "close>ma50";
                        break;
                    default:
                        text = "?";
                        break;
                }
                return text;
            }
        }

        public decimal Last24Hours { get; set; }

        public string EventText { get; set; }


        //public int IntervalId { get; set; }
        //[JsonIgnore]
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
        //[JsonIgnore]
        public float PSarDave { get; set; }
        //[JsonIgnore]
        public float PSarJason { get; set; }
        //[JsonIgnore]
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

}
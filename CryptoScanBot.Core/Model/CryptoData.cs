using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

// Basic indicator data

// Shared between Position, Signal and Indicator data
public class CryptoData
{
    // Bollinger Bands
    public double? BollingerBandsDeviation { get; set; }
    [Computed]
    public double? BollingerBandsUpperBand { get { return Sma20 + BollingerBandsDeviation; } }
    [Computed]
    public double? BollingerBandsLowerBand { get { return Sma20 - BollingerBandsDeviation; } }
    public double? BollingerBandsPercentage { get; set; }

    //public double? KeltnerUpperBand { get; set; }
    //public double? KeltnerLowerBand { get; set; }
    //public double? KeltnerCenterLine { get; set; }
    //public double? KeltnerCenterLineSlope { get; set; }

    // MACD indicator values
    public double? MacdValue { get; set; } // blue - Oscillator
    public double? MacdSignal { get; set; } // red - moving average
    public double? MacdHistogram { get; set; } // kan ook calculated worden (source - value of andersom)
    //public double? MacdHistogram2 { get { return MacdSignal - MacdValue; } }
    [Computed]
    public double? SlopeMacd { get; set; }
    
    // Stochastic indicator values
    /// <summary>
    /// Stoch Oscillator %K (blue), calculated from the last 14 candles
    /// </summary>
    public double? StochOscillator { get; set; } // Stochastic oscillator %K (blue)
    /// <summary>
    /// Stoch Signal %D (red), average from the last 3 %K values
    /// </summary>
    public double? StochSignal { get; set; } // Stochastic oscillator %D (red)
    [Computed]
    public double? SlopeStoch { get; set; }

    // EMA (Exponential Moving Average) indicator values
#if EXTRASTRATEGIES
    //public double? Ema8 { get; set; }
    //public double? Ema9 { get; set; }
    //public double? Ema20 { get; set; }
    //public double? SlopeEma20 { get; set; }
    //public double? Ema50 { get; set; }
    //public double? SlopeEma50 { get; set; }
    //public double? Ema100 { get; set; }
    //public double? SlopeEma100 { get; set; }
    //public double? Ema200 { get; set; }
    //public double? SlopeEma200 { get; set; }
#endif


    // SMA (Simple Moving Average) indicator values
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    public double? SlopeSma20 { get; set; }
    public double? Sma50 { get; set; }
    public double? SlopeSma50 { get; set; }
    public double? Sma100 { get; set; }
    public double? SlopeSma100 { get; set; }
    public double? Sma200 { get; set; }
    public double? SlopeSma200 { get; set; }

    // RSI indicator
    public double? Rsi { get; set; }
    public double? SlopeRsi { get; set; }

    // Parabolic Sar indicator
    public double? PSar { get; set; }

    /// <summary>
    /// Copy common indicator values
    /// </summary>
    public virtual void AssignValues(CryptoData source)
    {
        // Bollinger bands indicator values
        BollingerBandsDeviation = source.BollingerBandsDeviation;
        BollingerBandsPercentage = source.BollingerBandsPercentage;

        // MACD indicator values
        MacdValue = source.MacdValue;
        MacdSignal= source.MacdSignal;
        MacdHistogram = source.MacdHistogram;
        SlopeMacd = source.SlopeMacd;

        // Stochastic indicator values
        StochSignal = source.StochSignal;
        StochOscillator = source.StochOscillator;

        // RSI indicator values
        Rsi = source.Rsi;
        SlopeRsi = source.SlopeRsi;

        // EMA indicator values
        //Ema9 = source.Ema9;
        //public double? Ema8 { get; set; }
        //public double? Ema20 { get; set; }
        //public double? SlopeEma20 { get; set; }
        //public double? Ema50 { get; set; }
        //public double? SlopeEma50 { get; set; }
        //public double? Ema100 { get; set; }
        //public double? SlopeEma100 { get; set; }
        //public double? Ema200 { get; set; }
        //public double? SlopeEma200 { get; set; }

        // SMA indicator values
        //public double? Sma8 { get; set; }
        Sma20 = source.Sma20;
        SlopeSma20 = source.SlopeSma20;
        Sma50 = source.Sma50;
        SlopeSma50 = source.SlopeSma50;
        Sma100 = source.Sma100;
        SlopeSma100 = source.SlopeSma100;
        Sma200 = source.Sma200;
        SlopeSma200 = source.SlopeSma200;

        // Parabolic SAR indicator value
        PSar = source.PSar;
    }
}

// Shared between Position and Signal
public class CryptoData2: CryptoData
{
    public decimal SignalPrice { get; set; }
    public decimal SignalVolume { get; set; }

    public CryptoTradeSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side.ToString().ToLower(); } }

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return SignalHelper.GetAlgorithm(Strategy); } }


    public double Last24HoursChange { get; set; }
    public double Last24HoursEffective { get; set; }
    public double Last10DaysEffective { get; set; }

    public float TrendPercentage { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }

    public int LuxIndicator5m { get; set; }

    // Wellicht introduceren en weghalen uit de "Alarm"?
    public int CandlesWithZeroVolume { get; set; } // Candles zonder volume
    public int CandlesWithFlatPrice { get; set; } // De zogenaamde platte candles
    public int AboveBollingerBandsSma { get; set; } // Aantal candles die boven de BB.Sma uitkomen
    public int AboveBollingerBandsUpper { get; set; } // Aantal candles die boven de BB.Upper uitkomen

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

    // Statistics, the min and max differences against the signalprice
    public decimal PriceMin { get; set; }
    public double PriceMinPerc { get; set; }
    public decimal PriceMax { get; set; }
    public double PriceMaxPerc { get; set; }


    public override void AssignValues(CryptoData source)
    {
        base.AssignValues(source);

        if (source is CryptoData2 source2)
        {
            SignalPrice = source2.SignalPrice;
            SignalVolume = source2.SignalVolume;

            Last24HoursChange = source2.Last24HoursChange;
            Last24HoursEffective = source2.Last24HoursEffective;
            Last10DaysEffective = source2.Last10DaysEffective;

            TrendPercentage = source2.TrendPercentage;
            TrendIndicator = source2.TrendIndicator;

            LuxIndicator5m = source2.LuxIndicator5m;

            // Wellicht introduceren en weghalen uit de "Alarm"?
            CandlesWithZeroVolume = source2.CandlesWithZeroVolume;
            CandlesWithFlatPrice = source2.CandlesWithFlatPrice;
            AboveBollingerBandsSma = source2.AboveBollingerBandsSma;
            AboveBollingerBandsUpper = source2.AboveBollingerBandsUpper;

            // Een aantal trendindicatoren
            Trend15m = source2.Trend15m;
            Trend30m = source2.Trend30m;
            Trend1h = source2.Trend1h;
            Trend4h = source2.Trend4h;
            Trend1d = source2.Trend1d;

            // Barometers
            Barometer15m = source2.Barometer15m;
            Barometer30m = source2.Barometer30m;
            Barometer1h = source2.Barometer1h;
            Barometer4h = source2.Barometer4h;
            Barometer1d = source2.Barometer1d;

            PriceMin = source2.PriceMin;
            PriceMax = source2.PriceMax;
            PriceMinPerc = source2.PriceMinPerc;
            PriceMaxPerc = source2.PriceMaxPerc;
        }
    }
}

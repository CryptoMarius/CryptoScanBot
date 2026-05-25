using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

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

#if DEBUG
    // Keltner Channel (EMA centerline +/- ATR * multiplier). Not persisted to DB; only used
    // by signal classes that combine BB with KC (TTM Squeeze family).
    // To re-enable: uncomment these properties AND un-comment the keltnerList calculation
    // in IndicatorData.cs (CollectCandles / CalculateIndicators).
    //[Computed]
    //public double? KeltnerUpperBand { get; set; }
    //[Computed]
    //public double? KeltnerLowerBand { get; set; }
    //[Computed]
    //public double? KeltnerCenterLine { get; set; }
    //public double? KeltnerCenterLineSlope { get; set; }
#endif

    // MACD indicator values
    public double? MacdValue { get; set; } // blue - Oscillator
    public double? MacdSignal { get; set; } // red - moving average
    public double? MacdHistogram { get; set; } // kan ook calculated worden (source - value of andersom)
    //public double? MacdHistogram2 { get { return MacdSignal - MacdValue; } }
    //[Computed]
    //public double? SlopeMacd { get; set; }


    // Stochastic indicator values
    /// <summary>
    /// Stoch Oscillator %K (blue), calculated from the last 14 candles
    /// </summary>
    public double? StochOscillator { get; set; } // Stochastic oscillator %K (blue, fast line)
    /// <summary>
    /// Stoch Signal %D (orange), average from the last 3 %K values
    /// </summary>
    public double? StochSignal { get; set; } // Stochastic signal %D (orange, slow line)
    //public float StochSurface { get; set; }
    //public float StochSurface2 { get; set; }
    //[Computed]
    //public double? SlopeStoch { get; set; }

    // EMA (Exponential Moving Average) indicator values
#if DEBUG
    //[Computed]
    //public double? Ema5 { get; set; }
    ////public double? Ema8 { get; set; }
    //[Computed]
    //public double? Ema9 { get; set; }
    //public double? Ema20 { get; set; }
    //public double? SlopeEma20 { get; set; }
    //public double? SlopeEma50 { get; set; }
    //public double? Ema100 { get; set; }
    //public double? SlopeEma100 { get; set; }
    //public double? Ema200 { get; set; }
    //public double? SlopeEma200 { get; set; }
    //[Computed]
    //public double? Tema { get; set; }
#endif

    // SMA (Simple Moving Average) indicator values
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    //public double? SlopeSma20 { get; set; }
    public double? Sma50 { get; set; }
    //public double? SlopeSma50 { get; set; }
    public double? Sma100 { get; set; }
    //public double? SlopeSma100 { get; set; }
    public double? Sma200 { get; set; }
    //public double? SlopeSma200 { get; set; }

#if DEBUG
    public double? Ema50 { get; set; }
    [Computed]
    public double? Wma05Low { get; set; }
    [Computed]
    public double? Wma05High { get; set; }
    [Computed]
    public double? Wma10Low { get; set; }
    [Computed]
    public double? Wma10High { get; set; }
    // ATR(14) — used by BBMA Omni: RejectedEMA50 big-body filter, MHV gap calculation.
    // Not persisted to DB; computed in IndicatorData.CalculateIndicators.
    // ATR 14 is the standard ATR, nothing special
    [Computed]
    public double? Atr14 { get; set; }
#endif

    // RSI indicator
    public double? Rsi { get; set; }
    //public double? SlopeRsi { get; set; }
    //public float RsiSurface { get; set; }
    //public float RsiSurface2 { get; set; }

    // Parabolic Sar indicator
    public double? PSar { get; set; }

    [Computed]
    public short? Lux5mValue { get; set; }

    /// <summary>
    /// Copy common indicator values
    /// </summary>
    public virtual void AssignValues(CryptoData source)
    {
        // Bollinger bands indicator values
        BollingerBandsDeviation = source.BollingerBandsDeviation;
        BollingerBandsPercentage = source.BollingerBandsPercentage;

#if DEBUG
        //KeltnerUpperBand = source.KeltnerUpperBand;
        //KeltnerCenterLine = source.KeltnerCenterLine;
        //KeltnerLowerBand = source.KeltnerLowerBand;
#endif

        // MACD indicator values
        MacdValue = source.MacdValue;
        MacdSignal = source.MacdSignal;
        MacdHistogram = source.MacdHistogram;
        //SlopeMacd = source.SlopeMacd;

        // Stochastic indicator values
        StochSignal = source.StochSignal;
        StochOscillator = source.StochOscillator;
        //StochSurface = source.StochSurface;

        // RSI indicator values
        Rsi = source.Rsi;
        //SlopeRsi = source.SlopeRsi;
        //RsiSurface = source.RsiSurface;

        // EMA indicator values
#if DEBUG
        //Ema5 = source.Ema5;
        //public double? Ema8 { get; set; }
        //Ema9 = source.Ema9;
        //Ema20 = source.Ema20;
        //public double? SlopeEma20 { get; set; }
        //Tema = source.Tema;
        //public double? SlopeEma50 { get; set; }
        //public double? Ema100 { get; set; }
        //public double? SlopeEma100 { get; set; }
        //public double? Ema200 { get; set; }
        //public double? SlopeEma200 { get; set; }
#endif



        // SMA indicator values
        //public double? Sma8 { get; set; }
        Sma20 = source.Sma20;
        //SlopeSma20 = source.SlopeSma20;
        Sma50 = source.Sma50;
        //SlopeSma50 = source.SlopeSma50;
        Sma100 = source.Sma100;
        //SlopeSma100 = source.SlopeSma100;
        Sma200 = source.Sma200;
        //SlopeSma200 = source.SlopeSma200;

#if DEBUG
        Ema50 = source.Ema50;
        Wma05Low = source.Wma05Low;
        Wma05High = source.Wma05High;
        Wma10Low = source.Wma10Low;
        Wma10High = source.Wma10High;
        Atr14 = source.Atr14;
#endif

        // Parabolic SAR indicator value
        PSar = source.PSar;

        Lux5mValue = source.Lux5mValue;
    }
}

// Shared between Position and Signal
public class CryptoData2 : CryptoData
{
    public decimal SignalPrice { get; set; }
    public double SignalVolume { get; set; }

    public CryptoTradeSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side.ToString().ToLower(); } }

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return RegisterAlgorithms.GetAlgorithm(Strategy); } }

    public float Last24HoursChange { get; set; }
    public float LastXDaysEffective { get; set; }

    public int? LuxIndicator5m { get; set; }

    // Wellicht introduceren en weghalen uit de "Alarm"?
    public short CandlesWithZeroVolume { get; set; } // Candles zonder volume
    public short CandlesWithFlatPrice { get; set; } // De zogenaamde platte candles
    public short AboveBollingerBandsSma { get; set; } // Aantal candles die boven de BB.Sma uitkomen
    public short AboveBollingerBandsUpper { get; set; } // Aantal candles die boven de BB.Upper uitkomen

    // Barometers
    public float? Barometer15m { get; set; }
    public float? Barometer30m { get; set; }
    public float? Barometer1h { get; set; }
    public float? Barometer4h { get; set; }
    public float? Barometer1d { get; set; }

    // Market trend percentage (primary)
    public float TrendPercentagePrimary { get; set; }
    public float TrendPercentageSecondary { get; set; }

    // Trend
    public CryptoTrendIndicator? Trend15m { get; set; }
    public CryptoTrendIndicator? Trend30m { get; set; }
    public CryptoTrendIndicator? Trend1h { get; set; }
    public CryptoTrendIndicator? Trend4h { get; set; }
    public CryptoTrendIndicator? Trend1d { get; set; }
    // Trend on interval
    public CryptoTrendIndicator TrendInterval { get; set; }

    // Statistics, the min and max differences against the signalprice
    public decimal PriceMin { get; set; }
    public float PriceMinPerc { get; set; }
    public decimal PriceMax { get; set; }
    public float PriceMaxPerc { get; set; }
    public CryptoSignalStatus SignalStatus { get; set; }

    public float AvgBB { get; set; }

    public override void AssignValues(CryptoData source)
    {
        base.AssignValues(source);

        if (source is CryptoData2 source2)
        {
            SignalPrice = source2.SignalPrice;
            SignalVolume = source2.SignalVolume;

            Last24HoursChange = source2.Last24HoursChange;
            LastXDaysEffective = source2.LastXDaysEffective;

            LuxIndicator5m = source2.LuxIndicator5m;

            // Wellicht introduceren en weghalen uit de "Alarm"?
            CandlesWithZeroVolume = source2.CandlesWithZeroVolume;
            CandlesWithFlatPrice = source2.CandlesWithFlatPrice;
            AboveBollingerBandsSma = source2.AboveBollingerBandsSma;
            AboveBollingerBandsUpper = source2.AboveBollingerBandsUpper;

            // Trends
            Trend15m = source2.Trend15m;
            Trend30m = source2.Trend30m;
            Trend1h = source2.Trend1h;
            Trend4h = source2.Trend4h;
            Trend1d = source2.Trend1d;
            TrendInterval = source2.TrendInterval;

            // Market trends
            TrendPercentagePrimary = source2.TrendPercentagePrimary;
            TrendPercentageSecondary = source2.TrendPercentageSecondary;

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
            SignalStatus = source2.SignalStatus;

            AvgBB = source2.AvgBB;
        }
    }
}

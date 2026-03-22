using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Experiment;
using CryptoScanner.Core.Signal.Momentum;
using CryptoScanner.Core.Signal.Other;

namespace CryptoScanner.Core.Signal;

public static class RegisterAlgorithms
{
    /// <summary>
    /// All available strategies + indexed
    /// </summary>
    public static readonly SortedList<CryptoSignalStrategy, AlgorithmDefinition> AlgorithmDefinitionList = [];


    public static void Register(AlgorithmDefinition algorithmDefinition)
    {
        AlgorithmDefinitionList.Add(algorithmDefinition.Strategy, algorithmDefinition);
    }

    // a class contructor get called later (when something of the class is touched, cannot use it to register something)


    static RegisterAlgorithms()
    {
        //***************************************************
        // Jump
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "jump",
            Strategy = CryptoSignalStrategy.Jump,
            AnalyzeLongType = typeof(SignalCandleJumpShort),
            AnalyzeShortType = typeof(SignalCandleJumpLong),
            BypassFilters = true, // Informational: always fire, bypass barometer/volume/feedback filters
        });

        //***************************************************
        // SBMx (a special kind of STOBB)
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "sbm1",
            Strategy = CryptoSignalStrategy.Sbm1,
            AnalyzeLongType = typeof(SignalSbm1Long),
            AnalyzeShortType = typeof(SignalSbm1Short),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "sbm2",
            Strategy = CryptoSignalStrategy.Sbm2,
            AnalyzeLongType = typeof(SignalSbm2Long),
            AnalyzeShortType = typeof(SignalSbm2Short),
        });


        Register(new AlgorithmDefinition()
        {
            Name = "sbm3",
            Strategy = CryptoSignalStrategy.Sbm3,
            AnalyzeLongType = typeof(SignalSbm3Long),
            AnalyzeShortType = typeof(SignalSbm3Short),
        });



        //***************************************************
        // STOBB
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "stobb",
            Strategy = CryptoSignalStrategy.Stobb,
            AnalyzeLongType = typeof(SignalStobbLong),
            AnalyzeShortType = typeof(SignalStobbShort),
        });
        Register(new AlgorithmDefinition()
        {
            Name = "stobb.multi",
            Strategy = CryptoSignalStrategy.StobbMulti,
            AnalyzeLongType = typeof(SignalStobbMultiLong),
            AnalyzeShortType = typeof(SignalStobbMultiShort),
        });



        //***************************************************
        // WGHBM - Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.
        //***************************************************
        // https://www.tradingview.com/script/0F1sNM49-WGHBM/ (not available anymore)
        Register(new AlgorithmDefinition()
        {
            Name = "storsi", // was WGHM = We Gaan Het Meemaken..
            Strategy = CryptoSignalStrategy.StoRsi,
            AnalyzeLongType = typeof(SignalStoRsiLong),
            AnalyzeShortType = typeof(SignalStoRsiShort),
        });

        // another combined with a higher timeframe
        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi",
            Strategy = CryptoSignalStrategy.StoRsiMulti,
            AnalyzeLongType = typeof(SignalStoRsiMultiLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiShort),
        });

#if DEBUG
        // another combined with a higher timeframe
        Register(new AlgorithmDefinition()
        {
            Name = "stoch",
            Strategy = CryptoSignalStrategy.Stoch,
            AnalyzeLongType = typeof(SignalStochLong),
            AnalyzeShortType = typeof(SignalStochShort),
        });

        // 5m + 1h directional: 1h was in extreme zone and is on its way to the other side,
        // 5m confirms the same direction
        Register(new AlgorithmDefinition()
        {
            Name = "stoch.dir",
            Strategy = CryptoSignalStrategy.StochDir,
            AnalyzeLongType = typeof(SignalStochDirLong),
            AnalyzeShortType = typeof(SignalStochDirShort),
        });

#endif

        //***************************************************
        // Level approaching
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "dlz",
            Strategy = CryptoSignalStrategy.DominantLevel,
            AnalyzeLongType = typeof(SignalDominantLevelLong),
            AnalyzeShortType = typeof(SignalDominantLevelShort),
            BypassFilters = true, // Informational: bypass volume and feedback filters
        });

        Register(new AlgorithmDefinition()
        {
            Name = "dlz.near",
            Strategy = CryptoSignalStrategy.DominantLevelNear,
            AnalyzeLongType = typeof(SignalDominantLevelNearLong),
            AnalyzeShortType = typeof(SignalDominantLevelNearShort),
            BypassFilters = true, // Informational: bypass volume and feedback filters
        });


        //#if DEBUG
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "BbRsiEngulf",
        //            Strategy = CryptoSignalStrategy.BbRsiEngulfing,
        //            AnalyzeLongType = typeof(SignalBbRsiEngulfingLong),
        //            AnalyzeShortType = typeof(SignalBbRsiEngulfingShort),
        //        });
        //#endif

        Register(new AlgorithmDefinition()
        {
            Name = "fvg",
            Strategy = CryptoSignalStrategy.FairValueGap,
            AnalyzeLongType = typeof(SignalFairValueGapLong),
            AnalyzeShortType = typeof(SignalFairValueGapShort),
            BypassFilters = true, // Informational: bypass volume and feedback filters
        });

        Register(new AlgorithmDefinition()
        {
            Name = "nwe",
            Strategy = CryptoSignalStrategy.NadarayaWatsonEnvelope,
            AnalyzeLongType = typeof(SignalLuxNadarayaWatsonEnvelope),
            AnalyzeShortType = typeof(SignalLuxNadarayaWatsonEnvelope),
        });

#if DEBUG
        Register(new AlgorithmDefinition()
        {
            Name = "nwe.pull",
            Strategy = CryptoSignalStrategy.NadarayaWatsonEnvelopePull,
            AnalyzeLongType = typeof(SignalLuxNadarayaWatsonEnvelopePull),
            AnalyzeShortType = typeof(SignalLuxNadarayaWatsonEnvelopePull),
        });
#endif

#if DEBUG
        // BBMA - Oma Ally
        Register(new AlgorithmDefinition()
        {
            Name = "bbma",
            Strategy = CryptoSignalStrategy.BbMa,
            AnalyzeLongType = typeof(SignalBbMaLong),
            AnalyzeShortType = typeof(SignalBbMaShort),
        });
#endif

#if DEBUG
        // BBMA Reentry - Oma Ally: price returns to the 510 zone after a CSD crossover
        Register(new AlgorithmDefinition()
        {
            Name = "bbma.reentry.old",
            Strategy = CryptoSignalStrategy.BbmaReentryOld,
            AnalyzeLongType = typeof(SignalBbmaReentryOldLong),
            AnalyzeShortType = typeof(SignalBbmaReentryOldShort),
        });
#endif

#if DEBUG
        // BBMA Reentry - Oma Ally: price returns to the 510 zone after a CSD crossover
        Register(new AlgorithmDefinition()
        {
            Name = "bbma.reentry.new",
            Strategy = CryptoSignalStrategy.BbmaReentryNew,
            AnalyzeLongType = typeof(SignalBbmaReentryNewLong),
            AnalyzeShortType = typeof(SignalBbmaReentryNewShort),
        });
#endif

#if DEBUG
        // Trend reversal
        Register(new AlgorithmDefinition()
        {
            Name = "trend",
            Strategy = CryptoSignalStrategy.Trend,
            AnalyzeLongType = typeof(SignalTrendLong),
            AnalyzeShortType = typeof(SignalTrendShort),
        });
#endif


        // Does not perform well in the signal statistics
        //#if DEBUG
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "rolling fft",
        //            Strategy = CryptoSignalStrategy.RollingFft,
        //            AnalyzeLongType = typeof(SignalRollingFft),
        //            AnalyzeShortType = typeof(SignalRollingFft),
        //        });
        //#endif


#if DEBUG
        Register(new AlgorithmDefinition()
        {
            Name = "rsi divergence",
            Strategy = CryptoSignalStrategy.RsiDivergence,
            AnalyzeLongType = typeof(SignalRsiDivergence),
            AnalyzeShortType = typeof(SignalRsiDivergence),
        });
#endif


//#if DEBUG

//        //***************************************************
//        // BbWickSma - BB wick rejection + SMA20 slope + SMA50 cross reversal
//        // Trade statistics is kind of bad
//        //***************************************************
//        Register(new AlgorithmDefinition()
//        {
//            Name = "bbwicksma",
//            Strategy = CryptoSignalStrategy.BbWickSma,
//            AnalyzeLongType = typeof(SignalBbWickSmaLong),
//            AnalyzeShortType = typeof(SignalBbWickSmaShort),
//        });
//#endif

#if DEBUG
        //***************************************************
        // BBMA Magic Extreme – WMA5(Low/High) AND WMA10(Low/High) both outside the Bollinger Band,
        // combined with a price wick that touches/breaks the band but closes back inside.
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "bbma.magic.extreme",
            Strategy = CryptoSignalStrategy.BbmaMagicExtreme,
            AnalyzeLongType = typeof(SignalBbmaMagicExtreme),
            AnalyzeShortType = typeof(SignalBbmaMagicExtreme),
        });
#endif

    }


    /// <summary>
    /// Return the algorithm definition
    /// </summary>
    public static bool GetAlgorithm(CryptoSignalStrategy strategy, out AlgorithmDefinition? definition)
    {
        return AlgorithmDefinitionList.TryGetValue(strategy, out definition);
    }

    /// <summary>
    /// Return the name of the algorithm
    /// </summary>
    public static string GetAlgorithm(CryptoSignalStrategy strategy)
    {
        if (GetAlgorithm(strategy, out AlgorithmDefinition? definition))
            return definition!.Name;
        return strategy.ToString();
    }

    /// <summary>
    /// Return an instance of the algorithm (long/short)
    /// </summary>
    public static SignalCreateBase? GetAlgorithm(CryptoTradeSide side, CryptoSignalStrategy strategy)
    {
        if (GetAlgorithm(strategy, out AlgorithmDefinition? definition))
        {
            Type? analyzeClass = null;
            if (side == CryptoTradeSide.Long && definition!.AnalyzeLongType != null)
                analyzeClass = definition!.AnalyzeLongType;
            if (side == CryptoTradeSide.Short && definition!.AnalyzeShortType != null)
                analyzeClass = definition!.AnalyzeShortType;

            if (analyzeClass != null)
            {
                SignalCreateBase? x = (SignalCreateBase?)Activator.CreateInstance(analyzeClass);
                x!.SignalSide = side;
                x!.SignalStrategy = strategy;
                return x;
            }
        }
        return null;
    }

}
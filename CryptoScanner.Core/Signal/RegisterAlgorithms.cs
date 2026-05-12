using CryptoScanner.Core.Enums;
#if DEBUG
using CryptoScanner.Core.Signal.Bbma;
#endif
using CryptoScanner.Core.Signal.Experiment;
using CryptoScanner.Core.Signal.Momentum;
using CryptoScanner.Core.Signal.Nwe;
using CryptoScanner.Core.Signal.Other;
using CryptoScanner.Core.Signal.Sbm;
using CryptoScanner.Core.Signal.Stobb;
using CryptoScanner.Core.Signal.Trend;

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
            AnalyzeLongType = typeof(SignalCandleJumpLong),
            AnalyzeShortType = typeof(SignalCandleJumpShort),
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
        });

        Register(new AlgorithmDefinition()
        {
            Name = "dlz.near",
            Strategy = CryptoSignalStrategy.DominantLevelNear,
            AnalyzeLongType = typeof(SignalDominantLevelNearLong),
            AnalyzeShortType = typeof(SignalDominantLevelNearShort),
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
        });

        Register(new AlgorithmDefinition()
        {
            Name = "nwe",
            Strategy = CryptoSignalStrategy.NadarayaWatsonEnvelope,
            AnalyzeLongType = typeof(SignalLuxNadarayaWatsonEnvelope),
            AnalyzeShortType = typeof(SignalLuxNadarayaWatsonEnvelope),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "nwe.np",
            Strategy = CryptoSignalStrategy.NadarayaWatsonEnvelopeNp,
            AnalyzeLongType = typeof(SignalLuxNadarayaWatsonEnvelopeNp),
            AnalyzeShortType = typeof(SignalLuxNadarayaWatsonEnvelopeNp),
        });

        // NWE × BB crossover: NWE curls through the BB band after extending beyond it
        Register(new AlgorithmDefinition()
        {
            Name = "nwe.bb",
            Strategy = CryptoSignalStrategy.NweBb,
            AnalyzeLongType = typeof(SignalNweBbLong),
            AnalyzeShortType = typeof(SignalNweBbShort),
        });

        //#if DEBUG
        //        // BBMA - Oma Ally
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "bbma.grok",
        //            Strategy = CryptoSignalStrategy.BbMaGrok,
        //            AnalyzeLongType = typeof(SignalBbMaGrokLong),
        //            AnalyzeShortType = typeof(SignalBbMaShort),
        //        });
        //#endif

        //#if DEBUG
        //        // BBMA - Oma Ally: price returns to the 510 zone after a CSD crossover
        //        // No confirmations from higher timeframe(s)
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "bbma.old",
        //            Strategy = CryptoSignalStrategy.BbmaReentryOld,
        //            AnalyzeLongType = typeof(SignalBbmaReentryOldLong),
        //            AnalyzeShortType = typeof(SignalBbmaReentryOldShort),
        //        });
        //#endif


#if DEBUG
        // BBMA - Oma Ally: price returns to the 510 zone after a CSD crossover
        // Confirmations from higher timeframe(s)
        Register(new AlgorithmDefinition()
        {
            Name = "bbma",
            Strategy = CryptoSignalStrategy.Bbma,
            AnalyzeLongType = typeof(SignalBbmaLong),
            AnalyzeShortType = typeof(SignalBbmaShort),
        });
#endif

#if DEBUG
        // Gaussian Scalp: 3-layer scalping strategy (Gaussian filter + RSI30 + MACD 24/52/9)
        Register(new AlgorithmDefinition()
        {
            Name = "gscalp",
            Strategy = CryptoSignalStrategy.GaussianScalp,
            AnalyzeLongType = typeof(SignalGaussianScalpLong),
            AnalyzeShortType = typeof(SignalGaussianScalpShort),
        });
#endif

#if DEBUG
        // Gaussian Pullback: wick-touch + close-above/below the Gaussian filter line during confirmed trend
        Register(new AlgorithmDefinition()
        {
            Name = "gpullback",
            Strategy = CryptoSignalStrategy.GaussianPullback,
            AnalyzeLongType = typeof(SignalGaussianPullbackLong),
            AnalyzeShortType = typeof(SignalGaussianPullbackShort),
        });
#endif

#if DEBUG
        // Trend reversal (Dow Theory)
        Register(new AlgorithmDefinition()
        {
            Name = "trend",
            Strategy = CryptoSignalStrategy.Trend,
            AnalyzeLongType = typeof(SignalTrendLong),
            AnalyzeShortType = typeof(SignalTrendShort),
        });
#endif

#if DEBUG
        // BOS/CHoCH: Break of Structure and Change of Character signals
        Register(new AlgorithmDefinition()
        {
            Name = "bos",
            Strategy = CryptoSignalStrategy.TrendBosChoch,
            AnalyzeLongType = typeof(SignalBosChochLong),
            AnalyzeShortType = typeof(SignalBosChochShort),
        });
#endif

#if DEBUG
        // Box Theory (Darvas-style): breakout from a consolidation box
        Register(new AlgorithmDefinition()
        {
            Name = "box",
            Strategy = CryptoSignalStrategy.Box,
            AnalyzeLongType = typeof(SignalBoxLong),
            AnalyzeShortType = typeof(SignalBoxShort),
        });
#endif


        //#if DEBUG
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "rsi divergence",
        //            Strategy = CryptoSignalStrategy.RsiDivergence,
        //            AnalyzeLongType = typeof(SignalRsiDivergence),
        //            AnalyzeShortType = typeof(SignalRsiDivergence),
        //        });
        //#endif


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
using CryptoScanner.Core.Enums;
#if DEBUG
using CryptoScanner.Core.Signal.Bbma;
using CryptoScanner.Core.Signal.Squeeze;
using CryptoScanner.Core.Signal.StochMacd;
using CryptoScanner.Core.Signal.WaveTrend;
#endif
using CryptoScanner.Core.Signal.Dlz;
using CryptoScanner.Core.Signal.Fvg;
using CryptoScanner.Core.Signal.Jump;
using CryptoScanner.Core.Signal.Nwe;
using CryptoScanner.Core.Signal.Sbm;
using CryptoScanner.Core.Signal.Stobb;
using CryptoScanner.Core.Signal.Storsi;
using CryptoScanner.Core.Signal.Trend;

namespace CryptoScanner.Core.Signal;

// Class for registering all algorithms
public class AlgorithmDefinition
{
    public required string Name { get; set; }
    public required CryptoSignalStrategy Strategy { get; set; }
    public required Type? AnalyzeLongType { get; set; }
    public required Type? AnalyzeShortType { get; set; }
}

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

        // NWE Repaining
        Register(new AlgorithmDefinition()
        {
            Name = "nwe",
            Strategy = CryptoSignalStrategy.NadarayaWatsonEnvelope,
            AnalyzeLongType = typeof(SignalLuxNadarayaWatsonEnvelope),
            AnalyzeShortType = typeof(SignalLuxNadarayaWatsonEnvelope),
        });

#if DEBUG
        // NWE not repainting
        Register(new AlgorithmDefinition()
        {
            Name = "nwe.np",
            Strategy = CryptoSignalStrategy.NadarayaWatsonEnvelopeNp,
            AnalyzeLongType = typeof(SignalLuxNadarayaWatsonEnvelopeNp),
            AnalyzeShortType = typeof(SignalLuxNadarayaWatsonEnvelopeNp),
        });
#endif

#if DEBUG
        // NWE × BB crossover: NWE curls through the BB band after extending beyond it
        Register(new AlgorithmDefinition()
        {
            Name = "nwe.bb",
            Strategy = CryptoSignalStrategy.NweBb,
            AnalyzeLongType = typeof(SignalNweBbLong),
            AnalyzeShortType = typeof(SignalNweBbShort),
        });
#endif

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
        // BBMA Omni - direct port of the OmniView MQL5 indicator state definitions
        // (Extreme / CSD / CSM / MLV / Reentry). Reuses the multi-TF setup from SignalBbma.
        Register(new AlgorithmDefinition()
        {
            Name = "bbma.omni",
            Strategy = CryptoSignalStrategy.BbmaOmni,
            AnalyzeLongType = typeof(SignalBbmaOmniLong),
            AnalyzeShortType = typeof(SignalBbmaOmniShort),
        });
#endif

#if DEBUG
        // WaveTrend Oscillator [LazyBear] — WT_LB. WT1 crosses WT2 inside the OS/OB zone,
        // with an optional SMA200 trend filter.
        Register(new AlgorithmDefinition()
        {
            Name = "wt.lb",
            Strategy = CryptoSignalStrategy.WaveTrend,
            AnalyzeLongType = typeof(SignalWaveTrendLong),
            AnalyzeShortType = typeof(SignalWaveTrendShort),
        });
#endif

#if DEBUG
        // Stoch + MACD crossover: trend filter (TrendPrimary) + Stoch OS/OB + MACD histogram zero-cross.
        // Source video: https://www.youtube.com/watch?v=vLbLZWi_Ypc
        Register(new AlgorithmDefinition()
        {
            Name = "stoch.macd",
            Strategy = CryptoSignalStrategy.StochMacd,
            AnalyzeLongType = typeof(SignalStochMacdLong),
            AnalyzeShortType = typeof(SignalStochMacdShort),
        });
#endif

#if DEBUG
        // TTM Squeeze (fade): counter-trend reversal after a recent squeeze.
        // Price wicks beyond BB at a Stoch extreme, Stoch crosses back.
        Register(new AlgorithmDefinition()
        {
            Name = "squeeze.fade",
            Strategy = CryptoSignalStrategy.SqueezeFade,
            AnalyzeLongType = typeof(SignalSqueezeFadeLong),
            AnalyzeShortType = typeof(SignalSqueezeFadeShort),
        });

        // TTM Squeeze (breakout): squeeze just released, momentum kicks in via Stoch cross.
        Register(new AlgorithmDefinition()
        {
            Name = "squeeze.brk",
            Strategy = CryptoSignalStrategy.SqueezeBrk,
            AnalyzeLongType = typeof(SignalSqueezeBrkLong),
            AnalyzeShortType = typeof(SignalSqueezeBrkShort),
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

#if DEBUG
        // TrendHtf: trend CONTINUATION entry. HTF bias + ADX regime + established
        // TrendBos direction + pullback pivot + fresh break-of-pivot. Goes WITH the
        // established trend (unlike SignalBosChoch which is a reversal hunter).
        Register(new AlgorithmDefinition()
        {
            Name = "trend.htf",
            Strategy = CryptoSignalStrategy.TrendHtf,
            AnalyzeLongType = typeof(SignalTrendHtfLong),
            AnalyzeShortType = typeof(SignalTrendHtfShort),
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
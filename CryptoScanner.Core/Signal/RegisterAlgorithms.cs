using CryptoScanner.Core.Enums;
#if DEBUG
using CryptoScanner.Core.Signal.Bbma;
#endif
#if DEBUG
using CryptoScanner.Core.Signal.Choch;
#endif
using CryptoScanner.Core.Signal.Dlz;
using CryptoScanner.Core.Signal.Fvg;
using CryptoScanner.Core.Signal.Jump;
using CryptoScanner.Core.Signal.Sbm;
using CryptoScanner.Core.Signal.Smc;
using CryptoScanner.Core.Signal.Stobb;
using CryptoScanner.Core.Signal.Storsi;
using CryptoScanner.Core.Signal.Trend;
// AtrRb, Baba and Bre usings removed — migrated to CryptoScanner.Analyzers.
using CryptoScanner.Core.Signal.Experiment;

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


#if DEBUG
        Register(new AlgorithmDefinition()
        {
            Name = "BbRsiEngulf",
            Strategy = CryptoSignalStrategy.BbRsiEngulfing,
            AnalyzeLongType = typeof(SignalBbRsiEngulfingLong),
            AnalyzeShortType = typeof(SignalBbRsiEngulfingShort),
        });
#endif

#if DEBUG
        Register(new AlgorithmDefinition()
        {
            Name = "IchimokuKumoBreakout",
            Strategy = CryptoSignalStrategy.IchimokuKumoBreakout,
            AnalyzeLongType = typeof(SignalIchimokuKumoBreakoutLong),
            AnalyzeShortType = typeof(SignalIchimokuKumoBreakoutShort),
        });
#endif

        Register(new AlgorithmDefinition()
        {
            Name = "fvg",
            Strategy = CryptoSignalStrategy.FairValueGap,
            AnalyzeLongType = typeof(SignalFairValueGapLong),
            AnalyzeShortType = typeof(SignalFairValueGapShort),
        });

        // SMC supply/demand order block — price returns to a fresh/strong base zone.
        // "smc" fires on a touch into the zone.
        Register(new AlgorithmDefinition()
        {
            Name = "smc",
            Strategy = CryptoSignalStrategy.OrderBlock,
            AnalyzeLongType = typeof(SignalOrderBlockLong),
            AnalyzeShortType = typeof(SignalOrderBlockShort),
        });

        // smc.rejection — entry-grade: fires on the confirmed bounce/rejection off the zone.
        Register(new AlgorithmDefinition()
        {
            Name = "smc.rejection",
            Strategy = CryptoSignalStrategy.OrderBlockRejection,
            AnalyzeLongType = typeof(SignalOrderBlockRejectionLong),
            AnalyzeShortType = typeof(SignalOrderBlockRejectionShort),
        });

        //        // NWE Repaining
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "nwe",
        //            Strategy = CryptoSignalStrategy.Nwe,
        //            AnalyzeLongType = typeof(SignalNwe),
        //            AnalyzeShortType = typeof(SignalNwe),
        //        });

        //#if DEBUG
        //        // NWE not repainting
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "nwe.np",
        //            Strategy = CryptoSignalStrategy.NweNp,
        //            AnalyzeLongType = typeof(SignalNweNp),
        //            AnalyzeShortType = typeof(SignalNweNp),
        //        });
        //#endif

        //#if DEBUG
        //        // NWE × BB crossover: NWE curls through the BB band after extending beyond it
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "nwe.bb",
        //            Strategy = CryptoSignalStrategy.NweBb,
        //            AnalyzeLongType = typeof(SignalNweBbLong),
        //            AnalyzeShortType = typeof(SignalNweBbShort),
        //        });

        //#endif

        // no signals at all
        //#if DEBUG
        //        // BBMA - Oma Ally: price returns to the 510 zone after a CSD crossover
        //        // Confirmations from higher timeframe(s)
        //        Register(new AlgorithmDefinition()
        //        {
        //            Name = "bbma",
        //            Strategy = CryptoSignalStrategy.Bbma,
        //            AnalyzeLongType = null, //typeof(SignalBbmaLong),
        //            AnalyzeShortType = null, //typeof(SignalBbmaShort),
        //        });
        //#endif

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
        // Trend reversal (Dow Theory)
        Register(new AlgorithmDefinition()
        {
            Name = "trend",
            Strategy = CryptoSignalStrategy.Trend,
            AnalyzeLongType = typeof(SignalTrendLong),
            AnalyzeShortType = typeof(SignalTrendShort),
        });
#endif


        // Baba, AtrRb and Bre strategies have been migrated to the Analyzers plugin architecture
        // and are now registered dynamically via PluginManager.

#if DEBUG
        //***************************************************
        // CHoCH — fires on a Change of Character of the ZigZag-derived structure.
        // Primary / Secondary chooses which trend slot is read. The .pullback variants
        // additionally require an opposite zigzag pivot + breakthrough before stepping in.
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "choch.primary",
            Strategy = CryptoSignalStrategy.ChochPrimary,
            AnalyzeLongType = typeof(SignalChochPrimaryLong),
            AnalyzeShortType = typeof(SignalChochPrimaryShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "choch.primary.pullback",
            Strategy = CryptoSignalStrategy.ChochPrimaryPullback,
            AnalyzeLongType = typeof(SignalChochPrimaryPullbackLong),
            AnalyzeShortType = typeof(SignalChochPrimaryPullbackShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "choch.secondary",
            Strategy = CryptoSignalStrategy.ChochSecondary,
            AnalyzeLongType = typeof(SignalChochSecondaryLong),
            AnalyzeShortType = typeof(SignalChochSecondaryShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "choch.secondary.pullback",
            Strategy = CryptoSignalStrategy.ChochSecondaryPullback,
            AnalyzeLongType = typeof(SignalChochSecondaryPullbackLong),
            AnalyzeShortType = typeof(SignalChochSecondaryPullbackShort),
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
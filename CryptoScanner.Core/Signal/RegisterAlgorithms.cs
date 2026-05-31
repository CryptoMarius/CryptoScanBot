using CryptoScanner.Core.Enums;
#if DEBUG
using CryptoScanner.Core.Signal.Bbma;
using CryptoScanner.Core.Signal.WaveTrend;
#endif
using CryptoScanner.Core.Signal.Dlz;
using CryptoScanner.Core.Signal.Fvg;
using CryptoScanner.Core.Signal.Jump;
using CryptoScanner.Core.Signal.Nwe;
using CryptoScanner.Core.Signal.Sbm;
using CryptoScanner.Core.Signal.Smc;
using CryptoScanner.Core.Signal.Stobb;
using CryptoScanner.Core.Signal.StobbDlz;
using CryptoScanner.Core.Signal.StobbFvg;
using CryptoScanner.Core.Signal.StobbSmc;
using CryptoScanner.Core.Signal.StobbZone;
using CryptoScanner.Core.Signal.Storsi;
using CryptoScanner.Core.Signal.StorsiDlz;
using CryptoScanner.Core.Signal.StorsiFvg;
using CryptoScanner.Core.Signal.StorsiSmc;
using CryptoScanner.Core.Signal.StorsiZone;
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

        Register(new AlgorithmDefinition() { 
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
            Name = "stobb.dlz",
            Strategy = CryptoSignalStrategy.StobbDlz,
            AnalyzeLongType = typeof(SignalStobbDlzLong),
            AnalyzeShortType = typeof(SignalStobbDlzShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.fvg",
            Strategy = CryptoSignalStrategy.StobbFvg,
            AnalyzeLongType = typeof(SignalStobbFvgLong),
            AnalyzeShortType = typeof(SignalStobbFvgShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.smc",
            Strategy = CryptoSignalStrategy.StobbSmc,
            AnalyzeLongType = typeof(SignalStobbSmcLong),
            AnalyzeShortType = typeof(SignalStobbSmcShort),
        });


        Register(new AlgorithmDefinition()
        {
            Name = "stobb.multi",
            Strategy = CryptoSignalStrategy.StobbMulti,
            AnalyzeLongType = typeof(SignalStobbMultiLong),
            AnalyzeShortType = typeof(SignalStobbMultiShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.multi.dlz",
            Strategy = CryptoSignalStrategy.StobbMultiDlz,
            AnalyzeLongType = typeof(SignalStobbMultiDlzLong),
            AnalyzeShortType = typeof(SignalStobbMultiDlzShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.multi.fvg",
            Strategy = CryptoSignalStrategy.StobbMultiFvg,
            AnalyzeLongType = typeof(SignalStobbMultiFvgLong),
            AnalyzeShortType = typeof(SignalStobbMultiFvgShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.multi.smc",
            Strategy = CryptoSignalStrategy.StobbMultiSmc,
            AnalyzeLongType = typeof(SignalStobbMultiSmcLong),
            AnalyzeShortType = typeof(SignalStobbMultiSmcShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.zone",
            Strategy = CryptoSignalStrategy.StobbZone,
            AnalyzeLongType = typeof(SignalStobbZoneLong),
            AnalyzeShortType = typeof(SignalStobbZoneShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "stobb.multi.zone",
            Strategy = CryptoSignalStrategy.StobbMultiZone,
            AnalyzeLongType = typeof(SignalStobbMultiZoneLong),
            AnalyzeShortType = typeof(SignalStobbMultiZoneShort),
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

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.dlz",
            Strategy = CryptoSignalStrategy.StoRsiDlz,
            AnalyzeLongType = typeof(SignalStoRsiDlzLong),
            AnalyzeShortType = typeof(SignalStoRsiDlzShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.fvg",
            Strategy = CryptoSignalStrategy.StoRsiFvg,
            AnalyzeLongType = typeof(SignalStoRsiFvgLong),
            AnalyzeShortType = typeof(SignalStoRsiFvgShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.smc",
            Strategy = CryptoSignalStrategy.StoRsiSmc,
            AnalyzeLongType = typeof(SignalStoRsiSmcLong),
            AnalyzeShortType = typeof(SignalStoRsiSmcShort),
        });


        // another combined with a higher timeframe
        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi",
            Strategy = CryptoSignalStrategy.StoRsiMulti,
            AnalyzeLongType = typeof(SignalStoRsiMultiLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi.dlz",
            Strategy = CryptoSignalStrategy.StoRsiMultiDlz,
            AnalyzeLongType = typeof(SignalStoRsiMultiDlzLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiDlzShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi.fvg",
            Strategy = CryptoSignalStrategy.StoRsiMultiFvg,
            AnalyzeLongType = typeof(SignalStoRsiMultiFvgLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiFvgShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi.smc",
            Strategy = CryptoSignalStrategy.StoRsiMultiSmc,
            AnalyzeLongType = typeof(SignalStoRsiMultiSmcLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiSmcShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.zone",
            Strategy = CryptoSignalStrategy.StoRsiZone,
            AnalyzeLongType = typeof(SignalStoRsiZoneLong),
            AnalyzeShortType = typeof(SignalStoRsiZoneShort),
        });

        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi.zone",
            Strategy = CryptoSignalStrategy.StoRsiMultiZone,
            AnalyzeLongType = typeof(SignalStoRsiMultiZoneLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiZoneShort),
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

        // NWE Repaining
        Register(new AlgorithmDefinition()
        {
            Name = "nwe",
            Strategy = CryptoSignalStrategy.Nwe,
            AnalyzeLongType = typeof(SignalNwe),
            AnalyzeShortType = typeof(SignalNwe),
        });

#if DEBUG
        // NWE not repainting
        Register(new AlgorithmDefinition()
        {
            Name = "nwe.np",
            Strategy = CryptoSignalStrategy.NweNp,
            AnalyzeLongType = typeof(SignalNweNp),
            AnalyzeShortType = typeof(SignalNweNp),
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

//#if DEBUG
//        // BBMA - Oma Ally: price returns to the 510 zone after a CSD crossover
//        // Confirmations from higher timeframe(s)
//        Register(new AlgorithmDefinition()
//        {
//            Name = "bbma",
//            Strategy = CryptoSignalStrategy.Bbma,
//            AnalyzeLongType = typeof(SignalBbmaLong),
//            AnalyzeShortType = typeof(SignalBbmaShort),
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
        // Trend reversal (Dow Theory)
        Register(new AlgorithmDefinition()
        {
            Name = "trend",
            Strategy = CryptoSignalStrategy.Trend,
            AnalyzeLongType = typeof(SignalTrendLong),
            AnalyzeShortType = typeof(SignalTrendShort),
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
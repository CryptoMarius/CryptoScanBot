using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Dlz;
using CryptoScanner.Core.Signal.Fvg;
using CryptoScanner.Core.Signal.Smc;

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
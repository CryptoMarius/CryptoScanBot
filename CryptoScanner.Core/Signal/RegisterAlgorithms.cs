using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal;

// Class for registering all algorithms
public class AlgorithmDefinition
{
    public required string Name { get; set; }
    public required Type? AnalyzeLongType { get; set; }
    public required Type? AnalyzeShortType { get; set; }

    /// <summary>Fires on a zone touch (DLZ/FVG/SMC) — see StrategyRegistration.IsZoneStrategy.</summary>
    public bool IsZoneStrategy { get; set; }
}

public static class RegisterAlgorithms
{
    /// <summary>
    /// All available strategies, keyed by name. The name is what the settings, the database
    /// (Signal.Strategy / Position.Strategy) and the plugins use to address a strategy.
    /// Note the iteration order is alphabetical, and that is the order SignalPrepare/SignalExecute
    /// evaluate the strategies in.
    /// </summary>
    public static readonly SortedList<string, AlgorithmDefinition> AlgorithmDefinitionList = [];


    public static void Register(AlgorithmDefinition algorithmDefinition)
    {
        AlgorithmDefinitionList.Add(algorithmDefinition.Name, algorithmDefinition);
    }

    /// <summary>
    /// Return the algorithm definition by name
    /// </summary>
    public static bool GetAlgorithm(string name, out AlgorithmDefinition? definition)
    {
        return AlgorithmDefinitionList.TryGetValue(name, out definition);
    }

    /// <summary>
    /// True when the strategy fires on a zone touch. Unknown strategies count as not-a-zone, which
    /// matches how they are treated everywhere else (the normal indicator path).
    /// </summary>
    public static bool IsZoneStrategy(string? name)
        => name != null && GetAlgorithm(name, out AlgorithmDefinition? definition) && definition!.IsZoneStrategy;


    /// <summary>
    /// Return an instance of the algorithm (long/short)
    /// </summary>
    public static SignalCreateBase? GetAlgorithm(CryptoTradeSide side, string? name)
    {
        if (name != null && GetAlgorithm(name, out AlgorithmDefinition? definition))
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
                x!.SignalStrategy = definition!.Name;
                return x;
            }
        }
        return null;
    }

}
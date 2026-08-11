using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal;

// Class for registering all algorithms
public class AlgorithmDefinition
{
    public required string Name { get; set; }
    public required CryptoSignalStrategy Strategy { get; set; }
    public required Type? AnalyzeLongType { get; set; }
    public required Type? AnalyzeShortType { get; set; }

    /// <summary>Fires on a zone touch (DLZ/FVG/SMC) — see StrategyRegistration.IsZoneStrategy.</summary>
    public bool IsZoneStrategy { get; set; }
}

public static class RegisterAlgorithms
{
    /// <summary>
    /// All available strategies, keyed by name. The name is what the settings, the database
    /// (Signal.Strategy2 / Position.Strategy2) and the plugins already use to address a strategy;
    /// keying on it here takes <see cref="CryptoSignalStrategy"/> off the lookup path.
    /// Note this also fixes the iteration order alphabetically instead of by enum value, which is
    /// the order SignalPrepare/SignalExecute evaluate the strategies in.
    /// </summary>
    public static readonly SortedList<string, AlgorithmDefinition> AlgorithmDefinitionList = [];

    // Legacy index: the enum value a strategy was registered with. Only here to keep the callers
    // that still hold a CryptoSignalStrategy working while that enum is being phased out — it goes
    // away together with the enum.
    private static readonly Dictionary<CryptoSignalStrategy, string> nameByStrategy = [];


    public static void Register(AlgorithmDefinition algorithmDefinition)
    {
        AlgorithmDefinitionList.Add(algorithmDefinition.Name, algorithmDefinition);
        nameByStrategy[algorithmDefinition.Strategy] = algorithmDefinition.Name;
    }

    /// <summary>
    /// Return the algorithm definition by name
    /// </summary>
    public static bool GetAlgorithm(string name, out AlgorithmDefinition? definition)
    {
        return AlgorithmDefinitionList.TryGetValue(name, out definition);
    }

    /// <summary>True when a strategy with this name is registered.</summary>
    public static bool IsRegistered(CryptoSignalStrategy strategy) => nameByStrategy.ContainsKey(strategy);

    /// <summary>
    /// True when the strategy fires on a zone touch. Unknown strategies count as not-a-zone, which
    /// matches how they are treated everywhere else (the normal indicator path).
    /// </summary>
    public static bool IsZoneStrategy(string? name)
        => name != null && GetAlgorithm(name, out AlgorithmDefinition? definition) && definition!.IsZoneStrategy;

    /// <summary>
    /// The enum value a strategy name was registered with. Only needed to fill the numeric
    /// Strategy columns that the database still has next to the Strategy2 name; goes away with
    /// the enum. An unregistered name yields the default, same as reading a 0 from the database.
    /// </summary>
    public static CryptoSignalStrategy StrategyOf(string? name)
        => name != null && GetAlgorithm(name, out AlgorithmDefinition? definition)
            ? definition!.Strategy : default;

    /// <summary>The strategy name for a stored enum value, or the enum's own name when unknown.</summary>
    public static string NameOf(CryptoSignalStrategy strategy)
        => nameByStrategy.TryGetValue(strategy, out string? name) ? name : strategy.ToString();


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
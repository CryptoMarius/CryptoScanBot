using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Experiment;
using CryptoScanBot.Core.Signal.Momentum;
using CryptoScanBot.Core.Signal.Other;

namespace CryptoScanBot.Core.Signal;

public static class SignalHelper
{
    /// <summary>
    /// All available strategies + indexed
    /// </summary>
    static public readonly SortedList<CryptoSignalStrategy, AlgorithmDefinition> AlgorithmDefinitionList = [];


    public static void Register(AlgorithmDefinition algorithmDefinition)
    {
        AlgorithmDefinitionList.Add(algorithmDefinition.Strategy, algorithmDefinition);
    }

    // a class contructor get called later (when something of the class is touched, cannot use it to register something)

    //public static void RegisterLong(CryptoSignalStrategy strategy, string name, Type? analyzeType)
    //{
    //    if (!GetAlgorithm(strategy, out AlgorithmDefinition? definition))
    //        Register(new AlgorithmDefinition() { Name = name, Strategy = strategy, AnalyzeLongType = null, AnalyzeShortType = analyzeType, });

    //    definition!.AnalyzeLongType = analyzeType;
    //}

    //public static void RegisterShort(CryptoSignalStrategy strategy, string name, Type? analyzeType)
    //{
    //    if (!GetAlgorithm(strategy, out AlgorithmDefinition? definition))
    //        Register(new AlgorithmDefinition() { Name = name, Strategy = strategy, AnalyzeLongType = null, AnalyzeShortType = analyzeType, });

    //    definition!.AnalyzeShortType = analyzeType;
    //}


    static SignalHelper()
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


#if EXTRASTRATEGIESPSARRSI
        //***************************************************
        // PSAR + RSI
        // https://blog.elearnmarkets.com/trading-signals-using-parabolic-sar-rsi/
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "psar rsi",
            Strategy = CryptoSignalStrategy.PSarRsi,
            AnalyzeLongType = typeof(SignalPSarRsiLong),
            AnalyzeShortType = typeof(SignalPSarRsiShort),
        });

        //***************************************************
        // Experiment RSI - Stoch.K
        //***************************************************
        Register(new AlgorithmDefinition()
        {
            Name = "rsi/stoch.k",
            Strategy = CryptoSignalStrategy.RsiStochK,
            AnalyzeLongType = typeof(SignalRsiStochKLong),
            AnalyzeShortType = typeof(SignalRsiStochKShort),
        });
#endif


        ////***************************************************
        //// Experiment FLUX
        ////***************************************************
        //Register(new AlgorithmDefinition()
        //{
        //    Name = "ema9.d",
        //    Strategy = CryptoSignalStrategy.Lux,
        //    AnalyzeLongType = typeof(SignalLuxLong),
        //    AnalyzeShortType = typeof(SignalFluxShort),
        //});


        ////***************************************************
        //// Experiment Ross - Ross Cameron
        ////***************************************************
        //Register(new AlgorithmDefinition()
        //{
        //    Name = "vol.5x",
        //    Strategy = CryptoSignalStrategy.Ross,
        //    AnalyzeLongType = typeof(SignalRossLong),
        //    AnalyzeShortType = typeof(SignalRossShort),
        //});

        ////***************************************************
        //// Experiment Ross - Ross Cameron
        ////***************************************************
        //Register(new AlgorithmDefinition()
        //{
        //    Name = "vwap.c",
        //    Strategy = CryptoSignalStrategy.Ross2,
        //    AnalyzeLongType = typeof(SignalRoss2Long),
        //    AnalyzeShortType = typeof(SignalRoss2Short),
        //});


        //// En de lijst eenmalig indexeren
        //AlgorithmDefinitionIndex.Clear();
        //foreach (AlgorithmDefinition algorithmDefinition in AlgorithmDefinitionList)
        //    AlgorithmDefinitionIndex.Add(algorithmDefinition.Strategy, algorithmDefinition);
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
    public static SignalCreateBase? GetAlgorithm(CryptoTradeSide mode, CryptoSignalStrategy strategy, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        if (GetAlgorithm(strategy, out AlgorithmDefinition? definition))
        {
            if (mode == CryptoTradeSide.Long && definition!.AnalyzeLongType != null)
                return definition.InstantiateAnalyzeLong(symbol, interval, candle);
            if (mode == CryptoTradeSide.Short && definition!.AnalyzeShortType != null)
                return definition.InstantiateAnalyzeShort(symbol, interval, candle);
        }
        return null;
    }

}
﻿using CryptoScanBot.Core.Enums;
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

        // anotehr combined with a higher timeframe
        Register(new AlgorithmDefinition()
        {
            Name = "storsi.multi",
            Strategy = CryptoSignalStrategy.StoRsiMulti,
            AnalyzeLongType = typeof(SignalStoRsiMultiLong),
            AnalyzeShortType = typeof(SignalStoRsiMultiShort),
        });


        

        Register(new AlgorithmDefinition()
        {
            Name = "test.ema",
            Strategy = CryptoSignalStrategy.Test1,
            AnalyzeLongType = typeof(SignalTest1Long),
            AnalyzeShortType = null,
        });

        Register(new AlgorithmDefinition()
        {
            Name = "test.tema",
            Strategy = CryptoSignalStrategy.Test2,
            AnalyzeLongType = typeof(SignalTest2Long),
            AnalyzeShortType = null,
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
    public static SignalCreateBase? GetAlgorithm(CryptoTradeSide mode, CryptoSignalStrategy strategy, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        if (GetAlgorithm(strategy, out AlgorithmDefinition? definition))
        {
            if (mode == CryptoTradeSide.Long && definition!.AnalyzeLongType != null)
                return (SignalCreateBase?)Activator.CreateInstance(definition!.AnalyzeLongType, [symbol, interval, candle]);
            if (mode == CryptoTradeSide.Short && definition!.AnalyzeShortType != null)
                return (SignalCreateBase?)Activator.CreateInstance(definition!.AnalyzeShortType, [symbol, interval, candle]);
        }
        return null;
    }

}
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;
using CryptoScanBot.Core.Signal.Experiment;
using CryptoScanBot.Core.Signal.Momentum;
using CryptoScanBot.Core.Signal.Other;

namespace CryptoScanBot.Signal;

public static class SignalHelper
{
    /// Een lijst met alle mogelijke strategieën (en attributen)
    static public readonly List<AlgorithmDefinition> AlgorithmDefinitionList = [];

    // Alle beschikbare strategieën, nu geindexeerd
    static public readonly SortedList<CryptoSignalStrategy, AlgorithmDefinition> AlgorithmDefinitionIndex = [];

    static SignalHelper()
    {
        //***************************************************
        // Jump
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "jump",
            Strategy = CryptoSignalStrategy.Jump,
            AnalyzeLongType = typeof(SignalCandleJumpShort),
            AnalyzeShortType = typeof(SignalCandleJumpLong),
        });

        //***************************************************
        // SBMx (a special kind of STOBB)
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "sbm1",
            Strategy = CryptoSignalStrategy.Sbm1,
            AnalyzeLongType = typeof(SignalSbm1Long),
            AnalyzeShortType = typeof(SignalSbm1Short),
        });

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "sbm2",
            Strategy = CryptoSignalStrategy.Sbm2,
            AnalyzeLongType = typeof(SignalSbm2Long),
            AnalyzeShortType = typeof(SignalSbm2Short),
        });


        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "sbm3",
            Strategy = CryptoSignalStrategy.Sbm3,
            AnalyzeLongType = typeof(SignalSbm3Long),
            AnalyzeShortType = typeof(SignalSbm3Short),
        });



        //***************************************************
        // STOBB
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
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
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
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
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "psar rsi",
            Strategy = CryptoSignalStrategy.PSarRsi,
            AnalyzeLongType = typeof(SignalPSarRsiLong),
            AnalyzeShortType = typeof(SignalPSarRsiShort),
        });
#endif

        //***************************************************
        // Experiment RSI - Stoch.K
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "rsi/stoch.k",
            Strategy = CryptoSignalStrategy.RsiStochK,
            AnalyzeLongType = typeof(SignalRsiStochKLong),
            AnalyzeShortType = typeof(SignalRsiStochKShort),
        });


        //***************************************************
        // Experiment FLUX - JanH
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "lux",
            Strategy = CryptoSignalStrategy.Lux,
            AnalyzeLongType = typeof(SignalLuxLong),
            AnalyzeShortType = typeof(SignalFluxShort),
        });



        // En de lijst eenmalig indexeren
        AlgorithmDefinitionIndex.Clear();
        foreach (AlgorithmDefinition algorithmDefinition in AlgorithmDefinitionList)
            AlgorithmDefinitionIndex.Add(algorithmDefinition.Strategy, algorithmDefinition);
    }



    public static SignalCreateBase? GetSignalAlgorithm(CryptoTradeSide mode, CryptoSignalStrategy strategy, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        if (AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition? definition))
        {
            if (mode == CryptoTradeSide.Long && definition.AnalyzeLongType != null)
                return definition.InstantiateAnalyzeLong(symbol, interval, candle);
            if (mode == CryptoTradeSide.Short && definition.AnalyzeShortType != null)
                return definition.InstantiateAnalyzeShort(symbol, interval, candle);
        }
        return null;
    }

    public static string GetSignalAlgorithmText(CryptoSignalStrategy strategy)
    {
        if (AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition? definition))
            return definition.Name;
        return strategy.ToString();
    }
}
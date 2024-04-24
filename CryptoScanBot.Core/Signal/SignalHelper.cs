using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;
using CryptoScanBot.Core.Signal.Momentum;
using CryptoScanBot.Core.Signal.Other;
using CryptoScanBot.Signal.Other;
using CryptoScanBot.Signal.Slope;

namespace CryptoScanBot.Signal;


// Een oude enumeratie "SignalStrategy" is vervallen en overgenomen door de combinatie
//  van Mode en SignalStrategy (zonder de oversold/overbought toevoegingen)
public class AlgorithmDefinition
{
    public string Name { get; set; }
    public CryptoSignalStrategy Strategy { get; set; }
    public Type AnalyzeLongType { get; set; }
    public Type AnalyzeShortType { get; set; }

    public SignalCreateBase InstantiateAnalyzeLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        return (SignalCreateBase)Activator.CreateInstance(AnalyzeLongType, new object[] { symbol, interval, candle });
    }

    public SignalCreateBase InstantiateAnalyzeShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        return (SignalCreateBase)Activator.CreateInstance(AnalyzeShortType, new object[] { symbol, interval, candle });
    }
}


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

        //AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        //{
        //    Name = "sbm4",
        //    Strategy = CryptoSignalStrategy.Sbm4,
        //    AnalyzeLongType = typeof(SignalSbm4Long),
        //    AnalyzeShortType = typeof(SignalSbm4Short),
        //});

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


#if EXTRASTRATEGIES
        //***************************************************
        // Flux (kind of SBM + flux on 5m)
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "flux",
            Strategy = CryptoSignalStrategy.Flux,
            AnalyzeLongType = typeof(SignalFluxLong),
            AnalyzeShortType = typeof(SignalFluxShort),
        });


        //***************************************************
        // PriceCrossed ..
        //***************************************************

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "close>ema20",
            Strategy = CryptoSignalStrategy.PriceCrossedEma20,
            AnalyzeLongType = typeof(SignalPriceCrossedEma20),
        });

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "close>ema50",
            Strategy = CryptoSignalStrategy.PriceCrossedEma50,
            AnalyzeLongType = typeof(SignalPriceCrossedEma50),
        });

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "close>sma20",
            Strategy = CryptoSignalStrategy.PriceCrossedSma20,
            AnalyzeLongType = typeof(SignalPriceCrossedSma20),
        });

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "close>sma50",
            Strategy = CryptoSignalStrategy.PriceCrossedSma50,
            AnalyzeLongType = typeof(SignalPriceCrossedSma50),
        });


        //***************************************************
        // Engulfing
        //***************************************************

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "Engulfing",
            Strategy = CryptoSignalStrategy.BullishEngulfing,
            AnalyzeLongType = typeof(SignalEngulfingLong),
            AnalyzeShortType = typeof(SignalEngulfingShort),
        });


        //***************************************************
        // Kumo breakout
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "Kumo Breakout",
            Strategy = CryptoSignalStrategy.IchimokuKumoBreakout,
            AnalyzeLongType = typeof(IchimokuKumoBreakoutLong),
            AnalyzeShortType = typeof(IchimokuKumoBreakoutShort),
        });

        //***************************************************
        // EMA cross (9, 26)
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "ema cross",
            Strategy = CryptoSignalStrategy.EmaCross926,
            AnalyzeLongType = typeof(SignalEmaCross926Long),
            AnalyzeShortType = typeof(SignalEmaCross926Short),
        });

#endif

#if EXTRASTRATEGIESSLOPEEMA
        //***************************************************
        // Slope EMA
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "ema 20 slope",
            Strategy = CryptoSignalStrategy.SlopeEma20,
            AnalyzeLongType = typeof(SignalSlopeEma20Long),
            AnalyzeShortType = typeof(SignalSlopeEma20Short),
        });

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "ema 50 slope",
            Strategy = CryptoSignalStrategy.SlopeEma50,
            AnalyzeLongType = typeof(SignalSlopeEma50Long),
            AnalyzeShortType = typeof(SignalSlopeEma50Short),
        });
#endif

#if EXTRASTRATEGIESSLOPESMA
        //***************************************************
        // Slope SMA
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "sma 20 slope",
            Strategy = CryptoSignalStrategy.SlopeSma20,
            AnalyzeLongType = typeof(SignalSlopeSma20Long),
            AnalyzeShortType = typeof(SignalSlopeSma20Short),
        });

        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "sma 50 slope",
            Strategy = CryptoSignalStrategy.SlopeSma50,
            AnalyzeLongType = typeof(SignalSlopeSma50Long),
            AnalyzeShortType = typeof(SignalSlopeSma50Short),
        });

#endif

#if EXTRASTRATEGIESSLOPEKELTNER
        //***************************************************
        // Slope Keltner
        //***************************************************
        AlgorithmDefinitionList.Add(new AlgorithmDefinition()
        {
            Name = "keltner slope",
            Strategy = CryptoSignalStrategy.SlopeKeltner,
            AnalyzeLongType = typeof(SignalSlopeKeltnerLong),
            AnalyzeShortType = typeof(SignalSlopeKeltnerShort),
        });
#endif

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

        // En de lijst eenmalig indexeren
        AlgorithmDefinitionIndex.Clear();
        foreach (AlgorithmDefinition algorithmDefinition in AlgorithmDefinitionList)
            AlgorithmDefinitionIndex.Add(algorithmDefinition.Strategy, algorithmDefinition);
    }

    public static SignalCreateBase GetSignalAlgorithm(CryptoTradeSide mode, CryptoSignalStrategy strategy, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        if (AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition definition))
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
        if (AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition definition))
            return definition.Name;
        return strategy.ToString();
    }
}
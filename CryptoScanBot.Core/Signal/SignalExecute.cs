using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

namespace CryptoScanBot.Core.Signal;

public class SignalExecute
{
    // Tellertje die getoond wordt in applicatie (indicatie van aantal meldingen)
    private static int analyseCount;
    public static int AnalyseCount { get { return analyseCount; } }
    public static void ResetAnalyseCount() => analyseCount = 0;
    
    private static Dictionary<(CryptoSignalStrategy strategy, CryptoTradeSide side, bool check), 
        SortedList<string, CryptoInterval>> Executing { get; set; } = [];


    private static void Add(AlgorithmDefinition strategyDef, CryptoTradeSide side, bool checkStuff, string intervalName)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriodName[intervalName];
        var key = (strategyDef.Strategy, side, checkStuff);
        Executing.TryAdd(key, []);
        Executing[key].TryAdd(intervalName, interval);
    }


    public static void Prepare()
    {
        // New setup
        Executing.Clear();

        foreach (AlgorithmDefinition strategyDef in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            if (strategyDef.Strategy < CryptoSignalStrategy.DominantLevel)
            {
                if (GlobalData.Settings.Signal.Long.Strategy.Contains(strategyDef.Name))
                {
                    foreach (string intervalName in GlobalData.Settings.Signal.Long.Interval)
                    {
                        Add(strategyDef, CryptoTradeSide.Long, true, intervalName);
                    }
                }
                if (GlobalData.Settings.Signal.Short.Strategy.Contains(strategyDef.Name))
                {
                    foreach (string intervalName in GlobalData.Settings.Signal.Short.Interval)
                    {
                        Add(strategyDef, CryptoTradeSide.Short, true, intervalName);
                    }
                }
            }
            else if (strategyDef.Strategy == CryptoSignalStrategy.FairValueGap)
            {
                // Detect the zone touches on the 1m
                if (GlobalData.Settings.Signal.Long.Strategy.Contains(strategyDef.Name))
                {
                    Add(strategyDef, CryptoTradeSide.Long, false, "1m");
                }
                if (GlobalData.Settings.Signal.Short.Strategy.Contains(strategyDef.Name))
                {
                    Add(strategyDef, CryptoTradeSide.Short, false, "1m");
                }
            }
            else if (strategyDef.Strategy == CryptoSignalStrategy.DominantLevel || strategyDef.Strategy == CryptoSignalStrategy.DominantLevelNear)
            {
                // Detect the zone touches on the 1m
                if (GlobalData.Settings.Signal.Long.Strategy.Contains(strategyDef.Name))
                {
                    Add(strategyDef, CryptoTradeSide.Long, false, "1m");
                }
                if (GlobalData.Settings.Signal.Short.Strategy.Contains(strategyDef.Name))
                {
                    Add(strategyDef, CryptoTradeSide.Short, false, "1m");
                }
            }
        }

        // Remove the unused items
        foreach (var key in Executing.ToList())
        {
            if (key.Value.Count == 0)
                Executing.Remove(key.Key);
        }
    }


    public static async Task<List<CryptoSignal>> ExecuteAsync(CryptoSymbol symbol, 
        Dictionary<CryptoIntervalPeriod, 
        List<CryptoCandle>> preparedHistoryCandles, 
        long lastCandle1mCloseTime)
    {
        List<CryptoSignal> signalList = [];
        //GlobalData.Logger.Info($"CreateSignals(start):" + LastCandle1m.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));
        //if (GlobalData.Settings.Signal.Active && symbol.QuoteData!.FetchCandles && symbol.Status == 1)
        //{

            // TODO: CHECK: This is something different than before (parameter false/true combined with zones!)
            // I'm not sure if this is really such a bad thing, you do not want to trade an inconsistent volume
            // But lets keep the parameter for now..

            // Is the volume valid within a certain minimal limit
            if (!symbol.CheckValidMinimalVolume(false, lastCandle1mCloseTime, 60, out string response))
            {
                if (GlobalData.Settings.Signal.LogMinimalVolume)
                    GlobalData.AddTextToLogTab("Analyse " + response);
                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    GlobalData.AddTextToLogTab("Analyse " + response);
                return [];
            }

            // Is the price valid within a certain minimal limit
            if (!symbol.CheckValidMinimalPrice(out response))
            {
                if (GlobalData.Settings.Signal.LogMinimalPrice)
                    GlobalData.AddTextToLogTab("Analyse " + response);
                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    GlobalData.AddTextToLogTab("Analyse " + response);
                return [];
            }

        foreach (var entry in Executing)
        {
            foreach (var interval in entry.Value.Values)
            {
                if (lastCandle1mCloseTime % interval.Duration == 0)
                {
                    var side = entry.Key.side;
                    if (entry.Key.check) // true for the standard strategies bug false for dlz and fvg zones
                    {
                        // Barometer check
                        if (!BarometerHelper.ValidBarometerConditions(GlobalData.ActiveExchange!, symbol.Quote, TradingConfig.Signals[side].Barometer, out string reaction))
                        {
                            if (TradingConfig.Signals[side].BarometerLog)
                                GlobalData.AddTextToLogTab($"{symbol.Name} {side} {reaction}");
                            continue;
                        }
                    }
                    //GlobalData.Logger.Info($"analyze({interval.Name}):" + LastCandle1m.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true));

                    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                        GlobalData.AddTextToLogTab($"Debug Signal create {symbol.Name} {interval.Name} {side}");
                    //ScannerLog.Logger.Trace($"SignalCreate.Start {symbol.Name} {Interval.Name}");
                    //GlobalData.AddTextToLogTab($"SignalCreate.Start {symbol.Name} {Interval.Name} {Side}");


                    if (RegisterAlgorithms.GetAlgorithm(entry.Key.strategy, out AlgorithmDefinition? strategyDefinition))
                    {
                        SignalCreate createSignal = new(symbol, interval, side, lastCandle1mCloseTime);

                        // The candle list can be missing in action, too little candles for example
                        if (preparedHistoryCandles.TryGetValue(interval.IntervalPeriod, out var history))
                        {
                            createSignal.History = history;
                            createSignal.Candle = history[^1];


                            // TODO: Set the right Candle!!!!!
                            if (await createSignal.ExecuteAlgorithmAsync(strategyDefinition!))
                                signalList.AddRange(createSignal.SignalList);


                            // Counter for mainscreen so you can see symbols analyzing etc..
                            Interlocked.Increment(ref analyseCount);
                        }
                    }
                }
            }
        }
 
        return signalList;
    }

}

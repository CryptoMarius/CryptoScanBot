using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Core.Signal;

public class SignalExecute
{
    // Tellertje die getoond wordt in applicatie (indicatie van aantal meldingen)
    private static int analyseCount;
    public static int AnalyseCount { get { return analyseCount; } }
    public static void ResetAnalyseCount() => analyseCount = 0;


    // Quick index on what to execute (with some specials for detecting fvg and dlz on the 1m)
    // (the zones for the fvg and dlz are prepared via other code and are stored in the database)
    private static Dictionary<(CryptoSignalStrategy strategy, CryptoTradeSide side, bool checkBarometer),
        SortedList<string, CryptoInterval>> Executing
    { get; set; } = [];


    private static void Add(AlgorithmDefinition strategyDef, CryptoTradeSide side, bool checkBarometer, string intervalName)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriodName[intervalName];
        var key = (strategyDef.Strategy, side, checkBarometer);
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


    public static async Task ExecuteAsync(CryptoSymbol symbol,
        CryptoIndicatorDataList preparedIndicatorDataList,
        CandleTime lastCandle1mCloseTime)
    {
        //GlobalData.Logger.Info($"CreateSignals(start):" + LastCandle1m.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));

        //List<CryptoSignal> signalList = [];
        foreach (var entry in Executing.ToList())
        {
            foreach (var interval in entry.Value.Values)
            {
                if (lastCandle1mCloseTime % interval.Duration == 0)
                {
                    try
                    {
                        var side = entry.Key.side;
                        if (entry.Key.checkBarometer) // Skip for the dlz and fvg zones
                        {
                            // Barometer check
                            if (!BarometerHelper.ValidBarometerConditions(GlobalData.ActiveExchange!, symbol.Quote, TradingConfig.Signals[side].Barometer, out string reaction))
                            {
                                if (TradingConfig.Signals[side].BarometerLog)
                                    GlobalData.AddTextToLogTab($"{symbol.Name} {side} {reaction}");
                                continue;
                            }

                            // Barometer consensus check (only higher-timeframe barometers)
                            if (!BarometerHelper.CheckConsensusBarometer(GlobalData.ActiveExchange!, symbol.Quote,
                                interval.IntervalPeriod, TradingConfig.Signals[side].BarometerMinConsensus, side, out reaction))
                            {
                                if (TradingConfig.Signals[side].BarometerLog)
                                    GlobalData.AddTextToLogTab($"{symbol.Name} {side} {reaction}");
                                continue;
                            }
                        }
                        //GlobalData.Logger.Info($"analyze({interval.Name}):" + LastCandle1m.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true));


                        if (RegisterAlgorithms.GetAlgorithm(entry.Key.strategy, out AlgorithmDefinition? strategyDefinition))
                        {
                            // Quality filters (volume + feedback) do not apply to informational strategies
                            bool isInformational = strategyDefinition!.BypassFilters;

                            // Performance feedback: skip underperforming strategies
                            if (!isInformational && StrategyPerformanceMonitor.IsBlocked(entry.Key.strategy, side))
                                continue;

                            if (preparedIndicatorDataList.TryGetValue(interval.IntervalPeriod, out var indicatorData) && indicatorData != null)
                            {
                                // Relative volume check: skip for informational strategies
                                if (!isInformational)
                                {
                                    var volume = TradingConfig.Signals[side].Volume;
                                    if (!VolumeHelper.CheckRelativeVolume(indicatorData, volume, out string volReaction))
                                    {
                                        if (volume.Log)
                                            GlobalData.AddTextToLogTab($"{symbol.Name} {side} {volReaction}");
                                        continue;
                                    }
                                }

                                SignalCreate createSignal = new()
                                {
                                    Symbol = symbol,
                                    Interval = interval,
                                    Side = side,
                                    Candle = indicatorData.LastCandle,
                                    CandleData = indicatorData.LastCandleData,
                                    IndicatorData = indicatorData,
                                    IndicatorDataList = preparedIndicatorDataList,
                                };

                                string text = "";
                                if (await createSignal.ExecuteAlgorithmAsync(strategyDefinition!))
                                {
                                    text = "*";
                                    //signalList.AddRange(createSignal.SignalList);
                                }

                                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                                    ScannerLog.Logger.Info($"Debug Signal create {symbol.Name} {interval.Name} {side} {text}");
                                //ScannerLog.Logger.Trace($"SignalCreate.Start {symbol.Name} {Interval.Name}");
                                //GlobalData.AddTextToLogTab($"SignalCreate.Start {symbol.Name} {Interval.Name} {Side}");

                                // Counter for mainscreen so you can see symbols analyzing etc..
                                Interlocked.Increment(ref analyseCount);
                            }
                            else GlobalData.AddTextToLogTab($"Debug Signal create {symbol.Name} {interval.Name} {side} Error collecting history");
                        }
                        else GlobalData.AddTextToLogTab($"Debug Signal create {symbol.Name} {interval.Name} {side} Error collecting algorithm {entry.Key.strategy}");
                    }
                    catch (Exception error)
                    {
                        // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {entry.Key.strategy} error Monitor {error.Message}");
                    }
                }
            }
        }

        //return signalList;
    }

}

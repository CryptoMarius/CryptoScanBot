using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using System.Diagnostics;

namespace CryptoScanner.Core.Signal;

public class SignalExecute
{
    // Tellertje die getoond wordt in applicatie (indicatie van aantal meldingen)
    private static int analyseCount;
    public static int AnalyseCount { get { return analyseCount; } }
    public static void ResetAnalyseCount() => analyseCount = 0;


    // Quick index on what to execute (with some specials for detecting fvg and dlz on the 1m)
    // (the zones for the fvg and dlz are prepared via other code and are stored in the database)
    private static Dictionary<(string strategy, CryptoTradeSide side, bool checkBarometer),
        SortedList<string, CryptoInterval>> Executing
    { get; set; } = [];


    private static void Add(AlgorithmDefinition strategyDef, CryptoTradeSide side, bool checkBarometer, string intervalName)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriodName[intervalName];
        var key = (strategyDef.Name, side, checkBarometer);
        Executing.TryAdd(key, []);
        Executing[key].TryAdd(intervalName, interval);
    }


    public static void Prepare()
    {
        // New setup
        Executing.Clear();

        foreach (AlgorithmDefinition strategyDef in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            if (!strategyDef.IsZoneStrategy)
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
            else
            {
                // Detect the zone touches on the 1m. Used to be three separate branches naming FVG,
                // DLZ and SMC by enum value, all three doing exactly this.
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


    public static async Task ExecuteAsync(CryptoSymbol symbol, CandleTime lastCandle1mCloseTime)
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

                            // Barometer consensus check — only runs when explicitly enabled (same as Volume.Active)
                            if (TradingConfig.Signals[side].BarometerConsensusActive &&
                                !BarometerHelper.CheckConsensusBarometer(GlobalData.ActiveExchange!, symbol.Quote,
                                    interval.IntervalPeriod, TradingConfig.Signals[side].Barometer, TradingConfig.Signals[side].BarometerMinConsensus, side, out reaction))
                            {
                                if (TradingConfig.Signals[side].BarometerLog)
                                    GlobalData.AddTextToLogTab($"{symbol.Name} {side} {reaction}");
                                continue;
                            }
                        }
                        //GlobalData.Logger.Info($"analyze({interval.Name}):" + LastCandle1m.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true));


                        if (RegisterAlgorithms.GetAlgorithm(entry.Key.strategy, out AlgorithmDefinition? strategyDefinition))
                        {
                            // Indicator data now lives on the symbol's CryptoSymbolInterval.Data (filled by
                            // IndicatorEngine.PrepareIndicators in SignalPrepare). The interval candle that
                            // just closed has open time = close - duration.
                            CandleTime candleOpenTime = lastCandle1mCloseTime - interval.Duration;
                            if (symbol.GetSymbolInterval(interval.IntervalPeriod).TryGetCandle(candleOpenTime, out MyData? indicatorData) && indicatorData != null)
                            {
                                //// Relative volume check: skip for informational strategies
                                //if (entry.Key.checkBarometer)
                                //{
                                //    var volume = TradingConfig.Signals[side].Volume;
                                //    if (!VolumeHelper.CheckRelativeVolume(indicatorData, volume, out string volReaction))
                                //    {
                                //        if (volume.Log)
                                //            GlobalData.AddTextToLogTab($"{symbol.Name} {side} {volReaction}");
                                //        continue;
                                //    }
                                //}

                                SignalCreate createSignal = new()
                                {
                                    Symbol = symbol,
                                    Interval = interval,
                                    Side = side,
                                    Candle = indicatorData.Candle,
                                    CandleData = indicatorData.CandleData,
                                };

                                string text = "";
                                long profAlgoStart = Stopwatch.GetTimestamp();
                                bool signalCreated = await createSignal.ExecuteAlgorithmAsync(strategyDefinition!);
                                // checkBarometer==true → normal strategy; false → FVG/DLZ/SMC zone-touch.
                                PipelineProfiler.RecordSignalExecuteCall(
                                    Stopwatch.GetTimestamp() - profAlgoStart, entry.Key.checkBarometer, signalCreated);
                                if (signalCreated)
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
                        GlobalData.AddErrorToLogTab($"{symbol.Name} {interval.Name} {entry.Key.strategy} error Monitor {error.Message}");
                    }
                }
            }
        }

        //return signalList;
    }

}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;

using System.Text;

namespace CryptoScanner.Commands;

public class CommandShowMemoryObjects : CommandBase
{
    public override async void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Show memory");

        StringBuilder log = new();

        foreach (var exchange in GlobalData.ExchangeListName.Values)
        {
            log.AppendLine("");
            log.AppendLine($"---------------------------------------------------------------------------------------------------");
            log.AppendLine($"Exchange {exchange.Name}");
            foreach (var symbol in exchange.SymbolListName.Values)
            {
                log.AppendLine("");
                log.AppendLine($"---------------------------------------");
                log.AppendLine($"  Symbol: {symbol.Name} IsActive: {symbol.Status} PriceDisplayFormat: {symbol.PriceDisplayFormat} QuantityDisplayFormat: {symbol.QuantityDisplayFormat} LastPrice: {symbol.LastPrice} Volume: {symbol.Volume}");
                //if (symbol.LastPrice != null)
                //    log.AppendLine($"    LastPrice: {symbol.LastPrice}");
                //else
                //    log.AppendLine($"    LastPrice: null");

                foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
                {
                    log.AppendLine($"      Interval: {symbolInterval.Interval.Name} Candle synchronized: {symbolInterval.LastCandleSynchronized} Candles: {symbolInterval.CandleList.Count} LastCandle: {symbolInterval.LastCandle?.DateLocal}");
                    //if (symbolInterval.LastCandleSynchronized != null)
                    //    log.AppendLine($"      Candle synchronized: {symbolInterval.LastCandleSynchronized}");
                    //else
                    //    log.AppendLine($"      Candle synchronized: null");
                    //log.AppendLine($"      Candles: {symbolInterval.CandleList.Count}");
                    //if (symbolInterval.LastCandle != null)
                    //    log.AppendLine($"      LastCandle: {symbolInterval.LastCandle.DateLocal}");
                    //else
                    //    log.AppendLine($"      LastCandle: null");

                    if (symbolInterval.SignalList.Count > 0)
                        log.AppendLine($"      Signal count: {symbolInterval.SignalList.Count}");

                    if (symbolInterval.DlzZones.LongOpen.Count > 0)
                        log.AppendLine($"      DlzZones.LongOpen: {symbolInterval.DlzZones.LongOpen.Count}");
                    if (symbolInterval.DlzZones.ShortOpen.Count > 0)
                        log.AppendLine($"      DlzZones.ShortOpen: {symbolInterval.DlzZones.ShortOpen.Count}");
                    if (symbolInterval.DlzZones.LongClosed.Count > 0)
                        log.AppendLine($"      DlzZones.LongClosed: {symbolInterval.DlzZones.LongClosed.Count}");
                    if (symbolInterval.DlzZones.ShortClosed.Count > 0)
                        log.AppendLine($"      DlzZones.ShortClosed: {symbolInterval.DlzZones.ShortClosed.Count}");

                    if (symbolInterval.FvgZones.LongOpen.Count > 0)
                        log.AppendLine($"      FvgZones.LongOpen: {symbolInterval.FvgZones.LongOpen.Count}");
                    if (symbolInterval.FvgZones.ShortOpen.Count > 0)
                        log.AppendLine($"      FvgZones.ShortOpen: {symbolInterval.FvgZones.ShortOpen.Count}");
                    if (symbolInterval.FvgZones.LongClosed.Count > 0)
                        log.AppendLine($"      FvgZones.LongClosed: {symbolInterval.FvgZones.LongClosed.Count}");
                    if (symbolInterval.FvgZones.ShortClosed.Count > 0)
                        log.AppendLine($"      FvgZones.ShortClosed: {symbolInterval.FvgZones.ShortClosed.Count}");

                    if (symbolInterval.TrendPrimary.Trend != CryptoTrendIndicator.Unknown)
                        log.AppendLine($"      TrendPrimary: {symbolInterval.TrendPrimary.Trend}");
                    if (symbolInterval.TrendSecondary.Trend != CryptoTrendIndicator.Unknown)
                        log.AppendLine($"      TrendSecondary: {symbolInterval.TrendSecondary.Trend}");
                    log.AppendLine($"");
                }
            }
        }

        log.AppendLine($"");
        log.AppendLine($"");

        log.AppendLine($"Global data:");
        log.AppendLine($"ExternalUrls: {GlobalData.ExternalUrls.Count}");

        log.AppendLine($"IntervalList: {GlobalData.IntervalList.Count}");
        log.AppendLine($"IntervalListId: {GlobalData.IntervalListId.Count}");
        log.AppendLine($"IntervalListPeriodName: {GlobalData.IntervalListPeriodName.Count}");
        log.AppendLine($"IntervalListPeriod: {GlobalData.IntervalListPeriod.Count}");

        log.AppendLine($"ExchangeListId: {GlobalData.ExchangeListId.Count}");
        log.AppendLine($"ExchangeListName: {GlobalData.ExchangeListName.Count}");

        log.AppendLine($"SignalQueue: {GlobalData.SignalQueue.Count}");
        log.AppendLine($"LiveDataQueue: {GlobalData.LiveDataQueue.Count}");
        log.AppendLine($"LiveDataQueueAdded: {GlobalData.LiveDataQueueAdded.Count}");

        log.AppendLine($"StrategiesSettings: {GlobalData.StrategiesSettings.Count}");

        log.AppendLine($"GC.GetTotalMemory: {GC.GetTotalMemory(true)}");

        var app = Application.Current;
        if (app?.Styles == null) return;

        foreach (var style in app.Styles)
        {
            if (style is IResourceDictionary rd)
            {
                foreach (var key in rd.Keys)
                {
                    if (rd.TryGetValue(key, out var val) && val is IBrush)
                    {
                        log.AppendLine($"Resource key={key} type={val.GetType().Name}");
                    }
                }

                // ThemeDictionaries (Light/Dark) if present
                if (rd.ThemeDictionaries != null)
                {
                    foreach (var kv in rd.ThemeDictionaries)
                    {
                        log.AppendLine($"Theme variant={kv.Key}");
                        if (kv.Value is IResourceDictionary trd)
                        {
                            foreach (var k in trd.Keys)
                            {
                                if (trd.TryGetValue(k, out var v) && v is IBrush)
                                    log.AppendLine($"  {k} => {v.GetType().Name}");
                            }
                        }
                    }
                }
            }
        }




        // debug
        string filename = Path.Combine(GlobalData.AppDataFolder, "Memory information.txt");
        File.WriteAllText(filename, log.ToString());

        System.Diagnostics.Debug.WriteLine($"Saved 'Memory information.txt'");


        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
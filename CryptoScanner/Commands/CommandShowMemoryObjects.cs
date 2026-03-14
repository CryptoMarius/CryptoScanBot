using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using Microsoft.Diagnostics.Runtime;

using System.Diagnostics;
using System.Text;

namespace CryptoScanner.Commands;

public class CommandShowMemoryObjects : CommandBase
{
    public override async void Execute(object? parameter)
    {
        _ = Task.Run(() => { DumpSomething(); });
    }

    public static void DumpSomething()
    {
        System.Diagnostics.Debug.WriteLine($"Show memory");
        DateTime startTime = DateTime.UtcNow;
        string folder = Path.Combine(GlobalData.AppDataFolder, "$debug", $"Memory Dump", $"{startTime:yyyy-MM-dd HHmmss}");
        Directory.CreateDirectory(folder);

        int dataCount = 0;
        int candleCount = 0;
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
                    log.AppendLine($"      Interval: {symbolInterval.Interval.Name} Candle synchronized: {symbolInterval.LastCandleSynchronized?.ToDateTime()} Candles: {symbolInterval.CandleList.Count} LastCandle: {symbolInterval.LastCandle.Date}");
                    candleCount += symbolInterval.CandleList.Count;

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
        log.AppendLine($"Total candles: {candleCount}");
        log.AppendLine($"Total candles with data: {dataCount}");

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

        //----------------------------------------------------------------------------------------------------------
        string filename = Path.Combine(folder, "Memory information1.txt");
        File.WriteAllText(filename, log.ToString());
        log.Clear();

        //// does not work..
        //var app = Application.Current;
        //if (app?.Styles == null)
        //    return;

        //foreach (var style in app.Styles)
        //{
        //    if (style is IResourceDictionary rd) //IResourceProvider
        //    {
        //        foreach (var key in rd.Keys)
        //        {
        //            if (rd.TryGetValue(key, out var val))
        //            {
        //                if (val is IBrush)
        //                    log.AppendLine($"Resource key={key} type={val.GetType().Name}");
        //                //else if (val is IColor)
        //                //    log.AppendLine($"Resource key={key} type={val.GetType().Name}");
        //            }
        //        }

        //        // ThemeDictionaries (Light/Dark) if present
        //        if (rd.ThemeDictionaries != null)
        //        {
        //            foreach (var kv in rd.ThemeDictionaries)
        //            {
        //                log.AppendLine($"Theme variant={kv.Key}");
        //                if (kv.Value is IResourceDictionary trd)
        //                {
        //                    foreach (var k in trd.Keys)
        //                    {
        //                        if (trd.TryGetValue(k, out var v) && v is IBrush)
        //                            log.AppendLine($"  {k} => {v.GetType().Name}");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //log.AppendLine($"");
        //log.AppendLine($"");



        //----------------------------------------------------------------------------------------------------------
        int pid = Process.GetCurrentProcess().Id;
        using var dataTarget = DataTarget.AttachToProcess(pid, suspend: false);
        using var runtime = dataTarget.ClrVersions[0].CreateRuntime();

        var stats = new Dictionary<string, (int Count, long TotalSize)>();

        // Loop door alle objecten in de heap
        foreach (var obj in runtime.Heap.EnumerateObjects())
        {
            var type = obj.Type;
            if (type == null) continue;

            string typeName = type.Name ?? "Unknown";
            long size = (long)obj.Size;

            if (stats.ContainsKey(typeName))
            {
                var current = stats[typeName];
                stats[typeName] = (current.Count + 1, current.TotalSize + size);
            }
            else
            {
                stats[typeName] = (1, size);
            }
        }

        // Schrijf naar bestand
        log.AppendLine($"Memory Dump - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        //log.AppendLine($"Total Heap Size: {runtime.Heap.TotalHeapSize:N0} bytes");
        log.AppendLine();
        log.AppendLine("Type Name | Count | Avg Size | Total Size (bytes)");
        log.AppendLine(new string('-', 100));

        foreach (var item in stats.OrderByDescending(x => x.Value.TotalSize).ThenBy(x => x.Key))
        {
            long avgSize = item.Value.Count > 0 ? item.Value.TotalSize / item.Value.Count : 0;
            log.AppendLine($"{item.Key} | {item.Value.Count:N0} | {avgSize:N0} | {item.Value.TotalSize:N0}");
        }

        log.AppendLine();
        log.AppendLine($"Total unique types: {stats.Count}");
        log.AppendLine($"Total objects: {stats.Sum(x => x.Value.Count):N0}");
        log.AppendLine($"");
        log.AppendLine($"");


        //----------------------------------------------------------------------------------------------------------
        // Force GC zodat we alleen levende objecten krijgen
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long totalMemory = GC.GetTotalMemory(false);
        GCMemoryInfo memInfo = GC.GetGCMemoryInfo();

        log.AppendLine($"Memory Dump - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine($"Total Memory: {totalMemory:N0} bytes ({totalMemory / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"Heap Size: {memInfo.HeapSizeBytes:N0} bytes ({memInfo.HeapSizeBytes / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"Memory Load: {memInfo.MemoryLoadBytes:N0} bytes ({memInfo.MemoryLoadBytes / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"Total Available Memory: {memInfo.TotalAvailableMemoryBytes:N0} bytes ({memInfo.TotalAvailableMemoryBytes / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"High Memory Load Threshold: {memInfo.HighMemoryLoadThresholdBytes:N0} bytes ({memInfo.HighMemoryLoadThresholdBytes / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"Fragmented Bytes: {memInfo.FragmentedBytes:N0} bytes ({memInfo.FragmentedBytes / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine();

        log.AppendLine("GC Collections:");
        log.AppendLine($"  Generation 0: {GC.CollectionCount(0):N0}");
        log.AppendLine($"  Generation 1: {GC.CollectionCount(1):N0}");
        log.AppendLine($"  Generation 2: {GC.CollectionCount(2):N0}");
        log.AppendLine();

        // Generation info (alleen beschikbaar in .NET 5+)
        log.AppendLine("Generation Details:");
        if (memInfo.GenerationInfo.Length > 0)
        {
            for (int i = 0; i < memInfo.GenerationInfo.Length; i++)
            {
                var genInfo = memInfo.GenerationInfo[i];
                log.AppendLine($"  Generation {i}:");
                log.AppendLine($"    Size Before: {genInfo.SizeBeforeBytes:N0} bytes ({genInfo.SizeBeforeBytes / 1024.0 / 1024.0:F2} MB)");
                log.AppendLine($"    Size After: {genInfo.SizeAfterBytes:N0} bytes ({genInfo.SizeAfterBytes / 1024.0 / 1024.0:F2} MB)");
                log.AppendLine($"    Fragmentation Before: {genInfo.FragmentationBeforeBytes:N0} bytes");
                log.AppendLine($"    Fragmentation After: {genInfo.FragmentationAfterBytes:N0} bytes");
            }
        }
        else
        {
            log.AppendLine("  (Generation details not available)");
        }

        log.AppendLine();
        log.AppendLine("Process Info:");
        using var process = Process.GetCurrentProcess();
        log.AppendLine($"  Working Set: {process.WorkingSet64:N0} bytes ({process.WorkingSet64 / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"  Private Memory: {process.PrivateMemorySize64:N0} bytes ({process.PrivateMemorySize64 / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"  Virtual Memory: {process.VirtualMemorySize64:N0} bytes ({process.VirtualMemorySize64 / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"  Paged Memory: {process.PagedMemorySize64:N0} bytes ({process.PagedMemorySize64 / 1024.0 / 1024.0:F2} MB)");
        log.AppendLine($"");
        log.AppendLine($"");



        //----------------------------------------------------------------------------------------------------------
        filename = Path.Combine(folder, "Memory information2.txt");
        File.WriteAllText(filename, log.ToString());


        filename = Path.Combine(folder, "Memory DumpLargeObjects.txt");
        DumpLargeObjects(filename);

        //----------------------------------------------------------------------------------------------------------
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }



    public static void DumpLargeObjects(string outputPath)
    {
        int pid = Environment.ProcessId;

        using var dataTarget = DataTarget.AttachToProcess(pid, suspend: false);
        using var runtime = dataTarget.ClrVersions[0].CreateRuntime();

        var writer = new StreamWriter(outputPath);
        writer.WriteLine($"Large Object Analysis - {DateTime.Now}");
        writer.WriteLine();

        // Find objects larger than 85KB (Large Object Heap threshold)
        var largeObjects = new Dictionary<string, List<ulong>>();

        foreach (var obj in runtime.Heap.EnumerateObjects())
        {
            if (obj.Size > 85000) // 85KB = LOH threshold
            {
                string typeName = obj.Type?.Name ?? "Unknown";

                if (!largeObjects.ContainsKey(typeName))
                    largeObjects[typeName] = new List<ulong>();

                largeObjects[typeName].Add(obj.Size);
            }
        }

        writer.WriteLine("=== LARGE OBJECTS (>85KB, on Large Object Heap) ===");
        foreach (var kvp in largeObjects.OrderByDescending(x => x.Value.Sum(s => (long)s)))
        {
            long totalSize = kvp.Value.Sum(s => (long)s);
            writer.WriteLine($"{kvp.Key}");
            writer.WriteLine($"  Count: {kvp.Value.Count}");
            writer.WriteLine($"  Total: {totalSize:N0} bytes ({totalSize / 1024.0 / 1024.0:F2} MB)");
            // FIX: Cast to long for Average
            writer.WriteLine($"  Average: {kvp.Value.Select(x => (long)x).Average():N0} bytes");
            writer.WriteLine($"  Max: {kvp.Value.Max():N0} bytes");
            writer.WriteLine();
        }

        // Find those WeakHashList dictionaries specifically
        writer.WriteLine("=== AVALONIA WEAK EVENT DICTIONARIES ===");
        foreach (var obj in runtime.Heap.EnumerateObjects())
        {
            string typeName = obj.Type?.Name ?? "";

            if (typeName.Contains("WeakHashList") && typeName.Contains("Dictionary"))
            {
                writer.WriteLine($"Found: {typeName}");
                writer.WriteLine($"  Address: 0x{obj.Address:X}");
                writer.WriteLine($"  Size: {obj.Size:N0} bytes");

                // Try to find segment/generation
                var seg = runtime.Heap.GetSegmentByAddress(obj.Address);
                if (seg != null)
                {
                    int gen = (int)seg.GetGeneration(obj.Address);
                    writer.WriteLine($"  Generation: {gen}");
                }
                writer.WriteLine();
            }
        }

        writer.Close();
    }
}

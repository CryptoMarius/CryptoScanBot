using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using Microsoft.Diagnostics.Runtime;

using System.Diagnostics;
using System.Text;

namespace CryptoScanner.Core.Diagnostics;

/// <summary>
/// Writes a set of memory diagnostic files to "$debug\Memory Dump\{timestamp}" in the data folder.
/// Host independent: the Avalonia CommandShowMemoryObjects and the Photino "Dump memory info" menu
/// both call <see cref="Execute"/>, so both produce exactly the same files.
///
/// Files:
///   Memory information1.txt      - application state (exchanges, symbols, intervals, global lists)
///   Memory information2.txt      - managed/native summary, heap type statistics, GC and process info
///   Memory DumpLargeObjects.txt  - objects on the Large Object Heap (&gt; 85 KB)
/// </summary>
public static class MemoryDump
{
    /// <summary>
    /// Write all diagnostic files and return the folder they were written to. Safe to call from a
    /// background thread; enumerating the heap can take a minute on a large process.
    /// </summary>
    public static string Execute()
    {
        DateTime startTime = DateTime.UtcNow;
        string folder = Path.Combine(GlobalData.AppDataFolder, "$debug", "Memory Dump", $"{startTime:yyyy-MM-dd HHmmss}");
        Directory.CreateDirectory(folder);

        StringBuilder log = new();

        //----------------------------------------------------------------------------------------------------------
        DumpApplicationState(log);
        File.WriteAllText(Path.Combine(folder, "Memory information1.txt"), log.ToString());
        log.Clear();

        //----------------------------------------------------------------------------------------------------------
        // The summary comes first: if the working set is far bigger than the managed heap the leak is
        // native (web view messages, unmanaged buffers) and the type statistics below will not show it.
        DumpMemorySummary(log);
        DumpHeapStatistics(log);
        DumpGarbageCollectorInfo(log);
        File.WriteAllText(Path.Combine(folder, "Memory information2.txt"), log.ToString());
        log.Clear();

        //----------------------------------------------------------------------------------------------------------
        DumpLargeObjects(Path.Combine(folder, "Memory DumpLargeObjects.txt"));

        //----------------------------------------------------------------------------------------------------------
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return folder;
    }


    /// <summary>
    /// Everything the scanner keeps in memory itself: per symbol and interval the candles, signals,
    /// zones and indicator state, followed by the global lists and queues.
    /// </summary>
    public static void DumpApplicationState(StringBuilder log)
    {
        int dataCount = 0;
        int candleCount = 0;

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

                foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
                {
                    log.AppendLine($"      Interval: {symbolInterval.Interval.Name} Candle synchronized: {symbolInterval.LastCandleSynchronized?.ToDateTime()} Candles: {symbolInterval.CandleList.Count} LastCandle: {symbolInterval.CandleList.LastCandle.Date}");
                    candleCount += symbolInterval.CandleList.Count;
                    dataCount += symbolInterval.Data.Count;

                    // The indicator data (one CryptoData per candle) is the biggest managed consumer
                    // per interval, so report it next to the candle count.
                    if (symbolInterval.Data.Count > 0)
                        log.AppendLine($"      Indicator data: {symbolInterval.Data.Count}");
                    if (symbolInterval.IndicatorHub != null)
                        log.AppendLine($"      IndicatorHub added: {symbolInterval.IndicatorHubAddCount} last: {symbolInterval.IndicatorHubLastAdded?.ToDateTime()}");
                    if (symbolInterval.ZigZagIndicators.Count > 0)
                        log.AppendLine($"      ZigZagIndicators: {symbolInterval.ZigZagIndicators.Count}");

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

                    if (symbolInterval.SmcZones.Count > 0)
                        log.AppendLine($"      SmcZones: {symbolInterval.SmcZones.Count}");

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
    }


    /// <summary>
    /// Managed versus native memory. The working set minus the managed heap is everything the garbage
    /// collector does not own: web view messages, unmanaged buffers, loaded images, thread stacks.
    /// A large difference means the type statistics below will not explain the memory usage.
    /// </summary>
    public static void DumpMemorySummary(StringBuilder log)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var process = Process.GetCurrentProcess();
        GCMemoryInfo memInfo = GC.GetGCMemoryInfo();
        long managed = GC.GetTotalMemory(false);
        long workingSet = process.WorkingSet64;
        long other = workingSet - managed;

        log.AppendLine($"Memory summary - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine($"  Working set (total)   : {Mb(workingSet)}");
        log.AppendLine($"  Managed heap          : {Mb(managed)}");
        log.AppendLine($"  Heap committed        : {Mb(memInfo.TotalCommittedBytes)}");
        log.AppendLine($"  Native / not managed  : {Mb(other)}  ({100.0 * other / Math.Max(1, workingSet):F1}% of the working set)");
        log.AppendLine($"  Private memory        : {Mb(process.PrivateMemorySize64)}");
        log.AppendLine($"  Virtual memory        : {Mb(process.VirtualMemorySize64)}");
        log.AppendLine($"  Paged memory          : {Mb(process.PagedMemorySize64)}");
        log.AppendLine($"  Threads               : {process.Threads.Count}");
        log.AppendLine($"  Handles               : {process.HandleCount}");
        log.AppendLine($"  Started               : {SafeStartTime(process)}");
        log.AppendLine();
        if (other > 4 * managed && other > 512L * 1024 * 1024)
            log.AppendLine("  NOTE: most of the memory is NOT on the managed heap, so the type statistics below will not explain it.");
        log.AppendLine();
        log.AppendLine();
    }


    /// <summary>
    /// Count and total size per type on the managed heap, biggest first.
    /// </summary>
    public static void DumpHeapStatistics(StringBuilder log)
    {
        log.AppendLine($"Memory Dump - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine();

        var stats = new Dictionary<string, (int Count, long TotalSize)>();
        try
        {
            using var dataTarget = AttachToSelf();
            using var runtime = dataTarget.ClrVersions[0].CreateRuntime();

            // Walk all objects on the heap
            foreach (var obj in runtime.Heap.EnumerateObjects())
            {
                var type = obj.Type;
                if (type == null) continue;

                string typeName = type.Name ?? "Unknown";
                long size = (long)obj.Size;

                if (stats.TryGetValue(typeName, out var current))
                    stats[typeName] = (current.Count + 1, current.TotalSize + size);
                else
                    stats[typeName] = (1, size);
            }
        }
        catch (Exception error)
        {
            log.AppendLine($"Heap statistics not available: {error.Message}");
            log.AppendLine();
            log.AppendLine();
            return;
        }

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
    }


    /// <summary>
    /// Garbage collector counters, generation sizes and fragmentation.
    /// </summary>
    public static void DumpGarbageCollectorInfo(StringBuilder log)
    {
        // Force a collection so only live objects are left
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

        // Generation info (only available in .NET 5+)
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
    }


    /// <summary>
    /// Everything on the Large Object Heap (objects over 85 KB), grouped per type.
    /// </summary>
    public static void DumpLargeObjects(string outputPath)
    {
        using var writer = new StreamWriter(outputPath);
        writer.WriteLine($"Large Object Analysis - {DateTime.Now}");
        writer.WriteLine();

        ClrRuntime runtime;
        DataTarget dataTarget;
        try
        {
            dataTarget = AttachToSelf();
            runtime = dataTarget.ClrVersions[0].CreateRuntime();
        }
        catch (Exception error)
        {
            writer.WriteLine($"Not available: {error.Message}");
            return;
        }

        using (dataTarget)
        using (runtime)
        {
            // Find objects larger than 85KB (Large Object Heap threshold)
            var largeObjects = new Dictionary<string, List<ulong>>();
            // The weak event tables of the UI framework are collected in the same pass; walking the
            // whole heap a second time for them is expensive on a large process.
            var weakEventObjects = new List<(string TypeName, ulong Address, ulong Size)>();

            foreach (var obj in runtime.Heap.EnumerateObjects())
            {
                string typeName = obj.Type?.Name ?? "Unknown";

                if (obj.Size > 85000) // 85KB = LOH threshold
                {
                    if (!largeObjects.TryGetValue(typeName, out var list))
                    {
                        list = [];
                        largeObjects[typeName] = list;
                    }

                    list.Add(obj.Size);
                }

                if (typeName.Contains("WeakHashList") && typeName.Contains("Dictionary"))
                    weakEventObjects.Add((typeName, obj.Address, obj.Size));
            }

            writer.WriteLine("=== LARGE OBJECTS (>85KB, on Large Object Heap) ===");
            foreach (var kvp in largeObjects.OrderByDescending(x => x.Value.Sum(s => (long)s)))
            {
                long totalSize = kvp.Value.Sum(s => (long)s);
                writer.WriteLine($"{kvp.Key}");
                writer.WriteLine($"  Count: {kvp.Value.Count}");
                writer.WriteLine($"  Total: {totalSize:N0} bytes ({totalSize / 1024.0 / 1024.0:F2} MB)");
                writer.WriteLine($"  Average: {kvp.Value.Select(x => (long)x).Average():N0} bytes");
                writer.WriteLine($"  Max: {kvp.Value.Max():N0} bytes");
                writer.WriteLine();
            }

            writer.WriteLine("=== WEAK EVENT DICTIONARIES ===");
            foreach (var (typeName, address, size) in weakEventObjects)
            {
                writer.WriteLine($"Found: {typeName}");
                writer.WriteLine($"  Address: 0x{address:X}");
                writer.WriteLine($"  Size: {size:N0} bytes");

                // Try to find segment/generation
                var seg = runtime.Heap.GetSegmentByAddress(address);
                if (seg != null)
                {
                    int gen = (int)seg.GetGeneration(address);
                    writer.WriteLine($"  Generation: {gen}");
                }
                writer.WriteLine();
            }
        }
    }


    /// <summary>
    /// Open a ClrMD view on our OWN process. AttachToProcess refuses that with "Attaching to the
    /// current process is not supported"; a snapshot is the supported route and is also the safer
    /// one, because the heap is walked over a frozen copy instead of over memory that the scanner
    /// threads keep changing while we read it.
    /// </summary>
    private static DataTarget AttachToSelf()
    {
        return DataTarget.CreateSnapshotAndAttach(Environment.ProcessId);
    }


    private static string Mb(long bytes)
    {
        return $"{bytes:N0} bytes ({bytes / 1024.0 / 1024.0:F2} MB)";
    }


    private static string SafeStartTime(Process process)
    {
        try
        {
            return $"{process.StartTime:yyyy-MM-dd HH:mm:ss} (running for {DateTime.Now - process.StartTime:d\\.hh\\:mm\\:ss})";
        }
        catch
        {
            return "unknown";
        }
    }
}

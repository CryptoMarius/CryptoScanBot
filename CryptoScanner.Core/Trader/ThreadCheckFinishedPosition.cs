using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using System.Diagnostics;

namespace CryptoScanner.Core.Trader;


public class ThreadCheckFinishedPosition
{
    private readonly SemaphoreSlim QueueSemaphore = new(1);
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly Dictionary<string, (CryptoPosition position, string? orderId, CryptoOrderStatus? status)> Queue = [];

    // Reused DB connection(s) for the emulator's per-candle position checks (see AddToQueue). ONE PER
    // THREAD: serial replay reuses a single connection; parallel replay (TickRunner.RunParallel) runs the
    // symbol pipeline on several worker threads at once, and a SQLite connection is not thread-safe, so
    // each thread gets its own. trackAllValues lets CloseEmulatorConnection dispose them all at run end.
    private ThreadLocal<CryptoDatabase> _emulatorDatabase = NewEmulatorDatabase();

    private static ThreadLocal<CryptoDatabase> NewEmulatorDatabase() => new(OpenDatabase, trackAllValues: true);


    public void Stop()
    {
        cancellationToken.Cancel();
        CloseEmulatorConnection();
        GlobalData.AddTextToLogTab("Stop position check finished handler");
    }

    /// <summary>
    /// Releases the reused emulator connection(s) — one per worker thread (see <see cref="AddToQueue"/>).
    /// Called at the END of an emulator run (EmulatorDb.FinishRun), when the parallel replay has finished
    /// so no thread is using them, so the DB file is not left with an open handle: the emulator deletes
    /// the file on a Reset, which fails on Windows while a handle is open, and a stale handle would point
    /// at the recreated file. A fresh (empty) set is installed for the next run.
    /// </summary>
    public void CloseEmulatorConnection()
    {
        ThreadLocal<CryptoDatabase> previous = _emulatorDatabase;
        _emulatorDatabase = NewEmulatorDatabase();
        foreach (CryptoDatabase database in previous.Values)
            database.Dispose();
        previous.Dispose();
    }

    public async Task AddToQueue(CryptoPosition position, string? orderId = null, CryptoOrderStatus? status = null)
    {
        if (GlobalData.IsEmulatorMode)
        {
            // Opening a fresh CryptoDatabase (connection + PRAGMA setup) on every call dominated the run
            // profile (positionCheck was ~52% of the pipeline). Reuse a per-thread connection instead:
            // .Value opens one on first use on this thread and returns the same one afterwards. Per-thread
            // (not one shared) because parallel replay runs this on several worker threads concurrently.
            await ProcessPosition(_emulatorDatabase.Value!, position, orderId, status);
        }
        else
        {

            await QueueSemaphore.WaitAsync();
            try
            {
                ////await position.ProcessPositionSemaphore.WaitAsync();
                //try
                //{
                //var bla = Queue.Contains(position);
                //var isPresent = Queue.Any(d => d.position == position);
                //if (Queue.Any(d => d.position == position)) //Dupes.ContainsKey(position.Symbol.Name))
                if (Queue.TryGetValue(position.Symbol.Name, out (CryptoPosition position, string? orderId, CryptoOrderStatus? status) foundPosition))
                {
                    if (status.HasValue)
                        foundPosition.status = status;
                    if (orderId != null)
                        foundPosition.orderId = orderId;
                    ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} duplicate {position.Status} check={position.ForceCheckPosition} {position.DelayUntil} {orderId} {status}");
                    return;
                }
                //}
                //finally
                //{
                //    //ProcessPositionSemaphore.Release();
                //}

                //Queue.Add((position, orderId, status));
                _ = Queue.TryAdd(position.Symbol.Name, (position, orderId, status));
            }
            finally
            {
                QueueSemaphore.Release();
            }
        }
    }

    private static CryptoDatabase OpenDatabase()
    {
        var database = new CryptoDatabase();
        database.Open();
        return database;
    }


    //private static async Task<bool> UpdatePositionStatisticsAsync(CryptoPosition position)
    //{
    //    if (position.CloseTime == null && GlobalData.IsEmulatorMode)
    //    {
    //        CryptoSymbolInterval symbolInterval = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
    //        if (symbolInterval.CandleList.Count > 0)
    //        {
    //            // Lock to avoid problems
    //            CryptoCandle candle;
    //            await position.Symbol.Data.CandleLock.WaitAsync();
    //            try
    //            {
    //                candle = symbolInterval.CandleList.Values.Last(); // todo, not working for emulator!
    //            }
    //            finally
    //            {
    //                position.Symbol.Data.CandleLock.Release();
    //            }


    //            try
    //            {
    //                if (candle.Low < position.PriceMin || position.PriceMin == 0)
    //                {
    //                    position.PriceMin = candle.Low;
    //                    position.PriceMinPerc = (float)(100 * (position.PriceMin / position.SignalPrice - 1));
    //                    return true;
    //                }

    //                if (candle.High > position.PriceMax || position.PriceMax == 0)
    //                {
    //                    position.PriceMax = candle.High;
    //                    position.PriceMaxPerc = (float)(100 * (position.PriceMax / position.SignalPrice - 1));
    //                    return true;
    //                }
    //            }
    //            catch
    //            {
    //                // ignore (sometimes low of high value not set, need locking?)
    //            }
    //        }
    //    }
    //    return false;
    //}



    private async Task PositionReadyCancelAllOrderAndMove(CryptoDatabase database, CryptoPosition position)
    {
        bool removePosition = true;
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
            {
                if (step.Status == CryptoOrderStatus.New && step.Side == entryOrderSide)
                {
                    string cancelReason = "cancel";
                    ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: {cancelReason}");
                    var (succes, tradeParams) = await TradeTools.CancelOrder(database, position, part, step,
                        GlobalData.Clock.UtcNow, CryptoOrderStatus.PositionClosed, cancelReason);
                    if (!succes)
                    {
                        // nog nooit gezien, maar kan geen kwaad
                        ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: {cancelReason} failed");
                        ExchangeBase.Dump(position, succes, tradeParams, "DCA ORDER ANNULEREN NIET GELUKT!!! (retry)");
                        position.ForceCheckPosition = true;
                        position.DelayUntil = GlobalData.Clock.UtcNow.AddSeconds(10);
                        await AddToQueue(position); // doe nog maar een keer... Endless loop?
                        removePosition = false;
                    }

                }
            }
        }

        if (removePosition)
        {
            // Send the position to the closed positions ViewModel
            PositionTools.RemovePosition(GlobalData.ActiveExchange!, position, true);
        }
    }



    private static async Task PositionOpenAsUsual(CryptoPosition position, string? orderId)
    {
        // PositionMonitor aanroep code verplaatst vanuit kline-ticker thread naar hier
        var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (symbolPeriod.CandleList.Count > 0)
        {
            CryptoCandle lastCandle1m;

            // Live path takes the most recent candle. The emulator runs in a separate process
            // with its own DB and clock, so the legacy BackTest branch is no longer needed here.
            await position.Symbol.Data.CandleLock.WaitAsync();
            try
            {
                lastCandle1m = symbolPeriod.CandleList.Values.Last();
            }
            finally
            {
                position.Symbol.Data.CandleLock.Release();
            }

            ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: {position.Symbol.Name} CheckThePosition {orderId}");
            PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
            await positionMonitor.CheckThePosition(position); // CancelOrdersIfClosedOrTimeoutOrReposition?

            // Bij nader inzien kan die status hier nooit ready zijn...
            //if (position.Status == CryptoPositionStatus.Ready)
            //{
            //    ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: {position.Symbol.Name} ready, nog een keer!");
            //    position.DelayUntil = GlobalData.GetCurrentDateTime().AddSeconds(10);
            //    await AddToQueue(position); // nog eens, en dan laten verplaatsen naar gesloten posities
            //}
        }
    }



    private async Task ProcessPosition(CryptoDatabase database, CryptoPosition position, string? orderId, CryptoOrderStatus? status)
    {
        //ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} pickup {position.Status} check={position.ForceCheckPosition} {reason}");

        // Profiling: ProcessPosition is the body AddToQueue runs synchronously in emulator mode, i.e.
        // the entire "positionCheck" bucket in PipelineProfiler. Splits it into the ForceCheckPosition
        // recalculation, the status==New short-circuit, and the two terminal branches (Ready/Timeout/
        // TakeOver cleanup vs. the normal Waiting/Trading check) so the gap between the positionCheck
        // total and the already-instrumented PositionResults/CheckThePosition sub-buckets is explained.
        // ppTotalStart/finally covers every exit path (early returns and the catch block included).
        long ppTotalStart = Stopwatch.GetTimestamp();
        long ppForceCheckTicks = 0;
        long ppStatusNewTicks = 0;
        long ppReadyTicks = 0;
        long ppOpenAsUsualTicks = 0;
        try
        {
            try
            {
                if (!GlobalData.IsEmulatorMode)
                    await position.ProcessPositionSemaphore.WaitAsync();
                try
                {
                    // Geef de exchange en de aansturende code de kans om de administratie af te ronden
                    // We wachten hier dus bewust voor de zekerheid een redelijk lange periode.
                    if (!GlobalData.IsEmulatorMode && position.DelayUntil.HasValue && position.DelayUntil.Value >= GlobalData.Clock.UtcNow)
                    {
                        //ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} delay {position.Status} check={position.ForceCheckPosition} {position.DelayUntil} {reason}");
                        await AddToQueue(position, orderId, status); // opnieuw, na een vertraging
                        await Task.Delay(500);
                        return;
                    }


                    //GlobalData.AddTextToLogTab($"ThreadCheckFinishedPosition: Positie {position.Symbol.Name} controleren! {position.Status} {position.DelayUntil}");
                    //ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} checking {position.Status} check={position.ForceCheckPosition} {orderId}");


                    // een extra orderid en een status erbij (nullable)

                    // OrderStatus:
                    // status = New                     -> de order ophalen
                    // status = PartiallyFilled         -> positie tenminste trading, verder geheel negeren
                    // status = Filled                  -> positie tenminste trading, order ophalen, trade(s) ophalen, status, herberekenen, eventueel closen
                    // status = PartiallyFilledClosed   -> positie tenminste trading, order ophalen, trade(s) ophalen, status, bijwerken step, herberekenen, eventueel closen
                    // status = Canceled                -> door trader of gebruiker?, eventueel closen als takeover.

                    // PositieStatus:
                    // Status = Ready                   -> orders ophalen, trades ophalen, herberekenen, indien geen wijzigingen verplaatsen naar closed


                    // het nieuwe idee
                    if (status.HasValue)
                    {
                        // De positie status aanpassen
                        switch (status)
                        {
                            //case CryptoOrderStatus.New: // only a takeprofit order
                            case CryptoOrderStatus.Filled:
                            case CryptoOrderStatus.PartiallyFilled:
                            case CryptoOrderStatus.PartiallyAndClosed:
                                if (position.Status == CryptoPositionStatus.Waiting)
                                {
                                    position.Status = CryptoPositionStatus.Trading;
                                    position.ForceCheckPosition = true;
                                }
                                break;
                        }
                    }


                    // Controleer orders, trades en herbereken de quantity, commissie etc
                    if (position.ForceCheckPosition)
                    {
                        position.ForceCheckPosition = false;
                        long forceCheckStart = Stopwatch.GetTimestamp();
                        await TradeTools.CalculatePositionResultsViaOrders(database, position, forceCalculation: true);
                        ppForceCheckTicks = Stopwatch.GetTimestamp() - forceCheckStart;
                    }


                    // With status new it is enoughh to Calculate the position (fetch and check orders), there is nothing that will change..
                    if (status.HasValue && status == CryptoOrderStatus.New)
                    {
                        long statusNewStart = Stopwatch.GetTimestamp();
                        //ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} checking {position.Status} check={position.ForceCheckPosition} {orderId} status ==CryptoOrderStatus.New");
                        ppStatusNewTicks = Stopwatch.GetTimestamp() - statusNewStart;
                        return;
                    }

                    if (position.Status >= CryptoPositionStatus.Ready) // (Ready, Timeout and TakeOver)
                    {
                        long readyStart = Stopwatch.GetTimestamp();
                        await PositionReadyCancelAllOrderAndMove(database, position);
                        ppReadyTicks = Stopwatch.GetTimestamp() - readyStart;
                    }
                    else
                    {
                        long openAsUsualStart = Stopwatch.GetTimestamp();
                        await PositionOpenAsUsual(position, orderId); // Waiting and Trading
                        ppOpenAsUsualTicks = Stopwatch.GetTimestamp() - openAsUsualStart;
                    }
                }
                finally
                {
                    if (!GlobalData.IsEmulatorMode)
                        position.ProcessPositionSemaphore.Release();
                }

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} ERROR position ThreadCheckFinishedPosition thread {error.Message}");
            }
        }
        finally
        {
            PipelineProfiler.RecordProcessPosition(
                total: Stopwatch.GetTimestamp() - ppTotalStart,
                forceCheck: ppForceCheckTicks,
                statusNew: ppStatusNewTicks,
                ready: ppReadyTicks,
                openAsUsual: ppOpenAsUsualTicks);
        }
    }




    public async Task ExecuteAsync()
    {
        //GlobalData.AddTextToLogTab("ThreadCheckFinishedPosition thread start");

        cancellationToken.TryReset();
        using CryptoDatabase database = new();
        database.Open();

        while (!cancellationToken.Token.IsCancellationRequested)
        {
            if (Queue.Count == 0)
            {
                Thread.Sleep(100);
                continue;
            }

            (CryptoPosition position, string? orderId, CryptoOrderStatus? status)? item = null;
            await QueueSemaphore.WaitAsync();
            try
            {
                if (Queue.Count > 0)
                {
                    item = Queue.First().Value;
                    Queue.Remove(item.Value.position.Symbol.Name);
                }
            }
            finally
            {
                QueueSemaphore.Release();
            }

            if (item != null)
                await ProcessPosition(database, item.Value.position, item.Value.orderId, item.Value.status);
        }

        //GlobalData.AddTextToLogTab("ThreadCheckFinishedPosition thread exit");
    }
}

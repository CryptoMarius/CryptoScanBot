using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

namespace CryptoScanBot.Core.Intern;


public class ThreadCheckFinishedPosition
{
    private readonly SemaphoreSlim QueueSemaphore = new(1);
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly Dictionary<string, (CryptoPosition position, string orderId, CryptoOrderStatus? status)> Queue = [];


    public void Stop()
    {
        cancellationToken.Cancel();
        GlobalData.AddTextToLogTab("Stop position check finished handler");
    }

    public async Task AddToQueue(CryptoPosition position, string? orderId = null, CryptoOrderStatus? status = null)
    {
        if (GlobalData.BackTest)
        {
            using CryptoDatabase database = new();
            database.Open();
            await ProcessPosition(database, position, orderId, status);
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

    private static bool UpdatePositionStatistics(CryptoPosition position)
    {
        if (position.CloseTime == null && position.Account.AccountType != CryptoAccountType.BackTest)
        {
            CryptoSymbolInterval symbolInterval = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            if (symbolInterval.CandleList.Count > 0)
            {
                CryptoCandle candle = symbolInterval.CandleList.Values[^1]; // todo, not working for emulator!
                try
                {
                    if (candle.Low < position.PriceMin || position.PriceMin == 0)
                    {
                        position.PriceMin = candle.Low;
                        position.PriceMinPerc = (double)(100 * (position.PriceMin / position.SignalPrice - 1));
                        return true;
                    }

                    if (candle.High > position.PriceMax || position.PriceMax == 0)
                    {
                        position.PriceMax = candle.High;
                        position.PriceMaxPerc = (double)(100 * (position.PriceMax / position.SignalPrice - 1));
                        return true;
                    }
                }
                catch
                {
                    // ignore (sometimes low of high value not set, need locking?)
                }
            }
        }
        return false;
    }



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
                        GlobalData.GetCurrentDateTime(position.Account), CryptoOrderStatus.PositionClosed, cancelReason);
                    if (!succes)
                    {
                        // nog nooit gezien, maar kan geen kwaad
                        ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: {cancelReason} failed");
                        ExchangeBase.Dump(position, succes, tradeParams, "DCA ORDER ANNULEREN NIET GELUKT!!! (retry)");
                        position.ForceCheckPosition = true;
                        position.DelayUntil = GlobalData.GetCurrentDateTime(position.Account).AddSeconds(10);
                        await AddToQueue(position); // doe nog maar een keer... Endless loop?
                        removePosition = false;
                    }

                }
            }
        }

        if (removePosition)
        {
            // Positie is afgerond (wellicht dubbel op met de code in de PositionTools)
            PositionTools.RemovePosition(position.Account, position, true);
            if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            {
                GlobalData.PositionsHaveChanged("");
                GlobalData.AddTextToLogTab($"ThreadCheckFinishedPosition: Position {position.Symbol.Name} moved ({position.Status})");
            }
        }
    }



    private static async Task PositionOpenAsUsual(CryptoPosition position, string? orderId)
    {
        // PositionMonitor aanroep code verplaatst vanuit kline-ticker thread naar hier
        var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (symbolPeriod.CandleList.Count > 0)
        {
            CryptoCandle? lastCandle1m;

            // Deze routine is vanwege de Last() niet geschikt voor de emulator
            // Hoe lossen we dat nu weer op, want wordt strakt een echt probleem.
            //Monitor.Enter(position.Symbol.CandleList);
            await position.Symbol.CandleLock.WaitAsync();
            try
            {
                if (GlobalData.BackTest)
                {
                    lastCandle1m = GlobalData.BackTestCandle;
                    if (lastCandle1m == null)
                        return;
                }
                else
                    lastCandle1m = symbolPeriod.CandleList.Values.Last();
            }
            finally
            {
                //Monitor.Exit(position.Symbol.CandleList);
                position.Symbol.CandleLock.Release();
            }

            ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: {position.Symbol.Name} CheckThePosition {orderId}");
            PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle1m);
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
        try
        {
            if (!GlobalData.BackTest)
                await position.ProcessPositionSemaphore.WaitAsync();
            try
            {
                // Geef de exchange en de aansturende code de kans om de administratie af te ronden
                // We wachten hier dus bewust voor de zekerheid een redelijk lange periode.
                if (!GlobalData.BackTest && position.DelayUntil.HasValue && position.DelayUntil.Value >= GlobalData.GetCurrentDateTime(position.Account))
                {
                    //ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} delay {position.Status} check={position.ForceCheckPosition} {position.DelayUntil} {reason}");
                    await AddToQueue(position, orderId, status); // opnieuw, na een vertraging
                    await Task.Delay(500);
                    return;
                }


                //GlobalData.AddTextToLogTab($"ThreadCheckFinishedPosition: Positie {position.Symbol.Name} controleren! {position.Status} {position.DelayUntil}");
                ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} checking {position.Status} check={position.ForceCheckPosition} {orderId}");


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
                    await TradeTools.CalculatePositionResultsViaOrders(database, position, forceCalculation: true);
                }


                // With status new it is enoughh to Calculate the position (fetch and check orders), there is nothing that will change..
                if (status.HasValue && status == CryptoOrderStatus.New)
                {
                    ScannerLog.Logger.Trace($"ThreadCheckFinishedPosition.Execute: Positie {position.Symbol.Name} checking {position.Status} check={position.ForceCheckPosition} {orderId} status ==CryptoOrderStatus.New");
                    return;
                }

                if (GlobalData.Settings.General.DebugSignalStrength)
                {
                    if (UpdatePositionStatistics(position))
                    {
                        GlobalData.ThreadSaveObjects!.AddToQueue(position);
                    }
                }

                if (position.Status >= CryptoPositionStatus.Ready) // (Ready, Timeout and TakeOver)
                    await PositionReadyCancelAllOrderAndMove(database, position);
                else
                    await PositionOpenAsUsual(position, orderId); // Waiting and Trading
            }
            finally
            {
                if (!GlobalData.BackTest) 
                    position.ProcessPositionSemaphore.Release();
            }

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} ERROR position ThreadCheckFinishedPosition thread {error.Message}");
        }
    }




    public async Task ExecuteAsync()
    {
        GlobalData.AddTextToLogTab("ThreadCheckFinishedPosition thread start");

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

            (CryptoPosition position, string orderId, CryptoOrderStatus? status)? item = null;
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

        GlobalData.AddTextToLogTab("ThreadCheckFinishedPosition thread exit");
    }
}

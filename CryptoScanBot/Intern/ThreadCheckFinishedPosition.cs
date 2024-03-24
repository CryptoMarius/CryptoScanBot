
using System.Collections.Concurrent;

using Bybit.Net.Enums.V5;

using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Model;
using CryptoScanBot.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Intern;

#if TRADEBOT

// Bedoeld om een positie te controleren nadat deze op ready is gezet

public class ThreadCheckFinishedPosition
{
    private readonly SemaphoreSlim Semaphore = new(1);
    private readonly SortedList<string, CryptoPosition> Dupes = [];
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<(CryptoPosition position, string orderId, CryptoOrderStatus? status)> Queue = [];

    public ThreadCheckFinishedPosition()
    {
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab("Stop position check finished handler");
    }

    public async Task AddToQueue(CryptoPosition position, string orderId = null, CryptoOrderStatus? status = null)
    {
        await Semaphore.WaitAsync();
        try
        {
            ////await position.Semaphore.WaitAsync();
            //try
            //{
            if (Dupes.ContainsKey(position.Symbol.Name))
            {
                ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: Positie {position.Symbol.Name} duplicate {position.Status} check={position.ForceCheckPosition} {position.DelayUntil} {orderId} {status}");
                return;
            }
            //}
            //finally
            //{
            //    //Semaphore.Release();
            //}

            Dupes.Add(position.Symbol.Name, position);
            Queue.Add((position, orderId, status));
        }
        finally
        {
            Semaphore.Release();
        }
    }



    public async Task ExecuteAsync()
    {
        using CryptoDatabase database = new();
        database.Open();

        // Een aparte queue die orders afhandeld (met als achterliggende reden de juiste afhandel volgorde)
        foreach ((CryptoPosition position, string orderId, CryptoOrderStatus? status) in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            //ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: Positie {position.Symbol.Name} pickup {position.Status} check={position.ForceCheckPosition} {reason}");
            try
            {
                await position.Semaphore.WaitAsync();
                try
                {
                    Dupes.Remove(position.Symbol.Name);

                    // Geef de exchange en de aansturende code de kans om de administratie af te ronden
                    // We wachten hier dus bewust voor de zekerheid een redelijk lange periode.
                    if (position.DelayUntil.HasValue && position.DelayUntil.Value > DateTime.UtcNow)
                    {
                        //ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: Positie {position.Symbol.Name} delay {position.Status} check={position.ForceCheckPosition} {position.DelayUntil} {reason}");
                        await AddToQueue(position); // opnieuw, na een vertraging
                        await Task.Delay(500);
                        continue;
                    }


                    //GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} controleren! {position.Status} {position.DelayUntil}");
                    ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: Positie {position.Symbol.Name} checking {position.Status} check={position.ForceCheckPosition} {orderId}");


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
                            case CryptoOrderStatus.New:
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


                    // Voor status=new is ophalen van de orders genoeg er veranderd niets aan de positie
                    if (status.HasValue && status ==CryptoOrderStatus.New)
                    {
                        ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: Positie {position.Symbol.Name} checking {position.Status} check={position.ForceCheckPosition} {orderId} status ==CryptoOrderStatus.New");
                        continue;
                    }



                    if (position.Status == CryptoPositionStatus.Ready)
                    {
                        bool removePosition = true;
                        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                        {
                            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                            {
                                if (step.Status == CryptoOrderStatus.New && step.Side == entryOrderSide)
                                {
                                    string cancelReason = $"{position.Symbol.Name} annuleren openstaande dca order";
                                    ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {cancelReason}");
                                    var (succes, tradeParams) = await TradeTools.CancelOrder(database, position, part, step, 
                                        DateTime.UtcNow, CryptoOrderStatus.PositionClosed, cancelReason);
                                    if (!succes)
                                    {
                                        ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {cancelReason} mislukt");
                                        ExchangeBase.Dump(position, succes, tradeParams, "DCA ORDER ANNULEREN NIET IN 1x GELUKT!!! (herkansing)");
                                        position.ForceCheckPosition = true;
                                        position.DelayUntil = DateTime.UtcNow.AddSeconds(10);
                                        await AddToQueue(position); // doe nog maar een keer... Endless loop?
                                        removePosition = false;
                                    }

                                }
                            }
                        }

                        if (removePosition)
                        {
                            // Positie is afgerond (wellicht dubbel op met de code in de PositionTools)
                            PositionTools.RemovePosition(position.TradeAccount, position, true);
                            if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                            {
                                GlobalData.PositionsHaveChanged("");
                                GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} verplaatst naar {position.Status}");
                            }
                        }
                    }
                    else
                    {
                        // De normale positie afhandeling

                        // PositionMonitor aanroep code verplaatst vanuit kline-ticker thread naar hier
                        var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                        if (symbolPeriod.CandleList.Count > 0)
                        {
                            CryptoCandle lastCandle1m;

                            // Deze routine is vanwege de Last() niet geschikt voor de emulator
                            // Hoe lossen we dat nu weer op, want wordt strakt een echt probleem.
                            Monitor.Enter(position.Symbol.CandleList);
                            try
                            {
                                lastCandle1m = symbolPeriod.CandleList.Values.Last();
                            }
                            finally
                            {
                                Monitor.Exit(position.Symbol.CandleList);
                            }

                            ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {position.Symbol.Name} CheckThePosition {orderId}");
                            PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                            await positionMonitor.CheckThePosition(position); // CancelOrdersIfClosedOrTimeoutOrReposition?

                            // Bij nader inzien kan die status hier nooit ready zijn...
                            //if (position.Status == CryptoPositionStatus.Ready) 
                            //{
                            //    ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {position.Symbol.Name} ready, nog een keer!");
                            //    position.DelayUntil = DateTime.UtcNow.AddSeconds(10);
                            //    await AddToQueue(position); // nog eens, en dan laten verplaatsen naar gesloten posities
                            //}
                        }
                    }
                }
                finally
                {
                    position.Semaphore.Release();
                }

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} ERROR position ThreadCheckFinishedPosition thread {error.Message}");
            }
        }
        GlobalData.AddTextToLogTab("ThreadCheckFinishedPosition thread exit");
    }
}
#endif

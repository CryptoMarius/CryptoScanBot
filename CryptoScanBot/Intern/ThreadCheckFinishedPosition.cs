
using System.Collections.Concurrent;

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
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<(CryptoPosition, bool, string)> Queue = new();

    public ThreadCheckFinishedPosition()
    {
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab("Stop position check finished handler");
    }

    public void AddToQueue(CryptoPosition position, bool check, string reason, bool addExtraDelay = false)
    {
        // Extra delay omdat de exchange mogelijk de administratie niet rond heeft.
        // Met name als het druk op de exchange is kan het problemen geven.
        if (addExtraDelay)
        {
            // Eigenlijk wil je de positie niet in deze queue hebben.
            // Hoe voorkom je duplicaten in deze queue, niet netjes.
            position.DelayUntil = DateTime.UtcNow.AddSeconds(5);
        }

        Queue.Add((position, check, reason));
    }



    public async Task ExecuteAsync()
    {
        using CryptoDatabase database = new();
        database.Open();

        // Een aparte queue die orders afhandeld (met als achterliggende reden de juiste afhandel volgorde)
        foreach ((CryptoPosition position, bool check, string reason) in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            //GlobalData.AddTextToLogTab($"{position.Symbol.Name} debug position ThreadCheckFinishedPosition Execute...");
            try
            {
                //Monitor.Enter(position); // Object synchronization method was called from an unsynchronized block of code (at exit)
                try
                {
                    // Geef de exchange en de aansturende code de kans om de administratie af te ronden
                    // We wachten hier dus bewust voor de zekerheid een redelijk lange periode.
                    if (position.DelayUntil.HasValue && position.DelayUntil.Value > DateTime.UtcNow)
                    {
                        AddToQueue(position, check, reason); // opnieuw, na een vertraging
                        await Task.Delay(500);
                        continue;
                    }


                    //GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} controleren! {position.Status} {position.DelayUntil}");
                    ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: Positie {position.Symbol.Name} controleren! {position.Status} {position.DelayUntil} {reason}");

                    // Controleer orders
                    if (check)
                    {
                        await TradeTools.LoadOrdersFromDatabaseAndExchangeAsync(database, position);
                        await TradeTools.CalculatePositionResultsViaOrders(database, position);
                    }


                    if (position.Status == CryptoPositionStatus.Ready)
                    {
                        bool removePosition = true;

                        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                        {
                            //if (!part.CloseTime.HasValue)
                            {
                                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                                {
                                    if (step.Status == CryptoOrderStatus.New && step.Side == entryOrderSide)
                                    {
                                        ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {position.Symbol.Name} annuleren dca order");

                                        // Aankondiging dat we deze order gaan annuleren (de tradehandler weet dan dat wij het waren en het niet de user was via de exchange)
                                        step.CancelInProgress = true;

                                        // Annuleer de vorige buy order
                                        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
                                        var (succes, tradeParams) = await exchangeApi.Cancel(position.TradeAccount, position.Symbol, step);
                                        if (succes)
                                        {
                                            step.CloseTime = DateTime.UtcNow;
                                            step.Status = CryptoOrderStatus.PositionClosed;
                                            database.Connection.Update<CryptoPositionStep>(step);

                                            if (GlobalData.Settings.Trading.LogCanceledOrders)
                                                ExchangeBase.Dump(position, succes, tradeParams, $"annuleren vanwege sluiten {position.Side} positie");
                                        }
                                        else
                                        {
                                            ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {position.Symbol.Name} annuleren dca order mislukt");
                                            ExchangeBase.Dump(position, succes, tradeParams, "DCA ORDER ANNULEREN NIET IN 1x GELUKT!!! (herkansing)");
                                            AddToQueue(position, true, "herkansing annuleren dca order", true); // doe nog maar een keer... Endless loop?
                                            removePosition = false;
                                        }
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

                            ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {position.Symbol.Name} CheckThePosition {reason}");
                            PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                            await positionMonitor.CheckThePosition(position);

                            if (position.Status == CryptoPositionStatus.Ready)
                            {
                                ScannerLog.Logger.Trace($"ThreadDoubleCheckPosition.Execute: {position.Symbol.Name} ready, nog een keer!");
                                AddToQueue(position, true, "positie is reaady, laten verplaatsen", true); // nog eens, en dan laten verplaatsen naar gesloten posities
                            }
                        }
                    }
                }
                finally
                {
                    //Monitor.Exit(position);
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

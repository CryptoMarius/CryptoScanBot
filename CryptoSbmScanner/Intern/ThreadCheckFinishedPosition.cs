
using System.Collections.Concurrent;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

#if TRADEBOT

// Bedoeld om een positie te controleren nadat deze op ready is gezet

public class ThreadCheckFinishedPosition
{
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<CryptoPosition> Queue = new();

    public ThreadCheckFinishedPosition()
    {
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab("Stop position check finished handler");
    }

    public void AddToQueue(CryptoPosition position)
    {
        // De positie is net klaar gemeld, maar pijnlijke ervaring leert ons dat het niet altijd juist is.
        // Er kunnen fouten ontstaan doordat orders of trades enzovoort niet correct afgehandeld zijn, of
        // dat er een bijkoop order net gevallen is. Dus extra controles doen na het administratief sluiten
        // van de positie.
        Queue.Add(position);
    }

    public async Task ExecuteAsync()
    {
        using CryptoDatabase database = new();
        database.Open();

        // Een aparte queue die orders afhandeld (met als achterliggende reden de juiste afhandel volgorde)
        foreach (CryptoPosition position in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            try
            {
                // Geef de exchange en de aansturende code de kans om de administratie af te ronden
                // We wachten hier dus bewust voor de zekerheid een redelijk lange periode.
                if (position.DelayUntil.HasValue && position.DelayUntil.Value > DateTime.UtcNow)
                {
                    AddToQueue(position); // opnieuw, na een vertraging
                    await Task.Delay(500);
                    continue;
                }

                //GlobalData.AddTextToLogTab($"{position.Symbol.Name} debug position ThreadCheckFinishedPosition Execute...");


                //GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} nog eens controleren! {position.Status}");

                if (position.Status == CryptoPositionStatus.Ready)
                {
                    bool removePosition = true;
                    // Controleer de openstaande orders, zijn ze ondertussen gevuld
                    // Haal de trades van deze positie op vanaf de 1e order
                    // TODO - Hoe doen we dit met papertrading (er is niets geregeld!)
                    await TradeTools.LoadTradesfromDatabaseAndExchange(database, position, true);
                    TradeTools.CalculatePositionResultsViaTrades(database, position, false);

                    CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                    foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                    {
                        //if (!part.CloseTime.HasValue)
                        {
                            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                            {
                                if (step.Status == CryptoOrderStatus.New && step.Side == entryOrderSide)
                                {
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
                                        ExchangeBase.Dump(position, succes, tradeParams, "DCA ORDER ANNULEREN NIET IN 1x GELUKT!!! (herkansing)");
                                        AddToQueue(position); // doe nog maar een keer... Endless loop?
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
                            //GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} status aangepast naar {position.Status}");
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

                        PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                        await positionMonitor.CheckThePosition(position);

                        if (position.Status == CryptoPositionStatus.Ready)
                            AddToQueue(position); // nog eens, en dan laten verplaatsen naar gesloten posities
                    }
                }

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} ERROR position ThreadCheckFinishedPosition thread {error.Message}");
            }
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n ThreadCheckFinishedPosition THREAD EXIT");
    }
}
#endif

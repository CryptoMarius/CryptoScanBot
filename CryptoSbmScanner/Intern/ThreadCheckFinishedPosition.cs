
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
        // Een aparte queue die orders afhandeld (met als achterliggende reden de juiste afhandel volgorde)
        foreach (CryptoPosition position in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            try
            {
                // Geef de exchange en de aansturende code de kans om de administratie af te ronden
                // We wachten hier dus bewust voor de zekerheid een redelijk lange periode.
                Thread.Sleep(10000);

                //GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} nog eens controleren! {position.Status}");

                // Controleer de openstaande orders, zijn ze ondertussen gevuld
                // Haal de trades van deze positie op vanaf de 1e order
                // TODO - Hoe doen we dit met papertrading (er is niets geregeld!)
                using CryptoDatabase database = new();
                database.Open();

                await TradeTools.LoadTradesfromDatabaseAndExchange(database, position, true);
                TradeTools.CalculatePositionResultsViaTrades(database, position, false);


                CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                if (position.Status == CryptoPositionStatus.Ready)
                {
                    foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                    {
                        if (!part.CloseTime.HasValue)
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
                                            ExchangeBase.Dump(position.Symbol, succes, tradeParams, $"annuleren vanwege sluiten {position.Side} positie");


                                        // Positie is afgerond (wellicht dubbel op met de code in de PositionTools)
                                        PositionTools.RemovePosition(position.TradeAccount, position);
                                        if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                                            GlobalData.PositionsHaveChanged("");
                                    }
                                    else
                                    {
                                        ExchangeBase.Dump(position.Symbol, succes, tradeParams, "DCA ORDER ANNULEREN NIET GELUKT!!! (herkansing)");
                                        AddToQueue(position); // doe nog maar een keer... Endless loop?
                                    }
                                }
                            }
                        }
                    }
                }
                
                
                //// Als de positie gesloten is, maar er bij nader inzien toch extra geinvesteerd is dan weer openzetten
                //if (position.Status == CryptoPositionStatus.Ready && position.Quantity != 0 && position.Invested != 0)
                //{
                //    position.CloseTime = null;
                //    position.Status = CryptoPositionStatus.Trading;
                //    GlobalData.AddTextToLogTab($"ThreadDoubleCheckPosition: Positie {position.Symbol.Name} status aangepast naar {position.Status}");


                //    // Ehh, moeten we dan nog een part opnieuw openen??
                //    // TODO In de gaten houden wat exact het probleem is!

                //    // Bij nader inzien: Het probleem was dat we een foutmelding kregen omdat de tijd van de computer
                //    // aan het driften was, dat geeft dan op een van de api calls een foutmelding waardoor dingen fout
                //    // gaan.
                //}


            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} ERROR position ThreadCheckFinishedPosition thread {error.Message}");
            }
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n ThreadCheckFinishedPosition THREAD EXIT");
    }
}
#endif

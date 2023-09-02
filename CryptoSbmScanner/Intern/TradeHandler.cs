using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

static public class TradeHandler
{
    /// <summary>
    /// Deze routine is een reactie op een gemaakte trade (vanuit de emulator of vanuit de exchange userdata stream)
    /// Momenteel wordt enkel de step, trade en/of positie bijgewerkt, er wordt niet geannuleerd of geplaatst
    /// Omdat deze in BULK (kan) worden aangeroepen worden hier verder geen besissingen gemaakt (denk aan 5 openstaande oco's)
    /// </summary>
    public static async Task HandleTradeAsync(CryptoSymbol symbol,
        CryptoOrderType orderType,
        CryptoOrderSide orderSide,
        CryptoOrderStatus orderStatus,
        CryptoTrade data // een tijdelijke trade voor de interne datatransfer
        )
    {
        using CryptoDatabase databaseThread = new();
        {
            databaseThread.Open();

            string s;
            string msgInfo = $"{symbol.Name} side={orderSide} type={orderType} status={orderStatus} order={data.OrderId} " +
                $"trade={data.TradeId} price={data.Price.ToString0()} quantity={data.Quantity.ToString0()} value={data.QuoteQuantity.ToString0()}";
            //string s = string.Format("handletrade#1 {0}", msgInfo);
            //if (data.TradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest)
            //    s += string.Format(" ({0})", data.TradeAccount.Name);
            //else if (data.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
            //    s += string.Format(" ({0})", data.TradeAccount.Name);
            //// Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
            //if (orderStatus != CryptoOrderStatus.Canceled)
            //    GlobalData.AddTextToLogTab(s);


            // Heerlijk, de oude TradeDash pling is terug ;-)
            if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
            {
                if (GlobalData.Settings.General.SoundTradeNotification)
                    GlobalData.PlaySomeMusic("sound-trade-notification.wav");
            }


            CryptoPosition position = null;
            CryptoPositionPart part = null;
            CryptoPositionStep step = null;

            // Zoek de openstaande positie in het geheugen op
            if (data.TradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
            {
                for (int i = 0; i < positionList.Count; i++)
                {
                    CryptoPosition posTemp = positionList.Values[i];
                    if (posTemp.Orders.TryGetValue(data.OrderId, out step))
                    {
                        position = posTemp;
                        //s = string.Format("handletrade#2 {0} positie gevonden, name={1} id={2} positie.status={3} (memory)",
                        //    msgInfo, step.Name, step.Id, position.Status);
                        // Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
                        //if (orderStatus != CryptoOrderStatus.Canceled)
                        //GlobalData.AddTextToLogTab(s);
                        break;
                    }
                }
            }


            // *********************************************************************
            // Dit is een apart stukje code, WANT: de positie staat niet in het
            // geheugen. De timeout en de buy hebben elkaar gekruist, daarom de
            // positie hier alsnog laden vanuit de database)
            // *********************************************************************
            if (position == null)
            {
                // Controleer via de database of we de positie kunnen vinden
                string sql = string.Format("select * from positionstep where OrderId={0} or Order2Id={1}", data.OrderId, data.OrderId);
                step = databaseThread.Connection.QueryFirstOrDefault<CryptoPositionStep>(sql);
                if (step != null && step.Id > 0)
                {
                    // De positie en child objects alsnog uit de database laden
                    position = databaseThread.Connection.Get<CryptoPosition>(step.PositionId);
                    PositionTools.AddPosition(data.TradeAccount, position);
                    PositionTools.LoadPosition(databaseThread, position);
                    if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                        GlobalData.PositionsHaveChanged("");

                    // De positie terugzetten naar trading (wordt verderop toch opnieuw doorgerekend)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        position.Status = CryptoPositionStatus.Trading;

                    s = string.Format("handletrade#3 {0} step hersteld, name={1} id={2} positie.status={3} (database)", msgInfo, step.Name, step.Id, position.Status);
                    GlobalData.AddTextToLogTab(s);
                }
                else
                {
                    // De step kan niet gevonden worden! We negeren deze order en gaan verder..
                    // We hebben handmatig een order geplaatst (buiten de bot om).

                    // Wel de trades van deze symbol bijwerken (voor de statistiek)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        await ExchangeHelper.FetchTradesAsync(data.TradeAccount, symbol);

                    s = string.Format("handletrade#4 {0} geen step gevonden. Statistiek bijwerken (exit)", msgInfo);
                    GlobalData.AddTextToLogTab(s);

                    // Gebruiker informeren (een trade blijft interessant tenslotte)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        GlobalData.AddTextToTelegram(msgInfo);

                    return;
                }
            }

            //  Bij iedere step hoort een part (maar technisch kan alles)
            part = PositionTools.FindPositionPart(position, step.PositionPartId);
            if (part == null)
                throw new Exception("Probleem met het vinden van een part");

            //s = string.Format("handletrade#5 {0} step gevonden, name={1} id={2} positie.status={3}", msgInfo, step.Name, step.Id, position.Status);
            // Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
            //if (orderStatus != CryptoOrderStatus.Canceled)
            //    GlobalData.AddTextToLogTab(s);


            // *********************************************************************
            // Positie/Part/Step gevonden, nu verder met het afhandelen van de trade
            // *********************************************************************

            // Er is een trade gemaakt binnen deze positie.
            // Synchroniseer de trades en herberekenen het geheel
            // (oh dear: ik herinner me diverse storingen van Binance, vertragingen, enzovoort)
            await PositionTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            PositionTools.CalculatePositionResultsViaTrades(databaseThread, position);


            // Een order kan geannuleerd worden door de gebruiker, en dan gaan we ervan uit dat de gebruiker de hele order overneemt.
            // Hierop reageren door de positie te sluiten (de statistiek wordt gereset zodat het op conto van de gebruiker komt)
            if (orderStatus == CryptoOrderStatus.Canceled)
            {
                // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                if (part.Status == CryptoPositionStatus.TakeOver || part.Status == CryptoPositionStatus.Timeout || step.Status == CryptoOrderStatus.Expired)
                {
                    // Wij, anders was de status van de step niet op expired gezet of de part op timeout gezet
                }
                else
                {
                    // De gebruiker heeft de order geannuleerd, het is nu de verantwoordelijkheid van de gebruiker om het recht te trekken
                    part.Profit = 0;
                    part.Invested = 0;
                    part.Returned = 0;
                    part.Commission = 0;
                    part.Percentage = 0;
                    part.CloseTime = data.TradeTime;
                    //CryptoPositionStatus? oldStatus = part.Status;
                    part.Status = CryptoPositionStatus.TakeOver;
                    //if (oldStatus != part.Status)
                    //    GlobalData.AddTextToLogTab($"{symbol.Name} Debug: positie part status van {oldStatus} naar {part.Status}");
                    PositionTools.SavePositionPart(databaseThread, part);

                    s = string.Format("handletrade#7 {0} positie part cancelled, user takeover? part.status={1}", msgInfo, part.Status);
                    GlobalData.AddTextToLogTab(s);
                    GlobalData.AddTextToTelegram(s);

                    // De gebruiker heeft de order geannuleerd, het is nu de verantwoordelijkheid van de gebruiker om het recht te trekken
                    position.Profit = 0;
                    position.Invested = 0;
                    position.Returned = 0;
                    position.Commission = 0;
                    position.Percentage = 0;
                    position.CloseTime = data.TradeTime;
                    //oldStatus = position.Status;
                    position.Status = CryptoPositionStatus.TakeOver;
                    //if (oldStatus != position.Status)
                    //    GlobalData.AddTextToLogTab($"{symbol.Name} Debug: positie part status van {oldStatus} naar {part.Status}");
                    PositionTools.SavePosition(databaseThread, position);

                    s = string.Format("handletrade#7 {0} positie cancelled, user takeover? position.status={1}", msgInfo, position.Status);
                    GlobalData.AddTextToLogTab(s);
                    GlobalData.AddTextToTelegram(s);
                }
                return;
            }


            // De sell order is uitgevoerd, de positie afmelden
            // Ehh, gebeurde dat dan niet in de herberekening?

            if (step.Side == CryptoOrderSide.Sell && step.Status == CryptoOrderStatus.Filled)
            {
                // We zijn uit deze trade, alles verkocht
                // (wel vanuit een long positie geredeneerd)
                //decimal perc = 0;
                //if (part.BreakEvenPrice > 0)
                //    perc = (decimal)(100 * ((step.AvgPrice / part.BreakEvenPrice) - 1));
                //s = $"handletrade {msgInfo} part sold ({part.Percentage:N2}%)";
                //GlobalData.AddTextToLogTab(s);
                //GlobalData.AddTextToTelegram(s);

                part.CloseTime = data.TradeTime;
                //CryptoPositionStatus? oldStatus = part.Status;
                part.Status = CryptoPositionStatus.Ready;
                //if (oldStatus != part.Status && position.Quantity > 0) // geen 2 meldingen achter elkaar (van de part status en dan de totale positie)
                //    GlobalData.AddTextToLogTab($"{symbol.Name} Debug: Part status van {oldStatus} naar {part.Status}");
                PositionTools.SavePositionPart(databaseThread, part);



                // Sluit de positie indien afgerond
                if (position.Quantity == 0)
                {
                    // We zijn uit deze trade, alles verkocht
                    //s = $"handletrade#8 {msgInfo} positie ready {position.Percentage:N2}";
                    //GlobalData.AddTextToLogTab(s);
                    //GlobalData.AddTextToTelegram(s);

                    position.CloseTime = data.TradeTime;
                    //oldStatus = position.Status;
                    position.Status = CryptoPositionStatus.Ready;
                    PositionTools.SavePosition(databaseThread, position);
                    //if (oldStatus != position.Status)
                    //    GlobalData.AddTextToLogTab($"Debug: position status van {oldStatus} naar {position.Status}");

                    // Annuleer alle openstaande dca orders
                    // DIT IS NIET GOED (Want in Papertrading MEERDERE DCA'S DIE TEGELIJK VERKOCHT WORDEN!)
                    // stel dat de prijs sterk gedaald is dan zijn er X DCA's, en als alleen die laatste verkocht wordt als JoJo dan is dit niet goed
                    //foreach (CryptoPositionPart partX in position.Parts.Values.ToList())
                    //{
                    //    if (partX.Quantity == 0)
                    //    {
                    //        partX.CloseTime = data.TradeTime;
                    //        partX.Status = CryptoPositionStatus.Ready;
                    //        PositionTools.SavePositionPart(databaseThread, partX);

                    //        foreach (CryptoPositionStep stepX in partX.Steps.Values.ToList())
                    //        {
                    //            // Annuleer de openstaande orders indien gevuld (voor alle DCA's)
                    //            if (stepX.Side == CryptoOrderSide.Buy && (stepX.Status == CryptoOrderStatus.New))
                    //            {
                    //                stepX.CloseTime = data.TradeTime;
                    //                stepX.Status = CryptoOrderStatus.Expired;
                    //                PositionTools.SavePositionStep(databaseThread, position, stepX);
                    //                var (cancelled, cancelParams) = await Api.Cancel(data.TradeAccount, symbol, stepX);
                    //                //CancelOrder(databaseThread, data.TradeAccount, symbol, partX, stepX); oeps, is static
                    //                Api.Dump(symbol, cancelled, cancelParams, "annuleer alle dca's");
                    //            }
                    //        }
                    //    }
                    //}
                    // In de veronderstelling dat dit allemaal lukt
                    position.Symbol.LastTradeDate = DateTime.UtcNow;
                    databaseThread.Connection.Update<CryptoSymbol>(position.Symbol);
                }


                // Experiment: Dit achterwege laten, dit wordt nu via de 1m candle wel opgepakt (duurt wat langer)
                // De gedachte is hierbij dat als we meerdere OCO"S of sell's op hetzelfde tijdstip ontvangen dat
                // we niet alle sell's opnieuw gaan (her) plaatsen vanwege een gewijzigde BE (beetje te knullig)
                // Wellicht willen we dit sneller doen en na circa 5 seconden alsnog iets willen doen (1m is lang)

                // Het idee is om de monitoring aan te roepen voor het plaatsen van een sell e.d.
                // Probleem: Hoe kom je vanuit hier naar de laatste candle?
                // TODO - controleren of dit wel de juiste candle is.
                //CryptoCandle candle1m = null;
                //long candleOpenTimeInterval = CandleTools.GetUnixTime(data.EventTime, 60);
                //if (!symbol.CandleList.TryGetValue(candleOpenTimeInterval, out candle1m))
                //    symbol.CandleList.TryGetValue(candleOpenTimeInterval - 60, out candle1m);
                //PositionMonitor positionMonitor = new(position.Symbol, candle1m);
                //GlobalData.AddTextToLogTab(position.Symbol.Name + " Debug: positionMonitor.HandlePosition met herplaatsing sells");
                //await positionMonitor.HandlePosition(tradeAccount, databaseThread, position, true);

                position.Reposition = true;
                PositionTools.SavePosition(databaseThread, position);

                //s = "";
                if (position.Status == CryptoPositionStatus.Ready)
                    s = $"handletrade {msgInfo} position ready ({position.Percentage:N2}%)";
                else
                    //if (part.Status == CryptoPositionStatus.Ready)
                    s = $"handletrade {msgInfo} part sold ({part.Percentage:N2}%)";
                GlobalData.AddTextToLogTab(s);
                GlobalData.AddTextToTelegram(s);


                // Vanwege deadlock problemen afgesterd, uitzoeken! Via papertrading zal er nooit een probleem
                // optreden (dat loopt geheel synchroon). Met de user-data stream en 1m candle stream zou het
                // wel kunnen conflicteren. VOOR de aanroep naar deze code moet er gelocked worden!

                // Is er een openstaande positie (via de openstaande posities in het geheugen)
                // NB: Dit gedeelte kan wat mij betreft vervallen (omdat de query ook gedaan wordt)
                if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
                {
                    // Lukt dat wel? Wat is de 1m candle enzovoort?

                    //await tradeAccount.PositionListSemaphore.WaitAsync();
                    //try
                    //{
                    //}
                    //finally
                    //{
                    //    //tradeAccount.PositionListSemaphore.Release();
                    //}
                }

                return;
            }


            if (step.Side == CryptoOrderSide.Buy && step.Status == CryptoOrderStatus.Filled)
            {
                s = $"handletrade {msgInfo} part buy";
                GlobalData.AddTextToLogTab(s);
                GlobalData.AddTextToTelegram(s);

                position.Reposition = true;
                PositionTools.SavePosition(databaseThread, position);
                return;
            }

            return;
        }
    }

}

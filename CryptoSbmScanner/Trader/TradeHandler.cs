using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Trader;

#if TRADEBOT
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
            
            // Extra logging ingeschakeld
            s = string.Format("handletrade#1 {0}", msgInfo);
            if (data.TradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest)
                s += string.Format(" ({0})", data.TradeAccount.Name);
            else if (data.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                s += string.Format(" ({0})", data.TradeAccount.Name);
            // Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
            if (orderStatus != CryptoOrderStatus.Canceled)
                GlobalData.AddTextToLogTab(s);



            // De oude TradeDash pling....
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
                        s = $"handletrade#2 {msgInfo} positie gevonden, name={step.Side} id={step.Id} positie.status={position.Status} (memory)";
                        // Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
                        if (orderStatus != CryptoOrderStatus.Canceled)
                           GlobalData.AddTextToLogTab(s);
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
                // Controleer via de database of we de positie kunnen vinden (opnieuw inladen van de positie)
                string sql = string.Format("select * from positionstep where OrderId=@OrderId or Order2Id=@OrderId");
                step = databaseThread.Connection.QueryFirstOrDefault<CryptoPositionStep>(sql, new { data.OrderId });
                if (step != null && step.Id > 0)
                {
                    // De positie en child objects alsnog uit de database laden
                    position = databaseThread.Connection.Get<CryptoPosition>(step.PositionId);

                    // Na het sluiten van de positie annuleren we tevens de (optionele) openstaande DCA order.
                    // Deze code veroorzaakt nu een reload van de positie en dat is niet echt nodig.
                    if (orderStatus == CryptoOrderStatus.Canceled && position.CloseTime.HasValue)
                    {
                        s = string.Format("handletrade#3.1 {0} step gevonden, name={1} id={2} positie.status={3} (cancel + positie gesloten)", msgInfo, step.Side, step.Id, position.Status);
                        GlobalData.AddTextToLogTab(s);
                        return;
                    }


                    // De positie en child objects alsnog uit de database laden
                    PositionTools.AddPosition(data.TradeAccount, position);
                    PositionTools.LoadPosition(databaseThread, position);
                    if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                        GlobalData.PositionsHaveChanged("");

                    // De positie terugzetten naar trading (wordt verderop toch opnieuw doorgerekend)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        position.Status = CryptoPositionStatus.Trading;

                    s = string.Format("handletrade#3.2 {0} step hersteld, name={1} id={2} positie.status={3} (database)", msgInfo, step.Side, step.Id, position.Status);
                    GlobalData.AddTextToLogTab(s);
                }
                else
                {
                    // De step kan niet gevonden worden! We negeren deze order en gaan verder..
                    // We hebben handmatig een order geplaatst (buiten de bot om).

                    // Wel de trades van deze symbol bijwerken (voor de statistiek)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        await ExchangeHelper.FetchTradesAsync(data.TradeAccount, symbol);

                    s = $"handletrade#4 {msgInfo} geen step gevonden. Statistiek bijwerken (exit)";
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

            s = $"handletrade#5 {msgInfo} step gevonden, name={step.Side} id={step.Id} positie.status={position.Status}";
            // Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
            if (orderStatus != CryptoOrderStatus.Canceled)
                GlobalData.AddTextToLogTab(s);


            // *********************************************************************
            // Positie+Part+Step gevonden, nu verder met het afhandelen van de trade
            // *********************************************************************

            
            //Monitor.Enter(position);
            try
            {
                // Er is een trade gemaakt binnen deze positie c.q. annuleren.

                // Synchroniseer de trades en herberekenen het geheel
                // (oh dear: ik herinner me diverse storingen van Binance, vertragingen, enzovoort)
                // --> en oh dear, jawel, ook op bybit futures kan de timing fout gaan!
                await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position, false);
                await ExchangeHelper.FetchTradesForOrderAsync(position.TradeAccount, position.Symbol, step.OrderId);
                TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);


                // Een order kan geannuleerd worden door de gebruiker, en dan gaan we ervan uit dat de gebruiker de hele order overneemt.
                // Hierop reageren door de positie te sluiten (de statistiek wordt gereset zodat het op conto van de gebruiker komt)
                if (orderStatus == CryptoOrderStatus.Canceled)
                {
                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    if (step.CancelInProgress || step.Status > CryptoOrderStatus.Canceled)
                    {
                        // Wij hebben de order geannuleerd via de CancelStep/CancelOrder/Status
                        // Probleem is dat de step.Status pas na het annuleren wordt gezet en bewaard. 
                        // Geconstateerd: een cancel via de user stream kan (te) snel gaan

                        // NB: Er is nu wat overlappende code door die CancelInProgress
                    }
                    else
                    {
                        // De gebruiker heeft de order geannuleerd, het is vanaf nu zijn/haar verantwoordelijkheid...
                        // Om de statistieken niet te beinvloeden zetten we alles op 0
                        part.Profit = 0;
                        part.Invested = 0;
                        part.Returned = 0;
                        part.Commission = 0;
                        part.Percentage = 0;
                        part.CloseTime = data.TradeTime;
                        databaseThread.Connection.Update<CryptoPositionPart>(part);

                        //s = string.Format("handletrade#7 {0} positie part cancelled, user takeover?", msgInfo);
                        //GlobalData.AddTextToLogTab(s);
                        //GlobalData.AddTextToTelegram(s);

                        // De gebruiker heeft de positie geannuleerd
                        position.Profit = 0;
                        position.Invested = 0;
                        position.Returned = 0;
                        position.Commission = 0;
                        position.Percentage = 0;
                        position.CloseTime = data.TradeTime;
                        if (!position.CloseTime.HasValue)
                            position.CloseTime = DateTime.UtcNow;

                        position.Status = CryptoPositionStatus.TakeOver;
                        databaseThread.Connection.Update<CryptoPosition>(position);

                        s = $"handletrade#7 {msgInfo} positie cancelled, user takeover? position.status={position.Status}";
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);
                    }
                    return;
                }



                // Discussie, willen we dit op deze plek of zetten we het centraal?
                // Het enige wat zinvol is de cancel, de rest gaat toch via de trades.
                // De rest van de controles doen we tevens bij het opstarten en ieder uur.
                // 1 thread voor de controles, locking minder relevant, en duidelijker??


                if (step.Status == CryptoOrderStatus.Filled)
                {
                    CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
                    if (step.Side == takeProfitOrderSide)
                    {
                        // De take profit order is uitgevoerd, de positie afmelden
                        //s = $"handletrade {msgInfo} part takeprofit ({part.Percentage:N2}%)";
                        //GlobalData.AddTextToLogTab(s);
                        //GlobalData.AddTextToTelegram(s);

                        part.CloseTime = data.TradeTime;
                        databaseThread.Connection.Update<CryptoPositionPart>(part);

                        // Sluit de positie indien afgerond
                        if (position.Quantity == 0)
                        {
                            // We zijn uit deze trade, alles verkocht
                            s = $"handletrade#8 {msgInfo} positie ready {position.Percentage:N2}";
                            GlobalData.AddTextToLogTab(s);
                            //GlobalData.AddTextToTelegram(s);

                            position.CloseTime = data.TradeTime;
                            if (position.Invested > 0)
                            {
                                // Gevaarlijk, als er een buy niet gedetecteerd is dan wordt de trade zomaar afgesloten
                                // En dat lijkt soms wel te gebeuren vanwege de exchange, internet of datetime perikelen.
                                position.Status = CryptoPositionStatus.Ready;
                                GlobalData.ThreadDoubleCheckPosition.AddToQueue(position);
                            }
                            databaseThread.Connection.Update<CryptoPosition>(position);

                            // In de veronderstelling dat dit allemaal lukt
                            position.Symbol.LastTradeDate = position.CloseTime;
                            databaseThread.Connection.Update<CryptoSymbol>(position.Symbol);
                        }

                        position.Reposition = true;
                        databaseThread.Connection.Update<CryptoPosition>(position);

                        //s = "";
                        if (position.Status == CryptoPositionStatus.Timeout)
                            s = $"handletrade {msgInfo} position timeout ({position.Percentage:N2}%)";
                        else if (position.Status == CryptoPositionStatus.Ready)
                            s = $"handletrade {msgInfo} position ready ({position.Percentage:N2}%)";
                        else
                            s = $"handletrade {msgInfo} part takeprofit ({part.Percentage:N2}%)";
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);
                        return;
                    }


                    CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                    if (step.Side == entryOrderSide)
                    {
                        s = $"handletrade {msgInfo} part entry";
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);

                        position.Reposition = true;
                        databaseThread.Connection.Update<CryptoPosition>(position);
                        return;
                    }
                }
            }
            finally
            {
                //Monitor.Exit(position);
            }

            // Vanwege deadlock problemen afgesterd, uitzoeken! Via papertrading zal er nooit een probleem
            // optreden (dat loopt geheel synchroon). Met de user-data stream en 1m candle stream zou het
            // wel kunnen conflicteren. VOOR de aanroep naar deze code moet er gelocked worden!

            // Is er een openstaande positie (via de openstaande posities in het geheugen)
            // NB: Dit gedeelte kan wat mij betreft vervallen (omdat de query ook gedaan wordt)
            //if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
            //{
            // Lukt dat wel? Wat is de 1m candle enzovoort?

            //await tradeAccount.PositionListSemaphore.WaitAsync();
            //try
            //{
            // call................. 
            //}
            //finally
            //{
            //    //tradeAccount.PositionListSemaphore.Release();
            //}
            //}
            return;
        }
    }
}
#endif

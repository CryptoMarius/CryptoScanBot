using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using CryptoSbmScanner.Trading;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;


public class TradeParams
{
    // standaard buy of sell
    public CryptoTradeDirection Side { get; set; }
    public CryptoOrderType OrderType { get; set; }
    public long OrderId { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuoteQuantity { get; set; }
    public DateTime CreateTime { get; set; }

    // OCO gerelateerd
    public decimal? StopPrice { get; set; }
    public decimal? LimitPrice { get; set; }
    public long? Order2Id { get; set; }
    public long? OrderListId { get; set; }
}

public class PositionMonitor
{
    // Tellertje die getoond wordt in applicatie (indicatie van aantal meldingen)
    public static int AnalyseCount = 0;

    //public CryptoTradeAccount TradeAccount { get; set; }

    // Symbol/Exchange
    public CryptoSymbol Symbol { get; set; }
    public Model.CryptoExchange Exchange { get; set; }

    // De laatste gesloten 1m candle
    public CryptoCandle LastCandle1m { get; set; }
    // De sluittijd van deze candle (als unixtime) - De CurrentTime bij backtesting
    public long LastCandle1mCloseTime { get; set; } 
    // De sluittijd van deze candle (als DateTime) - De CurrentTime bij backtesting
    public DateTime LastCandle1mCloseTimeDate { get; set; }



    public PositionMonitor(CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        //TradeAccount = tradeAccount; //CryptoTradeAccount tradeAccount, 
        Symbol = symbol;
        Exchange = symbol.Exchange;

        // De laatste 1m candle die definitief is
        LastCandle1m = lastCandle1m;
        LastCandle1mCloseTime = lastCandle1m.OpenTime + 60; 
        LastCandle1mCloseTimeDate = CandleTools.GetUnixDate(LastCandle1mCloseTime); 
    }



    public static async Task HandleTradeAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, BinanceStreamOrderUpdate data)
    {
        // Wat een chaotische code? - verbeteren? enkel via Database of geheugen zoeken, reduceren?

        using (CryptoDatabase databaseThread = new())
        {
            /* ExecutionType: New = 0, Canceled = 1, Replaced = 2, Rejected = 3, Trade = 4, Expired = 5 */
            /* OrderStatus:  New = 0, PartiallyFilled = 1, Filled = 2, Canceled = 3, PendingCancel = 4, Rejected = 5, Expired = 6, Insurance = 7, Adl = 8 */
            string msgInfo = string.Format("{0} side={1} type={2} status={3} order={4} price={5} quantity={6} {7}=filled={8} /{9}", data.Symbol, data.Side, data.Type, data.Status,
                data.Id, data.Price.ToString0(), data.Quantity.ToString0(), symbol.Quote, data.QuoteQuantityFilled.ToString0(), data.QuoteQuantity.ToString0());

            // We zijn slechts geinteresseerd in een paar statussen. want de andere zijn niet interessant voor de order afhandeling,
            // het wordt enkel interessant na filled, partiallyfilled of Canceled! Nieuwe orders lijken mij niet interessant.
            // Opmerking - observatie: Daar wordt de log aardig wat rustiger van, heerlijk!
            string s = string.Format("handletrade#1 {0}", msgInfo);
            if (tradeAccount.AccountType == CryptoTradeAccountType.BackTest)
                s += " (backtest)";
            else if (tradeAccount.AccountType == CryptoTradeAccountType.PaperTrade)
                s += " (paper)";
            if (!((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled) || (data.Status == OrderStatus.Canceled)))
            {
                GlobalData.AddTextToLogTab(s + " (ignored1)");
                return;
            }
            // Geannuleerde sell opdrachten (vanwege het annuleren van een stop-limit of OCO)
            if ((data.Side == OrderSide.Sell) && (data.Status == OrderStatus.Expired))
            {
                // volgens mij komen we hier nooit? (want status=expired wordt al gefilterd) zie (ignored1)
                GlobalData.AddTextToLogTab(s + " (cancelled sell, ignored)");
                return;
            }
            // Geannuleerde buy opdrachten (vanwege het annuleren van een stop-limit of OCO)
            if ((data.Side == OrderSide.Buy) && (data.Status == OrderStatus.Expired))
            {
                // volgens mij komen we hier nooit? (want status=expired wordt al gefilterd) zie (ignored1)
                GlobalData.AddTextToLogTab(s + " (cancelled buy, ignored)");
                return;
            }
            GlobalData.AddTextToLogTab(s);


            // Is er een openstaande positie (via de openstaande posities in het geheugen)
            // NB: Dit gedeelte kan wat mij betreft vervallen (omdat de query ook gedaan wordt)
            await tradeAccount.PositionListSemaphore.WaitAsync();
            try
            {

                // Een ping laten horen
                if ((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled))
                {
                    if (GlobalData.Settings.General.SoundTradeNotification)
                        GlobalData.PlaySomeMusic("Tradedash - Notification.wav");
                }


                databaseThread.Close();
                databaseThread.Open();

                CryptoPosition position = null;
                CryptoPositionPart part = null;
                CryptoPositionStep step = null;

                // Hoe kom je vanuit hier naar de laatste candle?
                long LastCandle1mCloseTime = CandleTools.GetUnixTime(data.EventTime, 60);
                DateTime LastCandle1mCloseTimeDate = CandleTools.GetUnixDate(LastCandle1mCloseTime);
                PositionTools positionTools = new(tradeAccount, symbol, LastCandle1mCloseTimeDate);


                // Er kunnen meerdere posities bij de munt open staan
                if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
                {
                    for (int i = 0; i < positionList.Count; i++)
                    {
                        CryptoPosition posTemp = positionList.Values[i];
                        if (posTemp.Orders.TryGetValue(data.Id, out step))
                        {
                            position = posTemp;

                            s = string.Format("handletrade#2 {0} positie gevonden, name={1} id={2} positie.status={3} (memory)", msgInfo, step.Name, step.Id, position.Status);
                            GlobalData.AddTextToLogTab(s);
                            break;
                        }
                    }
                }


                // De positie staat niet in het geheugen (de timeout en de buy hebben elkaar wellicht gekruist op Binance, alsnog laden!)
                if (position == null)
                {
                    // TODO - Dit is nog helemaal niet bijgewerkt, combineren met de code in de ThreadLoadData AUB

                    // Controleer via de database of we een positie niet alsnog moeten inladen
                    string sql = string.Format("select * from positionstep where OrderId={0} or Order2Id={1}", data.Id, data.Id);
                    step = databaseThread.Connection.QueryFirstOrDefault<CryptoPositionStep>(sql);
                    if (step != null && step.Id > 0)
                    {
                        // De gevonden positie en stappen alsnog uit de database laden
                        position = databaseThread.Connection.Get<CryptoPosition>(step.PositionId);
                        PositionTools.AddPosition(tradeAccount, position); // Leuk, MAAR ALLE PARTS ONTBREKEN ;-)

                        sql = string.Format("select * from positionstep where PositionId={0} order by id", position.Id);
                        foreach (CryptoPositionStep stepX in databaseThread.Connection.Query<CryptoPositionStep>(sql))
                        {
                            // Order index opbouwen
                            if (stepX.OrderId.HasValue)
                                position.Orders.Add((long)stepX.OrderId, stepX);
                            if (stepX.Order2Id.HasValue)
                                position.Orders.Add((long)stepX.Order2Id, stepX);
                        }

                        if ((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled))
                        {
                            CryptoPositionStatus? oldStatus = position.Status;
                            position.Status = CryptoPositionStatus.Trading;
                            GlobalData.AddTextToLogTab(String.Format("Debug#3: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));
                        }

                        s = string.Format("handletrade#3 {0} step hersteld, name={1} id={2} positie.status={3} (database)", msgInfo, step.Name, step.Id, position.Status);
                        GlobalData.AddTextToLogTab(s);
                    }
                    else
                    {
                        // De trades van deze coin laten bijwerken (voor de statistiek)
                        if ((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled))
                        {
                            // Haal de trades van deze coin op (voor statistiek)
                            await Task.Run(async () => { await BinanceFetchTrades.FetchTradesForSymbol(tradeAccount, symbol); }); // wachten tot deze klaar is
                        }

                        s = string.Format("handletrade#4 {0} geen step gevonden. Statistiek bijwerken (exit)", msgInfo);
                        GlobalData.AddTextToLogTab(s);

                        // Wellicht optioneel?
                        if ((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled))
                            GlobalData.AddTextToTelegram(msgInfo);

                        return;
                    }
                }


                part = PositionTools.FindPositionPart(position, step.PositionPartId);
                if (part == null)
                    throw new Exception("Probleem met het vinden van een part (dat zou onmogelijk moeten zijn, maar voila)");


                s = string.Format("handletrade#5 {0} step gevonden, name={1} id={2} positie.status={3}", msgInfo, step.Name, step.Id, position.Status);
                GlobalData.AddTextToLogTab(s);


                // *********************************************************************
                // Positie/Part/Step gevonden, nu verder met het afhandelen van de trade
                // *********************************************************************


                // Er is een trade gemaakt die belangrijk voor deze posities is
                // Synchroniseer de trades voor de break-even point en winst.
                // Laad tevens de steps als deze nog niet geladen zijn.
                await PositionTools.LoadTradesfromDatabaseAndBinance(databaseThread, position);
                {
                    if (data.Status == OrderStatus.Filled && !symbol.TradeList.TryGetValue(data.TradeId, out CryptoTrade tradeLast))
                    {
                        // Deze is nog niet bewaard in de database, maar dat geeft niet
                        tradeLast = new CryptoTrade();
                        Helper.PickupTrade(symbol, tradeLast, data);
                        tradeLast.TradeAccount = tradeAccount;
                        tradeLast.TradeAccountId = tradeAccount.Id;
                        symbol.TradeList.Add(tradeLast.TradeId, tradeLast);

                        s = string.Format("handletrade#6 {0} missende trade, toegevoegd!", msgInfo);
                        GlobalData.AddTextToLogTab(s);
                    }
                }
                // Herberekenen
                PositionTools.CalculatePositionViaTrades(databaseThread, position);


                // Altijd de step bijwerken (deze informatie krijgen we eenmalig)
                if (step.Status != OrderStatus.Expired)
                    step.Status = data.Status;
                step.QuantityFilled = data.QuantityFilled;
                step.QuoteQuantityFilled = data.QuoteQuantityFilled;
                if (data.Status >= OrderStatus.Filled)
                {
                    // Hmmm, welke van deze twee is er nu gevuld?
                    step.CloseTime = data.UpdateTime;
                    if (!step.CloseTime.HasValue)
                        step.CloseTime = data.EventTime;
                }
                databaseThread.Connection.Update<CryptoPositionStep>(step);




                //// Even het aantal vermelden (dat was een goed hulpje bij het uitzoeken)
                //if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
                //{
                //    if (exchange.AssetList.TryGetValue(position.Symbol.Base, out CryptoAsset asset))
                //    {
                //        GlobalData.AddTextToLogTab(string.Format("Available asset {0} {1} Free={2}", asset.Quote, asset.Total.ToString0(), asset.Free.ToString0()));
                //    }
                //}


                // Een order kan geannuleerd worden door de gebruiker, en dan gaan we ervan uit dat de gebruiker de order overneemt.
                // Hierop reageren door de positie te sluiten (de statistiek wordt gereset zodat het op conto van de gebruiker komt)
                if (data.Status == OrderStatus.Canceled)
                {
                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    //if ((position.Status == CryptoPositionStatus.positionPending) || (position.Status == CryptoPositionStatus.positionWaiting))
                    //{
                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    if ((part.Status == CryptoPositionStatus.TakeOver) || (part.Status == CryptoPositionStatus.Timeout) || (step.Status == OrderStatus.Expired))
                    {
                        // Wij! Anders was de status niet op expired gezet of de positie op timeout gezet
                        // Eventueel kunnen we deze open laten staan als de step.QuantityFilled niet 0 is?
                        //if (!step.CloseTime.HasValue)
                        //  step.CloseTime = data.EventTime;
                    }
                    else
                    {
                        // De gebruiker heeft de order geannuleerd, het is nu de verantwoordelijkheid van de gebruiker om het recht te trekken
                        part.Profit = 0;
                        part.Invested = 0;
                        part.Percentage = 0;
                        part.CloseTime = data.EventTime;
                        //CryptoPositionStatus? oldStatus = part.Status;
                        part.Status = CryptoPositionStatus.TakeOver;
                        //if (oldStatus != part.Status)
                          //  GlobalData.AddTextToLogTab(String.Format("Debug: positie part status van {0} naar {1}", oldStatus.ToString(), part.Status.ToString()));

                        s = string.Format("handletrade#7 {0} positie part cancelled, user takeover? part.status={1}", msgInfo, part.Status);
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);


                        // De gebruiker heeft de order geannuleerd, het is nu de verantwoordelijkheid van de gebruiker om het recht te trekken
                        position.Profit = 0;
                        position.Invested = 0;
                        position.Percentage = 0;
                        position.CloseTime = data.EventTime;
                        //oldStatus = position.Status;
                        position.Status = CryptoPositionStatus.TakeOver;
                        //if (oldStatus != position.Status)
                          //  GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                        s = string.Format("handletrade#7 {0} positie cancelled, user takeover? position.status={1}", msgInfo, position.Status);
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);
                    }


                    PositionTools.HandleAdministration(databaseThread, position);
                    return;
                }


                // Zorg dat de status van de bovenliggende part en position een update krijgen
                if (position.Mode == CryptoTradeDirection.Long)
                {
                    if (step.Mode == CryptoTradeDirection.Long && part.Invested > 0 && part.Status == CryptoPositionStatus.Waiting)
                    {
                        part.Status = CryptoPositionStatus.Trading;
                        databaseThread.Connection.Update<CryptoPositionPart>(part);
                    }
                    if (step.Mode == CryptoTradeDirection.Long && position.Invested > 0 && position.Status == CryptoPositionStatus.Waiting)
                    {
                        position.Status = CryptoPositionStatus.Trading;
                        databaseThread.Connection.Update<CryptoPosition>(position);
                    }
                }


                // De sell order is uitgevoerd, de positie afmelden
                if (step.Mode == CryptoTradeDirection.Short && data.Status == OrderStatus.Filled)
                {
                    // We zijn uit deze ene trade, alles verkocht
                    s = string.Format("handletrade#8 {0} positie part sold", msgInfo);
                    GlobalData.AddTextToLogTab(s);
                    GlobalData.AddTextToTelegram(s);

                    part.CloseTime = data.EventTime;
                    //CryptoPositionStatus? oldStatus = part.Status;
                    part.Status = CryptoPositionStatus.Ready;
                    //if (oldStatus != part.Status)
                      //  GlobalData.AddTextToLogTab(String.Format("Debug: position part status van {0} naar {1}", oldStatus.ToString(), part.Status.ToString()));
                    databaseThread.Connection.Update<CryptoPositionPart>(part);

                    PositionTools.HandleAdministration(databaseThread, position);


                    // Sluit de positie indien afgerond
                    if (position.Quantity == 0)
                    {
                        // We zijn uit deze trade, alles verkocht
                        s = string.Format("handletrade#8 {0} positie ready", msgInfo);
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);

                        position.CloseTime = data.EventTime;
                        //oldStatus = position.Status;
                        position.Status = CryptoPositionStatus.Ready;
                        //if (oldStatus != position.Status)
                          //  GlobalData.AddTextToLogTab(String.Format("Debug: position status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                        // Annuleer openstaande dca orders
                        // Dat komt niet goed? (wat ben ik eigenlijk aan het doen, specificaties?)
                        // stel dat de prijs sterk gedaald is dan zijn er X DCA's, en als alleen die laatste verkocht wordt als JoJo dan is dit niet goed
                        foreach (CryptoPositionPart partX in position.Parts.Values)
                        {
                            if (partX.Quantity == 0)
                            {
                                foreach (CryptoPositionStep stepX in partX.Steps.Values)
                                {
                                    // Annuleer de openstaande orders indien gevuld (voor alle DCA's)
                                    if (stepX.Mode == CryptoTradeDirection.Long && (stepX.Status == OrderStatus.New))
                                    {
                                        stepX.CloseTime = data.EventTime;
                                        stepX.Status = OrderStatus.Expired;
                                        databaseThread.Connection.Update<CryptoPositionStep>(stepX);
                                        await BinanceApi.Cancel(tradeAccount, symbol, position, stepX.OrderId);
                                    }
                                }
                            }
                        }

                        PositionTools.HandleAdministration(databaseThread, position);
                    }



                    // Niet zomaar een laatste candle nemen in verband met Backtesting
                    // TODO - controle of dit echt wel klopt (lijkt onbetrouwbaar?)
                    CryptoCandle candle1m = null;
                    long candleOpenTimeInterval = CandleTools.GetUnixTime(data.EventTime, 60);
                    if (!symbol.CandleList.TryGetValue(candleOpenTimeInterval, out candle1m))
                        symbol.CandleList.TryGetValue(candleOpenTimeInterval - 60, out candle1m);
                    PositionMonitor positionMonitor = new(position.Symbol, candle1m);

                    await positionMonitor.HandlePosition(tradeAccount, databaseThread, position, true);
                    return;
                }

                return;
            }
            finally
            {
                tradeAccount.PositionListSemaphore.Release();
            }
        }
    }


    ///// <summary>
    ///// Controleer de openstaande posities van deze symbol
    ///// </summary>
    ///// <param name="symbol"></param>
    //public static async Task CheckOpenPositions(CryptoSymbol symbol)
    //{
    //    using (CryptoDatabase databaseThread = new())
    //    {
    //        databaseThread.Open();

    //        await symbol.Exchange.PositionListSemaphore.WaitAsync();
    //        try
    //        {
    //            // Er kunnen meerdere posities bij deze munt open staan
    //            if (symbol.Exchange.PositionList.TryGetValue(symbol.Name, out var positionList))
    //            {
    //                // DONE: Niet bijkopen als de barometer te laag staat
    //                // DONE: Niet bijkopen als er gepauzeerd is vanwege BTC drop

    //                // Controleren van order(s)
    //                // GET /api/v3/order (HMAC SHA256)        = Check an order's status.
    //                // GET /api/v3/openOrders  (HMAC SHA256)  = Current open orders (USER_DATA)
    //                // GET /api/v3/allOrders (HMAC SHA256)    = All orders (USER_DATA)

    //                for (int i = positionList.Values.Count - 1; i >= 0; i--)
    //                {
    //                    //GlobalData.AddTextToBarometerTab("Monitor position " + symbol.Name + " (debug)");

    //                    CryptoPosition position = positionList.Values[i];
    //                    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(position.Interval.IntervalPeriod);
    //                    SortedList<long, CryptoCandle> intervalCandles = symbolInterval.CandleList;
    //                    CryptoCandle candleLast = intervalCandles.Values.Last();


    //                    // Vraag: Is er reeds gekocht? 
    //                    // Nee, dan na zoveel minuten annuleren (mits bb%, barometer enzovoort)
    //                    // Ja: Dan na zoveel minuten bijkopen (mits bb%, barometer enzovoort)

    //                    if (position.Status == CryptoPositionStatus.Waiting)
    //                    {
    //                        bool removePosition = false;


    //                        // Controleer de 1h barometer
    //                        decimal? Barometer1h = symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h].PriceBarometer;
    //                        if (Barometer1h.HasValue && (Barometer1h <= GlobalData.Settings.Trading.Barometer01hBotMinimal))
    //                        {
    //                            removePosition = true;
    //                            GlobalData.AddTextToLogTab(string.Format("Monitor {0} position (barometer negatief {1} ) REMOVE start", symbol.Name, Barometer1h, GlobalData.Settings.Trading.Barometer01hBotMinimal));
    //                        }


    //                        // Als BTC snel gedaald is
    //                        if (!removePosition && GlobalData.Settings.Trading.PauseTradingUntil >= DateTime.UtcNow)
    //                        {
    //                            removePosition = true;
    //                            GlobalData.AddTextToLogTab(string.Format("Monitor position de bot is gepauseerd omdat {0} (REMOVE start)", GlobalData.Settings.Trading.PauseTradingText));
    //                        }


    //                        // Is de order ouder dan X minuten dan deze verwijderen
    //                        if (!removePosition && position.CreateTime.AddSeconds(GlobalData.Settings.Trading.GlobalBuyRemoveTime * symbolInterval.Interval.Duration) < DateTime.UtcNow)
    //                        {
    //                            removePosition = true;
    //                            GlobalData.AddTextToLogTab("Monitor position (buy expired) REMOVE " + symbol.Name + " start");
    //                        }


    //                        if (removePosition)
    //                        {
    //                            // Statistieken resetten zodat we makkelijk kunnen tellen & presenteren
    //                            position.Profit = 0;
    //                            position.Invested = 0;
    //                            position.Percentage = 0;
    //                            position.CloseTime = DateTime.UtcNow;
    //                            CryptoPositionStatus? oldStatus = position.Status;
    //                            position.Status = CryptoPositionStatus.Timeout;
    //                            databaseThread.Connection.Update<CryptoPosition>(position);
    //                            if (oldStatus != position.Status)
    //                                GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

    //                            // Annuleer de openstaande buy order
    //                            foreach (CryptoPositionPart part in position.Parts.Values)
    //                            {
    //                                foreach (CryptoPositionStep step in part.Steps.Values)
    //                                {
    //                                    if (step.Mode == TradeDirection.Long && step.Status == OrderStatus.New)
    //                                    {
    //                                        string text = string.Format("{0} POSITION BUY ORDER removed {1} {2} {3}", symbol.Name,
    //                                            step.Price.ToString0(), step.Quantity.ToString0(), (step.Price * step.Quantity).ToString0());
    //                                        GlobalData.AddTextToLogTab(text);
    //                                        GlobalData.AddTextToTelegram(text);

    //                                        WebCallResult<BinanceOrderBase> result = await BinanceApi.Cancel(position.PaperTrade, symbol, position, step.OrderId);
    //                                        if (result == null)
    //                                        {
    //                                            // Geen geldige order? De cancel kan er niets mee
    //                                            step.Status = OrderStatus.Expired;
    //                                            step.CloseTime = position.CloseTime;
    //                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
    //                                        }
    //                                        else if (result.Success)
    //                                        {
    //                                            // Gelukt!
    //                                            step.Status = OrderStatus.Expired;
    //                                            step.CloseTime = position.CloseTime;
    //                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
    //                                        }
    //                                        else if (!result.Success)
    //                                        {
    //                                            // tsja, dat weet ik niet, de cancel logt zelf al het een en ander
    //                                            // vervelend is dat de positie de status timeout heeft gekregen.
    //                                            // maar goed, timing issues gebeuren toch niet, right.. todo!
    //                                        }
    //                                    }
    //                                }
    //                            }

    //                            // Direct weghalen (naar gesloten posities voor de display)
    //                            PositionTools.RemovePosition(position);
    //                        }

    //                    }
    //                    else if (position.Status == CryptoPositionStatus.Trading)
    //                    {
    //                        // We zitten dus in een trade (volledig, maar de niet volledige zijn nog wel een probleem!)
    //                        // We willen niet direct bijkopen maar met een vertraging bijkopen (voorkomen dat alle dca's in 1x worden gekocht).
    //                        // Dit voorkomt dat we bij een plotselinge koersdaling alle DCA's in 1m wordt uitgevoerd en dan ook nog eens een 
    //                        // stopLimit raken (we raken dus een "kleiner" gedeelte van onze investering kwijt).

    //                        // Even een quick fix voor de barometer
    //                        // Geobserveerd dat ondanks dat de barometer heel erg hoog staat er een market order wordt aanbevolen!
    //                        // Onderstaand werkt dus niet goed, of de GlobalData.Settings.QuoteCoins heeft plotseling geen BNB en BTC meer? 
    //                        // Dat zou wel erg raar zijn, melding ingebouwd!

    //                        // Annuleer openstaande DCA orders als de barometer TE negatief is
    //                        // TODO: Willen we dit wel? (de backtest laat zien dat het niet nodig is)

    //                        // TODO - Vandaag ging BTC 5% naar beneden en alle 10 OCO's orders raakten de StopLimit
    //                        // De vraag is dus of we in zo'n situatoe de OCO niet willen omzetten naar een gewone Sell
    //                        // (de stop-limit tijdelijk verwijderen zodat we niet uitgestopt worden)

    //                        decimal? Barometer1h = symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h].PriceBarometer;
    //                        if ((Barometer1h.HasValue && Barometer1h <= -1.5m) || GlobalData.Settings.Trading.DcaMethod != CryptoDcaMethod.FixedPercentage)
    //                        {

    //                            if (GlobalData.Settings.Trading.DcaMethod != CryptoDcaMethod.TrailViaKcPsar)
    //                                continue;

    //                            // Annuleer de DCAx buy orders
    //                            foreach (CryptoPositionPart part in position.Parts.Values)
    //                            {
    //                                foreach (CryptoPositionStep step in part.Steps.Values)
    //                                {
    //                                    if (step.Mode == TradeDirection.Long && step.Status == OrderStatus.New)
    //                                    {
    //                                        string text = string.Format("{0} CANCEL {1} order, de barometer is te laag (of niet fixed dca)", symbol.Name, step.Name);
    //                                        GlobalData.AddTextToLogTab(text);
    //                                        GlobalData.AddTextToTelegram(text);

    //                                        WebCallResult<BinanceOrderBase> result = await BinanceApi.Cancel(position.PaperTrade, symbol, position, step.OrderId);
    //                                        if (result == null)
    //                                        {
    //                                            // Geen geldige order? De cancel kan er niets mee
    //                                            step.Status = OrderStatus.Expired;
    //                                            step.CloseTime = DateTime.UtcNow;
    //                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
    //                                        }
    //                                        else if (result.Success)
    //                                        {
    //                                            // Gelukt!
    //                                            step.Status = OrderStatus.Expired;
    //                                            step.CloseTime = DateTime.UtcNow;
    //                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
    //                                        }
    //                                        else if (!result.Success)
    //                                        {
    //                                            // tsja, dat weet ik niet, de cancel logt zelf al het een en ander
    //                                        }
    //                                    }
    //                                }
    //                            }
    //                        }
    //                        else
    //                        {
    //                            if (GlobalData.Settings.Trading.DcaMethod == CryptoDcaMethod.FixedPercentage)
    //                            {
    //                                // Bij een positievere barometer de DCA order alsnog plaatsen

    //                                // Is er een DCA order aanwezig?
    //                                int sequence = 0;
    //                                CryptoPositionStep buyStep = null;
    //                                decimal lastPrice = decimal.MaxValue;
    //                                DateTime lastDate = DateTime.MinValue;
    //                                foreach (CryptoPositionPart part in position.Parts.Values)
    //                                {
    //                                    foreach (CryptoPositionStep step in part.Steps.Values)
    //                                    {
    //                                        if (step.Mode == TradeDirection.Long && step.Status <= OrderStatus.Filled) // New,PartiallyFilled,Filled,
    //                                        {

    //                                            // Onthoud de laatste prijs en datum dat een buy order gevuld is
    //                                            if (step.Status == OrderStatus.Filled)
    //                                            {
    //                                                // Buy=1, Dca1=2, Dca2=3, Dca2=4, enzovoort
    //                                                sequence++;

    //                                                if (step.Price < lastPrice)
    //                                                    lastPrice = step.Price;

    //                                                if (step.CloseTime.HasValue && (DateTime)step.CloseTime > lastDate)
    //                                                    lastDate = (DateTime)step.CloseTime;
    //                                            }

    //                                            // Er is nog een openstaand buy opdracht (laat maar)
    //                                            if (step.Status == OrderStatus.New)
    //                                            {
    //                                                buyStep = step;
    //                                                break;
    //                                            }
    //                                        }

    //                                    }

    //                                }

    //                                // Indien niet geplaatst dan de DCA order alsnog plaatsen (niet meer dan X DCA's plaatsen)
    //                                // (en stiekum wachten we tot het weer rustig is in de markt door de BB with te controleren)
    //                                if (buyStep == null && sequence <= GlobalData.Settings.Trading.DcaCount)
    //                                {
    //                                    // met SBM en/of CC#2 is dit niet langer interessant
    //                                    //if (candleLast.CandleData != null && candleLast.CandleData.BollingerBandsPercentage.HasValue && (decimal)candleLast.CandleData.BollingerBandsPercentage <= 5.0m)
    //                                    {
    //                                        if (lastDate == DateTime.MaxValue) // onmogelijk, anders waren we niet in positie gekomen..
    //                                            lastDate = DateTime.UtcNow;
    //                                        DateTime date = lastDate;

    //                                        if (date.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) < DateTime.UtcNow)
    //                                        {
    //                                            // Plaats de dca.buy 
    //                                            string name = string.Format("DCA{0}", sequence);
    //                                            (bool result, TradeParams tradeParams) result = await BinanceApi.PlaceBuyOrder(position.PaperTrade, position, sequence, position.Symbol, lastPrice, name);
    //                                            if (result.result)
    //                                                PositionTools.CreatePositionStep(databaseThread, position, null, result.tradeParams, name);
    //                                        }
    //                                    }
    //                                }

    //                            }
    //                        }
    //                    }
    //                    else if (position.Status == CryptoPositionStatus.Timeout || position.Status == CryptoPositionStatus.Ready)
    //                    {
    //                        // De positie na een poosje verwijderen. Achtergrond: bij een timeout kan het gebeuren dat als wij de order
    //                        // annuleren Binance alsnog de order kan vullen, dus daarom verwijderen we de positie gewoon x candles later
    //                        // Vanwege de nieuwe "GlobalData.RemovePosition(position)" is dit nu waarschijnlijk geheel overbodig.
    //                        // (maar omdat we weten dat het vroeger speelde even wat debug werk)

    //                        string text = string.Format("{0} POSITION timeout HANGING? {1} waarom?", symbol.Name, position.Status);
    //                        GlobalData.AddTextToLogTab(text);
    //                        GlobalData.AddTextToTelegram(text);

    //                        // De order is reeds afgesloten, maar vanwege issues met alsnog kopen pas na x minuten weghalen (timing issues).
    //                        // (wel een idee om het dan meer via de database te spelen, maar de timing issues zullen dan blijven bestaan)
    //                        if (position.CloseTime.HasValue && (position.CloseTime?.AddMinutes(4) < DateTime.UtcNow))
    //                        {
    //                            //databaseThread.Connection.Update<CryptoPosition>(position); geen changes, blijf eraf
    //                            //symbol.ClearSignals(); huh, blijf eraf
    //                            PositionTools.RemovePosition(position);
    //                            GlobalData.AddTextToLogTab(String.Format("Debug: positie removed {0}", position.Status.ToString()));
    //                            GlobalData.AddTextToLogTab("Monitor position (hanging timeout?) REMOVE " + symbol.Name + " start");
    //                        }
    //                    }

    //                }
    //            }
    //        }
    //        finally
    //        {
    //            symbol.Exchange.PositionListSemaphore.Release();
    //        }
    //    }
    //}



    public static bool PrepareIndicators(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, CryptoCandle candle, out string reaction)
    {
        if (candle.CandleData == null)
        {
            // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
            List<CryptoCandle> History = CandleIndicatorData.CalculateCandles(symbol, symbolInterval.Interval, candle.OpenTime, out reaction);
            if (History == null)
            {
                //GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                //symbolInterval.Signal = null;
                return false;
            }

            if (History.Count == 0)
            {
                reaction = "Geen candles";
                //GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                //symbolInterval.Signal = null;
                //reaction = 
                return false;
            }

            CandleIndicatorData.CalculateIndicators(History);
        }

        reaction = "";
        return true;
    }


    private static CryptoPosition HasPosition(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoSymbolInterval symbolInterval)
    {
        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (var position in positionList.Values)
            {
                // Alleen voor long trades en het betrokken interval
                if (position.Mode != CryptoTradeDirection.Long || position.IntervalId != symbolInterval.IntervalId)
                    continue;

                return position;
            }
        }

        return null;
    }



    /// <summary>
    /// Controles die noodzakelijk zijn voor een eerste koop
    /// </summary>
    private static bool ValidFirstBuyConditions(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, out string reaction)
    {
        // Is de barometer goed genoeg dat we willen traden?
        if (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval15m, GlobalData.Settings.Trading.Barometer15mBotMinimal, out reaction) ||
        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval30m, GlobalData.Settings.Trading.Barometer30mBotMinimal, out reaction)) ||
        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1h, GlobalData.Settings.Trading.Barometer01hBotMinimal, out reaction)) ||
        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval4h, GlobalData.Settings.Trading.Barometer04hBotMinimal, out reaction)) ||
        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1d, GlobalData.Settings.Trading.Barometer24hBotMinimal, out reaction)))
            return false;


        // Staat op de whitelist (kan leeg zijn)
        if (!SymbolTools.CheckSymbolWhiteListOversold(symbol, CryptoTradeDirection.Long, out reaction))
            return false;


        // Staat niet in de blacklist
        if (!SymbolTools.CheckSymbolBlackListOversold(symbol, CryptoTradeDirection.Long, out reaction))
            return false;


        // Heeft de munt genoeg 24h volume
        if (!SymbolTools.CheckValidMinimalVolume(symbol, out reaction))
            return false;


        // Heeft de munt een redelijke prijs
        if (!SymbolTools.CheckValidMinimalPrice(symbol, out reaction))
            return false;


        // Is de munt te nieuw? (hebben we vertrouwen in nieuwe munten?)
        if (!SymbolTools.CheckNewCoin(symbol, out reaction))
            return false;


        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
        if (!SymbolTools.CheckMinimumTickPercentage(symbol, out reaction))
            return false;

        if (!SymbolTools.CheckAvailableSlots(tradeAccount, symbol, out reaction))
            return false;


        return true;
    }


    private static bool CheckTradingAndSymbolConditions(CryptoSymbol symbol, CryptoCandle lastCandle1m, out string reaction)
    {
        // Als de bot niet actief is dan ook geen monitoring (queue leegmaken)
        // Blijkbaar is de bot dan door de gebruiker uitgezet, verwijder de signalen
        if (!GlobalData.Settings.Trading.Active)
        {
            reaction = "trade-bot deactivated";
            return false;
        }

        // we doen (momenteel) alleen long posities
        if (!symbol.LastPrice.HasValue)
        {
            reaction = "symbol price null";
            return false;
        }

        // Om te voorkomen dat we te snel achter elkaar in dezelfde munt stappen
        if (symbol.LastTradeDate.HasValue && symbol.LastTradeDate?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > lastCandle1m.Date) // DateTime.UtcNow
        {
            reaction = "is in cooldown";
            return false;
        }

        // Als een munt snel is gedaald dan stoppen
        if (GlobalData.Settings.Trading.PauseTradingUntil >= lastCandle1m.Date)
        {
            reaction = string.Format(" de bot is gepauseerd omdat {0}", GlobalData.Settings.Trading.PauseTradingText);
            return false;
        }

        reaction = "";
        return true;
    }


    public void CreateOrExtendPosition()
    {
        string lastPrice = Symbol.LastPrice?.ToString(Symbol.PriceDisplayFormat);
        string text = "Monitor " + Symbol.Name;
        // Anders is het erg lastig traceren
        if (GlobalData.BackTest)
            text += " candle=" + LastCandle1m.DateLocal;
        text += " price=" + lastPrice;



        // **************************************************
        // Global checks zoals barometer, active bot etc..
        // **************************************************
        if (!CheckTradingAndSymbolConditions(Symbol, LastCandle1m, out string reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            Symbol.ClearSignals();
            return;
        }


        //GlobalData.AddTextToLogTab("Monitor " + symbol.Name); te druk in de log

        // ***************************************************************************
        // Per interval kan een signaal aanwezig zijn, regel de aankoop of de bijkoop
        // ***************************************************************************
        long candle1mCloseTime = LastCandle1m.OpenTime + 60;
        foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList)
        {
            // alleen voor de intervallen waar de candle net gesloten is
            // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
            if (candle1mCloseTime % symbolInterval.Interval.Duration == 0)
            {

                CryptoSignal signal = symbolInterval.Signal;
                if (signal == null)
                    continue;

                text = "Monitor " + signal.DisplayText;
                if (GlobalData.BackTest)
                    text += " candle=" + LastCandle1m.DateLocal;
                text += " price=" + lastPrice;

                // We doen alleen long posities
                if (signal.Mode != CryptoTradeDirection.Long)
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " only acception long signals (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Mogen we traden op dit interval
                if (!TradingConfig.MonitorInterval.ContainsKey(signal.Interval.IntervalPeriod))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Mogen we traden met deze strategy
                if (!TradingConfig.Config[CryptoTradeDirection.Long].MonitorStrategy.ContainsKey(signal.Strategy))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this strategy (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }


                // Er zijn (technisch) niet altijd candles aanwezig
                if (!symbolInterval.CandleList.Any())
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // De candle van het signaal terugzoeken (niet zomaar de laatste candle nemen, dit vanwege backtest!)
                long unix = candle1mCloseTime - symbolInterval.Interval.Duration;
                //long unix = CandleTools.GetUnixTime(lastCandle1m.OpenTime, symbolInterval.Interval.Duration);
                //long unix = CandleTools.GetUnixTime(candleCloseTime - symbolInterval.Interval.Duration, symbolInterval.Interval.Duration);
                if (!symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle candleInterval))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Indicators uitrekeninen (indien noodzakelijk)
                if (!PrepareIndicators(Symbol, symbolInterval, candleInterval, out reaction))
                {
                    GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }


                // Bestaan het gekozen strategy wel, klinkt raar, maar is (op dit moment) niet altijd geimplementeerd
                SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(signal.Mode, signal.Strategy, signal.Symbol, signal.Interval, candleInterval);
                if (algorithm == null)
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " unknown algorithm (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                if (algorithm.GiveUp(signal))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " " + algorithm.ExtraText + " giveup (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                if (!algorithm.AllowStepIn(signal))
                {
                    GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + "  (not allowed yet, waiting)");
                    continue;
                }


                //******************************************
                // GO!GO!GO! kan een aankoop of bijkoop zijn
                //******************************************

                foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values)
                {
                    if (!PositionTools.ValidTradeAccount(tradeAccount))
                        continue;

                    using (CryptoDatabase databaseThread = new())
                    {
                        databaseThread.Close();
                        databaseThread.Open();
                        try
                        {
                            PositionTools positionTools = new(tradeAccount, Symbol, LastCandle1mCloseTimeDate);

                            // TODO - Assets, wordt op 2 plekken gecontroleerd (ook in de BinanceApi.DoOnSignal)

                            //string reaction;
                            decimal assetQuantity;
                            if (tradeAccount.AccountType == CryptoTradeAccountType.RealTrading)
                            {
                                // Is er een API key aanwezig (met de juiste opties)
                                if (!SymbolTools.CheckValidApikey(out reaction))
                                {
                                    GlobalData.AddTextToLogTab(text + " " + reaction);
                                    continue;
                                }

                                // Hoeveel muntjes hebben we?
                                var resultPortFolio = SymbolTools.CheckPortFolio(tradeAccount, Symbol);
                                if (!resultPortFolio.result)
                                {
                                    GlobalData.AddTextToLogTab(text + " " + reaction);
                                    continue;
                                }
                                assetQuantity = resultPortFolio.value;
                            }
                            else
                                assetQuantity = 100000m; // genoeg.. (todo? assets voor papertrading?)


                            // Is er "geld" om de order te kunnen plaatsen?
                            // De Quantity is in Quote bedrag (bij BTCUSDT de USDT dollar)
                            if (!SymbolTools.CheckValidAmount(Symbol, assetQuantity, out decimal buyAmount, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction);
                                continue;
                            }



                            CryptoPosition position = HasPosition(tradeAccount, Symbol, symbolInterval);
                            if (position == null)
                            {
                                // Aankoop controles (inclusief overhead van controles van de analyser)
                                // Deze code alleen uitvoeren voor de 1e aankoop (niet voor een bijkoop)
                                // BUG, we weten hier niet of het een aankoop of bijkoop wordt/is! (huh?)
                                if (!ValidFirstBuyConditions(tradeAccount, Symbol, out reaction))
                                {
                                    GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Positie nemen (wordt een buy.a of buy.b in een aparte part)
                                position = positionTools.CreatePosition(signal.Strategy, symbolInterval);
                                databaseThread.Connection.Insert<CryptoPosition>(position);
                                PositionTools.AddPosition(tradeAccount, position);

                                // Verderop doen we wat met deze stap
                                CryptoPositionPart part = positionTools.CreatePositionPart(position);
                                part.Name = "BUY";
                                part.BuyPrice = signal.Price; // voorlopige buyprice
                                position.BuyPrice = signal.Price; // voorlopige buyprice (kan eigenlijk weg, slechts ter debug)
                                PositionTools.InsertPositionPart(databaseThread, part);
                                PositionTools.AddPositionPart(position, part);
                            }
                            else
                            {
                                // De positie uitbreiden nalv een nieuw signaal
                                // (de xe bijkoop wordt altijd een aparte DCA)

                                // Verderop doen we wat met deze stap
                                CryptoPositionPart part = positionTools.CreatePositionPart(position);
                                part.Name = "DCA"; // niet identificerend, er zijn meerdere!
                                part.BuyPrice = signal.Price; // voorlopige buyprice
                                PositionTools.InsertPositionPart(databaseThread, part);
                                PositionTools.AddPositionPart(position, part);
                            }

                        }
                        finally
                        {
                            databaseThread.Close();
                        }
                    }
                }

            }

        }
    }


    public void CreateSignals()
    {
        if (GlobalData.Settings.Signal.SignalsActive && Symbol.QuoteData.CreateSignals)
        {
            // Een extra ToList() zodat we een readonly setje hebben (en we de instellingen kunnen muteren)
            foreach (CryptoInterval interval in TradingConfig.AnalyzeInterval.Values.ToList())
            {
                // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
                if (LastCandle1mCloseTime % interval.Duration == 0)
                {
                    // We geven als tijd het begin van de "laatste" candle (van dat interval)
                    SignalCreate createSignal = new(Symbol, interval);
                    createSignal.AnalyzeSymbol(LastCandle1mCloseTime - interval.Duration);

                    // Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                    // TODO: MultiTread aware maken ..
                    AnalyseCount++;
                }
            }
        }
    }


    private void CleanSymbolData()
    {
        // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
        // ieder geval de CandleData te clearen. Vanaf x candles terug tot de eerste de beste die null is.

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            Monitor.Enter(Symbol.CandleList);
            try
            {
                // Remove old indicator data
                SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                for (int i = candles.Count - 62; i > 0; i--)
                {
                    CryptoCandle c = candles.Values[i];
                    if (c.CandleData != null)
                        c.CandleData = null;
                    else break;
                }


                // Remove old candles
                long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(Symbol, interval, DateTime.UtcNow);
                while (candles.Values.Any())
                {
                    CryptoCandle c = candles.Values.First();
                    if (c == null)
                        break;
                    if (c.OpenTime < startFetchUnix)
                        candles.Remove(c.OpenTime);
                    else break;
                }
            }
            finally
            {
                Monitor.Exit(Symbol.CandleList);
            }
        }
    }


    private async Task CreateBinanceStreamOrderUpdate(CryptoPosition position, CryptoPositionStep step, decimal price)
    {
        //, OrderSide.Buy
        // BUG bestreiding (omdat ik niet weet waar het probleem zit even dmv de TradeHandled)

        // De trade wordt/werd dubbel aangemaakt (en dus statistiek van slag), dus om 
        // aan te geven dat deze step afgehandeld is (maar zou overbodig moeten zijn)
        // Zou wellicht alleen een probleem van de papertrading methodiek kunnen zijn?
        //if (position.Symbol.TradeList.ContainsKey((long)step.OrderId))
        //     return;
        if (step.TradeHandled)
            return;

        // Zet als afgehandeld
        // De trade wordt/werd dubbel aangemaakt (en dus statistiek van slag), dus om 
        // aan te geven dat deze step afgehandeld is (maar zou overbodig moeten zijn)
        // Zou wellicht alleen een probleem van de papertrading methodiek kunnen zijn?
        step.TradeHandled = true;

        // Datum van sluiten candle en een beetje extra
        DateTime now = LastCandle1mCloseTimeDate.AddSeconds(2);
        BinanceStreamOrderUpdate data = new()
        {
            BuyerIsMaker = true,
            Symbol = position.Symbol.Name,
            Status = OrderStatus.Filled,
            EventTime = now,
            CreateTime = now,
            UpdateTime = now,
            Price = price, // prijs bijwerken voor berekening break-even (is eigenlijk niet okay, 2e veld introduceren?)
            Quantity = step.Quantity,
            QuantityFilled = step.Quantity,
            QuoteQuantity = step.Quantity * price, // wat is nu wat?
            QuoteQuantityFilled = step.Quantity * price, // wat is nu wat?
            Fee = step.Quantity * step.Price * 0.75m / 100, // zonder kickback? (anders was het 0.65 dacht ik)
            FeeAsset = position.Symbol.Quote,
        };
        if (step.OrderId.HasValue)
            data.Id = (int)step.OrderId;
        //if (step.OrderListId.HasValue)
        //    data.OrderListId = (long)step.OrderListId;

        if (step.Mode == CryptoTradeDirection.Long)
            data.Side = OrderSide.Buy;
        if (step.Mode == CryptoTradeDirection.Short)
            data.Side = OrderSide.Sell;


        // TODO - Ik mis meerdere (Binance ondersteund tegenwoordig meer)?
        // Wellicht een eigen (beperktere) enumeratie maken
        if (step.OrderType == CryptoOrderType.Market)
            data.Type = SpotOrderType.Market;
        else
        if (step.OrderType == CryptoOrderType.Limit)
            data.Type = SpotOrderType.Limit;
        else
        if (step.OrderType == CryptoOrderType.Oco)
            data.Type = SpotOrderType.StopLoss; // ehh?
        else
        if (step.OrderType == CryptoOrderType.StopLimit)
            data.Type = SpotOrderType.StopLossLimit; // ehh?


        // Ergens tellen we de quantity dubbel, maar waar doen we dat dan? - opgelost
        //string msgInfo = string.Format("{0} side={1} type={2} status={3} order={4} price={5} quantity={6} {7}=filled={8} /{9}", data.Symbol, data.Side, data.Type, data.Status,
        //    data.Id, data.Price.ToString0(), data.Quantity.ToString0(), position.Symbol.Quote, data.QuoteQuantityFilled.ToString0(), data.QuoteQuantity.ToString0());
        //GlobalData.AddTextToLogTab("Debug:" + msgInfo);



        // Maak een Binance Trade
        CryptoTrade trade = new();
        Helper.PickupTrade(position.Symbol, trade, data);
        trade.TradeAccount = position.TradeAccount;
        trade.TradeAccountId = position.TradeAccount.Id;
        using (CryptoDatabase databaseThread = new())
        {
            databaseThread.Close();
            databaseThread.Open();
            try
            {
                databaseThread.Connection.Insert<CryptoTrade>(trade);
                trade.TradeId = trade.Id; // Een fake trade ID
                trade.IsBestMatch = false; // Even een debug ding
                databaseThread.Connection.Update<CryptoTrade>(trade);

                GlobalData.AddTrade(trade);
            }
            finally
            {
                databaseThread.Close();
            }
        }

        data.TradeId = trade.TradeId;
        await HandleTradeAsync(position.TradeAccount, position.Symbol, data);
        PositionTools.CalculateProfitAndBeakEvenPrice(position);

        using (CryptoDatabase databaseThread = new())
        {
            databaseThread.Close();
            databaseThread.Open();
            try
            {
                databaseThread.Connection.Update<CryptoPosition>(position);
            }
            finally
            {
                databaseThread.Close();
            }
        }
        // kan ook via GlobalData.ThreadMonitorOrder.AddToQueue(data, true);
        // maar dat duurt allemaal te lang (en dan is het out of sync?)
    }


    private async Task PaperTradingCheckOrders(CryptoTradeAccount tradeAccount)
    {
        // Is er iets gekocht of verkocht?
        // Zoja dan de HandleTrade aanroepen.

        if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var positionList))
        {
            foreach (CryptoPosition position in positionList.Values.ToList())
            {
                if (tradeAccount.AccountType == CryptoTradeAccountType.RealTrading)
                    continue;

                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    // reeds afgesloten
                    if (part.CloseTime.HasValue)
                        continue;

                    foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                    {
                        if (step.Status == OrderStatus.New) // && !step.TradeHandled
                        {
                            // TODO - Logica controleren met de StopLimit en StopLossLimit (OCO en Stop-Limit, grrr)
                            if (step.Mode == CryptoTradeDirection.Long)
                            {
                                if (step.OrderType == CryptoOrderType.Market) // is dit wel de juiste candle? ....  && step.CreateTime == candle.Date
                                    await CreateBinanceStreamOrderUpdate(position, step, LastCandle1m.Close);
                                if (step.StopPrice.HasValue && LastCandle1m.High >= step.StopPrice)
                                    await CreateBinanceStreamOrderUpdate(position, step, (decimal)step.StopPrice);
                                else if (LastCandle1m.Low < step.Price)
                                    await CreateBinanceStreamOrderUpdate(position, step, step.Price);

                                // Het lijkt erop dat de StopLimit order anders werkt? (aldus de emulator????)
                                // Ik stel voor om dat te controleren (invoeren API) en dan de json bekijken!

                                //((step.OrderType == CryptoOrderType.Limit) && (CandleLast.Low < step.Price)) ||               klopt
                                //((step.OrderType == CryptoOrderType.Oco) && (CandleLast.Low >= step.StopPrice)) ||            klopt
                                //((step.OrderType == CryptoOrderType.StopLimit) && (CandleLast.High >= step.StopPrice))        ? ehhhh ?

                            }
                            else if (step.Mode == CryptoTradeDirection.Short)
                            {
                                if (step.OrderType == CryptoOrderType.Market)  // is dit wel de juiste candle? ....  && step.CreateTime == candle.Date
                                    await CreateBinanceStreamOrderUpdate(position, step, LastCandle1m.Close);
                                else if (step.StopPrice.HasValue && LastCandle1m.Low <= step.StopPrice)
                                    await CreateBinanceStreamOrderUpdate(position, step, (decimal)step.StopPrice);
                                else if (LastCandle1m.High > step.Price)
                                    await CreateBinanceStreamOrderUpdate(position, step, step.Price);

                            }
                        }
                    }

                }
            }
        }
    }


    /// <summary>
    /// Bereken de sellprice van de part op basis van de instellingen
    /// </summary>
    private decimal CalculateSellPrice(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval)
    {
        // DISCUSSIE: Mhhhh, nemen we hiervoor de BreakEvenPrice van de positie of die van de part???
        decimal breakEven = position.BreakEvenPrice; // voorlopig

        decimal sellPrice; // = breakEven; // op zijn minst ;-)
        switch (GlobalData.Settings.Trading.SellMethod)
        {
            case CryptoSellMethod.FixedPercentage:
                // De sell price ligt X% hoger dan de buyPrice
                sellPrice = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));
                sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                break;
            case CryptoSellMethod.DynamicPercentage:
            case CryptoSellMethod.TrailViaKcPsar: // TODO (maar voorlopig even hetzelfde als Dynamisch)!
                sellPrice = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));
                decimal newPrice = GlobalData.Settings.Trading.DynamicTpPercentage * (decimal)candleInterval.CandleData.BollingerBandsDeviation / 100;
                newPrice += (decimal)candleInterval.CandleData.BollingerBandsLowerBand;
                if (newPrice > breakEven)
                {
                    sellPrice = newPrice;
                    GlobalData.AddTextToLogTab(position.Symbol.Name + " Dynamische prijs berekened!", true);
                }

                sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                break;
            default:
                throw new Exception("niet geimplementeerd?");
                //break;

        }
        return sellPrice;
    }

    private static decimal CalculateBuyOrDcaPrice(CryptoPositionPart part, CryptoBuyOrderMethod buyOrderMethod, decimal defaultPrice)
    {
        // Wat wordt de prijs? (hoe graag willen we in de trade?)
        decimal price = defaultPrice;
        switch (buyOrderMethod)
        {
            case CryptoBuyOrderMethod.LimitOrder:
                price = defaultPrice;
                break;
            case CryptoBuyOrderMethod.BidPrice:
                if (part.Symbol.BidPrice.HasValue)
                    price = (decimal)part.Symbol.BidPrice;
                break;
            case CryptoBuyOrderMethod.AskPrice:
                if (part.Symbol.AskPrice.HasValue)
                    price = (decimal)part.Symbol.AskPrice;
                break;
            case CryptoBuyOrderMethod.BidAndAskPriceAvg:
                if (part.Symbol.AskPrice.HasValue)
                    price = (decimal)(part.Symbol.AskPrice + part.Symbol.BidPrice) / 2;
                break;
            case CryptoBuyOrderMethod.MarketOrder:
                price = (decimal)part.Symbol.LastPrice;
                break;
                // TODO: maar voorlopig even afgesterd
                //case BuyPriceMethod.Sma20: 
                //    if (price > (decimal)CandleData.Sma20)
                //        price = (decimal)CandleData.Sma20;
                //    break;
                // TODO: maar voorlopig even afgesterd
                //case BuyPriceMethod.LowerBollingerband:
                //    decimal lowerBand = (decimal)(CandleData.Sma20 - CandleData.BollingerBandsDeviation);
                //    if (price > lowerBand)
                //        price = lowerBand;
                //    break;
        }

        return price;
    }


    public bool BuyStepTimeOut(CryptoPosition position, CryptoPositionStep step)
    {
        // Is de order ouder dan X minuten dan deze verwijderen
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(position.Interval.IntervalPeriod);
        if (step.Status == OrderStatus.New && step.CreateTime.AddSeconds(GlobalData.Settings.Trading.GlobalBuyRemoveTime * symbolInterval.Interval.Duration) < DateTime.UtcNow)
            return true;

        return false;
    }


    public async Task HandlePosition(CryptoTradeAccount tradeAccount, CryptoDatabase databaseThread, CryptoPosition position, bool repostionAllSellOrders = false)
    {
        // Wordt vanuit de HandleTradeAsync aangeroepen en vanuit de 1m candle loopje

        string lastPrice = Symbol.LastPrice?.ToString(Symbol.PriceDisplayFormat);
        string text = "Monitor " + Symbol.Name + "";
        if (GlobalData.BackTest)
            text += " candle=" + LastCandle1m.DateLocal.ToString();
        text += " price=" + lastPrice;

        // Als een munt snel is gedaald dan stoppen
        bool pauseBecauseOfTradingRules = false;
        if (GlobalData.Settings.Trading.PauseTradingUntil >= LastCandle1m.Date)
        {
            //reaction = string.Format(" de bot is gepauseerd omdat {0}", GlobalData.Settings.Trading.PauseTradingText);
            //return false;
            pauseBecauseOfTradingRules = true;
        }

        //// Controleer de 1h barometer
        bool pauseBecauseOfBarometer = false;
        decimal? Barometer1h = Symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h].PriceBarometer;
        if (Barometer1h.HasValue && (Barometer1h <= GlobalData.Settings.Trading.Barometer01hBotMinimal))
        {
            pauseBecauseOfBarometer = true;
            //GlobalData.AddTextToLogTab(string.Format("Monitor {0} position (barometer negatief {1} ) REMOVE start", symbol.Name, Barometer1h, GlobalData.Settings.Trading.Barometer01hBotMinimal));
        }

        //***********************************************************************************
        // Positie afgesloten? of alle sells opnieuw plaatsen (na verandering BE's)
        //***********************************************************************************
        // Afgesloten posities zijn niet langer interessant (tenzij we iets moeten annuleren/weghalen?)
        // Constatering: Als er iets gekocht wordt dan betekend het een verlaging van de BE.
        // ALLE Sell opdrachten moeten dan opnieuw gedaan worden (want: andere verkoop prijs)
        // De BUY en SELL orders worden opnieuw geplaatst indien een gedeelte van het geheel verkocht is.
        if (position.CloseTime.HasValue || repostionAllSellOrders)
        {
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    if (step.Status == OrderStatus.New)
                    {
                        // Andere DCA buy's mogen blijven staan
                        if (repostionAllSellOrders && !step.Name.Equals("SELL"))
                            continue;

                        await BinanceApi.Cancel(tradeAccount, Symbol, position, step.OrderId);
                        step.Status = OrderStatus.Expired;
                        step.CloseTime = LastCandle1mCloseTimeDate;
                        PositionTools.SavePositionStep(databaseThread, position, step);
                    }

                }
            }
            PositionTools.RemovePosition(tradeAccount, position);
            //GlobalData.AddTextToLogTab(String.Format("Debug: positie removed {0} {1}", position.Symbol.Name, position.Status.ToString()));
            return;
        }




        // Maak de beslissingen als de candle van het betrokken interval 'net' klaar is
        if (LastCandle1mCloseTime % position.Interval.Duration != 0)
            return;

        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(position.Interval.IntervalPeriod);
        //PositionTools positionTools = new(tradeAccount, Symbol, symbolInterval, LastCandle1mCloseTimeDate);

        // Niet zomaar een laatste candle nemen in verband met Backtesting
        long candleOpenTimeInterval = LastCandle1mCloseTime - symbolInterval.Interval.Duration;
        if (!symbolInterval.CandleList.TryGetValue(candleOpenTimeInterval, out CryptoCandle candleInterval) || candleInterval.CandleData == null)
        {
            string t = string.Format("candle 1m interval: {0}", LastCandle1m.DateLocal.ToString()) + ".." + LastCandle1mCloseTimeDate.ToLocalTime() + "\r\n" +
            string.Format("is de candle op het {0} interval echt missing in action?", position.Interval.Name) + "\r\n" +
                string.Format("position.CreateDate = {0}", position.CreateTime.ToString()) + "\r\n";
            throw new Exception("Candle niet aanwezig of niet berekend?" + "\r\n" + t);
        }



        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // Afgesloten parts zijn niet langer interessant (ehh, tenzij we nog iets moeten weghalen!)
            if (part.CloseTime.HasValue)
                continue;


            //***********************************************************************************
            // Controleer de 1e BUY's
            //***********************************************************************************
            // TODO: Lijkt enorm veel op de DCA, maar net iets anders
            if (part.Name.Equals("BUY"))
            {
                // Controleer de eerste BUY (implementatie van BUY.A en BUY.B)
                CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", false);
                if (step != null && (pauseBecauseOfTradingRules || pauseBecauseOfBarometer || BuyStepTimeOut(position, step)))
                {
                    // Verwijderen buy's vanwege lage barometer of pauseer stand
                    string text2;
                    if (pauseBecauseOfTradingRules || pauseBecauseOfBarometer)
                        text2 = string.Format("{0} POSITION part {1} ORDER Cancel because of barometer of pause rules", Symbol.Name, part.Id);
                    else
                        text2 = string.Format("{0} POSITION part {1} ORDER Cancel because of timeout", Symbol.Name, part.Id);
                    GlobalData.AddTextToLogTab(text2);
                    GlobalData.AddTextToTelegram(text2);

                    await BinanceApi.Cancel(tradeAccount, Symbol, position, step.OrderId);
                    step.Status = OrderStatus.Expired;
                    step.CloseTime = LastCandle1mCloseTimeDate;
                    PositionTools.SavePositionStep(databaseThread, position, step);
                }
                else if (step == null && part.Quantity == 0 && !pauseBecauseOfTradingRules && !pauseBecauseOfBarometer)
                {
                    // TODO BUY.trailing (C)
                    if (GlobalData.Settings.Trading.BuyStepInMethod == CryptoBuyStepInMethod.Immediately)
                    {
                        decimal price = CalculateBuyOrDcaPrice(part, GlobalData.Settings.Trading.BuyOrderMethod, part.BuyPrice);
                        var x = await BinanceApi.DoOnSignal(databaseThread, tradeAccount, position, part, part.CreateTime, price);
                        if (x.result)
                            Symbol.LastTradeDate = LastCandle1mCloseTimeDate;
                        else
                        {
                            GlobalData.AddTextToLogTab(text + " try buy failed, result= " + x.reaction);
                            GlobalData.AddTextToTelegram(text + " try buy failed, result= " + x.reaction);
                        }
                    }
                }
                else if (step != null && part.Quantity == 0) // && GlobalData.Settings.Trading.BuyStepInMethod == CryptoStepInMethod.TrailViaKcPsar
                {
                    // Trace down? (overlap met 2e IF)
                    // TODO - een stop limit order kunnen plaatsen
                }
            }


            //***********************************************************************************
            // Controleer DCA's
            //***********************************************************************************
            if (position.Quantity > 0 && part.Name.Equals("DCA"))
            {
                // TODO DCA.trailing (C)
                // Maak de eerste BUY (implementatie van DCA.A en DCA.B)
                CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", false);
                if (step != null && (pauseBecauseOfTradingRules || pauseBecauseOfBarometer || BuyStepTimeOut(position, step)))
                {
                    // Verwijderen dca-buy's vanwege lage barometer of pauseer stand
                    string text2;
                    if (pauseBecauseOfTradingRules || pauseBecauseOfBarometer)
                        text2 = string.Format("{0} POSITION part {1} ORDER Cancel because of barometer of pause rules", Symbol.Name, part.Id);
                    else
                        text2 = string.Format("{0} POSITION part {1} ORDER Cancel because of timeout", Symbol.Name, part.Id);
                    GlobalData.AddTextToLogTab(text2);
                    GlobalData.AddTextToTelegram(text2);

                    await BinanceApi.Cancel(tradeAccount, Symbol, position, step.OrderId);
                    step.Status = OrderStatus.Expired;
                    step.CloseTime = LastCandle1mCloseTimeDate;
                    PositionTools.SavePositionStep(databaseThread, position, step);
                }
                else if (step == null && part.Quantity == 0 && !pauseBecauseOfTradingRules && !pauseBecauseOfBarometer)
                {
                    // TODO DCA.trailing (C)
                    //if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoDcaStepInMethod.Immediately)???
                    {
                        decimal price = CalculateBuyOrDcaPrice(part, GlobalData.Settings.Trading.DcaOrderMethod, part.BuyPrice);
                        var x = await BinanceApi.DoOnSignal(databaseThread, tradeAccount, position, part, part.CreateTime, price);
                        if (x.result)
                            Symbol.LastTradeDate = LastCandle1mCloseTimeDate;
                        else
                        {
                            GlobalData.AddTextToLogTab(text + " try buy failed, result= " + x.reaction);
                            GlobalData.AddTextToTelegram(text + " try buy failed, result= " + x.reaction);
                        }
                    }
                }
                else if (step != null && part.Quantity == 0) // && GlobalData.Settings.Trading.DcaStepInMethod == CryptoStepInMethod.TrailViaKcPsar
                {
                    // Trace down? (overlap met 2e IF)
                    // TODO - een stop limit order kunnen plaatsen
                }
            }

            //***********************************************************************************
            // Controleer of de SELL orders geplaatst zijn - SellCheck()
            // Dit geld voor zowel de buy als de sell..
            // Controleer of er een sell aanwezig is voor de DCA
            if (position.Quantity > 0 && (part.Name.Equals("BUY") || part.Name.Equals("DCA")))
            {
                // TODO - Na een Sell alle sell orders opnieuw plaatsen (vanwege aangepaste BE)
                //!!!!! het gaat best okay!


                CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", true);
                if (step != null && (step.Status == OrderStatus.Filled || step.Status == OrderStatus.PartiallyFilled))
                {
                    // todo, is er genoeg Quantity van de symbol om het te kunnen verkopen?

                    step = PositionTools.FindPositionPartStep(part, "SELL", false);
                    if (step == null && part.Quantity > 0)
                    {
                        decimal sellPrice = CalculateSellPrice(position, part, candleInterval);

                        decimal sellQuantity = part.Quantity;
                        sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                        (bool result, TradeParams tradeParams) sellResult;
                        sellResult = await BinanceApi.Sell(tradeAccount, Symbol, sellQuantity, sellPrice);

                        // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                        if (sellResult.result)
                        {
                            string info = string.Format("{0} POSITION SELL ORDER PLACED price={1} quantity={2} quotequantity={3} type={4}", Symbol.Name,
                                sellPrice, sellQuantity, sellPrice * sellQuantity, sellResult.tradeParams.OrderType.ToString());
                            GlobalData.AddTextToLogTab(info);
                            GlobalData.AddTextToTelegram(info);


                            // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
                            part.SellPrice = sellPrice;
                            if (position.SellPrice == 0)
                                position.SellPrice = sellPrice; // (kan eigenlijk weg, slechts ter debug)

                            // Er zijn veel referenties naar datum's (met name in de backtest/papertrading)
                            //DateTime currentDate = CandleTools.GetUnixDate(candle.OpenTime + symbolInterval.Interval.Duration);
                            //PositionTools positionTools = new(tradeAccount, symbol, symbolInterval, currentDate);

                            // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                            var stepX = PositionTools.CreatePositionStep(position, part, sellResult.tradeParams, "SELL");
                            PositionTools.InsertPositionStep(databaseThread, position, stepX);
                            PositionTools.AddPositionPartStep(part, stepX);
                        }
                    }

                    else if (step != null && part.Quantity > 0) // Trailing SELL? SELL.C
                    {
                        // TODO - Controle - Trailing buy? SELL.C (via een instelling graag!)

                        decimal x = Math.Min((decimal)candleInterval.CandleData.KeltnerLowerBand, (decimal)candleInterval.CandleData.PSar) - 2 * Symbol.PriceTickSize;
                        if (x >= (decimal)part.BreakEvenPrice || (GlobalData.Settings.Trading.LockProfits && Math.Min(candleInterval.Open, candleInterval.Close) >= part.BreakEvenPrice))
                        {
                            // Met lockprofits wil je (in de winst hebben gestaan) in de winst blijven, ook al is het maar een beetje..
                            if (GlobalData.Settings.Trading.LockProfits && Math.Min(candleInterval.Open, candleInterval.Close) > x && x < part.BreakEvenPrice)
                                x = Math.Min(candleInterval.Open, candleInterval.Close) - 2 * Symbol.PriceTickSize;

                            // 1.5% hoger, dat moet genoeg speelruimte zijn? (en met een spike zijn we waarschijnlijk ook tevreden)
                            // Het blijft wel een dingetje waar je dan de sell price op zit, later nog eens naar kijken lijkt me.
                            decimal sellPrice = x + (1.5m * x / 100); //x + (2m * x / 100);
                            sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                            // Dat is dan de minimale waarde van KC en PSAR
                            decimal sellStop = x;
                            sellStop = sellStop.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                            // Limit prijs x% lager (die kan op 0 staan als je initieel geen SL gebruikt)
                            decimal percentage = Math.Abs(GlobalData.Settings.Trading.GlobalStopLimitPercentage);
                            if (percentage == 0)
                                percentage = 1.5m;
                            decimal sellLimit = sellStop - (sellStop * (percentage / 100));
                            sellLimit = sellLimit.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                            if (step.Status == OrderStatus.New && step.Mode == CryptoTradeDirection.Short && step.Price < sellPrice)
                            {
                                await BinanceApi.Cancel(tradeAccount, Symbol, position, step.OrderId);
                                step.Status = OrderStatus.Expired;
                                step.CloseTime = LastCandle1mCloseTimeDate;
                                PositionTools.SavePositionStep(databaseThread, position, step);

                                // Afhankelijk van de invoer stop of stoplimit een OCO of standaard sell plaatsen.
                                // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                                (bool result, TradeParams tradeParams) sellResult = await BinanceApi.SellOco(tradeAccount, Symbol, step.Quantity, sellPrice, sellStop, sellLimit);
                                if (sellResult.result)
                                {
                                    string textx = string.Format("{0} POSITION SELL ORDER LOCK PROFIT price={1} stopprice={2} stoplimit={3} quantity={4} quotequantity={5} type={6}",
                                        Symbol.Name, sellPrice, sellStop, sellLimit, step.Quantity, sellPrice * step.Quantity, sellResult.tradeParams.OrderType.ToString());
                                    GlobalData.AddTextToLogTab(textx);
                                    GlobalData.AddTextToTelegram(textx);

                                    // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
                                    part.SellPrice = sellPrice;
                                    if (position.SellPrice == 0)
                                        position.SellPrice = sellPrice; // (kan eigenlijk weg, slechts ter debug)

                                    // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                                    var stepX = PositionTools.CreatePositionStep(position, part, sellResult.tradeParams, "SELL");
                                    PositionTools.InsertPositionStep(databaseThread, position, stepX);
                                    PositionTools.AddPositionPartStep(part, stepX);
                                }
                            }

                        }
                    }

                }
            }
        }
    }


    /// <summary>
    /// De afhandeling van een nieuwe 1m candle.
    /// (de andere intervallen zijn ook berekend)
    /// </summary>
    public async Task NewCandleArrived()
    {
        try
        {
            if (!Symbol.IsSpotTradingAllowed)
                return;

            // Create signals per interval
            CreateSignals();

#if TRADEBOT
            using CryptoDatabase databaseThread = new();
            databaseThread.Close();
            databaseThread.Open();


            //#if BALANCING
            // TODO: Weer werkzaam maken
            //if (GlobalData.Settings.BalanceBot.Active && (symbol.IsBalancing))
            //GlobalData.ThreadBalanceSymbols.AddToQueue(symbol);
            //#endif


            // Simulate Binance Trade indien openstaande orders gevuld zijn
            if (GlobalData.BackTest)
                await PaperTradingCheckOrders(GlobalData.BinanceBackTestAccount);
            if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                await PaperTradingCheckOrders(GlobalData.BinancePaperTradeAccount);

            // Open or extend a position
            if (Symbol.SignalCount > 0)
                CreateOrExtendPosition();

            // Per (actief) trade account de posities controleren
            foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values)
            {
                if (!PositionTools.ValidTradeAccount(tradeAccount))
                    continue;

                // Check positions
                if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var positionList))
                {
                    foreach (CryptoPosition position in positionList.Values.ToList())
                        await HandlePosition(tradeAccount, databaseThread, position, false);
                }
            }
            
            // Simuleer een Binance Trade om eventuele (net gemaakte) market orders te vullen
            if (GlobalData.BackTest)
                await PaperTradingCheckOrders(GlobalData.BinanceBackTestAccount);
            if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                await PaperTradingCheckOrders(GlobalData.BinancePaperTradeAccount);
#endif


            // Remove old candles or CandleData
            if (!GlobalData.BackTest)
                CleanSymbolData();
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + Symbol.Name + " error Monitor thread\r\n" + error.ToString());
        }
    }

}

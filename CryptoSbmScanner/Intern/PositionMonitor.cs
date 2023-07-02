using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Exchange.Binance; // trading...
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

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


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, //BinanceStreamOrderUpdate item)
        CryptoOrderType orderType,
        CryptoOrderSide orderSide,
        CryptoOrderStatus orderStatus,
        long orderId, // The id of the order as assigned by Binance
        long tradeId, // The trade id
        //DateTime createTime, // Time the order was created
        DateTime eventTime, // The time the event happened
        decimal quantity, // The quantity of the order
        decimal quantityFilled, // The quantity of all trades that were filled for this order
        //decimal quoteQuantity, // Quote order quantity
        decimal quoteQuantityFilled, // Cummulative quantity
        decimal price, // The price of the order
        decimal commission, // The fee payed
        string commissionAsset, //The asset the fee was taken from
        bool isMaker // Whether the buyer is the maker
        )
    {
        // zou ook via de positie kunnen, want een trade zit in de context van een positie (als je die kan vinden tenminste)
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = tradeId;
        trade.OrderId = orderId;
        //trade.OrderListId = item.OrderListId;

        trade.Price = price;
        trade.Quantity = quantityFilled;
        trade.QuoteQuantity = price * quantityFilled;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#2stream QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = commission;
        trade.CommissionAsset = commissionAsset;

        trade.TradeTime = eventTime;

        trade.Side = orderSide; // == CryptoOrderSide.Buy;
        //trade.IsMaker = isMaker;
    }


    /// <summary>
    /// Deze routine is een reactie op een gemaakte trade (vanuit de emulator of vanuit de exchange userdata stream)
    /// Momenteel wordt enkel de step, trade en/of positie bijgewerkt, er wordt niet geannuleerd of geplaatst
    /// Omdat deze in BULK (kan) worden aangeroepen worden hier verder geen besissingen gemaakt (denk aan 5 openstaande oco's)
    /// </summary>
    public static async Task HandleTradeAsync(CryptoSymbol symbol,
        CryptoOrderType orderType,
        CryptoOrderSide orderSide,
        CryptoOrderStatus orderStatus,
        CryptoTrade data // is een tijdelijke trade voor de interne datatransfer
        )
    {
        // Vanwege deadlock problemen afgesterd, uitzoeken! Via papertrading zal er nooit een probleem
        // optreden (dat loopt geheel synchroon). Met de user-data stream en 1m candle stream zou het
        // wel kunnen conflicteren. VOOR de aanroep naar deze code moet er gelocked worden!

        // Is er een openstaande positie (via de openstaande posities in het geheugen)
        // NB: Dit gedeelte kan wat mij betreft vervallen (omdat de query ook gedaan wordt)
        //await tradeAccount.PositionListSemaphore.WaitAsync();
        //try
        //{
        //}
        //finally
        //{
        //    //tradeAccount.PositionListSemaphore.Release();
        //}

        using (CryptoDatabase databaseThread = new())
        {
            /* ExecutionType: New = 0, Canceled = 1, Replaced = 2, Rejected = 3, Trade = 4, Expired = 5 */
            /* OrderStatus:  New = 0, PartiallyFilled = 1, Filled = 2, Canceled = 3, PendingCancel = 4, Rejected = 5, Expired = 6, Insurance = 7, Adl = 8 */
            string msgInfo = string.Format("{0} side={1} type={2} status={3} order={4} trade={5} price={6} quantity={7} QuoteQuantity={8}", 
                symbol.Name, orderSide, orderType, orderStatus, data.OrderId, data.TradeId, 
                data.Price.ToString0(), data.Quantity.ToString0(), data.QuoteQuantity.ToString0());

            string s = string.Format("handletrade#1 {0}", msgInfo);
            if (data.TradeAccount.AccountType == CryptoTradeAccountType.BackTest)
                s += string.Format(" ({0})", data.TradeAccount.Name);
            else if (data.TradeAccount.AccountType == CryptoTradeAccountType.PaperTrade)
                s += string.Format(" ({0})", data.TradeAccount.Name);
            GlobalData.AddTextToLogTab(s);


            // Heerlijk, de oude TradeDash pling is terug ;-)
            if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
            {
                if (GlobalData.Settings.General.SoundTradeNotification)
                    GlobalData.PlaySomeMusic("Tradedash - Notification.wav");
            }


            databaseThread.Close();
            databaseThread.Open();

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

                        s = string.Format("handletrade#2 {0} positie gevonden, name={1} id={2} positie.status={3} (memory)", 
                            msgInfo, step.Name, step.Id, position.Status);
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
            // 
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
                    if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.AppStatusRunning)
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
                    // Waarschijnlijk hebben we handmatig een order geplaatst (buiten de bot om).

                    // Wel de trades van deze symbol bijwerken (voor de statistiek)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        await ExchangeClass.FetchTradesForSymbol(data.TradeAccount, symbol);

                    s = string.Format("handletrade#4 {0} geen step gevonden. Statistiek bijwerken (exit)", msgInfo);
                    GlobalData.AddTextToLogTab(s);

                    // Gebruiker informeren (een trade blijft interessant tenslotte)
                    if (orderStatus == CryptoOrderStatus.Filled || orderStatus == CryptoOrderStatus.PartiallyFilled)
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
                    // Wij, anders was de status van de step niet op expired gezet of de positie op timeout gezet
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
                    CryptoPositionStatus? oldStatus = part.Status;
                    part.Status = CryptoPositionStatus.TakeOver;
                    if (oldStatus != part.Status)
                        GlobalData.AddTextToLogTab(String.Format("Debug: positie part status van {0} naar {1}", oldStatus.ToString(), part.Status.ToString()));
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
                    oldStatus = position.Status;
                    position.Status = CryptoPositionStatus.TakeOver;
                    if (oldStatus != position.Status)
                        GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));
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
                s = string.Format("handletrade#8 {0} positie part sold", msgInfo);
                GlobalData.AddTextToLogTab(s);
                GlobalData.AddTextToTelegram(s);

                part.CloseTime = data.TradeTime;
                CryptoPositionStatus? oldStatus = part.Status;
                part.Status = CryptoPositionStatus.Ready;
                if (oldStatus != part.Status)
                    GlobalData.AddTextToLogTab(String.Format("Debug: position part status van {0} naar {1}", oldStatus.ToString(), part.Status.ToString()));
                PositionTools.SavePositionPart(databaseThread, part);



                // Sluit de positie indien afgerond
                if (position.Quantity == 0)
                {
                    // We zijn uit deze trade, alles verkocht
                    s = string.Format("handletrade#8 {0} positie ready", msgInfo);
                    GlobalData.AddTextToLogTab(s);
                    GlobalData.AddTextToTelegram(s);

                    position.CloseTime = data.TradeTime;
                    oldStatus = position.Status;
                    position.Status = CryptoPositionStatus.Ready;
                    PositionTools.SavePosition(databaseThread, position);
                    if (oldStatus != position.Status)
                        GlobalData.AddTextToLogTab(String.Format("Debug: position status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                    // Annuleer alle openstaande dca orders
                    // Dat komt niet goed? (wat ben ik eigenlijk aan het doen, specificaties?)
                    // stel dat de prijs sterk gedaald is dan zijn er X DCA's, en als alleen die laatste verkocht wordt als JoJo dan is dit niet goed
                    foreach (CryptoPositionPart partX in position.Parts.Values.ToList())
                    {
                        if (partX.Quantity == 0)
                        {
                            partX.CloseTime = data.TradeTime;
                            partX.Status = CryptoPositionStatus.Ready;
                            PositionTools.SavePositionPart(databaseThread, partX);

                            foreach (CryptoPositionStep stepX in partX.Steps.Values.ToList())
                            {
                                // Annuleer de openstaande orders indien gevuld (voor alle DCA's)
                                if (stepX.Side == CryptoOrderSide.Buy && (stepX.Status == CryptoOrderStatus.New))
                                {
                                    stepX.CloseTime = data.TradeTime;
                                    stepX.Status = CryptoOrderStatus.Expired;
                                    PositionTools.SavePositionStep(databaseThread, position, stepX);
                                    await BinanceApi.Cancel(data.TradeAccount, symbol, position, stepX.OrderId);
                                }
                            }
                        }
                    }
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

                position.RepositionSell = true;
                return;
            }

            position.RepositionSell = true;
            return;
        }
    }


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
        if (!SymbolTools.CheckSymbolWhiteListOversold(symbol, CryptoOrderSide.Buy, out reaction))
            return false;


        // Staat niet in de blacklist
        if (!SymbolTools.CheckSymbolBlackListOversold(symbol, CryptoOrderSide.Buy, out reaction))
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


    private decimal LowestBuyPart(CryptoPosition position)
    {
        // De laagste buy price van een openstaande part retourneren

        decimal lastBuyPrice = decimal.MaxValue;
        foreach (CryptoPositionPart partX in position.Parts.Values.ToList())
        {
            // Afgesloten DCA parts sluiten we uit
            // (zodat we meerdere jojo's kunnen uitvoeren)
            if (partX.Name.Equals("DCA") && partX.CloseTime.HasValue)
                continue;

            foreach (CryptoPositionStep stepX in partX.Steps.Values.ToList())
            {
                if (stepX.Side == CryptoOrderSide.Buy && stepX.CloseTime.HasValue)
                {
                    if (stepX.Price < lastBuyPrice)
                        lastBuyPrice = stepX.Price;
                }
            }
        }
        return lastBuyPrice;
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
                if (signal.Side != CryptoOrderSide.Buy)
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
                if (!TradingConfig.Config[CryptoOrderSide.Buy].MonitorStrategy.ContainsKey(signal.Strategy))
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
                SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(signal.Side, signal.Strategy, signal.Symbol, signal.Interval, candleInterval);
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

                foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
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



                            CryptoPosition position = PositionTools.HasPosition(tradeAccount, Symbol, symbolInterval);
                            if (position == null)
                            {
                                if (!GlobalData.Settings.Trading.OpenNewPositions)
                                {
                                    reaction = "openen van nieuwe posities is niet toegestaan";
                                    GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                    Symbol.ClearSignals();
                                    return;
                                }

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
                                position.PartCount = 1;
                                PositionTools.AddPosition(tradeAccount, position);

                                // Verderop doen we wat met deze stap
                                CryptoPositionPart part = positionTools.CreatePositionPart(position, "BUY", signal.Price); // voorlopige buyprice
                                PositionTools.InsertPositionPart(databaseThread, part);
                                PositionTools.AddPositionPart(position, part);

                                // In de veronderstelling dat dit allemaal lukt
                                Symbol.LastTradeDate = LastCandle1mCloseTimeDate;

                                if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.AppStatusRunning)
                                    GlobalData.PositionsHaveChanged("");
                            }
                            else
                            {
                                // Een bijkoop zonder buy is onmogelijk
                                decimal lastBuyPrice = LowestBuyPart(position);
                                if (lastBuyPrice == decimal.MaxValue)
                                {
                                    GlobalData.AddTextToLogTab(text + " Geen eerste BUY price gevonden (removed)");
                                    Symbol.ClearSignals();
                                    return;
                                }
                                decimal percentage = 100m * (lastBuyPrice - signal.Price) / lastBuyPrice;


                                if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoBuyStepInMethod.AfterNextSignal)
                                {
                                    // Het percentage moet in ieder geval x% onder de vorige buy opdracht zitten
                                    // (en dit heeft voordelen want dan hoef je niet te weten in welke DCA-index je zit!)
                                    // TODO: Dat zou (CC2 technisch) ook een percentage van de BB kunnen zijn als 3e optie.

                                    if (percentage < GlobalData.Settings.Trading.DcaPercentage)
                                    {
                                        GlobalData.AddTextToLogTab(text + " het is nog te vroeg voor een bijkoop (signal) " + percentage.ToString0("N2") + " (removed)");
                                        Symbol.ClearSignals();
                                        return;
                                    }
                                }

                                if (PositionTools.IsTrailingPosition(position) != null)
                                {
                                    GlobalData.AddTextToLogTab(text + " de positie is al  aan het trailen (signal genegeerd) " + percentage.ToString0("N2") + " (removed)");
                                    Symbol.ClearSignals();
                                    return;
                                }


                                //if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoDcaStepInMethod.FixedPercentage)
                                //{
                                //    // De eerste dca-bijkoop orders zou al geplaatst kunnen zijn
                                //    // (de 2e dca enzovoort ook al, maar dat gaat te ver lijkt me)
                                //    if (percentage < GlobalData.Settings.Trading.DcaPercentage)
                                //    {
                                //        GlobalData.AddTextToLogTab(text + " het is nog te vroeg voor een bijkoop (fixed) " + percentage.ToString0("N2") + " (removed)");
                                //        Symbol.ClearSignals();
                                //        return;
                                //    }
                                //}

                                //if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoDcaStepInMethod.TrailViaKcPsar)
                                //{
                                //    // De eerste dca-bijkoop orders zou al geplaatst kunnen zijn
                                //    // (de 2e dca enzovoort ook al, maar dat gaat te ver lijkt me)
                                //    if (percentage < GlobalData.Settings.Trading.DcaPercentage)
                                //    {
                                //        GlobalData.AddTextToLogTab(text + " het is nog te vroeg voor een bijkoop (trail)" + percentage.ToString0("N2") + " (removed)");
                                //        Symbol.ClearSignals();
                                //        return;
                                //    }
                                //}

                                // Maar in ieder geval alleen bijkopen als we ONDER de break-even prijs zitten
                                if (signal.Price < position.BreakEvenPrice)
                                {
                                    GlobalData.AddTextToLogTab(text + " extra DCA positie op percentage: " + percentage.ToString0("N2"));

                                    // De positie uitbreiden nalv een nieuw signaal
                                    // (de xe bijkoop wordt altijd een aparte DCA)
                                    position.PartCount += 1;

                                    // Verderop doen we wat met deze stap
                                    CryptoPositionPart part = positionTools.CreatePositionPart(position, "DCA", signal.Price); // voorlopige buyprice
                                    PositionTools.InsertPositionPart(databaseThread, part);
                                    PositionTools.AddPositionPart(position, part);

                                    // In de veronderstelling dat dit allemaal lukt
                                    Symbol.LastTradeDate = LastCandle1mCloseTimeDate;

                                }
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


        using CryptoDatabase databaseThread = new();
        databaseThread.Open();


        // Datum van sluiten candle en een beetje extra
        //DateTime now = LastCandle1mCloseTimeDate.AddSeconds(2);
        CryptoTrade trade = new()
        {
            TradeAccount = position.TradeAccount,
            TradeAccountId = position.TradeAccountId,
            Exchange = position.Symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,

            //Status = CryptoOrderStatus.Filled,
            TradeTime = LastCandle1mCloseTimeDate.AddSeconds(2),
            Price = price, // prijs bijwerken voor berekening break-even (is eigenlijk niet okay, 2e veld introduceren?)
            Quantity = step.Quantity,
            //QuantityFilled = step.Quantity,
            QuoteQuantity = step.Quantity * price, // wat is nu wat?
            //QuoteQuantityFilled = step.Quantity * price, // wat is nu wat?
            Commission = step.Quantity * step.Price * 0.1m / 100, // met BNB korting=0.075 (zonder kickback, anders was het 0.065?)
            CommissionAsset = position.Symbol.Quote,

            Side = step.Side,
        };
        if (step.OrderId.HasValue)
            trade.OrderId = (int)step.OrderId;


        // bewaar de gemaakte trade
        databaseThread.Connection.Insert<CryptoTrade>(trade);
        trade.TradeId = trade.Id; // Een fake trade ID
        databaseThread.Connection.Update<CryptoTrade>(trade);
        GlobalData.AddTrade(trade);

        await HandleTradeAsync(position.Symbol, step.OrderType, step.Side, CryptoOrderStatus.Filled, trade);

        //PositionTools.CalculateProfitAndBeakEvenPrice(position);
        //databaseThread.Connection.Update<CryptoPosition>(position);
    }


    private async Task PaperTradingCheckOrders(CryptoTradeAccount tradeAccount, bool onlyMarketOrders)
    {
        // Is er iets gekocht of verkocht?
        // Zoja dan de HandleTrade aanroepen.

        if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var positionList))
        {
            foreach (CryptoPosition position in positionList.Values.ToList())
            {
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    // reeds afgesloten
                    if (part.CloseTime.HasValue)
                        continue;

                    foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                    {
                        if (step.Status == CryptoOrderStatus.New) // && !step.TradeHandled
                        {
                            if (onlyMarketOrders && step.OrderType != CryptoOrderType.Market)
                                continue;

                            // TODO - Logica controleren met de StopLimit en StopLossLimit (OCO en Stop-Limit, grrr)
                            if (step.Side == CryptoOrderSide.Buy)
                            {
                                if (step.OrderType == CryptoOrderType.Market) // is dit wel de juiste candle? ....  && step.CreateTime == candle.Date
                                    await CreateBinanceStreamOrderUpdate(position, step, LastCandle1m.Close);
                                if (step.StopPrice.HasValue)
                                {
                                    if (LastCandle1m.High > step.StopPrice)
                                        await CreateBinanceStreamOrderUpdate(position, step, (decimal)step.StopPrice);
                                }
                                else if (LastCandle1m.Low < step.Price)
                                    await CreateBinanceStreamOrderUpdate(position, step, step.Price);

                                // Het lijkt erop dat de StopLimit order anders werkt? (aldus de emulator????)
                                // Ik stel voor om dat te controleren (invoeren API) en dan de json bekijken!
                                // Daarnaast heeft de OCO nog een limit maker en/of stopding nodig? (welke moet het zijn?)
                                // (en is dat wel relevant als we ALLEEN met trades blijven werken <zoals in papertrading>)

                                //((step.OrderType == CryptoOrderType.Limit) && (CandleLast.Low < step.Price)) ||               klopt
                                //((step.OrderType == CryptoOrderType.Oco) && (CandleLast.Low >= step.StopPrice)) ||            klopt
                                //((step.OrderType == CryptoOrderType.StopLimit) && (CandleLast.High >= step.StopPrice))        ? ehhhh ?

                            }
                            else if (step.Side == CryptoOrderSide.Sell)
                            {
                                if (step.OrderType == CryptoOrderType.Market)  // is dit wel de juiste candle? ....  && step.CreateTime == candle.Date
                                    await CreateBinanceStreamOrderUpdate(position, step, LastCandle1m.Close);
                                else if (step.StopPrice.HasValue)
                                {
                                    if (LastCandle1m.Low < step.StopPrice)
                                        await CreateBinanceStreamOrderUpdate(position, step, (decimal)step.StopPrice);
                                }
                                else if (LastCandle1m.High > step.Price)
                                    await CreateBinanceStreamOrderUpdate(position, step, step.Price);

                            }
                        }
                    }

                }
            }
        }
    }


    public bool BuyStepTimeOut(CryptoPosition position, CryptoPositionStep step)
    {
        // Gereserveerde DCA buy orders of trailing orders moeten we niet annuleren..
        if (step != null && step.Trailing == CryptoTrailing.None)
        {
            // Is de order ouder dan X minuten dan deze verwijderen
            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(position.Interval.IntervalPeriod);
            if (step.Status == CryptoOrderStatus.New && step.CreateTime.AddSeconds(GlobalData.Settings.Trading.GlobalBuyRemoveTime * symbolInterval.Interval.Duration) < DateTime.UtcNow)
                return true;
        }

        return false;
    }


    private bool Prepare(CryptoPosition position, out CryptoSymbolInterval symbolInterval, out CryptoCandle candleInterval, out bool pauseBecauseOfTradingRules, out bool pauseBecauseOfBarometer)
    {
        candleInterval = null;
        symbolInterval = Symbol.GetSymbolInterval(position.Interval.IntervalPeriod);


        // Als een munt snel is gedaald dan stoppen
        pauseBecauseOfTradingRules = false;
        if (GlobalData.Settings.Trading.PauseTradingUntil >= LastCandle1m.Date)
        {
            //reaction = string.Format(" de bot is gepauseerd omdat {0}", GlobalData.Settings.Trading.PauseTradingText);
            //return false;
            pauseBecauseOfTradingRules = true;
        }

        //// Controleer de 1h barometer
        pauseBecauseOfBarometer = false;
        decimal? Barometer1h = Symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h].PriceBarometer;
        if (Barometer1h.HasValue && (Barometer1h <= GlobalData.Settings.Trading.Barometer01hBotMinimal))
        {
            pauseBecauseOfBarometer = true;
            //GlobalData.AddTextToLogTab(string.Format("Monitor {0} position (barometer negatief {1} ) REMOVE start", symbol.Name, Barometer1h, GlobalData.Settings.Trading.Barometer01hBotMinimal));
        }

        // Maak de beslissingen als de candle van het betrokken interval afgesloten is (NIET die van de 1m candle!)
        if (LastCandle1mCloseTime % position.Interval.Duration != 0)
            return false;


        // Niet zomaar een laatste candle nemen in verband met Backtesting
        long candleOpenTimeInterval;
        long bla = LastCandle1mCloseTime % symbolInterval.Interval.Duration;
        candleOpenTimeInterval = LastCandle1mCloseTime - bla - symbolInterval.Interval.Duration;


        // Die indicator berekening had ik niet verwacht (waarschijnlijk vanwege cooldown?)
        Monitor.Enter(position.Symbol.CandleList);
        try
        {
            // Niet zomaar een laatste candle nemen in verband met Backtesting
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTimeInterval, out candleInterval))
            {
                string t = string.Format("candle 1m interval: {0}", LastCandle1m.DateLocal.ToString()) + ".." + LastCandle1mCloseTimeDate.ToLocalTime() + "\r\n" +
                string.Format("is de candle op het {0} interval echt missing in action?", position.Interval.Name) + "\r\n" +
                    string.Format("position.CreateDate = {0}", position.CreateTime.ToString()) + "\r\n";
                throw new Exception("Candle niet aanwezig?" + "\r\n" + t);
            }
            if (candleInterval.CandleData == null)
            {
                List<CryptoCandle> history = null;
                history = CandleIndicatorData.CalculateCandles(Symbol, symbolInterval.Interval, candleInterval.OpenTime, out string response);
                if (history == null)
                {
                    GlobalData.AddTextToLogTab("Analyse " + response);
                    throw new Exception("Candle niet berekend?" + "\r\n" + response);
                }

                // Eenmalig de indicators klaarzetten
                CandleIndicatorData.CalculateIndicators(history);
            }
        }
        finally
        {
            Monitor.Exit(position.Symbol.CandleList);
        }


        return true;
    }


    private static decimal CalculateBuyOrDcaPrice(CryptoPositionPart part, CryptoBuyOrderMethod buyOrderMethod, decimal defaultPrice)
    {
        // TODO: Uitzoeken, de Bid en Ask prijs zijn voor Bybit waarschijnlijk niet bekend via de PriceTicker

        // Wat wordt de prijs? (hoe graag willen we in de trade?)
        decimal price = defaultPrice;
        switch (buyOrderMethod)
        {
            case CryptoBuyOrderMethod.SignalPrice:
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


    private async Task HandleBuyPart(CryptoDatabase databaseThread, CryptoPosition position, CryptoPositionPart part, CryptoSymbolInterval symbolInterval,
        CryptoCandle candleInterval, CryptoBuyStepInMethod stepInMethod, CryptoBuyOrderMethod buyOrderMethod, bool pauseBecauseOfTradingRules, bool pauseBecauseOfBarometer)
    {
        // Controleer de 1e BUY (de implementatie van A en B)
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", false);

        // Verwijderen de buy vanwege een te lage barometer, pauseer stand of timeout (behalve trailing of reserved dca)
        if (pauseBecauseOfTradingRules || pauseBecauseOfBarometer || BuyStepTimeOut(position, step))
        {
            // Laat een trailing buy met rust (het gaat ten slotte toch al naar beneden)
            if (step != null && step.Trailing == CryptoTrailing.None)
            {
                string text2;
                if (pauseBecauseOfTradingRules || pauseBecauseOfBarometer)
                    text2 = string.Format("{0} POSITION part {1} ORDER {2} Cancel because of barometer of pause rules", Symbol.Name, part.Id, step.OrderId);
                else
                    text2 = string.Format("{0} POSITION part {1} ORDER {2} Cancel because of timeout", Symbol.Name, part.Id, step.OrderId);
                GlobalData.AddTextToLogTab(text2);
                GlobalData.AddTextToTelegram(text2);

                if (part.Name.Equals("BUY"))
                {
                    position.BuyPrice = null;
                    PositionTools.SavePosition(databaseThread, position);
                }
                await BinanceApi.Cancel(position.TradeAccount, Symbol, position, step.OrderId);
                step.Status = CryptoOrderStatus.Expired;
                step.CloseTime = LastCandle1mCloseTimeDate;
                PositionTools.SavePositionStep(databaseThread, position, step);
            }
        }
        else
        {
            // TODO, napluizen, er is teveel duplicaat code


            // Plaats een buy order (of stop-limit buy order voor trailing)
            if (step == null && part.Quantity == 0)
            {
                if (stepInMethod == CryptoBuyStepInMethod.Immediately || stepInMethod == CryptoBuyStepInMethod.AfterNextSignal)
                {
                    // Dit triggert een notificatie die technisch gezien eerder kan arriveren dan dat wij 
                    // de positie toevoegen, daarom locken we hier de posities voor het plaatsen van de buy.
                    CryptoOrderType orderType = CryptoOrderType.Limit;
                    if (GlobalData.Settings.Trading.BuyOrderMethod == CryptoBuyOrderMethod.MarketOrder)
                        orderType = CryptoOrderType.Market;

                    decimal price = CalculateBuyOrDcaPrice(part, buyOrderMethod, part.BuyPrice);
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    // quantity perikelen (en we verdubbelen de inzet zoals destijds in Zignally!)
                    decimal quoteAmount;
                    if (position.Invested == 0)
                        quoteAmount = Symbol.QuoteData.BuyAmount;
                    else
                        quoteAmount = (position.Invested - position.Returned) * GlobalData.Settings.Trading.DcaFactor;
                    decimal quantity = quoteAmount / price;
                    quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);


                    BinanceApi exchangeApi = new(position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate);
                    (bool result, TradeParams tradeParams) result = await exchangeApi.BuyOrSell(orderType, CryptoOrderSide.Buy, quantity, price, null, null);
                    if (result.result)
                    {
                        if (part.Name.Equals("BUY"))
                        {
                            position.BuyPrice = result.tradeParams.Price;
                            PositionTools.SavePosition(databaseThread, position);
                        }
                        step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY", CryptoTrailing.Trailing);
                        PositionTools.InsertPositionStep(databaseThread, position, step);
                        PositionTools.AddPositionPartStep(part, step);
                    }
                }
                else if (stepInMethod == CryptoBuyStepInMethod.TrailViaKcPsar)
                {
                    // De 1e trailing limit buy order
                    // TODO - een stop limit order kunnen plaatsen (nog niet eerder gedaan blijkbaar)
                    decimal? stop = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                    stop = stop?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                    decimal price = 1.015m * (decimal)stop; // ergens erboven
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    // quantity perikelen (en we verdubbelen de inzet zoals destijds in Zignally!)
                    decimal quoteAmount;
                    if (position.Invested == 0)
                        quoteAmount = Symbol.QuoteData.BuyAmount;
                    else
                        quoteAmount = (position.Invested - position.Returned) * GlobalData.Settings.Trading.DcaFactor;
                    decimal quantity = quoteAmount / (decimal)stop;
                    quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                    BinanceApi exchangeApi = new(position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate);
                    (bool result, TradeParams tradeParams) result = await exchangeApi.BuyOrSell(CryptoOrderType.StopLimit, CryptoOrderSide.Buy, 
                        quantity, price, stop, null);
                    if (result.result)
                    {
                        if (part.Name.Equals("BUY"))
                        {
                            position.BuyPrice = result.tradeParams.Price;
                            PositionTools.SavePosition(databaseThread, position);
                        }
                        step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY", CryptoTrailing.Trailing);
                        PositionTools.InsertPositionStep(databaseThread, position, step);
                        PositionTools.AddPositionPartStep(part, step);
                    }
                }
            }
            else if (step != null && part.Quantity == 0 && stepInMethod == CryptoBuyStepInMethod.TrailViaKcPsar && step.Trailing == CryptoTrailing.Trailing)
            {
                // De Xe trailing limit buy order
                // TODO - een stop limit order kunnen plaatsen (nog niet eerder gedaan blijkbaar)
                decimal price = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                if (price < step.StopPrice) // of was het de StopPrice? Grrrrr
                {
                    decimal? stop = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                    stop = stop?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                    price = 1.015m * (decimal)stop; // ergens erboven
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);


                    //string t2 = string.Format("{0} POSITION part {1} {2} ORDER {3} herpositionering stop limit buy trail {4} {5}", 
                    //    Symbol.Name, part.Id, part.Name, step.OrderId, price, CryptoOrderType.StopLimit);
                    //GlobalData.AddTextToLogTab(t2);

                    // Annuleer de vorige buy order
                    await BinanceApi.Cancel(position.TradeAccount, Symbol, position, step.OrderId);
                    step.Status = CryptoOrderStatus.Expired;
                    step.CloseTime = LastCandle1mCloseTimeDate;
                    PositionTools.SavePositionStep(databaseThread, position, step);

                    // quantity perikelen (en we verdubbelen de inzet zoals destijds in Zignally!)
                    decimal quoteAmount;
                    if (position.Invested == 0)
                        quoteAmount = Symbol.QuoteData.BuyAmount;
                    else
                        quoteAmount = (position.Invested - position.Returned) * GlobalData.Settings.Trading.DcaFactor;
                    decimal quantity = quoteAmount / price;
                    quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                    // Plaats nieuwe buy order (lagere)
                    BinanceApi exchangeApi = new(position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate);
                    (bool result, TradeParams tradeParams) result = await exchangeApi.BuyOrSell(CryptoOrderType.StopLimit, CryptoOrderSide.Buy,
                        quantity, price, stop, null);
                    if (result.result)
                    {
                        if (part.Name.Equals("BUY"))
                        {
                            position.BuyPrice = result.tradeParams.Price;
                            PositionTools.SavePosition(databaseThread, position);
                        }
                        step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY", CryptoTrailing.Trailing);
                        PositionTools.InsertPositionStep(databaseThread, position, step);
                        PositionTools.AddPositionPartStep(part, step);
                    }
                }
            }
        }

        // *** deze moet nog, maar die is best lastig (en de stoplimit voor Binance moet nog gemaakt/getest worden)
        //if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoDcaStepInMethod.TrailViaKcPsar)
        //{
        //    // De eerste dca-bijkoop orders zou al geplaatst kunnen zijn
        //    // (de 2e dca enzovoort ook al, maar dat gaat te ver lijkt me)
        //    if (percentage < GlobalData.Settings.Trading.DcaPercentage)
        //    {
        //        GlobalData.AddTextToLogTab(text + " het is nog te vroeg voor een bijkoop (trail)" + percentage.ToString0("N2") + " (removed)");
        //        Symbol.ClearSignals();
        //        return;
        //    }
        //}
    }


    /// <summary>
    /// Bereken de sellprice van de part op basis van de instellingen
    /// </summary>
    private decimal CalculateSellPrice(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval)
    {
        // We nemen hiervoor de BreakEvenPrice van de gehele positie
        decimal breakEven = position.BreakEvenPrice; // voorlopig
        // De sell price ligt standaard X% hoger dan de buyPrice
        decimal sellPrice = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));

        switch (GlobalData.Settings.Trading.SellMethod)
        {
            case CryptoSellMethod.FixedPercentage:
                // Al berekend
                break;
            case CryptoSellMethod.TrailViaKcPsar:
            // (voorlopig hetzelfde als Dynamisch)!
            case CryptoSellMethod.DynamicPercentage:
                // Alleen voor de 1e sell (omdat de rest boven een gezamelijke BE komt te staan)
                if (part.Name.Equals("BUY"))
                {
                    decimal newPrice = GlobalData.Settings.Trading.DynamicTpPercentage * (decimal)candleInterval.CandleData.BollingerBandsDeviation / 100;
                    newPrice += (decimal)candleInterval.CandleData.BollingerBandsLowerBand;
                    if (newPrice > breakEven || newPrice > sellPrice)
                    {
                        sellPrice = newPrice;
                        GlobalData.AddTextToLogTab(position.Symbol.Name + " dynamische prijs berekened!", true);
                    }
                }
                break;
            default:
                throw new Exception("niet geimplementeerd?");
                //break;

        }
        sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        return sellPrice;
    }


    private async Task HandleSellPart(CryptoDatabase databaseThread, CryptoPosition position, CryptoPositionPart part, CryptoSymbolInterval symbolInterval,
        CryptoCandle candleInterval, CryptoBuyStepInMethod stepInMethod, CryptoBuyOrderMethod buyOrderMethod, bool pauseBecauseOfTradingRules, bool pauseBecauseOfBarometer)
    {
        // is er iets te verkopen? (is dat niet hetzelfde als part.Quantity > 0)
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", true);
        if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
        {
            // todo, is er genoeg Quantity van de symbol om het te kunnen verkopen?

            step = PositionTools.FindPositionPartStep(part, "SELL", false);
            if (step == null && part.Quantity > 0)
            {
                decimal sellPrice = CalculateSellPrice(position, part, candleInterval);

                decimal sellQuantity = part.Quantity;
                sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                (bool result, TradeParams tradeParams) sellResult;
                BinanceApi exchangeApi = new(position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate);
                sellResult = await exchangeApi.BuyOrSell(CryptoOrderType.Limit, CryptoOrderSide.Sell, sellQuantity, sellPrice, null, null);

                // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                if (sellResult.result)
                {
                    // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
                    part.SellPrice = sellPrice;

                    if (part.Name.Equals("BUY"))
                    {
                        position.SellPrice = sellResult.tradeParams.Price;
                        PositionTools.SavePosition(databaseThread, position);
                    }

                    //if (!position.SellPrice.HasValue)
                    //{
                    //    position.SellPrice = part.SellPrice; // (kan eigenlijk weg, slechts ter debug en tracering, voila)
                    //    PositionTools.SavePosition(databaseThread, position);
                    //}

                    // Er zijn veel referenties naar datum's (met name in de backtest/papertrading)
                    //DateTime currentDate = CandleTools.GetUnixDate(candle.OpenTime + symbolInterval.Interval.Duration);
                    //PositionTools positionTools = new(tradeAccount, symbol, symbolInterval, currentDate);

                    // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                    var sellStep = PositionTools.CreatePositionStep(position, part, sellResult.tradeParams, "SELL");
                    PositionTools.InsertPositionStep(databaseThread, position, sellStep);
                    PositionTools.AddPositionPartStep(part, sellStep);

                    string info = string.Format("{0} POSITION SELL ORDER {1} PLACED price={2} quantity={3} quotequantity={4} type={5}", Symbol.Name,
                        sellStep.OrderId, sellStep.Price, sellStep.Quantity, sellStep.Price * sellStep.Quantity,
                        sellResult.tradeParams.OrderType.ToString());
                    GlobalData.AddTextToLogTab(info);
                    GlobalData.AddTextToTelegram(info);
                }
            }

            else if (step != null && part.Quantity > 0 && part.BreakEvenPrice > 0 && GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar) // Trailing SELL? SELL.C
            {
                // Controle - Trailing buy? SELL.C (via een instelling graag!)
                // Dat loop momenteel niet zo erg goed, de SL wordt erg vaak geraakt (is wat je mag verwachten natuurlijk)

                decimal x = Math.Min((decimal)candleInterval.CandleData.KeltnerLowerBand, (decimal)candleInterval.CandleData.PSar) - 2 * Symbol.PriceTickSize;
                if (x >= part.BreakEvenPrice || (GlobalData.Settings.Trading.LockProfits && Math.Min(candleInterval.Open, candleInterval.Close) >= part.BreakEvenPrice))
                {
                    // Met lockprofits wil je (in de winst hebben gestaan) in de winst blijven, ook al is het maar een beetje..
                    if (GlobalData.Settings.Trading.LockProfits && Math.Min(candleInterval.Open, candleInterval.Close) > x && x < part.BreakEvenPrice)
                        x = Math.Min(candleInterval.Open, candleInterval.Close) - 2 * Symbol.PriceTickSize;

                    // 1.5% hoger, dat moet genoeg speelruimte zijn? (en met een spike zijn we waarschijnlijk ook tevreden)
                    // Het blijft wel een dingetje waar je dan de sell price op zit, later nog eens naar kijken lijkt me.
                    //decimal sellPrice = x + (1.5m * x / 100); //x + (2m * x / 100);
                    decimal sellPrice = (decimal)candleInterval.CandleData.BollingerBandsUpperBand - 1 * Symbol.PriceTickSize;
                    sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                    if (Symbol.LastPrice.HasValue && Symbol.LastPrice > sellPrice)
                    {
                        decimal oldPrice = sellPrice;
                        sellPrice = (decimal)Symbol.LastPrice + 1 * Symbol.PriceTickSize;
                        GlobalData.AddTextToLogTab("SELL correction: " + Symbol.Name + " " + oldPrice.ToString("N6") + " to " + sellPrice.ToString0());
                    }

                    // Dat is dan de minimale waarde van KC en PSAR
                    decimal sellStop = x;
                    sellStop = sellStop.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    // Limit prijs x% lager (die kan op 0 staan als je initieel geen SL gebruikt)
                    decimal sellLimit = sellStop - (sellStop * (1.5m / 100));
                    sellLimit = sellLimit.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);


                    // Als de prijs reeds boven de KC.High zit hoger gaan zitten!
                    if (Math.Min(candleInterval.Open, candleInterval.Close) > (decimal)candleInterval.CandleData.KeltnerUpperBand)
                    {
                        sellStop = ((decimal)candleInterval.CandleData.KeltnerLowerBand + (decimal)candleInterval.CandleData.KeltnerUpperBand ) / 2;
                        sellStop = sellStop.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                        sellLimit = sellStop - (sellStop * (1.5m / 100));
                        sellLimit = sellLimit.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                    }



                    // Een extra controle op de low (anders wordt ie direct gevuld)
                    if (step.Status == CryptoOrderStatus.New && step.Side == CryptoOrderSide.Sell && candleInterval.Low > sellStop && (step.StopPrice == null || sellStop > step.StopPrice))
                    {
                        await BinanceApi.Cancel(position.TradeAccount, Symbol, position, step.OrderId);
                        step.Status = CryptoOrderStatus.Expired;
                        step.CloseTime = LastCandle1mCloseTimeDate;
                        PositionTools.SavePositionStep(databaseThread, position, step);

                        // Afhankelijk van de invoer stop of stoplimit een OCO of standaard sell plaatsen.
                        // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                        BinanceApi exchangeApi = new(position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate);
                        //(bool result, TradeParams tradeParams) sellResult = await exchangeApi.SellOco(CryptoOrderType.Oco, CryptoOrderSide.Short, 
                        //    step.Quantity, sellPrice, sellStop, sellLimit);
                        (bool result, TradeParams tradeParams) sellResult = await exchangeApi.BuyOrSell(CryptoOrderType.Oco, CryptoOrderSide.Sell,
                            step.Quantity, sellPrice, sellStop, sellLimit);
                        if (sellResult.result)
                        {
                            // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
                            part.SellPrice = sellPrice;
                            if (!position.SellPrice.HasValue)
                            {
                                position.SellPrice = part.SellPrice; // (kan eigenlijk weg, slechts ter debug en tracering, voila)
                                PositionTools.SavePosition(databaseThread, position);
                            }

                            // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                            var sellStep = PositionTools.CreatePositionStep(position, part, sellResult.tradeParams, "SELL", CryptoTrailing.Trailing);
                            PositionTools.InsertPositionStep(databaseThread, position, sellStep);
                            PositionTools.AddPositionPartStep(part, sellStep);

                            string textx = string.Format("{0} POSITION SELL ORDER {1} LOCK PROFIT price={2} stopprice={3} stoplimit={4} quantity={5} quotequantity={6} type={7}",
                                Symbol.Name, sellStep.OrderId, sellStep.Price, sellStep.StopPrice, sellStep.StopLimitPrice, sellStep.Quantity, sellStep.Price * sellStep.Quantity,
                                sellResult.tradeParams.OrderType.ToString());
                            GlobalData.AddTextToLogTab(textx);
                            GlobalData.AddTextToTelegram(textx);
                        }
                    }

                }
            }

        }
    }


    private async Task CheckTimeout(CryptoTradeAccount tradeAccount, CryptoDatabase databaseThread, CryptoPosition position)
    {
        // Buy orders die niet gevuld worden binnen zoveel tijd annuleren

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (part.Name.Equals("BUY"))
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    if (step.Status == CryptoOrderStatus.New && step.Name.Equals("BUY"))
                    {
                        if (step.CreateTime.AddMinutes(GlobalData.Settings.Trading.GlobalBuyRemoveTime) < LastCandle1mCloseTimeDate)
                        {
                            await BinanceApi.Cancel(tradeAccount, Symbol, position, step.OrderId);
                            step.Status = CryptoOrderStatus.Expired;
                            step.CloseTime = LastCandle1mCloseTimeDate;
                            PositionTools.SavePositionStep(databaseThread, position, step);

                            string text2 = string.Format("{0} POSITION part {1} ORDER {2} {3} Cancel because of timeout", Symbol.Name, part.Id, step.OrderId, step.Name);
                            GlobalData.AddTextToLogTab(text2);
                            GlobalData.AddTextToTelegram(text2);

                            PositionTools.CalculatePositionResultsViaTrades(databaseThread, position);
                        }
                    }
                }
            }
        }
    }

    private async Task ClosePosition(CryptoTradeAccount tradeAccount, CryptoDatabase databaseThread, CryptoPosition position, bool repostionAllSellOrders = false)
    {
        //***********************************************************************************
        // Positie afgesloten? of alle sells opnieuw plaatsen (na verandering BE's)
        //***********************************************************************************

        // Afgesloten posities zijn niet langer interessant (tenzij we iets moeten annuleren/weghalen?)
        // Constatering: Als er iets gekocht wordt dan betekend het een verlaging van de BE.
        // ALLE Sell opdrachten moeten dan opnieuw gedaan worden (want: andere verkoop prijs)
        // De BUY en SELL orders worden opnieuw geplaatst indien een gedeelte van het geheel verkocht is.

        GlobalData.AddTextToLogTab(string.Format("{0} HandlePosition {1} repostionAllSellOrders={2}", Symbol.Name, position.Id, repostionAllSellOrders));

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status == CryptoOrderStatus.New)
                {
                    // Verwijder alle sell order (die worden verderop opnieuw geplaatst)
                    if (repostionAllSellOrders && !step.Name.Equals("SELL"))
                        continue;

                    await BinanceApi.Cancel(tradeAccount, Symbol, position, step.OrderId);
                    step.Status = CryptoOrderStatus.Expired;
                    step.CloseTime = LastCandle1mCloseTimeDate;
                    PositionTools.SavePositionStep(databaseThread, position, step);

                    string text2;
                    if (repostionAllSellOrders)
                        text2 = string.Format("{0} POSITION part {1} ORDER {2} {3} Cancel because of reposition", Symbol.Name, part.Id, step.OrderId, step.Name);
                    else
                        text2 = string.Format("{0} POSITION part {1} ORDER {2} {3} Cancel because of closing position", Symbol.Name, part.Id, step.OrderId, step.Name);
                    GlobalData.AddTextToLogTab(text2);
                    GlobalData.AddTextToTelegram(text2);
                }

            }
        }
    }

    private void CheckAddDca(CryptoTradeAccount tradeAccount, CryptoDatabase databaseThread, CryptoPosition position, bool repostionAllSellOrders = false)
    {
        // Moeten we een DCA plaatsen?
        if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoBuyStepInMethod.FixedPercentage && GlobalData.Settings.Trading.DcaPercentage > 0)
        {
            if (position.Status != CryptoPositionStatus.Trading)
                return;

            // Leuk bedacht, maar verre van correct, een DCA wordt dus nooit opnieuw geplaatst, maar altijd x% onder 
            // de laatste. In dit systeem kan de DCA vaker op hetzelfde % staan (zolang de koers zich maar hersteld)
            // (maar ik heb zo snel nog geen wiskundige oplossing hiervoor, wellicht price boven de laatste afgesloten DCA?)

            CryptoPositionStep step = null;
            foreach (CryptoPositionPart partX in position.Parts.Values.ToList())
            {
                foreach (CryptoPositionStep stepX in partX.Steps.Values.ToList())
                {
                    if (stepX.Status != CryptoOrderStatus.Expired && stepX.Trailing != CryptoTrailing.Trailing && stepX.Name.Equals("BUY"))
                    {
                        // Staat er al een openstaande DCA?
                        if (stepX.Trailing == CryptoTrailing.Reserved)
                            return;
                        // Onthoud de laatst geplaatste BUY
                        step = stepX;
                    }
                }
            }

            if (step != null && step.CloseTime.HasValue && step.CreateTime.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) < LastCandle1mCloseTimeDate)
            {
                decimal price = step.Price - (GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m);
                if (position.Symbol.LastPrice < price)
                    price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
                GlobalData.AddTextToLogTab(position.Symbol.Name + " DCA bijplaatsen op " + price.ToString0(position.Symbol.PriceDisplayFormat) + " (debug)");

                // De positie uitbreiden nalv een nieuw signaal
                // (de xe bijkoop wordt altijd een aparte DCA)
                position.PartCount += 1;

                // Verderop doen we wat met deze stap
                PositionTools positionTools = new(tradeAccount, Symbol, LastCandle1mCloseTimeDate);
                CryptoPositionPart part = positionTools.CreatePositionPart(position, "DCA", price); // voorlopige buyprice
                PositionTools.InsertPositionPart(databaseThread, part);
                PositionTools.AddPositionPart(position, part);

                // Duplicaat code, dit doet de HandleBuyPart routine al
                //BinanceApi exchangeApi = new(position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate);
                //(bool result, TradeParams tradeParams) result = await exchangeApi.PlaceBuyOrder(CryptoOrderType.Limit, null, price);
                //if (result.result)
                //{
                //    step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY", CryptoTrailing.Reserved);
                //    PositionTools.InsertPositionStep(databaseThread, position, step);
                //    PositionTools.AddPositionPartStep(part, step);
                //}
            }
        }
    }



    public async Task HandlePosition(CryptoTradeAccount tradeAccount, CryptoDatabase databaseThread, CryptoPosition position, bool repostionAllSellOrders = false)
    {
        // Wordt zowel vanuit de HandleTradeAsync aangeroepen als vanuit de 1m candle loop

        // PROBLEEM: De locking geeft momenteel problemen, maar het lijkt zonder ook prima te werken (nog eens puzzelen wat dit kan zijn!)
        //await tradeAccount.PositionListSemaphore.WaitAsync();
        //try
        // {

        await CheckTimeout(tradeAccount, databaseThread, position);

        // Positie sluiten of alle sell's weghalen (de naamgeving is verre van correct)
        if (position.CloseTime.HasValue || repostionAllSellOrders || position.RepositionSell)
            await ClosePosition(tradeAccount, databaseThread, position, repostionAllSellOrders);
        position.RepositionSell = false;

        // Een afgesloten posities is niet meer interessant, verplaatsen
        if (position.CloseTime.HasValue)
        {
            PositionTools.RemovePosition(tradeAccount, position);
            if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.AppStatusRunning)
                GlobalData.PositionsHaveChanged("");
            return;
        }


        if (Prepare(position, out CryptoSymbolInterval symbolInterval, out CryptoCandle candleInterval, out bool pauseBecauseOfTradingRules, out bool pauseBecauseOfBarometer))
        {
            // Moeten we een extra DCA plaatsen? (weggevallen functionaliteit geloof ik)
            if (!(pauseBecauseOfTradingRules || pauseBecauseOfBarometer))
                CheckAddDca(tradeAccount, databaseThread, position, repostionAllSellOrders);


            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                // Afgesloten parts zijn niet interessant
                if (part.CloseTime.HasValue)
                    continue;

                // Controleer BUY
                if (part.Name.Equals("BUY"))
                {
                    await HandleBuyPart(databaseThread, position, part, symbolInterval, candleInterval, GlobalData.Settings.Trading.BuyStepInMethod,
                        GlobalData.Settings.Trading.BuyOrderMethod, pauseBecauseOfTradingRules, pauseBecauseOfBarometer);
                }

                // Controleer DCA's
                if (position.Quantity > 0 && part.Name.Equals("DCA"))
                {
                    await HandleBuyPart(databaseThread, position, part, symbolInterval, candleInterval, CryptoBuyStepInMethod.TrailViaKcPsar, //GlobalData.Settings.Trading.DcaStepInMethod,
                        GlobalData.Settings.Trading.DcaOrderMethod, pauseBecauseOfTradingRules, pauseBecauseOfBarometer);
                }

                // Controleer SELL 
                if (position.Quantity > 0)
                {
                    await HandleSellPart(databaseThread, position, part, symbolInterval, candleInterval, GlobalData.Settings.Trading.DcaStepInMethod,
                        GlobalData.Settings.Trading.DcaOrderMethod, pauseBecauseOfTradingRules, pauseBecauseOfBarometer);
                }
            }
        }
        //}
        //finally
        //{
        //    //tradeAccount.PositionListSemaphore.Release();
        //}
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
                await PaperTradingCheckOrders(GlobalData.ExchangeBackTestAccount, false);
            if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                await PaperTradingCheckOrders(GlobalData.ExchangePaperTradeAccount, false);

            // Open or extend a position
            if (Symbol.SignalCount > 0)
                CreateOrExtendPosition();

            // Per (actief) trade account de posities controleren
            foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
            {
                // Aan de hand van de instellingen accounts uitsluiten
                if (!PositionTools.ValidTradeAccount(tradeAccount))
                    continue;

                // Check the positions
                if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var positionList))
                {
                    foreach (CryptoPosition position in positionList.Values.ToList())
                        await HandlePosition(tradeAccount, databaseThread, position, false);
                }
            }

            // Simuleer een Binance Trade om eventuele (net gemaakte) market orders te vullen
            // (het zou nog mooier zijn als we deze even in een lijst verzamelen, maar voila)
            if (GlobalData.BackTest)
                await PaperTradingCheckOrders(GlobalData.ExchangeBackTestAccount, true);
            if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                await PaperTradingCheckOrders(GlobalData.ExchangePaperTradeAccount, true);
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
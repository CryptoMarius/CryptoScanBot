//using CryptoSbmScanner.Binance;
//using CryptoSbmScanner.Model;
//using CryptoSbmScanner.Settings;
//using CryptoSbmScanner.Signal;
//using CryptoSbmScanner.Trading;
//using Skender.Stock.Indicators;

//using System.Collections.Concurrent;


//namespace CryptoSbmScanner.Intern;

//public class ThreadMonitorSignal
//{
    //public int monitorCount = 0; //Tellertje die in de taakbalk c.q. applicatie titel komt (indicatie meldingen)
    //private readonly BlockingCollection<CryptoSymbol> Queue = new();
    //private readonly CancellationTokenSource cancellationToken = new();

    //public void Stop()
    //{
    //    cancellationToken.Cancel();

    //    GlobalData.AddTextToLogTab(string.Format("Stopping monitoring signal"));
    //}

    //public void AddToQueue(CryptoSymbol data)
    //{
    //    Queue.Add(data);
    //}


    //public static bool CheckAvailableSlots(CryptoSymbol symbol, bool paperTrading, out string reaction)
    //{
    //    if (!SymbolTools.CheckAvailableSlotsExchange(symbol, paperTrading, GlobalData.Settings.Trading.SlotsMaximalExchange, out reaction))
    //        return false;

    //    if (!SymbolTools.CheckAvailableSlotsSymbol(symbol, paperTrading, GlobalData.Settings.Trading.SlotsMaximalSymbol, out reaction))
    //        return false;

    //    if (!SymbolTools.CheckAvailableSlotsBase(symbol, paperTrading, GlobalData.Settings.Trading.SlotsMaximalBase, out reaction))
    //        return false;

    //    if (!SymbolTools.CheckAvailableSlotsQuote(symbol, paperTrading, symbol.QuoteData.SlotsMaximal, out reaction))
    //        return false;

    //    reaction = "";
    //    return true;
    //}

    //public static async Task ProcessSignalAsync(CryptoSymbol symbol)
    //{
    //    // ******************************************
    //    // Check positions (remove, dca, trace, etc)
    //    // ******************************************

    //    // TODO: Heractiveren of verplaatsen naar de ThreadMonitorCandles?
    //    //if (symbol.PositionCount > 0)
    //    //{
    //    //    PositionMonitor monitorAlgorithm = new PositionMonitor();
    //    //    await monitorAlgorithm.CheckOpenPositions(symbol);
    //    //}

    //    bool hasPosition = symbol.PositionCount > 0;
    //    string lastPrice = symbol.LastPrice?.ToString(symbol.DisplayFormat);
    //    string text = "Monitor " + symbol.Name + " price=" + lastPrice;


    //    // **************************************************
    //    // Global checks like barometer, active bot etc..
    //    // **************************************************


    //    // Als de bot niet actief is dan ook geen monitoring (queue leegmaken)
    //    // Blijkbaar is de bot dan door de gebruiker uitgezet, verwijder de signalen
    //    if (!GlobalData.Settings.Trading.Active)
    //    {
    //        GlobalData.AddTextToLogTab(text + " trade-bot deactivated (removed)");
    //        symbol.ClearSignals();
    //        return;
    //    }

    //    // we doen (momenteel) alleen long posities
    //    if (!symbol.LastPrice.HasValue)
    //    {
    //        GlobalData.AddTextToLogTab(text + " symbol price null (removed)");
    //        symbol.ClearSignals();
    //        return;
    //    }

    //    // Om te voorkomen dat we te snel achter elkaar in dezelfde munt stappen
    //    if (symbol.LastTradeDate.HasValue && symbol.LastTradeDate > DateTime.UtcNow.AddMinutes(-GlobalData.Settings.Trading.GlobalBuyCooldownTime))
    //    {
    //        GlobalData.AddTextToLogTab(text + " is in cooldown (removed)");
    //        symbol.ClearSignals();
    //        return;
    //    }

    //    // Als een munt snel is gedaald dan stoppen
    //    if (GlobalData.Settings.Trading.PauseTradingUntil >= DateTime.UtcNow)
    //    {
    //        GlobalData.AddTextToLogTab(text + string.Format(" de bot is gepauseerd omdat {0} (removed)", GlobalData.Settings.Trading.PauseTradingText));
    //        symbol.ClearSignals();
    //        return;
    //    }


    //    if (!hasPosition)
    //    {
    //        // Alleen bij de 1e aankoop? (onzeker? kan ook later)
    //        // Is de barometer goed genoeg dat we willen traden?
    //        if (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval15m, GlobalData.Settings.Trading.Barometer15mBotMinimal, out string reaction) ||
    //        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval30m, GlobalData.Settings.Trading.Barometer30mBotMinimal, out reaction)) ||
    //        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1h, GlobalData.Settings.Trading.Barometer01hBotMinimal, out reaction)) ||
    //        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval4h, GlobalData.Settings.Trading.Barometer04hBotMinimal, out reaction)) ||
    //        (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1d, GlobalData.Settings.Trading.Barometer24hBotMinimal, out reaction))
    //        )
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //            symbol.ClearSignals();
    //            return;
    //        }


    //        // Bij nader inzien: Een hele partij onzin, want dit is door de Analyser gecontroleerd.
    //        // (in ieder geval de new coin, price, volume, white & black list)
    //        // De barometers kunnen wel verschillen

    //        // Indien sprake is van een whitelist, staat deze erop?
    //        if (!SymbolTools.CheckSymbolWhiteListOversold(symbol, out reaction))
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //            symbol.ClearSignals();
    //            return;
    //        }


    //        // Indien sprake is van een blacklist, staat deze erop?
    //        if (!SymbolTools.CheckSymbolBlackListOversold(symbol, out reaction))
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //            symbol.ClearSignals();
    //            return;
    //        }


    //        // Heeft de munt genoeg 24h volume? (via de instellingen, minimale volume)
    //        if (!SymbolTools.CheckValidMinimalVolume(symbol, out reaction))
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //            symbol.ClearSignals();
    //            return;
    //        }


    //        // Heeft de munt een redelijke prijs (geen "barcode" chart), moet nog in de instellingen erbij
    //        // TODO: Gaarne nieuwe instelling om deze te kunnen configureren
    //        if (!SymbolTools.CheckValidMinimalPrice(symbol, out reaction))
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //            symbol.ClearSignals();
    //            return;
    //        }


    //        // Is de munt te nieuw? (hebben we vertrouwen in nieuwe munten?)
    //        if (!SymbolTools.CheckNewCoin(symbol, out reaction))
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //            symbol.ClearSignals();
    //            return;
    //        }


    //        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
    //        // Dit gebruikt ik wel in een bot om uit de barcode charts weg te blijven, laat ik nog even staan
    //        decimal barcodePercentage = 100 * (symbol.PriceTickSize) / (decimal)symbol.LastPrice.Value;
    //        if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
    //        {
    //            // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
    //            GlobalData.AddTextToLogTab(string.Format("{0} Tick percentage te hoog {1:N3} (removed)", symbol.Name, barcodePercentage));
    //            symbol.ClearSignals();
    //            return;
    //        }
    //    }


    //    //GlobalData.AddTextToLogTab("Monitor " + symbol.Name); te druk in de log

    //    // **************************************************
    //    // Per interval kan er een signaal aanwezig zijn
    //    // **************************************************
    //    foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
    //    {
    //        CryptoSignal signal = symbolInterval.Signal;
    //        if (signal == null)
    //            continue;

    //        text = "Monitor " + signal.DisplayText + " price=" + lastPrice;

    //        // Willen we traden op dit interval
    //        if (!TradingConfig.MonitorInterval.ContainsKey(signal.Interval.IntervalPeriod))
    //        {
    //            GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this interval (removed)");
    //            symbolInterval.Signal = null;
    //            continue;
    //        }

    //        // Willen we traden met deze strategy
    //        if (!TradingConfig.Config[TradeDirection.Long].MonitorStrategy.ContainsKey(signal.Strategy))
    //        {
    //            GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this strategy (removed)");
    //            symbolInterval.Signal = null;
    //            continue;
    //        }

    //        //??????????????????????????????????????????????? TODO!
    //        // TODO: we doen (momenteel) alleen long posities
    //        if (signal.Mode != TradeDirection.Long)
    //        {
    //            GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " only acception long signals (removed)");
    //            symbolInterval.Signal = null;
    //            continue;
    //        }

    //        // Er zijn (technisch) niet altijd candles aanwezig
    //        if (!symbolInterval.CandleList.Any())
    //        {
    //            GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
    //            symbolInterval.Signal = null;
    //            continue;
    //        }
    //        CryptoCandle candle = symbolInterval.CandleList.Values.Last();

    //        // Bestaan het gekozen strategy wel, klinkt raar, maar is (op dit moment) niet altijd geimplementeerd
    //        SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(signal.Mode, signal.Strategy, signal.Symbol, signal.Interval, candle);
    //        if (algorithm == null)
    //        {
    //            GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " unknown algorithm (removed)");
    //            symbolInterval.Signal = null;
    //            continue;
    //        }

    //        if (candle.CandleData == null)
    //        {
    //            // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
    //            List<CryptoCandle> History = CandleIndicatorData.CalculateCandles(symbol, signal.Interval, candle.OpenTime, out string reaction);
    //            if (History == null)
    //            {
    //                GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
    //                symbolInterval.Signal = null;
    //                continue;
    //            }

    //            if (History.Count == 0)
    //            {
    //                reaction = "Geen candles";
    //                GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
    //                symbolInterval.Signal = null;
    //                continue;
    //            }

    //            CandleIndicatorData.CalculateIndicators(History);
    //        }

    //        //GlobalData.AddTextToLogTab("Monitor check GiveUp() debug " + signal.DisplayText);
    //        if (algorithm.GiveUp(signal))
    //        {
    //            GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " " + algorithm.ExtraText + " giveup (removed)");
    //            symbolInterval.Signal = null;
    //            continue;
    //        }


    //        if (!algorithm.AllowStepIn(signal))
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + "  (not allowed yet, waiting)");
    //            continue;
    //        }

    //        //******************************************
    //        // Dit lijkt een okay signaal
    //        // (wat een reeks van controles!)
    //        //******************************************
    //        //******************************************
    //        // GO.GO.GO.
    //        //******************************************

    //        GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY ***");

    //        // We stappen in, haal het signaal weg uit de lijst
    //        // Later aangepast met de volgende gedachte in het achterhoofd, alle algoritmes die oversold
    //        // hebben kunnen we wachten op een horizontale sma20 en die doet het ook verrassend goed


    //        //******************************************
    //        // notify "possible" start of position
    //        //******************************************
    //        if (GlobalData.Settings.Trading.DoNotEnterTrade)
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY *** (papertrade) (removed)");
    //            GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY *** (papertrade) (removed)");
    //            // Deze heeft voorrang boven de andere 3..
    //            symbolInterval.Signal = null;
    //            continue;
    //        }


    //        //******************************************
    //        // Webhook Altrady - open position via PaperTrading
    //        // (no slot checks, ackward interface, no adjustments)
    //        //******************************************
    //        if (GlobalData.Settings.Trading.TradeViaAltradyWebhook)
    //        {
    //            GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY (altrady webhook) *** entering....");
    //            GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY (altrady webhook) *** entering....");
    //            AltradyWebhook.ExecuteBuy(signal);
    //        }


    //        //******************************************
    //        // Binance PaperTrading (via bot)
    //        //******************************************
    //        if (GlobalData.Settings.Trading.TradeViaPaperTrading)
    //        {
    //            if (hasPosition)
    //            {
    //                // de positie aanpassen/uitbreiden enzovoort
    //                // TODO - over nadenken of dit wel de juiste plek is.....
    //                // Of we zetten alles hier heen (dat is niet mogelijk, alleen na een signaal)
    //                // Maar interessant, wellichtn iet eens zo lastig als ik eerst dacht.

    //                //if (GlobalData.Settings.Trading.DcaMethod == DcaMethod.FixedPercentage)
    //                {
    //                    // Bijkoop op een vaste percentage (dit wordt door de MonitorCandle gedaan)
    //                    // Dit gaat door middel van een limit-buy order op het opgegeven percentage
    //                    // Deze wordt na een bepaalde (cooldown) tijd geplaatst
    //                }
    //                //if (GlobalData.Settings.Trading.DcaMethod == DcaMethod.AfterNextSignal)
    //                {
    //                    // Bijkoop op dezelfde manier als we de 1e aankoop hebben gedaan (geen vaste percentages)
    //                    // Dit gaat door middel van een limit-buy order 
    //                    // Deze wordt na een bepaalde (cooldown) tijd geplaatst
    //                    // Uitzoeken: wellicht met een verplichte percentage?
    //                    // En tevens een ideale insteek voor een jojo trade (meerdere tp's, c.q. trace naar boven)
    //                }
    //                //if (GlobalData.Settings.Trading.DcaMethod == DcaMethod.TrailViaKcPsar)
    //                {
    //                    // Bijkopen door op de bovenste KC/PSAR naar beneden te tracen (de cc#2 methode)
    //                    // De stop-limit buy moet iedere candle verplaatst worden (administratie in step nodig, iedere keer een nieuwe step maken (of eentje hergebruiken na x minuten))
    //                    // Deze wordt na een bepaalde (cooldown) tijd geplaatst
    //                    // Uitzoeken: wellicht met een verplichte percentage?
    //                }

    //                // Maar voorlopig even niet, de default is nu het gefixeerde percentage
    //                string reaction = "Afgesterd";
    //                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //                symbol.ClearSignals();
    //                return;
    //            }
    //            else
    //            {
    //                // Indien er geen slots zijn direct ophouden met controles
    //                if (!CheckAvailableSlots(symbol, true, out string reaction))
    //                {
    //                    GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //                    symbol.ClearSignals();
    //                    return;
    //                }
    //            }


    //            // todo..
    //            GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY (binance papertrading) *** entering....");
    //            GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY (binance papertrading) *** entering....");
    //            var x = await BinanceApi.DoOnSignal(signal, true);
    //            if (!x.result)
    //            {
    //                GlobalData.AddTextToLogTab(text + " try buy failed, result= " + x.reaction);
    //                GlobalData.AddTextToTelegram(text + " try buy failed, result= " + x.reaction);
    //                GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " BUY failed (removed)");
    //            }
    //        }



    //        //******************************************
    //        // Binance Trading (via bot)
    //        //******************************************
    //        if (GlobalData.Settings.Trading.TradeViaBinance)
    //        {
    //            // Indien er geen slots zijn direct ophouden met controles
    //            if (!CheckAvailableSlots(symbol, true, out string reaction))
    //            {
    //                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
    //                symbol.ClearSignals();
    //                return;
    //            }

    //            if (hasPosition)
    //            {
    //                // Een DCA starten (of een trace adhv de instellingen)
    //                // De bijkopp-cooldown is hier dus al okay
    //            }


    //            GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY (binance api) *** entering....");
    //            GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY (binance api) *** entering....");
    //            var x = await BinanceApi.DoOnSignal(signal, false);
    //            if (!x.result)
    //            {
    //                GlobalData.AddTextToLogTab(text + " try buy failed, result= " + x.reaction);
    //                GlobalData.AddTextToTelegram(text + " try buy failed, result= " + x.reaction);
    //                GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " BUY failed (removed)");
    //            }
    //        }

    //        // Voor de zekerheid het signaal weghalen (anders blijft het lang in 
    //        // de queue en blokkeert het meer recente signalen wat jammer zou zijn)
    //        symbolInterval.Signal = null;
    //    }

    //}



    //public async Task ExecuteAsync()
    //{
    //    try
    //    {
    //        // Analyseer de signalen en stap eventueel in
    //        foreach (CryptoSymbol symbol in Queue.GetConsumingEnumerable(cancellationToken.Token))
    //        {
    //            monitorCount++;
    //            try
    //            {
    //                await symbol.Exchange.PositionListSemaphore.WaitAsync();
    //                try
    //                {
    //                    await ProcessSignalAsync(symbol);
    //                }
    //                finally
    //                {
    //                    symbol.Exchange.PositionListSemaphore.Release();
    //                }
    //            }
    //            catch (Exception error)
    //            {
    //                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
    //                GlobalData.Logger.Error(error);
    //                GlobalData.AddTextToLogTab("\r\n" + "\r\n" + symbol.Name + " error monitor thread\r\n" + error.ToString());
    //            }
    //        }
    //    }
    //    catch (Exception error)
    //    {
    //        // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
    //        GlobalData.Logger.Error(error);
    //        GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error monitor thread\r\n" + error.ToString());
    //    }

    //    GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR THREAD EXIT");
    //}
//}

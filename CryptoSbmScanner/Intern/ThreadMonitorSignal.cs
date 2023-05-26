using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using CryptoSbmScanner.Trading;
using Skender.Stock.Indicators;

using System.Collections.Concurrent;


namespace CryptoSbmScanner.Intern;

public class ThreadMonitorSignal
{
    public int monitorCount = 0; //Tellertje die in de taakbalk c.q. applicatie titel komt (indicatie meldingen)
    private readonly BlockingCollection<CryptoSymbol> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stopping monitoring signal"));
    }

    public void AddToQueue(CryptoSymbol data)
    {
        Queue.Add(data);
    }


    private static async void ProcessSignalList(CryptoSymbol symbol)
    {
        // ******************************************
        // Check positions (remove, dca, trace, etc)
        // ******************************************

        // TODO: Reactivate of weghalen?
        //if (symbol.PositionCount > 0)
        //{
        //    PositionMonitor monitorAlgorithm = new PositionMonitor();
        //    await monitorAlgorithm.CheckOpenPositions(symbol);
        //}


        // ******************************************
        // De signals controleren
        // ******************************************


        // **************************************************
        // Global checks like barometer, active bot etc..
        // **************************************************


        // Als de bot niet actief is dan ook geen monitoring (queue leegmaken)
        // Blijkbaar is de bot dan door de gebruiker uitgezet, verwijder de signalen
        if (!GlobalData.Settings.Trading.Active)
        {
            GlobalData.AddTextToLogTab("Monitor " + symbol.Name + " trade-bot deactivated (removed)");
            symbol.ClearSignals();
            return;
        }

        // we doen (momenteel) alleen long posities
        if (!symbol.LastPrice.HasValue)
        {
            GlobalData.AddTextToLogTab("Monitor " + symbol.Name + " symbol price null (removed)");
            symbol.ClearSignals();
            return;
        }

        // Om te voorkomen dat we te snel achter elkaar in dezelfde munt stappen
        if (symbol.LastTradeDate.HasValue && symbol.LastTradeDate > DateTime.UtcNow.AddMinutes(-GlobalData.Settings.Trading.GlobalBuyCooldownTime))
        {
            GlobalData.AddTextToLogTab("Monitor " + symbol.Name + " is in cooldown (removed)");
            symbol.ClearSignals();
            return;
        }

        // Als een munt snel is gedaald dan stoppen
        if (GlobalData.Settings.Trading.PauseTradingUntil >= DateTime.UtcNow)
        {
            GlobalData.AddTextToLogTab(string.Format("De bot is gepauseerd omdat {0} (removed)", GlobalData.Settings.Trading.PauseTradingText));
            symbol.ClearSignals();
            return;
        }

        // Is de barometer goed genoeg dat we willen traden?
        if (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval15m, GlobalData.Settings.Trading.Barometer15mBotMinimal, out string reaction) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval30m, GlobalData.Settings.Trading.Barometer30mBotMinimal, out reaction)) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1h, GlobalData.Settings.Trading.Barometer01hBotMinimal, out reaction)) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval4h, GlobalData.Settings.Trading.Barometer04hBotMinimal, out reaction)) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1d, GlobalData.Settings.Trading.Barometer24hBotMinimal, out reaction))
            )
        {
            //if (GlobalData.Settings.Signal.LogBarometerToLow)
            {
                GlobalData.AddTextToLogTab("Monitor " + symbol.Name + " " + reaction + " (removed)");
            }
            symbol.ClearSignals();
            return;
        }

        string lastPrice = symbol.LastPrice?.ToString(symbol.DisplayFormat);
        string text = "Monitor " + symbol.Name + " price=" + lastPrice;


        // Bij nader inzien: Een hele partij onzin, want dit is door de Analyser gecontroleerd.
        // (in ieder geval de new coin, price, volume, white & black list)
        // De barometers kunnen wel verschillen

        // Indien sprake is van een whitelist, staat deze erop?
        if (!SymbolTools.CheckSymbolWhiteListOversold(symbol, out reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            symbol.ClearSignals();
            return;
        }


        //Indien sprake is van een blacklist, staat deze erop?
        if (!SymbolTools.CheckSymbolBlackListOversold(symbol, out reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            symbol.ClearSignals();
            return;
        }


        // Heeft de munt genoeg 24h volume? (via de instellingen, minimale volume)
        if (!SymbolTools.CheckValidMinimalVolume(symbol, out reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            symbol.ClearSignals();
            return;
        }


        // Heeft de munt een redelijke prijs (geen "barcode" chart), moet nog in de instellingen erbij
        // TODO: Gaarne nieuwe instelling om deze te kunnen configureren
        if (!SymbolTools.CheckValidMinimalPrice(symbol, out reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            symbol.ClearSignals();
            return;
        }



        // Is de munt nieuw? (hebben we vertrouwen in nieuwe munten?)
        if (!SymbolTools.CheckNewCoin(symbol, out reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            symbol.ClearSignals();
            return;
        }



        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
        // Dit gebruikt ik wel in een bot om uit de barcode charts weg te blijven, laat ik nog even staan
        decimal barcodePercentage = 100 * (symbol.PriceTickSize) / (decimal)symbol.LastPrice.Value;
        if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
        {
            // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
            GlobalData.AddTextToLogTab(string.Format("{0} Tick percentage te hoog {1:N3} (removed)", symbol.Name, barcodePercentage));
            symbol.ClearSignals();
            return;
        }


        GlobalData.AddTextToLogTab("Monitor " + symbol.Name);

        // **************************************************
        // Per interval kan er een signaal aanwezig zijn
        // **************************************************
        foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
        {
            if (symbolInterval.Signal == null)
                continue;
            CryptoSignal signal = symbolInterval.Signal;
            text = "Monitor " + signal.DisplayText + " price=" + lastPrice;

            // Willen we traden op dit interval
            if (!TradingConfig.MonitorInterval.ContainsKey(signal.Interval.IntervalPeriod))
            {
                GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this interval (removed)");
                symbolInterval.Signal = null;
                continue;
            }

            // Willen we traden met deze strategy
            if (!TradingConfig.Config[TradeDirection.Long].MonitorStrategy.ContainsKey(signal.Strategy))
            {
                GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this strategy (removed)");
                symbolInterval.Signal = null;
                continue;
            }

            //??????????????????????????????????????????????? TODO!
            // TODO: we doen (momenteel) alleen long posities
            if (signal.Mode != TradeDirection.Long)
            {
                GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " only acception long signals (removed)");
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
            CryptoCandle candle = symbolInterval.CandleList.Values.Last();

            // Bestaan het gekozen strategy wel, klinkt raar, maar is (op dit moment) niet altijd geimplementeerd
            SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(signal.Mode, signal.Strategy, signal.Symbol, signal.Interval, candle);
            if (algorithm == null)
            {
                GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " unknown algorithm (removed)");
                symbolInterval.Signal = null;
                continue;
            }

            if (candle.CandleData == null)
            {
                // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
                List<CryptoCandle> History = CandleIndicatorData.CalculateCandles(symbol, signal.Interval, candle.OpenTime, out reaction);
                if (History == null)
                {
                    GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                if (History.Count == 0)
                {
                    reaction = "Geen candles";
                    GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                CandleIndicatorData.CalculateIndicators(History);
            }

            //GlobalData.AddTextToLogTab("Monitor check GiveUp() debug " + signal.DisplayText);
            if (algorithm.GiveUp(signal))
            {
                GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " " + algorithm.ExtraText + " giveup (removed)");
                symbolInterval.Signal = null;
                continue;
            }



            //******************************************
            // Hehe, dan lijkt DIT wel een okay signaal
            // (wat een eindeloze reeks van controles!)
            //******************************************

            if (!algorithm.AllowStepIn(signal))
            {
                GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + "  (not allowed yet, waiting)");
                continue;
            }


            //******************************************
            // GO.GO.GO.
            //******************************************

            GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY ***");

            // We stappen in, haal het signaal weg uit de lijst
            // Later aangepast met de volgende gedachte in het achterhoofd, alle algoritmes die oversold
            // hebben kunnen we wachten op een horizontale sma20 en die doet het ook verrassend goed


            if (GlobalData.Settings.Trading.DoNotEnterTrade)
            {
                GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY *** (papertrade) (removed)");
                GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY *** (papertrade) (removed)");
                symbolInterval.Signal = null;
                continue;
            }



            // Webhook Altrady - testing via PaperTrading
            if (GlobalData.Settings.Trading.TradeViaAltradyWebhook)
            {
                GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY (altrady webhook) *** entering....");
                GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY (altrady webhook) *** entering....");
                AltradyWebhook.ExecuteBuy(signal);
                symbolInterval.Signal = null;
                continue;
            }

            if (GlobalData.Settings.Trading.TradeViaBinance)
            {                
                GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " *** try BUY (binance api) *** entering....");
                GlobalData.AddTextToTelegram(text + " " + algorithm.ExtraText + " *** try BUY (binance api) *** entering....");
                //SignalMonitor signalMonitor = new(signal);
                var x = await BinanceApi.DoOnSignal(signal);
                if (!x.result)
                {
                    GlobalData.AddTextToLogTab(text + " try buy failed, result= " + x.reaction);
                    GlobalData.AddTextToTelegram(text + " try buy failed, result= " + x.reaction);
                    // Voor de zekerheid het signaal direct weghalen (anders blijft het lang in 
                    // de queue en blokkeerd het meer recente signalen wat jammer zou zijn)
                    GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + " BUY failed (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }
            }

        }

    }



    public async Task ExecuteAsync()
    {
        try
        {
            // Analyseer de signalen en stap eventueel in
            foreach (CryptoSymbol symbol in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                monitorCount++;
                try
                {
                    await symbol.Exchange.PositionListSemaphore.WaitAsync();
                    try
                    {
                        ProcessSignalList(symbol);
                    }
                    finally
                    {
                        symbol.Exchange.PositionListSemaphore.Release();
                    }
                }
                catch (Exception error)
                {
                    // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                    GlobalData.Logger.Error(error);
                    GlobalData.AddTextToLogTab("\r\n" + "\r\n" + symbol.Name + " error monitor thread\r\n" + error.ToString());
                }
            }
        }
        catch (Exception)
        {
            //ignore
        }

        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR THREAD EXIT");
    }
}

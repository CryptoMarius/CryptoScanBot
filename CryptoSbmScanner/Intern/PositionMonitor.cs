using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Exchange.Binance; // trading...
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public class PositionMonitor : IDisposable
{
    // Tellertje die getoond wordt in applicatie (indicatie van aantal meldingen)
    public static int AnalyseCount = 0;
    public CryptoSymbol Symbol { get; set; }
    public Model.CryptoExchange Exchange { get; set; }

    // De laatste gesloten 1m candle
    public CryptoCandle LastCandle1m { get; set; }
    // De sluittijd van deze candle (als unixtime) - De CurrentTime bij backtesting
    public long LastCandle1mCloseTime { get; set; }
    // De sluittijd van deze candle (als DateTime) - De CurrentTime bij backtesting
    public DateTime LastCandle1mCloseTimeDate { get; set; }

    public CryptoDatabase Database { get; set; } = new();


    public PositionMonitor(CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        Symbol = symbol;
        Exchange = symbol.Exchange;

        // De laatste 1m candle die definitief is
        LastCandle1m = lastCandle1m;
        LastCandle1mCloseTime = lastCandle1m.OpenTime + 60;
        LastCandle1mCloseTimeDate = CandleTools.GetUnixDate(LastCandle1mCloseTime);

        Database.Open();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Database != null)
            {
                Database.Close();
                Database.Dispose();
                Database = null;
            }
        }
    }

#if TRADEBOT
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


    private static CryptoPositionStep LowestClosedBuyStep(CryptoPosition position)
    {
        // Retourneer de buy order van een niet afgesloten part (de laagste)
        CryptoPositionStep step = null;
        foreach (CryptoPositionPart partX in position.Parts.Values.ToList())
        {
            // Afgesloten DCA parts sluiten we uit (omdat we zogenaamde jojo's uitvoeren)
            if (!partX.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep stepX in partX.Steps.Values.ToList())
                {
                    // Voor de zekerheid enkel de Status=Filled erbij (onzeker wat er exact gebeurd met een cancel en het kan geen kwaad)
                    if (stepX.Side == CryptoOrderSide.Buy && stepX.CloseTime.HasValue && stepX.Status == CryptoOrderStatus.Filled)
                    {
                        if (step == null || stepX.Price < step.Price)
                            step = stepX;
                    }
                }
            }
        }
        return step;
    }


    private bool CanOpenAdditionalDca(CryptoPosition position, decimal signalPrice, out CryptoPositionStep step, out decimal percentage, out string reaction)
    {
        // Een bijkoop zonder een voorgaande buy is onmogelijk
        step = LowestClosedBuyStep(position);
        if (step == null)
        {
            reaction = "Geen eerste BUY price gevonden";
            percentage = 0;
            return false;
        }

        // Average prijs vanwege gespreide market of stoplimit order
        decimal lastBuyPrice = step.QuoteQuantityFilled / step.Quantity; // step.AvgPrice
        percentage = 100m * (lastBuyPrice - signalPrice) / lastBuyPrice;


        // het percentage geld voor alle mogelijkheden
        // Het percentage moet in ieder geval x% onder de vorige buy opdracht zitten
        // (en dit heeft voordelen want dan hoef je niet te weten in welke DCA-index je zit!)
        // TODO: Dat zou (CC2 technisch) ook een percentage van de BB kunnen zijn als 3e optie.
        if (percentage < GlobalData.Settings.Trading.DcaPercentage)
        {
            reaction = $" het is te vroeg voor een bijkoop vanwege het percentage {percentage.ToString0("N2")}";
            return false;
        }


        // Er moet in ieder geval cooldown tijd verstreken zijn ten opzichte van de vorige buy opdracht
        // Nu is dit de laagste gesloten buy order, maar is dat ook automatisch de laatste? (denk het wel)
        // De datum moet de laatste activiteit zijn die in deze positie heeft plaatsgevonden qua steps (close of creatie)
        if (step.CloseTime?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > LastCandle1mCloseTimeDate)
        {
            reaction = "het is te vroeg voor een bijkoop vanwege de cooldown";
            Symbol.ClearSignals();
            return false;
        }


        // Controle 3: Is er al een openstaande DCA?
        // TODO: Detectie van een gewijzigd percentage wordt niet uitgevoerd! (Settings Change)
        foreach (CryptoPositionPart partX in position.Parts.Values.ToList())
        {
            if (partX.Name.Equals("DCA") && !partX.CloseTime.HasValue)
            {
                // Dit kan ook, er is er net een DCA gemaakt (waarschijnlijk voor ander tijdframe)
                if (partX.Steps.Count == 0)
                {
                    reaction = "de positie heeft al een openstaande DCA";
                    return false;
                }

                foreach (CryptoPositionStep stepX in partX.Steps.Values.ToList())
                {
                    if (stepX.Name.Equals("BUY") && stepX.Status == CryptoOrderStatus.New)
                    {
                        // Er staan een buy order klaar, dus openen we geen nieuwe DCA
                        if (stepX.Trailing == CryptoTrailing.None && stepX.OrderType == CryptoOrderType.Limit)
                        {
                            reaction = "de positie heeft al een openstaande DCA";
                            return false;
                        }

                        // We zijn aan het trailen, dus openen we geen nieuwe DCA
                        if (stepX.Trailing == CryptoTrailing.Trailing && stepX.OrderType == CryptoOrderType.StopLimit)
                        {
                            reaction = "de positie heeft al een openstaande trailing DCA";
                            return false;
                        }
                    }
                }
            }
        }

        reaction = "";
        return true;
    }

    public void CreateOrExtendPositionViaSignal()
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
                // (kan aangeroepen worden op meerdere TF's)
                //******************************************

                foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
                {
                    if (!PositionTools.ValidTradeAccount(tradeAccount, Symbol))
                        continue;

                    // Ter debug 2 keer achter elkaar, waarom wordt er een positie gemaakt?
                    if (!PositionTools.ValidTradeAccount(tradeAccount, Symbol))
                        continue;

                    PositionTools positionTools = new(tradeAccount, Symbol, LastCandle1mCloseTimeDate);

                    // TODO - Assets, wordt op 2 plekken gecontroleerd (ook in de BinanceApi.DoOnSignal)

                    //string reaction;
                    decimal assetQuantity;
                    if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
                    {
                        // Is er een API key aanwezig (met de juiste opties)
                        if (!SymbolTools.CheckValidApikey(out reaction))
                        {
                            GlobalData.AddTextToLogTab(text + " " + reaction);
                            continue;
                        }

                        // TODO: Asset management in een future account werkt anders!

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



                    CryptoPosition position = PositionTools.HasPosition(tradeAccount, Symbol); //, symbolInterval
                    if (position == null)
                    {
                        if (GlobalData.Settings.Trading.DisableNewPositions)
                        {
                            reaction = "openen van nieuwe posities niet toegestaan";
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
                        position.PartCount = 1;
                        PositionTools.InsertPosition(Database, position);
                        PositionTools.AddPosition(tradeAccount, position);

                        // Verderop doen we wat met deze stap en zetten we de echte buy of step)
                        CryptoPositionPart part = positionTools.CreatePositionPart(position, "BUY", signal.Price); // voorlopige buyprice
                        part.StepInMethod = CryptoBuyStepInMethod.AfterNextSignal;
                        PositionTools.InsertPositionPart(Database, part);
                        PositionTools.AddPositionPart(position, part);
                        PositionTools.SavePosition(Database, position);

                        // In de veronderstelling dat dit allemaal lukt
                        Symbol.LastTradeDate = LastCandle1mCloseTimeDate;

                        if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                            GlobalData.PositionsHaveChanged("");
                    }
                    else
                    {
                        // Alleen bijkopen als we ONDER de break-even prijs zitten
                        if (signal.Price < position.BreakEvenPrice)
                        {
                            // En een paar aanvullende condities...
                            if (!CanOpenAdditionalDca(position, signal.Price, out CryptoPositionStep step, out decimal percentage, out reaction))
                            {
                                GlobalData.AddTextToLogTab($"{text} {symbolInterval.Interval.Name} {reaction} (removed)");
                                Symbol.ClearSignals();
                                return;
                            }


                            GlobalData.AddTextToLogTab($"{text} DCA positie op percentage {percentage.ToString0("N2")}");

                            // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                            // Verderop doen we wat met deze stap en zetten we de echte buy of step)
                            position.PartCount += 1;
                            CryptoPositionPart part = positionTools.CreatePositionPart(position, "DCA", signal.Price); // voorlopige buyprice
                            PositionTools.InsertPositionPart(Database, part);
                            part.StepInMethod = CryptoBuyStepInMethod.AfterNextSignal;
                            PositionTools.AddPositionPart(position, part);
                            PositionTools.SavePosition(Database, position);

                            // In de veronderstelling dat dit allemaal lukt (nieuw signaal gedurende een trail wordt hierdoor uitgesloten)
                            Symbol.LastTradeDate = LastCandle1mCloseTimeDate;
                        }
                    }

                }

            }

        }
    }


    public async Task PaperTradingCheckStep(CryptoPosition position, CryptoPositionStep step)
    {
        if (step.Status == CryptoOrderStatus.New)
        {
            if (step.Side == CryptoOrderSide.Buy)
            {
                if (step.OrderType == CryptoOrderType.Market) // is reeds afgehandeld
                    await PaperTrading.CreatePaperTradeObject(Database, position, step, LastCandle1m.Close, LastCandle1mCloseTimeDate);
                if (step.StopPrice.HasValue)
                {
                    if (LastCandle1m.High > step.StopPrice)
                        await PaperTrading.CreatePaperTradeObject(Database, position, step, (decimal)step.StopPrice, LastCandle1mCloseTimeDate);
                }
                else if (LastCandle1m.Low < step.Price)
                    await PaperTrading.CreatePaperTradeObject(Database, position, step, step.Price, LastCandle1mCloseTimeDate);
            }
            else if (step.Side == CryptoOrderSide.Sell)
            {
                if (step.OrderType == CryptoOrderType.Market)  // is reeds afgehandeld
                    await PaperTrading.CreatePaperTradeObject(Database, position, step, LastCandle1m.Close, LastCandle1mCloseTimeDate);
                else if (step.StopPrice.HasValue)
                {
                    if (LastCandle1m.Low < step.StopPrice)
                        await PaperTrading.CreatePaperTradeObject(Database, position, step, (decimal)step.StopPrice, LastCandle1mCloseTimeDate);
                }
                else if (LastCandle1m.High > step.Price)
                    await PaperTrading.CreatePaperTradeObject(Database, position, step, step.Price, LastCandle1mCloseTimeDate);

            }
        }
    }

    private async Task PaperTradingCheckOrders(CryptoTradeAccount tradeAccount)
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
                        await PaperTradingCheckStep(position, step);
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


    private bool Prepare(CryptoPosition position, out CryptoCandle candleInterval, out bool pauseBecauseOfTradingRules, out bool pauseBecauseOfBarometer)
    {
        candleInterval = null;
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(position.Interval.IntervalPeriod);


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
        // Mmmmmm, dan worden vervolg orders niet bepaald snel geplaatst!
        if (LastCandle1mCloseTime % position.Interval.Duration != 0)
            return false;


        // Niet zomaar een laatste candle nemen in verband met Backtesting
        long bla = LastCandle1mCloseTime % symbolInterval.Interval.Duration;
        long candleOpenTimeInterval = LastCandle1mCloseTime - bla - symbolInterval.Interval.Duration;


        // Die indicator berekening had ik niet verwacht (cooldown?)
#if USELOCKS
        Monitor.Enter(position.Symbol.CandleList);
#endif
        try
        {
            // Niet zomaar een laatste candle nemen in verband met Backtesting
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTimeInterval, out candleInterval))
            {
                string t = string.Format("candle 1m interval: {0}", LastCandle1m.DateLocal.ToString()) + ".." + LastCandle1mCloseTimeDate.ToLocalTime() + "\r\n" +
                string.Format("is de candle op het {0} interval echt missing in action?", position.Interval.Name) + "\r\n" +
                    string.Format("position.CreateDate = {0}", position.CreateTime.ToString()) + "\r\n";
                throw new Exception($"Candle niet aanwezig? {t}");
            }

            if (candleInterval.CandleData == null)
            {
                List<CryptoCandle> history = null;
                history = CandleIndicatorData.CalculateCandles(Symbol, symbolInterval.Interval, candleInterval.OpenTime, out string response);
                if (history == null)
                {
                    GlobalData.AddTextToLogTab("Analyse " + response);
                    throw new Exception($"{position.Symbol.Name} Candle {symbolInterval.Interval.Name} {candleInterval.DateLocal} niet berekend? {response}");
                }

                // Eenmalig de indicators klaarzetten
                CandleIndicatorData.CalculateIndicators(history);
            }
        }
        finally
        {
#if USELOCKS
            Monitor.Exit(position.Symbol.CandleList);
#endif
        }


        return true;
    }


    private static decimal CalculateBuyOrDcaPrice(CryptoPositionPart part, CryptoBuyOrderMethod buyOrderMethod, decimal defaultPrice)
    {
        // Wat wordt de prijs? (hoe graag willen we in de trade?)
        decimal price = defaultPrice;
        switch (buyOrderMethod)
        {
            case CryptoBuyOrderMethod.SignalPrice:
                //price = defaultPrice;
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
                // De optie is vervallen maar blijft interessant, echter welke BB gebruik je dan (de actuele denk ik?, dus rekening houden met BE enzovoort)
                // voorlopig even afgesterd
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


    private async Task<(bool cancelled, TradeParams tradeParams)> CancelOrder(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // waarom eigenlijk, wat wil ik nu met die buy en sell price? (debuggen, maar verder?)
        if (step.Side == CryptoOrderSide.Buy && part.Name.Equals("BUY"))
        {
            position.BuyPrice = null;
        }
        position.UpdateTime = LastCandle1mCloseTimeDate;
        PositionTools.SavePosition(Database, position);

        // Annuleer de vorige buy order
        var result = await Api.Cancel(position.TradeAccount, Symbol, step);
        step.Status = CryptoOrderStatus.Expired;
        step.CloseTime = LastCandle1mCloseTimeDate;
        PositionTools.SavePositionStep(Database, position, step);
        return result;
    }


    private async Task HandleBuyPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval, 
        CryptoBuyStepInMethod stepInMethod, CryptoBuyOrderMethod buyOrderMethod, bool pauseBecauseOfTradingRules, bool pauseBecauseOfBarometer)
    {
        // Controleer de BUY
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", false);

        // Verwijderen de buy vanwege een te lage barometer, pauseer stand of timeout (behalve trailing of reserved dca)
        if (pauseBecauseOfTradingRules || pauseBecauseOfBarometer || BuyStepTimeOut(position, step))
        {
            // Maar laat een trailing buy met rust (gaat ten slotte al naar beneden)
            if (step != null && step.Trailing == CryptoTrailing.None)
            {
                string text3;
                if (pauseBecauseOfTradingRules)
                    text3 = "annuleren vanwege trading regels";
                else if (pauseBecauseOfBarometer)
                    text3 = "annuleren vanwege lage barometer";
                else
                    text3 = "annuleren vanwege timeout";

                var (cancelled, tradeParams) = await CancelOrder(position, part, step);
                if (GlobalData.Settings.Trading.LogCanceledOrders)
                    Api.Dump(position.Symbol, cancelled, tradeParams, text3);

            }
            return;
        }



        // Als de instellingen veranderd zijn de lopende order annuleren
        if (step != null && part.StepInMethod != stepInMethod)
        {
            var (cancelled, tradeParams) = await CancelOrder(position, part, step);
            if (GlobalData.Settings.Trading.LogCanceledOrders)
                Api.Dump(position.Symbol, cancelled, tradeParams, "annuleren vanwege een verandering in instellingen");
            // Opnieuw de step bepalen en doorgaan (opnieuw plaatsen)
            step = PositionTools.FindPositionPartStep(part, "BUY", false);
        }



        // TODO: De Bid en Ask prijs zijn voor Bybit en Kucoin niet bekend via de PriceTicker

        // Enige defaults
        string logText = "placing";
        decimal? actionPrice = null;
        CryptoTrailing trailing = CryptoTrailing.None;
        CryptoOrderType orderType = CryptoOrderType.Limit;
        if (GlobalData.Settings.Trading.BuyOrderMethod == CryptoBuyOrderMethod.MarketOrder)
            orderType = CryptoOrderType.Market;

        switch (stepInMethod)
        {
            case CryptoBuyStepInMethod.Immediately:
                // Part is geopend door CreateOrExtendPositionViaSignal()
                // Dit is alleen de allereerste buy order (openen van een positie)
                if (step == null && part.Quantity == 0)
                    actionPrice = CalculateBuyOrDcaPrice(part, buyOrderMethod, part.SignalPrice);
                break;
            case CryptoBuyStepInMethod.AfterNextSignal:
                // Part is geopend door CreateOrExtendPositionViaSignal()
                // Er moet een buy order geplaatst worden.
                if (step == null && part.Quantity == 0)
                    actionPrice = CalculateBuyOrDcaPrice(part, buyOrderMethod, part.SignalPrice);
                break;
            case CryptoBuyStepInMethod.FixedPercentage:
                // Part is geopend door CheckAddDca()
                // Er moet een buy order geplaatst worden.
                orderType = CryptoOrderType.StopLimit;
                if (step == null && part.Quantity == 0)
                    actionPrice = CalculateBuyOrDcaPrice(part, buyOrderMethod, part.SignalPrice);
                break;
            case CryptoBuyStepInMethod.TrailViaKcPsar:
                // Part is geopend door CheckAddDca()
                // Er moet een buy order geplaatst worden.
                trailing = CryptoTrailing.Trailing;
                orderType = CryptoOrderType.StopLimit;

                // Moet de bestaande verplaatst worden?
                if (step != null && part.Quantity == 0 && step.Trailing == CryptoTrailing.Trailing)
                {
                    //decimal x = (decimal)candleInterval.CandleData.PSar + Symbol.PriceTickSize;
                    decimal x = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                    if (x < step.StopPrice && Symbol.LastPrice < x && candleInterval.High < x)
                    {
                        actionPrice = x;
                        logText = "trailing";
                        var (cancelled, tradeParams) = await CancelOrder(position, part, step);
                        if (GlobalData.Settings.Trading.LogCanceledOrders)
                            Api.Dump(position.Symbol, cancelled, tradeParams, "annuleren vanwege verandering stoploss");
                    }
                }

                if (step == null && part.Quantity == 0)
                {
                    // Alleen in een neergaande "trend" beginnen we met trailen (niet in een opgaande)
                    // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten (maar of dit okay is?)
                    if (Symbol.LastPrice >= (decimal)candleInterval.CandleData.PSar)
                        return;

                    //decimal x = (decimal)candleInterval.CandleData.PSar + Symbol.PriceTickSize;
                    decimal x = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                    if (Symbol.LastPrice < x && candleInterval.High < x)
                    {
                        logText = "trailing";
                        actionPrice = x;
                    }
                }
                break;
        }


        if (actionPrice.HasValue)
        {
            decimal? stop = null;
            decimal? limit = null;

            // quantity (we verdubbelen zoals Zignally!)
            decimal quoteAmount;
            if (position.Invested == 0)
                quoteAmount = Symbol.QuoteData.BuyAmount;
            else
                // de position.Commission is nieuw in de aankoop som, is dat wel okay?
                quoteAmount = (position.Invested - position.Returned + position.Commission) * GlobalData.Settings.Trading.DcaFactor;


            decimal price, quantity;
            switch (orderType)
            {
                case CryptoOrderType.Market:
                case CryptoOrderType.Limit:
                    // Voor market en limit nemen we de actionprice (quantiry berekenen)
                    price = (decimal)actionPrice;
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    quantity = quoteAmount / price; // "afgerond"
                    quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    break;
                case CryptoOrderType.StopLimit:
                    // Voor de stopLimit moet de price en stop berekend worden
                    price = (decimal)actionPrice + ((decimal)actionPrice * 1.5m / 100); // ergens erboven
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    stop = (decimal)actionPrice;
                    stop = stop?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    quantity = quoteAmount / (decimal)stop; // "afgerond"
                    quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    break;
                default:
                    // Voor de OCO moeten er 3 prijzen berekend worden
                    // De OCO en eventueel andere types worden niet ondersteund
                    // OCO = stoplimit + extra limit die x% onder de stop zit.

                    //price = (decimal)actionPrice + ((decimal)actionPrice * 1.5m / 100); // ergens erboven
                    //price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //stop = (decimal)actionPrice;
                    //stop = stop?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //limit = (decimal)actionPrice - ((decimal)actionPrice * 1.5m / 100); // ergens erboven
                    //limit = limit?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //quantity = quoteAmount / (decimal)stop; // "afgerond"
                    //quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    throw new Exception($"{orderType} niet ondersteund");
                    //break;
            }


            // Plaats de buy order
            Api exchangeApi = new();
            (bool result, TradeParams tradeParams) result = await exchangeApi.BuyOrSell(Database, position.TradeAccount,
                position.Symbol, LastCandle1mCloseTimeDate, orderType, CryptoOrderSide.Buy, quantity, price, stop, limit);
            if (result.result)
            {
                part.StepInMethod = stepInMethod;
                if (part.Name.Equals("BUY"))
                    position.BuyPrice = result.tradeParams.Price;
                step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY", trailing);
                PositionTools.InsertPositionStep(Database, position, step);
                PositionTools.AddPositionPartStep(part, step);
                PositionTools.SavePositionPart(Database, part);
                PositionTools.SavePosition(Database, position);

                Api.Dump(position.Symbol, result.result, result.tradeParams, logText);

                // Een eventuele market order direct laten vullen
                if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading && step.OrderType == CryptoOrderType.Market)
                    await PaperTrading.CreatePaperTradeObject(Database, position, step, LastCandle1m.Close, LastCandle1mCloseTimeDate);
            }
            else Api.Dump(position.Symbol, result.result, result.tradeParams, logText);
        }
    }


    private async Task HandleCheckProfitableSellPart(CryptoPosition position, CryptoPositionPart part)
    {
        // Is er iets om te verkopen in deze "part"? (part.Quantity > 0?)
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", true);
        if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
        {
            // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
            // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

            step = PositionTools.FindPositionPartStep(part, "SELL", false);
            if (step != null)
            {
                decimal breakEven = part.BreakEvenPrice;

                // Als de actuele prijs ondertussen substantieel hoger dan winst proberen te nemen (jojo)
                // Dit verstoord eigenlijk de trailing sell, maar het is maar even zo...
                decimal x = breakEven + breakEven * (1.25m / 100m); // Voorlopig even hardcoded 1%.... (vanwege ontbreken OCO en stop order )
                if (position.Symbol.LastPrice < x)
                    return;


                // Annuleer de sell order
                var (cancelled, tradeParams) = await CancelOrder(position, part, step);
                if (GlobalData.Settings.Trading.LogCanceledOrders)
                    Api.Dump(position.Symbol, cancelled, tradeParams, "annuleren vanwege een jojo");


                // En zet de nieuwe sell order vlak boven de bekende prijs met (helaas) een limit order (had liever een OCO gehad)
                decimal sellPrice = x + Symbol.PriceTickSize;
                if (position.Symbol.LastPrice > sellPrice)
                    sellPrice = (decimal)(position.Symbol.LastPrice + Symbol.PriceTickSize);
                decimal sellQuantity = part.Quantity;
                sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                (bool result, TradeParams tradeParams) result;
                Api exchangeApi = new();
                result = await exchangeApi.BuyOrSell(Database,
                    position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate,
                    CryptoOrderType.Limit, CryptoOrderSide.Sell, sellQuantity, sellPrice, null, null);

                if (result.result)
                {
                    if (part.Name.Equals("BUY"))
                        position.SellPrice = result.tradeParams.Price;
                    // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                    var sellStep = PositionTools.CreatePositionStep(position, part, result.tradeParams, "SELL");
                    PositionTools.InsertPositionStep(Database, position, sellStep);
                    PositionTools.AddPositionPartStep(part, sellStep);
                    part.StepOutMethod = CryptoBuyStepInMethod.FixedPercentage; // niet helemaal waar, hebben we ervan gemaakt
                    PositionTools.SavePositionPart(Database, part);
                    PositionTools.SavePosition(Database, position);
                }
                Api.Dump(position.Symbol, result.result, result.tradeParams, "placing");
            }
        }
    }

    private async Task PlaceFirstSellOrder(CryptoPosition position, CryptoPositionPart part)
    {
        // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
        // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

        //decimal sellPrice = CalculateSellPrice(position, part, candleInterval);

        // We nemen hiervoor de BreakEvenPrice van de gehele positie en de sell price ligt standaard X% hoger
        decimal breakEven = position.BreakEvenPrice;
        decimal sellPrice = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));
        if (Symbol.LastPrice.HasValue && Symbol.LastPrice > sellPrice)
        {
            decimal oldPrice = sellPrice;
            sellPrice = (decimal)Symbol.LastPrice + Symbol.PriceTickSize;
            GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction: {oldPrice:N6} to {sellPrice.ToString0()}");
        }
        sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);



        decimal sellQuantity = part.Quantity;
        sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

        (bool result, TradeParams tradeParams) sellResult;
        Api exchangeApi = new();
        sellResult = await exchangeApi.BuyOrSell(Database, 
            position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate,
            CryptoOrderType.Limit, CryptoOrderSide.Sell, sellQuantity, sellPrice, null, null);

        // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat?
        // Binance is een bitch af en toe!). Met name Binance wilde na het annuleren wel eens de assets niet
        // vrijgeven waardoor de portfolio niet snel genoeg bijgewerkt werd en de volgende opdracht dan de fout
        // in zou kunnen gaan. Geld voor alles wat we in deze tool doen, qua buy en sell gaat de herkansing wel 
        // goed, ook al zal je dan soms een repeterende fout voorbij zien komen (iedere minuut)

        if (sellResult.result)
        {
            // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
            //part.SellPrice = sellPrice;

            if (part.Name.Equals("BUY"))
                position.SellPrice = sellResult.tradeParams.Price;
            var sellStep = PositionTools.CreatePositionStep(position, part, sellResult.tradeParams, "SELL");
            PositionTools.InsertPositionStep(Database, position, sellStep);
            PositionTools.AddPositionPartStep(part, sellStep);
            part.StepOutMethod = CryptoBuyStepInMethod.FixedPercentage;
            PositionTools.SavePositionPart(Database, part);
            PositionTools.SavePosition(Database, position);
        }
    }

    private async Task HandleSellPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval)
    {
        // Is er wel iets om te verkopen in deze "part"? (part.Quantity > 0?)
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", true);
        if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
        {
            // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
            // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

            step = PositionTools.FindPositionPartStep(part, "SELL", false);
            if (step == null && part.Quantity > 0)
            {
                await PlaceFirstSellOrder(position, part);
            }
            else if (step != null && part.Quantity > 0 && part.BreakEvenPrice > 0 && GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar)
            {
                // Trailing SELL
                // Alleen in een opwaarste "trend" beginnen we met trailen (niet in een neergaande)
                // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten
                if (step.Trailing == CryptoTrailing.None && Symbol.LastPrice <= (decimal)candleInterval.CandleData.PSar)
                    return;

                decimal x;
                List<decimal> qqq = new();

                // Via de psar trailen ipv KC/psar? (dat zou zelfs een instelling kunnen zijn)
                //x = (decimal)candleInterval.CandleData.PSar - Symbol.PriceTickSize;
                //qqq.Add(x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize));

                x = Math.Min((decimal)candleInterval.CandleData.KeltnerLowerBand, (decimal)candleInterval.CandleData.PSar) - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > part.BreakEvenPrice)
                    qqq.Add(x);

                //x = (((decimal)candleInterval.CandleData.BollingerBandsUpperBand + (decimal)candleInterval.CandleData.BollingerBandsLowerBand) / 2m) - Symbol.PriceTickSize;
                x = (decimal)candleInterval.CandleData.Sma20 - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > part.BreakEvenPrice)
                    qqq.Add(x);

                x = (decimal)candleInterval.CandleData.KeltnerUpperBand - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > part.BreakEvenPrice)
                    qqq.Add(x);

                x = (decimal)candleInterval.CandleData.BollingerBandsUpperBand - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > part.BreakEvenPrice)
                    qqq.Add(x);

                // De hoogst mogelijke waarde nemen (extra controles op de low anders wordt ie direct gevuld)
                decimal stop = 0;
                qqq.Sort((valueA, valueB) => valueB.CompareTo(valueA));
                foreach (var stopX in qqq)
                {
                    if (step.Status == CryptoOrderStatus.New
                        && step.Side == CryptoOrderSide.Sell
                        && Symbol.LastPrice > stopX
                        && stopX > part.BreakEvenPrice
                        && candleInterval.Low > stopX
                        && (step.StopPrice == null || stopX > step.StopPrice))
                    {
                        decimal oldPrice = stop;
                        stop = stopX;
                        if (oldPrice > 0) 
                            GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction sellStop -> {oldPrice:N6} to {stop.ToString0()}");
                    }
                    //else break;
                }

                if (stop > 0)
                {
                    // price moet lager, 1.5% moet genoeg zijn.
                    decimal price = stop - (stop * 1.5m / 100); // ergens eronder
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);


                    var (cancelled, cancelParams) = await CancelOrder(position, part, step);
                    if (GlobalData.Settings.Trading.LogCanceledOrders)
                        Api.Dump(position.Symbol, cancelled, cancelParams, "annuleren vanwege verandering stoploss");

                    // Afhankelijk van de invoer stop of stoplimit een OCO of standaard sell plaatsen.
                    // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                    Api exchangeApi = new();
                    var (success, tradeParams) = await exchangeApi.BuyOrSell(Database,
                        position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate,
                        CryptoOrderType.StopLimit, CryptoOrderSide.Sell,
                        step.Quantity, price, stop, null); // Was een OCO met een sellLimit
                    if (success)
                    {
                        // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
                        if (!position.SellPrice.HasValue)
                            position.SellPrice = price; // part.SellPrice; // (kan eigenlijk weg, slechts ter debug en tracering, voila)
                        // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                        var sellStep = PositionTools.CreatePositionStep(position, part, tradeParams, "SELL", CryptoTrailing.Trailing);
                        PositionTools.InsertPositionStep(Database, position, sellStep);
                        PositionTools.AddPositionPartStep(part, sellStep);
                        part.StepOutMethod = CryptoBuyStepInMethod.TrailViaKcPsar;
                        PositionTools.SavePositionPart(Database, part);
                        PositionTools.SavePosition(Database, position);
                    }

                    decimal perc = 0;
                    if (part.BreakEvenPrice > 0)
                        perc = (decimal)(100 * ((stop / part.BreakEvenPrice) - 1));
                    Api.Dump(position.Symbol, success, tradeParams, $"locking ({perc:N2}%)");
                }
            }

        }
    }


    private void CheckAddDca(CryptoTradeAccount tradeAccount, CryptoPosition position)
    {
        // DCA plaatsen na een bepaalde percentage en de cooldowntijd?

        // Ondertussen gesloten (kan dat, is niet logisch?)
        if (position.Status != CryptoPositionStatus.Trading)
            return;


        if (GlobalData.Settings.Trading.DcaStepInMethod == CryptoBuyStepInMethod.FixedPercentage)
        {
            if (!CanOpenAdditionalDca(position, LastCandle1m.Close, out CryptoPositionStep step, out decimal _, out string _))
            {
                //GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
                return;
            }

            // DCA percentage prijs (voor de trailing wordt dit een prijs die toch geoverruled wordt)
            decimal price = step.Price - (GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m);
            if (position.Symbol.LastPrice < price)
                price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
            GlobalData.AddTextToLogTab(position.Symbol.Name + " DCA bijplaatsen op " + price.ToString0(position.Symbol.PriceDisplayFormat));

            // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
            // Verderop doen we wat met deze stap en zetten we de echte buy of step)
            position.PartCount += 1;
            PositionTools positionTools = new(tradeAccount, Symbol, LastCandle1mCloseTimeDate);
            CryptoPositionPart part = positionTools.CreatePositionPart(position, "DCA", price); // voorlopige buyprice
            part.StepInMethod = GlobalData.Settings.Trading.DcaStepInMethod;
            PositionTools.InsertPositionPart(Database, part);
            PositionTools.AddPositionPart(position, part);
            PositionTools.SavePosition(Database, position);
        }
    }


    private async Task CancelOrdersIfClosedOrTimeoutOrReposition(CryptoPosition position)
    {
        //bool changed = false;

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (!part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    if (step.Status == CryptoOrderStatus.New)
                    {

                        // De orders van een gesloten positie allemaal verwijderen (buy dca's)
                        if (step.Status == CryptoOrderStatus.New && position.CloseTime.HasValue)
                        {
                            var (cancelled, cancelParams) = await CancelOrder(position, part, step);
                            if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                                Api.Dump(position.Symbol, cancelled, cancelParams, "annuleren vanwege sluiten positie");
                        }

                        // Verwijder buy orders die niet gevuld worden binnen zoveel X minuten
                        // (??of candles? Mhhh, lastig traden op de hoge intervallen zie ik??)
                        if (step.Status == CryptoOrderStatus.New && step.Name.Equals("BUY"))
                        {
                            if (step.CreateTime.AddMinutes(GlobalData.Settings.Trading.GlobalBuyRemoveTime) < LastCandle1mCloseTimeDate)
                            {
                                //changed = true;
                                var (cancelled, cancelParams) = await CancelOrder(position, part, step);
                                if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                                    Api.Dump(position.Symbol, cancelled, cancelParams, "annuleren vanwege timeout");
                            }
                        }

                        // Verwijder sell orders vanwege een aanpassing in de BE door een buy of sell
                        if (step.Status == CryptoOrderStatus.New && position.Reposition && step.Name.Equals("SELL"))
                        {
                            var (cancelled, cancelParams) = await CancelOrder(position, part, step);
                            if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                                Api.Dump(position.Symbol, cancelled, cancelParams, "annuleren vanwege een buy of sell");
                        }

                    }
                }
            }
        }
        // Waarom? er is in principe niets veranderd
        // (lijkt zinloos, of heeft het toch een reden?)
        // Afgesterd, we gaan het zien ;-)
        // (het haalt geen orders op, dus prima?)
        //if (changed)

        // Werk de status van de steps en positie bij (wellicht overbodig)
        //PositionTools.CalculatePositionResultsViaTrades(Database, position);

        // Het doorrekenen voor de BE kost je zeker 2 tot 5 seconden! (de positie en alle steps worden bewaard, dus niet zomaar uitvoeren!)

        if (position.Reposition)
        {
            position.Reposition = false;
            PositionTools.SavePosition(Database, position);
        }
    }


    public async Task HandlePosition(CryptoTradeAccount tradeAccount, CryptoPosition position)
    {
        // PROBLEEM: De locking geeft momenteel problemen, maar het lijkt zonder ook prima te werken (nog eens puzzelen wat dit kan zijn!)
        //await tradeAccount.PositionListSemaphore.WaitAsync();
        //try
        // {
        //GlobalData.Logger.Info($"position:" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));

        // Verwijder alle openstaande orders indien de positie gesloten is
        // Verwijder de te lang openstaande buy orders (via de config timeout)
        // Verwijder openstaande orders als de BE aangepast is.
        //GlobalData.Logger.Info($"analyze.HandlePosition.CancelOrdersIfClosedOrTimeoutOrReposition.Start({Symbol.Name})");
        await CancelOrdersIfClosedOrTimeoutOrReposition(position);


        // Een afgesloten posities is niet meer interessant, verplaatsen
        //GlobalData.Logger.Info($"analyze.HandlePosition.CancelOrdersIfClosedOrTimeoutOrReposition.After({Symbol.Name})");
        if (position.CloseTime.HasValue)
        {
            PositionTools.RemovePosition(tradeAccount, position);
            if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                GlobalData.PositionsHaveChanged("");
            return;
        }


        // OEPS! Deze controleert of we een geldig candle interval hebben!!
        // Dat betekend dat de (net verwijderde) sell orders (icm 30m interal) niet zomaar opnieuw gezet worden

        if (Prepare(position, out CryptoCandle candleInterval, out bool pauseBecauseOfTradingRules, out bool pauseBecauseOfBarometer))
        {
            // Pauzeren door de regels of de barometer
            if (!(pauseBecauseOfTradingRules || pauseBecauseOfBarometer))
                CheckAddDca(tradeAccount, position);


            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                // Afgesloten parts zijn niet interessant
                if (!part.CloseTime.HasValue)
                {
                    // Controleer BUY
                    if (part.Name.Equals("BUY"))
                    {
                        await HandleBuyPart(position, part, candleInterval, GlobalData.Settings.Trading.BuyStepInMethod,
                            GlobalData.Settings.Trading.BuyOrderMethod, pauseBecauseOfTradingRules, pauseBecauseOfBarometer);
                    }

                    // Controleer DCA's
                    if (position.Quantity > 0 && part.Name.Equals("DCA"))
                    {
                        await HandleBuyPart(position, part, candleInterval, GlobalData.Settings.Trading.DcaStepInMethod,
                            GlobalData.Settings.Trading.DcaOrderMethod, pauseBecauseOfTradingRules, pauseBecauseOfBarometer);
                    }

                    // Controleer SELL 
                    if (position.Quantity > 0)
                    {
                        await HandleSellPart(position, part, candleInterval);
                    }


                    // Kunnen we een part afsluiten met meer dan x% winst (de zogenaamde jojo)
                    // Wel uitsluiten voor trailing want anders lopen er 2 methodes door elkaar.
                    if (position.Quantity > 0 && part.StepOutMethod != CryptoBuyStepInMethod.TrailViaKcPsar)
                    {
                        await HandleCheckProfitableSellPart(position, part);
                    }
                }
            }
        }



        // Is er wel een initiele SELL order aanwezig? zoniet dan dit alsnog doen!
        // (buiten de Prepare loop gehaald die intern een controle op het interval doet)
        // Dus nu wordt de sell order vrijwel direct geplaatst (na een 1m candle)
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (!part.CloseTime.HasValue)
            {
                CryptoPositionStep step = PositionTools.FindPositionPartStep(part, "BUY", true);
                if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
                {
                    step = PositionTools.FindPositionPartStep(part, "SELL", false);
                    if (step == null && part.Quantity > 0)
                    {
                        await PlaceFirstSellOrder(position, part);
                    }
                }
            }
        }



            //}
            //finally
            //{
            //    //tradeAccount.PositionListSemaphore.Release();
            //}
    }
#endif


    public void CreateSignals()
    {
        //GlobalData.Logger.Info($"CreateSignals(start):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
        if (GlobalData.Settings.Signal.SignalsActive && Symbol.QuoteData.CreateSignals)
        {
            // Een extra ToList() zodat we een readonly setje hebben (en we de instellingen kunnen muteren)
            foreach (CryptoInterval interval in TradingConfig.AnalyzeInterval.Values.ToList())
            {
                // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
                if (LastCandle1mCloseTime % interval.Duration == 0)
                {
                    //GlobalData.Logger.Info($"analyze({interval.Name}):" + LastCandle1m.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, false, true));

                    // We geven als tijd het begin van de "laatste" candle (van dat interval)
                    SignalCreate createSignal = new(Symbol, interval);
                    createSignal.AnalyzeSymbol(LastCandle1mCloseTime - interval.Duration);

                    // Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                    // TODO: MultiTread aware maken ..
                    AnalyseCount++;
                }
            }
        }
        //GlobalData.Logger.Info($"CreateSignals(stop):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
    }


    private void CleanSymbolData()
    {
        // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
        // ieder geval de CandleData te clearen. Vanaf x candles terug tot de eerste de beste die null is.

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (LastCandle1mCloseTime % interval.Duration == 0)
            {
#if USELOCKS
                Monitor.Enter(Symbol.CandleList);
#endif
                try
                {
                    // Remove old indicator data
                    SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                    for (int i = candles.Count - 62; i > 0; i--)
                    {
                        CryptoCandle c = candles.Values[i];
                        if (c.CandleData != null)
                        {
                            c.CandleData = null;
#if SHOWTIMING
                            GlobalData.Logger.Info($"removed candledata({interval.Name}):" + c.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
#endif
                        }
                        else break;
                    }


                    // Remove old candles
                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(Symbol, interval, DateTime.UtcNow);
                    DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                    while (candles.Values.Any())
                    {
                        CryptoCandle c = candles.Values[0];
                        if (c.OpenTime < startFetchUnix)
                        {
                            candles.Remove(c.OpenTime);
#if SHOWTIMING
                            GlobalData.Logger.Info($"removed({interval.Name}):" + c.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, false, true));
#endif
                        }
                        else break;
                    }
                }
                finally
                {
#if USELOCKS
                    Monitor.Exit(Symbol.CandleList);
#endif
                }
            }
        }
    }


    /// <summary>
    /// De afhandeling van een nieuwe 1m candle.
    /// (de andere intervallen zijn ook berekend)
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task NewCandleArrivedAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        try
        {
            if (!Symbol.IsSpotTradingAllowed)
                return;


            //if (Symbol.Name.Equals("LITUSDT"))
            //    GlobalData.AddTextToLogTab("DEBUG NewCandleArrivedAsync(): " + Symbol.Name);
            //GlobalData.Logger.Info($"analyze:" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));


            // Create signals per interval
            //GlobalData.Logger.Info($"analyze.CreateSignals({Symbol.Name})");
            CreateSignals();


#if TRADEBOT
            // Idee1: Zet de echte (gemiddelde) price in de step indien deze gevuld is (het is nu namelijk
            // onduidelijk voor welke prijs het exact verkocht is = lastig met meerdere trades igv market)
            // Idee2: Zet de buyPrice en de echte (gemiddelde)sellPrice in de part indien deze gevuld zijn ()
            // Probleem: De migratie van de oude naar een nieuwe situatie (als je het al zou uitvoeren)

            //GlobalData.Logger.Info($"analyze.Database({Symbol.Name})");
            Database.Open();


            //#if BALANCING
            // TODO: Weer werkzaam maken
            //if (GlobalData.Settings.BalanceBot.Active && (symbol.IsBalancing))
            //GlobalData.ThreadBalanceSymbols.AddToQueue(symbol);
            //#endif


            // Simulate Trade indien openstaande orders gevuld zijn
            //GlobalData.Logger.Info($"analyze.PaperTradingCheckOrders({Symbol.Name})");
            if (GlobalData.BackTest)
                await PaperTradingCheckOrders(GlobalData.ExchangeBackTestAccount);
            if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                await PaperTradingCheckOrders(GlobalData.ExchangePaperTradeAccount);

            // Open or extend a position
            //GlobalData.Logger.Info($"analyze.CreateOrExtendPositionViaSignal({Symbol.Name})");
            if (Symbol.SignalCount > 0)
                CreateOrExtendPositionViaSignal();

            // Per (actief) trade account de posities controleren
            foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
            {
                // Aan de hand van de instellingen accounts uitsluiten
                if (PositionTools.ValidTradeAccount(tradeAccount, Symbol))
                {
                    // Check the positions
                    if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var positionList))
                    {
                        foreach (CryptoPosition position in positionList.Values.ToList())
                        {
                            await HandlePosition(tradeAccount, position);
                        }
                    }
                }
            }
#endif

            // Remove old candles or CandleData
            //GlobalData.Logger.Info($"analyze.CleanSymbolData({Symbol.Name})");
            if (!GlobalData.BackTest)
                CleanSymbolData();

            //GlobalData.Logger.Info($"analyze.Done({Symbol.Name})");
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab($"{Symbol.Name} error Monitor {error.Message}");
        }
    }
}
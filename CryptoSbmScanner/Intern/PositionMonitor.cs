using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;
using CryptoSbmScanner.Trader;
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

    public bool PauseBecauseOfBarometer  { get; set; } = false;
    public bool PauseBecauseOfTradingRules { get; set; } = false;


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
    private bool CanOpenAdditionalDca(CryptoStepInMethod stepInMethod, CryptoPosition position, decimal signalPrice, 
        out CryptoPositionStep step, out decimal percentage, out string reaction)
    {
        // Een bijkoop zonder een voorgaande buy is onmogelijk
        step = PositionTools.GetLowestClosedBuy(position);
        if (step == null)
        {
            reaction = "Geen eerste BUY price gevonden";
            percentage = 0;
            return false;
        }


        // Average prijs vanwege gespreide market of stoplimit order
        decimal lastBuyPrice = step.QuoteQuantityFilled / step.Quantity; // step.AvgPrice
        percentage = 100m * (lastBuyPrice - signalPrice) / lastBuyPrice;


        if (stepInMethod != CryptoStepInMethod.FixedPercentage)
        {
            // het percentage geld voor alle mogelijkheden
            // Het percentage moet in ieder geval x% onder de vorige buy opdracht zitten
            // (en dit heeft voordelen want dan hoef je niet te weten in welke DCA-index je zit!)
            // TODO: Dat zou (CC2 technisch) ook een percentage van de BB kunnen zijn als 3e optie.
            // OPMERKING: Besloten om dit altijd te doen na de cooldown tijd
            if (percentage < GlobalData.Settings.Trading.DcaPercentage)
            {
                reaction = $" het is te vroeg voor een bijkoop vanwege het percentage {percentage.ToString0("N2")}";
                return false;
            }
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
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (part.Name.Equals("DCA") && !part.CloseTime.HasValue)
            {
                int openOrders = 0;
                foreach (CryptoPositionStep stepX in part.Steps.Values.ToList())
                {
                    // New of PartiallyFilled
                    if (!stepX.CloseTime.HasValue)
                        openOrders += 1;

                    if (stepX.Side == CryptoOrderSide.Buy)
                    {
                        if (stepX.Status == CryptoOrderStatus.New)
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

                // Er is al een DCA gemaakt maar het heeft nog geen orders of is gepauseerd vanwege barometer of andere oorzaken..
                if (openOrders == 0)
                {
                    reaction = "de positie heeft al een openstaande DCA";
                    return false;
                }

            }
        }

        reaction = "";
        return true;
    }

    public void ExtendPosition(CryptoPosition position, string name, CryptoInterval interval, CryptoSignalStrategy strategy, CryptoStepInMethod stepInMethod, decimal signalPrice)
    {
        CryptoPositionPart part = new()
        {
            Name = name,
            Strategy = strategy,
            Interval = interval,
            IntervalId = interval.Id,
            StepInMethod = stepInMethod,
            Side = CryptoOrderSide.Buy, // Aanpassen voor short
            SignalPrice = signalPrice,
            CreateTime = LastCandle1mCloseTimeDate,
            PositionId = position.Id,
            Symbol = Symbol,
            SymbolId = Symbol.Id,
            Exchange = Symbol.Exchange,
            ExchangeId = Symbol.ExchangeId
        };
        Database.Connection.Insert<CryptoPositionPart>(part);
        PositionTools.AddPositionPart(position, part);

        position.PartCount += 1;
        position.UpdateTime = part.CreateTime;
        Database.Connection.Update<CryptoPosition>(position);

        // Nieuwe parts worden hierdoor uitgesloten (denk ik?)
        Symbol.LastTradeDate = LastCandle1mCloseTimeDate;

        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {name} {stepInMethod} plaatsen op {signalPrice.ToString0(position.Symbol.PriceDisplayFormat)}");
    }


    private bool CheckApiAndAssets(CryptoTradeAccount tradeAccount, out string reaction)
    {
        PositionTools positionTools = new(tradeAccount, Symbol, LastCandle1mCloseTimeDate);
        if (!positionTools.CheckExchangeApiKeys(tradeAccount, out reaction))
            return false;

        if (!positionTools.CheckAvaliableAssets(tradeAccount, out decimal assetQuantity, out reaction))
            return false;

        // Is er genoeg beschikbaar om de order te kunnen plaatsen?
        if (!SymbolTools.CheckValidAmount(Symbol, assetQuantity, out decimal _, out reaction))
            return false;

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
        if (!PositionTools.CheckTradingAndSymbolConditions(Symbol, LastCandle1m, out string reaction))
        {
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            Symbol.ClearSignals();
            return;
        }


        //GlobalData.AddTextToLogTab("Monitor " + symbol.Name); te druk in de log

        // ***************************************************************************
        // Per interval kan een signaal aanwezig zijn, regel de aankoop of de bijkoop
        // ***************************************************************************
        foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList)
        {
            // alleen voor de intervallen waar de candle net gesloten is
            // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
            if (LastCandle1mCloseTime % symbolInterval.Interval.Duration == 0)
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
                long unix = LastCandle1mCloseTime - symbolInterval.Interval.Duration;
                //long unix = CandleTools.GetUnixTime(lastCandle1m.OpenTime, symbolInterval.Interval.Duration);
                //long unix = CandleTools.GetUnixTime(candleCloseTime - symbolInterval.Interval.Duration, symbolInterval.Interval.Duration);
                if (!symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle candleInterval))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Indicators uitrekenen (indien noodzakelijk)
                if (!CandleIndicatorData.PrepareIndicators(Symbol, symbolInterval, candleInterval, out reaction))
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

                foreach (CryptoTradeAccount tradeAccount in GlobalData.ActiveTradeAccountList.Values.ToList())
                {
                    if (!PositionTools.ValidTradeAccount(tradeAccount, Symbol))
                        continue;

                    CryptoPosition position = PositionTools.HasPosition(tradeAccount, Symbol);
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
                        if (!PositionTools.ValidFirstBuyConditions(tradeAccount, Symbol, LastCandle1m, out reaction))
                        {
                            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                            Symbol.ClearSignals();
                            return;
                        }

                        // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                        if (!CheckApiAndAssets(tradeAccount, out reaction))
                        {
                            GlobalData.AddTextToLogTab(text + " " + reaction);
                            Symbol.ClearSignals();
                            continue;
                        }

                        // Positie nemen (wordt een buy.a of buy.b in een aparte part)
                        PositionTools positionTools = new(tradeAccount, Symbol, LastCandle1mCloseTimeDate);
                        position = positionTools.CreatePosition(signal.Strategy, symbolInterval);
                        Database.Connection.Insert<CryptoPosition>(position);
                        PositionTools.AddPosition(tradeAccount, position);
                        ExtendPosition(position, "BUY", signal.Interval, signal.Strategy, CryptoStepInMethod.AfterNextSignal, signal.Price);

                        if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                            GlobalData.PositionsHaveChanged("");
                        return;
                    }
                    else
                    {
                        // Alleen bijkopen als we ONDER de break-even prijs zitten
                        if (signal.Price < position.BreakEvenPrice)
                        {
                            // En een paar aanvullende condities...
                            if (!CanOpenAdditionalDca(CryptoStepInMethod.AfterNextSignal, position, signal.Price, out CryptoPositionStep step, out decimal percentage, out reaction))
                            {
                                GlobalData.AddTextToLogTab($"{text} {symbolInterval.Interval.Name} {reaction} (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                            if (!CheckApiAndAssets(tradeAccount, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction);
                                Symbol.ClearSignals();
                                continue;
                            }

                            // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                            ExtendPosition(position, "DCA", signal.Interval, signal.Strategy, CryptoStepInMethod.AfterNextSignal, signal.Price);
                            return;
                        }
                    }

                }

            }

        }
    }


    private bool Prepare(CryptoPosition position, CryptoPositionPart part, out CryptoCandle candleInterval)
    {
        // Stukje migratie, het interval van de part kan null zijn
        CryptoInterval interval = position.Interval;
        if (part.Interval != null)
            interval = part.Interval;
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);



        // Maak beslissingen als de candle van het interval afgesloten is (dus NIET die van de 1m candle!)
        // Dus ook niet zomaar een laatste candle nemen in verband met Backtesting (echt even berekenen)
        candleInterval = null;
        if (LastCandle1mCloseTime % interval.Duration != 0)
            return false;
        long candleOpenTimeInterval = LastCandle1mCloseTime - interval.Duration;


        // Die indicator berekening had ik niet verwacht (cooldown?)
        Monitor.Enter(position.Symbol.CandleList);
        try
        {
            // Niet zomaar een laatste candle nemen in verband met Backtesting
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTimeInterval, out candleInterval))
            {
                string t = string.Format("candle 1m interval: {0}", LastCandle1m.DateLocal.ToString()) + ".." + LastCandle1mCloseTimeDate.ToLocalTime() + "\r\n" +
                string.Format("is de candle op het {0} interval echt missing in action?", interval.Name) + "\r\n" +
                    string.Format("position.CreateDate = {0}", position.CreateTime.ToString()) + "\r\n";
                throw new Exception($"Candle niet aanwezig? {t}");
            }

            if (candleInterval.CandleData == null)
            {
                List<CryptoCandle> history = null;
                history = CandleIndicatorData.CalculateCandles(Symbol, interval, candleInterval.OpenTime, out string response);
                if (history == null)
                {
                    GlobalData.AddTextToLogTab("Analyse " + response);
                    throw new Exception($"{position.Symbol.Name} Candle {interval.Name} {candleInterval.DateLocal} niet berekend? {response}");
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

    private decimal CorrectBuyOrDcaPrice(CryptoPosition position, decimal price)
    {
        // Gecorrigeerd op de laagste open of buy van de candle
        decimal x = Math.Min(LastCandle1m.Close, LastCandle1m.Open);
        if (x < price)
            price = x - position.Symbol.PriceTickSize;

        // Gecorrigeerd op de laatst bekende prijs
        if (position.Symbol.LastPrice.HasValue)
        {
            x = (decimal)position.Symbol.LastPrice;
            if (x < price)
                price = x - position.Symbol.PriceTickSize;
        }

        return price;
    }

    private decimal CalculateBuyOrDcaPrice(CryptoPosition position, CryptoPositionPart part, CryptoBuyOrderMethod buyOrderMethod, decimal defaultPrice)
    {
        // Wat wordt de prijs? (hoe graag willen we in de trade?)
        decimal price = defaultPrice;
        switch (buyOrderMethod)
        {
            case CryptoBuyOrderMethod.SignalPrice:
                //price = defaultPrice;
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            case CryptoBuyOrderMethod.BidPrice:
                if (part.Symbol.BidPrice.HasValue)
                    price = (decimal)part.Symbol.BidPrice;
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            case CryptoBuyOrderMethod.AskPrice:
                if (part.Symbol.AskPrice.HasValue)
                    price = (decimal)part.Symbol.AskPrice;
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            case CryptoBuyOrderMethod.BidAndAskPriceAvg:
                if (part.Symbol.AskPrice.HasValue)
                    price = (decimal)(part.Symbol.AskPrice + part.Symbol.BidPrice) / 2;
                price = CorrectBuyOrDcaPrice(position, price);
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


    private async Task<(bool cancelled, TradeParams tradeParams)> CancelOrder(CryptoPosition position, CryptoPositionPart part, 
        CryptoPositionStep step, CryptoOrderStatus newStatus = CryptoOrderStatus.Expired)
    {
        // waarom eigenlijk, wat wil ik nu met die buy en sell price? (debuggen, maar verder?)
        if (step.Side == CryptoOrderSide.Buy && part.Name.Equals("BUY"))
        {
            position.BuyPrice = null;
        }
        position.UpdateTime = LastCandle1mCloseTimeDate;
        Database.Connection.Update<CryptoPosition>(position);

        // Annuleer de vorige buy order
        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
        var result = await exchangeApi.Cancel(position.TradeAccount, Symbol, step);
        step.Status = newStatus;
        step.CloseTime = LastCandle1mCloseTimeDate;
        //PositionTools.SavePositionStep(Database, position, step);
        Database.Connection.Update<CryptoPositionStep>(step);

        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
            PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide,
                CryptoOrderStatus.Canceled, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

        return result;
    }

    /// <summary>
    /// Kunnen we de positie afsluiten met de opgegeven winst percentage
    /// </summary>
    private async Task HandleCheckProfitableSellPart(CryptoPosition position, CryptoPositionPart part, decimal percentage)
    {
        // Is er iets om te verkopen in deze "part"? (part.Quantity > 0?)
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Buy, true);
        if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
        {
            step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Sell, false);
            if (step != null)
            {
                // Als de actuele prijs ondertussen substantieel hoger dan winst proberen te nemen (jojo)
                // Dit verstoord eigenlijk de trailing sell, maar het is maar even zo...
                // Voorlopig even hardcoded (vanwege ontbreken OCO en stop order )
                decimal breakEven = part.BreakEvenPrice;
                decimal x = breakEven + breakEven * (percentage / 100m);
                if (position.Symbol.LastPrice < x)
                    return;

                // Als we reeds aan het trailen zijn heeft dat onze voorkeur (geen garanties op dat percentage)
                if (step.Trailing == CryptoTrailing.Trailing)
                {
                    GlobalData.AddTextToLogTab($"{Symbol.Name} is reeds aan het trailen, take profit exit");
                    return;
                }


                // Annuleer de sell order
                var (cancelled, tradeParams) = await CancelOrder(position, part, step, CryptoOrderStatus.JoJoSell);
                if (GlobalData.Settings.Trading.LogCanceledOrders)
                    ExchangeBase.Dump(position.Symbol, cancelled, tradeParams, "annuleren vanwege een jojo");


                // En zet de nieuwe sell order vlak boven de bekende prijs met (helaas) een limit order (had liever een OCO gehad)
                decimal sellPrice = x + Symbol.PriceTickSize;
                if (position.Symbol.LastPrice > sellPrice)
                    sellPrice = (decimal)(position.Symbol.LastPrice + Symbol.PriceTickSize);
                decimal sellQuantity = part.Quantity;
                sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                (bool result, TradeParams tradeParams) result;
                var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
                result = await exchangeApi.BuyOrSell(Database,
                    position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate,
                    CryptoOrderType.Limit, CryptoOrderSide.Sell, sellQuantity, sellPrice, null, null);

                if (result.result)
                {
                    if (part.Name.Equals("BUY"))
                        position.SellPrice = result.tradeParams.Price;
                    // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                    var sellStep = PositionTools.CreatePositionStep(position, part, result.tradeParams, "SELL");
                    Database.Connection.Insert<CryptoPositionStep>(step);
                    PositionTools.AddPositionPartStep(part, sellStep);
                    part.StepOutMethod = CryptoStepInMethod.FixedPercentage; // niet helemaal waar, hebben we ervan gemaakt
                    Database.Connection.Update<CryptoPositionPart>(part);
                    Database.Connection.Update<CryptoPosition>(position);

                    if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                        PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide, 
                            step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

                }
                ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, "placing");
            }
        }
    }


    private decimal CalculateSellPrice(CryptoPosition position)
    {
        decimal price; // = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));

        // We nemen hiervoor de BreakEvenPrice van de gehele positie en de sell price ligt standaard X% hoger
        decimal breakEven = position.BreakEvenPrice;
        if (GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar)
            price = breakEven + (breakEven * (2.0m / 100)); // In eerste instantie flink hoog!
        else
            price = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));

        if (Symbol.LastPrice.HasValue && Symbol.LastPrice > price)
        {
            decimal oldPrice = price;
            price = (decimal)Symbol.LastPrice + Symbol.PriceTickSize;
            GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction: {oldPrice:N6} to {price.ToString0()}");
        }

        price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        return price;
    }


    private async Task PlaceFirstSellOrder(CryptoPosition position, CryptoPositionPart part, string extraText)
    {
        decimal sellPrice = CalculateSellPrice(position);

        decimal sellQuantity = part.Quantity;
        sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

        (bool result, TradeParams tradeParams) result;
        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
        result = await exchangeApi.BuyOrSell(Database,
            position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate,
            CryptoOrderType.Limit, CryptoOrderSide.Sell, sellQuantity, sellPrice, null, null);

        // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat?
        // Binance is een bitch af en toe!). Met name Binance wilde na het annuleren wel eens de assets niet
        // vrijgeven waardoor de assets/pf niet snel genoeg bijgewerkt werd en de volgende opdracht dan de fout
        // in zou kunnen gaan. Geld voor alles wat we in deze tool doen, qua buy en sell gaat de herkansing wel 
        // goed, ook al zal je dan soms een repeterende fout voorbij zien komen (iedere minuut)

        if (result.result)
        {
            // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
            //part.SellPrice = sellPrice;

            if (part.Name.Equals("BUY"))
                position.SellPrice = result.tradeParams.Price;
            var step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "SELL");
            Database.Connection.Insert<CryptoPositionStep>(step);
            PositionTools.AddPositionPartStep(part, step);
            part.StepOutMethod = CryptoStepInMethod.FixedPercentage;
            Database.Connection.Update<CryptoPositionPart>(part);
            Database.Connection.Update<CryptoPosition>(position);

            if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide,
                    step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

            ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, extraText);
        }
    }



    private async Task HandleBuyPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval, 
        CryptoStepInMethod stepInMethod, CryptoBuyOrderMethod orderMethod)
    {
        // Controleer de BUY
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Buy, false);


        // defaults
        string logText = "placing";
        decimal? actionPrice = null;
        CryptoOrderType orderType;
        CryptoTrailing trailing = CryptoTrailing.None;

        switch (stepInMethod)
        {
            case CryptoStepInMethod.AfterNextSignal:
                orderType = CryptoOrderType.Limit;
                if (orderMethod == CryptoBuyOrderMethod.MarketOrder)
                    orderType = CryptoOrderType.Market;
                if (step == null && part.Quantity == 0)
                    actionPrice = CalculateBuyOrDcaPrice(position, part, orderMethod, part.SignalPrice);
                break;
            case CryptoStepInMethod.FixedPercentage:
                // Afspraak= niet bijplaatsen indien de BM te laag is (anders jojo=weghalen+bijplaatsen)
                orderType = CryptoOrderType.Limit;
                if (step == null && part.Quantity == 0 && !PauseBecauseOfBarometer)
                    actionPrice = CalculateBuyOrDcaPrice(position, part, orderMethod, part.SignalPrice);
                break;
            case CryptoStepInMethod.TrailViaKcPsar:
                trailing = CryptoTrailing.Trailing;
                orderType = CryptoOrderType.StopLimit;
                // Trailing is nogal afwijkend ten opzichte van de sell, zoveel mogelijk gelijk maken!

                // Moet de bestaande verplaatst worden (cq annuleren + opnieuw plaatsen)?
                if (step != null && part.Quantity == 0 && step.Trailing == CryptoTrailing.Trailing)
                {
                    decimal x = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                    if (x < step.StopPrice && Symbol.LastPrice < x && candleInterval.High < x)
                    {
                        actionPrice = x;
                        logText = "trailing";
                        var (cancelled, tradeParams) = await CancelOrder(position, part, step, CryptoOrderStatus.TrailingChange);
                        if (GlobalData.Settings.Trading.LogCanceledOrders)
                            ExchangeBase.Dump(position.Symbol, cancelled, tradeParams, "annuleren vanwege aanpassing stoploss trailing");
                    }
                }

                if (step == null && part.Quantity == 0)
                {
                    // Alleen in een neergaande "trend" beginnen we met trailen (niet in een opgaande)
                    // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten (maar of dit okay is?)
                    if (Symbol.LastPrice >= (decimal)candleInterval.CandleData.PSar)
                        return;

                    decimal x = Math.Max((decimal)candleInterval.CandleData.KeltnerUpperBand, (decimal)candleInterval.CandleData.PSar) + Symbol.PriceTickSize;
                    if (Symbol.LastPrice < x && candleInterval.High < x)
                    {
                        logText = "trailing";
                        actionPrice = x;
                    }
                }
                break;
            default:
                throw new Exception($"{stepInMethod} niet ondersteund");
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
            var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
            (bool result, TradeParams tradeParams) result = await exchangeApi.BuyOrSell(Database, position.TradeAccount,
                position.Symbol, LastCandle1mCloseTimeDate, orderType, CryptoOrderSide.Buy, quantity, price, stop, limit);
            if (result.result)
            {
                part.StepInMethod = stepInMethod;
                if (part.Name.Equals("BUY"))
                    position.BuyPrice = result.tradeParams.Price;
                step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY", trailing);
                Database.Connection.Insert<CryptoPositionStep>(step);
                PositionTools.AddPositionPartStep(part, step);
                Database.Connection.Update<CryptoPositionPart>(part);
                Database.Connection.Update<CryptoPosition>(position);

                ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, logText);

                // Een eventuele market order direct laten vullen
                if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading && step.OrderType == CryptoOrderType.Market)
                {
                    await PaperTrading.CreatePaperTradeObject(Database, position, step, LastCandle1m.Close, LastCandle1mCloseTimeDate);
                    position.Reposition = false; // anders twee keer achter elkaar indien papertrading of backtesting!
                }
            }
            else ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, logText);
        }
    }



    private async Task HandleSellPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval)
    {
        // Is er wel iets om te verkopen in deze "part"? (part.Quantity > 0?)
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Buy, true);
        if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
        {
            // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
            // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

            step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Sell, false);
            if (step == null && part.Quantity > 0)
            {
                await PlaceFirstSellOrder(position, part, "placing");
            }
            else if (step != null && part.Quantity > 0 && part.BreakEvenPrice > 0 && GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar)
            {
                bool doIt = false;

                // Als de actuele prijs ondertussen substantieel hoger dan winst proberen te nemen (jojo)
                // Dit verstoord eigenlijk de trailing sell, maar het is maar even zo...
                // Voorlopig even hardcoded (vanwege ontbreken OCO en stop order )
                // TODO: Hier nog eens een instelling van maken!
                // De winst ppercentage is nu eigenlijk de trigger prijs!
                decimal breakEven = part.BreakEvenPrice;
                decimal breakEvenExtra = breakEven + breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100m); 

                //if (position.Symbol.LastPrice > breakEvenExtra) // LastPrice is niet altijd gezet
                //    doIt = true;

                // Als de candle in zijn geheel boven de BE + extra zit beginnen met trailen (de zogenaamde trigger)
                if (candleInterval.Open > breakEvenExtra && candleInterval.Close > breakEvenExtra)
                    doIt = true;


                // Trailing SELL
                // Alleen in een opwaarste "trend" beginnen we met trailen (niet in een neergaande)
                // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten
                if (step.Trailing == CryptoTrailing.None && candleInterval.Low > (decimal)candleInterval.CandleData.PSar && !doIt)
                    return;


                decimal x;
                List<decimal> qqq = new();

                // Via de psar trailen ipv KC/psar? (dat zou zelfs een instelling kunnen zijn)
                //x = (decimal)candleInterval.CandleData.PSar - Symbol.PriceTickSize;
                //qqq.Add(x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize));
                x = breakEvenExtra;
                if (x > breakEvenExtra)
                    qqq.Add(x);

                x = Math.Min((decimal)candleInterval.CandleData.KeltnerLowerBand, (decimal)candleInterval.CandleData.PSar) - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > breakEvenExtra)
                    qqq.Add(x);

                //x = (((decimal)candleInterval.CandleData.BollingerBandsUpperBand + (decimal)candleInterval.CandleData.BollingerBandsLowerBand) / 2m) - Symbol.PriceTickSize;
                x = (decimal)candleInterval.CandleData.Sma20 - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > breakEvenExtra)
                    qqq.Add(x);

                x = (decimal)candleInterval.CandleData.KeltnerUpperBand - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > breakEvenExtra)
                    qqq.Add(x);

                x = (decimal)candleInterval.CandleData.BollingerBandsUpperBand - Symbol.PriceTickSize;
                x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                if (x > breakEvenExtra)
                    qqq.Add(x);

                // De hoogst mogelijke waarde nemen (extra controles op de low anders wordt ie direct gevuld)
                decimal stop = 0;
                qqq.Sort((valueA, valueB) => valueB.CompareTo(valueA));
                foreach (var stopX in qqq)
                {
                    if (step.Status == CryptoOrderStatus.New && step.Side == CryptoOrderSide.Sell
                        //&& Symbol.LastPrice > stopX
                        && stopX > breakEvenExtra
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
                    var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
                    
                    // price moet lager, 1.5% moet genoeg zijn.
                    decimal price = stop - (stop * 1.5m / 100); // ergens eronder
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);


                    var (cancelled, cancelParams) = await CancelOrder(position, part, step, CryptoOrderStatus.TrailingChange);
                    if (GlobalData.Settings.Trading.LogCanceledOrders)
                        ExchangeBase.Dump(position.Symbol, cancelled, cancelParams, "annuleren vanwege aanpassing stoploss trailing");

                    // Afhankelijk van de invoer stop of stoplimit een OCO of standaard sell plaatsen.
                    // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                    //Api exchangeApi = new();
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
                        Database.Connection.Insert<CryptoPositionStep>(sellStep);
                        PositionTools.AddPositionPartStep(part, sellStep);
                        part.StepOutMethod = CryptoStepInMethod.TrailViaKcPsar;
                        Database.Connection.Update<CryptoPositionPart>(part);
                        Database.Connection.Update<CryptoPosition>(position);

                        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                            PaperAssets.Change(position.TradeAccount, position.Symbol, tradeParams.OrderSide,
                                step.Status, tradeParams.Quantity, tradeParams.QuoteQuantity);
                    }

                    decimal perc = 0;
                    if (part.BreakEvenPrice > 0)
                        perc = (decimal)(100 * ((stop / part.BreakEvenPrice) - 1));
                    ExchangeBase.Dump(position.Symbol, success, tradeParams, $"locking ({perc:N2}%)");
                }
            }

        }
    }


    private void CheckAddDcaFixedPercentage(CryptoPosition position)
    {
        // Een DCA plaatsen na een bepaalde percentage en de cooldowntijd
        if (position.Status == CryptoPositionStatus.Trading && GlobalData.Settings.Trading.DcaStepInMethod == CryptoStepInMethod.FixedPercentage)
        {
            if (CanOpenAdditionalDca(CryptoStepInMethod.FixedPercentage, position, LastCandle1m.Close, out CryptoPositionStep step, out decimal _, out string _))
            {
                // DCA percentage prijs (voor de trailing wordt dit een prijs die toch geoverruled wordt)
                decimal price = step.Price - (GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m);
                if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice < price)
                    price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;


                // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                if (!CheckApiAndAssets(position.TradeAccount, out string reaction))
                {
                    string text = $"{position.Symbol.Name} + DCA bijplaatsen op {price.ToString0(position.Symbol.PriceDisplayFormat)}";
                    GlobalData.AddTextToLogTab(text + " " + reaction);
                    Symbol.ClearSignals();
                    return;
                }


                // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                ExtendPosition(position, "DCA", position.Interval, position.Strategy, CryptoStepInMethod.FixedPercentage, price);
            }
        }
    }


    private async Task CancelOrdersIfClosedOrTimeoutOrReposition(CryptoPosition position)
    {
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (!part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    if (step.Status != CryptoOrderStatus.New)
                        continue;

                    bool timeOut = false;
                    bool closePart = true;
                    string cancelText = "";
                    CryptoOrderStatus newStatus = CryptoOrderStatus.Expired;


                    if (step.Side == CryptoOrderSide.Buy)
                    {
                        // De orders van een gesloten positie allemaal annuleren (dat zijn de fixed percentage buy orders)
                        if (position.CloseTime.HasValue)
                        {
                            newStatus = CryptoOrderStatus.PositionClosed;
                            cancelText = "annuleren vanwege sluiten positie";
                        }


                        // Een eventuele aan- of bijkoop kan worden geannuleerd indien de instap te lang duurt ("Remove Time")
                        // (een toekomstige gereserveerde DCA buy orders of actieve trailing orders moeten we niet annuleren)
                        // Verwijder openstaande buy orders die niet gevuld worden binnen zoveel X minuten/candles?
                        // En dan mag eventueel de positie gesloten worden (indien het uit 1 deelpositie bestaat)
                        else if (part.StepInMethod != CryptoStepInMethod.FixedPercentage && step.Trailing == CryptoTrailing.None)
                        {
                            // Is de order ouder dan X minuten dan deze verwijderen
                            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(part.Interval.IntervalPeriod);
                            if (step.CreateTime.AddSeconds(GlobalData.Settings.Trading.GlobalBuyRemoveTime * symbolInterval.Interval.Duration) < LastCandle1mCloseTimeDate)
                            {
                                timeOut = true;
                                newStatus = CryptoOrderStatus.Timeout;
                                cancelText = "annuleren vanwege timeout";
                            }
                        }

                        // Verwijderen de buy vanwege een te lage barometer, pauseer stand of timeout (behalve trailing of reserved dca)
                        else if (PauseBecauseOfTradingRules)
                        {
                            timeOut = true;
                            closePart = false;
                            newStatus = CryptoOrderStatus.TradingRules;
                            cancelText = "annuleren vanwege trading regels";
                        }


                        // Verwijderen de buy vanwege een te lage barometer, pauseer stand of timeout (behalve trailing of reserved dca)
                        else if (PauseBecauseOfBarometer)
                        {
                            timeOut = true;
                            closePart = false;
                            newStatus = CryptoOrderStatus.BarameterToLow;
                            cancelText = "annuleren vanwege lage barometer";
                        }

                        // Als de instellingen veranderd zijn de lopende order annuleren
                        else if (part.Name.Equals("BUY") & part.StepInMethod != GlobalData.Settings.Trading.BuyStepInMethod)
                        {
                            newStatus = CryptoOrderStatus.ChangedSettings;
                            cancelText = "annuleren vanwege aanpassing buy instellingen";
                        }

                        // Als de instellingen veranderd zijn de lopende order annuleren
                        else if (part.Name.Equals("DCA") & part.StepInMethod != GlobalData.Settings.Trading.DcaStepInMethod)
                        {
                            newStatus = CryptoOrderStatus.ChangedSettings;
                            cancelText = "annuleren vanwege aanpassing dca instellingen";
                        }
                    }
                    else if (step.Side == CryptoOrderSide.Sell)
                    {
                        // Verwijder sell orders vanwege een aanpassing in de BE door een buy of sell
                        if (position.Reposition)
                        {
                            newStatus = CryptoOrderStatus.ChangedBreakEven;
                            cancelText = "annuleren vanwege aanpassing BE";
                        }


                        // De instellingen zijn gewijzigd....?
                        // Oh? Dat klopt niet, we hebben geen StepOutMethod in de instellingen! TODO: Controleren en fixen!
                        //else if (part.StepOutMethod != GlobalData.Settings.Trading.Ehhhh?)
                        //{
                        //    newStatus = CryptoOrderStatus.ChangedSettings;
                        //    cancelText = "annuleren vanwege aanpassing instellingen";
                        //}
                    }


                    if (cancelText != "")
                    {
                        var (cancelled, cancelParams) = await CancelOrder(position, part, step, newStatus);
                        if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                            ExchangeBase.Dump(position.Symbol, cancelled, cancelParams, cancelText);

                        if (cancelled)
                        {
                            // Na een timeout (barometer, tradingrules) even 5 minuten helemaal niets doen
                            if (newStatus == CryptoOrderStatus.TradingRules || newStatus == CryptoOrderStatus.BarameterToLow)
                                Symbol.LastTradeDate = LastCandle1mCloseTimeDate.AddMinutes(-GlobalData.Settings.Trading.GlobalBuyCooldownTime + 5);

                            if (timeOut)
                            {
                                // Door het verwijderen van de laatste buy kan een positie gesloten worden
                                if (closePart)
                                {
                                    part.CloseTime = LastCandle1mCloseTimeDate;
                                    Database.Connection.Update<CryptoPositionPart>(part);
                                }


                                // BUG: Volgens mij wordt de positie niet spontaan gesloten
                                PositionTools.CalculatePositionResultsViaTrades(Database, position);
                            }
                        }
                    }

                }
            }
        }


        // Pas op: Het doorrekenen voor de BE kost je 2 tot 5 seconden! (de positie en alle steps worden bewaard, dus niet zomaar uitvoeren!)

        if (position.Reposition)
        {
            position.Reposition = false;
            Database.Connection.Update<CryptoPosition>(position);
        }

        // Een afgesloten posities is niet meer interessant, verplaatsen
        //GlobalData.Logger.Info($"analyze.HandlePosition.CancelOrdersIfClosedOrTimeoutOrReposition.After({Symbol.Name})");
        if (position.CloseTime.HasValue)
        {
            PositionTools.RemovePosition(position.TradeAccount, position);
            if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                GlobalData.PositionsHaveChanged("");
        }
    }


    public async Task HandlePosition(CryptoPosition position)
    {
        //GlobalData.Logger.Info($"position:" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));

        // Itereer alle openstaande parts
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // voor de niet afgesloten parts...
            if (!part.CloseTime.HasValue)
            {

                // De prepare controleert of we een geldige candle in het interval (van de part of positie) hebben!
                if (Prepare(position, part, out CryptoCandle candleInterval))
                {
                    // Controleer de BUY
                    if (part.Name.Equals("BUY"))
                        await HandleBuyPart(position, part, candleInterval, GlobalData.Settings.Trading.BuyStepInMethod, GlobalData.Settings.Trading.BuyOrderMethod);

                    // Controleer de DCA
                    if (position.Quantity > 0 && part.Name.Equals("DCA"))
                        await HandleBuyPart(position, part, candleInterval, GlobalData.Settings.Trading.DcaStepInMethod, GlobalData.Settings.Trading.DcaOrderMethod);

                    // Controleer de SELL (indien we gekocht hebben)
                    if (position.Quantity > 0)
                        await HandleSellPart(position, part, candleInterval);
                }


                if (GlobalData.Settings.Trading.LockProfits)
                {
                    // Kunnen we afsluiten met winst?
                    if (position.Quantity > 0)
                    {
                        if (position.CreateTime.AddDays(-20) > LastCandle1mCloseTimeDate)
                            await HandleCheckProfitableSellPart(position, part, 0.25m);
                        else if (position.CreateTime.AddDays(-10) > LastCandle1mCloseTimeDate)
                            await HandleCheckProfitableSellPart(position, part, 0.50m);
                        else
                            await HandleCheckProfitableSellPart(position, part, GlobalData.Settings.Trading.ProfitPercentage);
                    }
                }

            }
        }



        // Is er wel een initiele SELL order aanwezig? zoniet dan dit alsnog doen!
        // (buiten de Prepare loop gehaald die intern een controle op het interval doet)
        // Dus nu wordt de sell order vrijwel direct geplaatst (na een 1m candle)
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // voor de niet afgesloten parts...
            if (!part.CloseTime.HasValue)
            {
                CryptoPositionStep step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Buy, true);
                if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
                {
                    if (position.Quantity > 0) // voldoende saldo om de sell te plaatsen
                    {
                        step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Sell, false);
                        if (step == null)
                        {
                            await PlaceFirstSellOrder(position, part, "placing");
                        }
                        else
                        {
                            // Als we het verkoop percentages aangepast hebben is het wel prettig dat de order aangepast wordt)
                            if (part.StepOutMethod == CryptoStepInMethod.FixedPercentage)
                            {
                                decimal sellPrice = CalculateSellPrice(position);
                                if (step.Price != sellPrice)
                                {
                                    var (cancelled, tradeParams) = await CancelOrder(position, part, step, CryptoOrderStatus.ChangedSettings);
                                    if (GlobalData.Settings.Trading.LogCanceledOrders)
                                    {
                                        string text3 = $"annuleren vanwege aanpassing percentage ({step.Price} -> {sellPrice})";
                                        ExchangeBase.Dump(position.Symbol, cancelled, tradeParams, text3);
                                    }
                                    await PlaceFirstSellOrder(position, part, "modifying");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
#endif


    public bool CreateSignals()
    {
        int createdSignals = 0;
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
                    if (createSignal.CreatedSignal)
                        createdSignals++;

                    // Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                    //AnalyseCount++;
                    Interlocked.Increment(ref AnalyseCount);
                }
            }
        }
        //GlobalData.Logger.Info($"CreateSignals(stop):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));

        return createdSignals > 0;
    }


    private void CleanSymbolData()
    {
        // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
        // ieder geval de CandleData te clearen. Vanaf x candles terug tot de eerste de beste die null is.

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (LastCandle1mCloseTime % interval.Duration == 0)
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
                    Monitor.Exit(Symbol.CandleList);
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

            //GlobalData.Logger.Info($"analyze:" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));

            // Create signals per interval
            //GlobalData.Logger.Info($"analyze.CreateSignals({Symbol.Name})");
            bool hasCreatedAsignal = CreateSignals();

#if TRADEBOT
            // Idee1: Zet de echte (gemiddelde) price in de step indien deze gevuld is (het is nu namelijk
            // onduidelijk voor welke prijs het exact verkocht is = lastig met meerdere trades igv market)
            // Idee2: Zet de buyPrice en de echte (gemiddelde)sellPrice in de part indien deze gevuld zijn ()
            // Probleem: De migratie van de oude naar een nieuwe situatie (als je het al zou uitvoeren)


            //#if BALANCING
            // TODO: Weer werkzaam maken
            //if (GlobalData.Settings.BalanceBot.Active && (symbol.IsBalancing))
            //GlobalData.ThreadBalanceSymbols.AddToQueue(symbol);
            //#endif


            // Simulate Trade indien openstaande orders gevuld zijn
            //GlobalData.Logger.Info($"analyze.PaperTradingCheckOrders({Symbol.Name})");
            if (GlobalData.BackTest)
                await PaperTrading.PaperTradingCheckOrders(Database, GlobalData.ExchangeBackTestAccount, Symbol, LastCandle1m);
            if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                await PaperTrading.PaperTradingCheckOrders(Database, GlobalData.ExchangePaperTradeAccount, Symbol, LastCandle1m);


            // Pauzeren vanwege de trading regels of te lage barometer
            PauseBecauseOfTradingRules = TradingRules.CheckTradingRules(LastCandle1m);
            PauseBecauseOfBarometer = TradingRules.CheckBarometerValues(Symbol, LastCandle1m);
            
            // Open or extend a position
            // Vraag, willen we wel DCA's als de barometer of trading rules een probleem zijn?
            if (hasCreatedAsignal)
                CreateOrExtendPositionViaSignal();

            // Per (actief) trade account de posities controleren
            foreach (CryptoTradeAccount tradeAccount in GlobalData.ActiveTradeAccountList.Values.ToList())
            {
                // Aan de hand van de instellingen accounts uitsluiten
                if (PositionTools.ValidTradeAccount(tradeAccount, Symbol))
                {
                    // Check the positions
                    if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var positionList))
                    {
                        foreach (CryptoPosition position in positionList.Values.ToList())
                        {
                            // Verwijder orders voor verschillende redenenen (timeout, barometer, tradingrules, positie gesloten, reposition enzovoort)
                            await CancelOrdersIfClosedOrTimeoutOrReposition(position);

                            if (!position.CloseTime.HasValue)
                            {
                                // Pauzeren vanwege de trading regels of te lage barometer
                                if (!(PauseBecauseOfTradingRules || PauseBecauseOfBarometer))
                                    CheckAddDcaFixedPercentage(position);

                                // Plaats of modificeer de buy of sell orders + optionele LockProfits
                                await HandlePosition(position);
                            }
                        }
                    }
                }
            }
#endif

            // Remove old candles or CandleData
            //GlobalData.Logger.Info($"analyze.CleanSymbolData({Symbol.Name})");
            //if (!GlobalData.BackTest)
            //    CleanSymbolData();

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
/// Dit is het koop en verkoop gedeelte (simpelweg gebaseerd op de candle.low en candle.high enzovoort)
/// Dat heeft beperkingen aangezien de werkelijkheid zich qua tijdlijn anders gedraagt dan low en/of high

using System.Text;

using Binance.Net.Enums;

using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;

namespace CryptoSbmScanner.BackTest;


public class BackTestEmulator
{
    private readonly CryptoSymbol Symbol;
    private readonly CryptoSymbolInterval SymbolInterval;
    private readonly CryptoInterval Interval;
    private readonly SortedList<long, CryptoCandle> Candles;
    private readonly CryptoBackConfig Config;

    public DateTime StartTime;
    public BackTestData Data = new();
    public SignalCreateBase CryptoBackTest;
    public CryptoBackTestResults Results;

    public CryptoCandle CandleLast;
    public StringBuilder Log;

    //************************************************
    // Persistent data voor de emulator
    // ID van de order in het log bestand (enkel ter tracering)
    public int DcaIndex = 0;
    public int BuyTimeOut = 0;
    // De laatste prijs waarop (bij)gekocht werd
    public decimal LastBuyPrice;
    //public CryptoPosition Position = null;
    //************************************************


    public BackTestEmulator(CryptoSymbol symbol, CryptoInterval interval, CryptoBackConfig config)
    {
        Symbol = symbol;
        //Exchange = symbol.Exchange;
        Interval = interval;
        //this.QuoteData = symbol.QuoteData;
        Config = config;

        SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        Candles = SymbolInterval.CandleList;
    }

    private decimal CalculateStopPrice(decimal whateverPrice, int dcaIndex, bool calculateStopPrice)
    {
        decimal value;
        decimal price = 0;

        switch (Config.DcaMethod)
        {
            case DcaMethod.Absolute:
                // Absoluut ten opzichte van de laatste dca order (is niet hetzelfde als ViaPrice vreemd genoeg)
                value = (Config.StoplossPercentage * dcaIndex * Data.Position.BuyPrice / 100m);
                price = Data.Position.BuyPrice - value;
                //stopPrice = price - value;
                break;
            case DcaMethod.Relative:
                // Relatief ten opzichte van de berekende break-even price
                decimal stoplossValue = (Config.StoplossPercentage * Data.Position.BreakEvenPrice / 100m);
                price = Data.Position.BreakEvenPrice - stoplossValue;
                break;
            case DcaMethod.ViaPrice:
                // dit was een foutieve benadering, maar doet het onverwacht wel goed (wel erg hoge stopploss!)
                // Ik had hier netjes in x blokken nieuwe orders verwacht, maar dat was dus niet zo!
                value = (Config.StoplossPercentage * dcaIndex * whateverPrice / 100m);
                price = whateverPrice - value;
                break;
            case DcaMethod.ViaLastPrice:
                // Omdat de buyprice naar beneden kan worden verplaatst (i.v.m. trailing)
                // Dit zit niet lekker in elkaar met die dcaindex en de laatste levels
                if (calculateStopPrice)
                    value = (Config.StoplossPercentage * 2 * LastBuyPrice / 100m);
                else
                    value = (Config.StoplossPercentage * 1 * LastBuyPrice / 100m);
                price = LastBuyPrice - value;
                break;
        }


        return price;
    }

    private void ZetVervolgOrders(string text, decimal price)
    {
        // Alleen de 1e order kan volgens de instellingen
        TakeProfitMethod takeProfitMethod = Config.TakeProfitMethod;
        if (DcaIndex > 0)
            takeProfitMethod = TakeProfitMethod.FixedProfit;


        // Annuleer openstaande orders
        // (ze moeten allemaal weg, ook de trailing orders)
        // Onthoud de laatste gevulde buy order
        CryptoPositionStep lastBuy = null;
        foreach (CryptoPositionPart part in Data.Position.Parts.Values)
        {
            for (int i = part.Steps.Values.Count - 1; i >= 0; i--)
            {
                CryptoPositionStep step = part.Steps.Values[i];

                if (step.Status == OrderStatus.New)
                {
                    step.Status = OrderStatus.Canceled;
                    part.Steps.Remove(step.Id);
                }

                // Neem de laagste dca (werkt wellicht niet 100% als er meerdere dca's zijn en hergebruikt worden)
                if ((i > 0) && (step.Mode == TradeDirection.Long) && (step.Status == OrderStatus.Filled))
                {
                    if ((lastBuy == null) || (step.Price < lastBuy.Price))
                        lastBuy = step;
                }
            }
        }


        // Plaats bijkoop orders
        // Plaats een bijkoop op de BE-Percentage -x (er is maar 1 buy order actief voor dit systeem)
        decimal quantity;
        decimal dcaPrice = price;
        if (DcaIndex < Config.AmountOfDca)
        {
            // We "verdubbelen" de inleg! (die was pijnlijk in Zignaly)
            quantity = Config.DcaFactor * Data.Position.Quantity;
            dcaPrice = CalculateStopPrice(price, DcaIndex, false);

            CryptoPositionPart part = PositionTools.CreatePositionPart(Data.Position, CandleLast);
            part.Id = ++BackTest.UniqueId;
            part.Name = "DCA";
            PositionTools.AddPositionPart(Data.Position, part);

            CryptoPositionStep buy = PositionTools.CreatePositionStep(part, TradeDirection.Long, CryptoOrderType.Limit, dcaPrice, quantity, null);
            buy.Id = ++BackTest.UniqueId;
            buy.OrderId = ++BackTest.UniqueId;
            buy.Name = "DCA";
            PositionTools.AddPositionPartStep(part, buy);

            // De buy wordt pas geactiveerd als het onder de activatie grens komt.
            // Pfft, waarom reserveer je die nu dan al?
            if (Config.UseBuyTracing)
            {
                buy.StopPrice = buy.Price;
                buy.TrailActivatePrice = buy.Price;
                buy.Trailing = CryptoTrailing.TrailWaiting;
                buy.OrderType = CryptoOrderType.NotOnMarketYet;
            }

            Log.AppendLine(string.Format("{0} placed {1} DcaIndex={2}", text, buy.DisplayText(Symbol.PriceDisplayFormat), DcaIndex));
        }




        // Bepaal hoe we de orders verdelen (indien er meer dan 1 zijn)
        // Een profittrail is een yoyo trade met de laatste buy quantity!
        // Reserveer alvast een hoeveelheid voor deze speciale order...
        bool doUseProfitTrailingLastOrder = false;
        decimal totalQuantity = Data.Position.Quantity;

        // Als we een ProfitTrail doen dan is dat een aparte order met de laatste dcaX.
        // Een ProfitTrail kan alleen als we de order opbreken (en de laatste dcaX als aparte order inzetten)
        if ((Config.UseProfitTrailing) && (lastBuy != null))
        {
            // De regeltjes van de exchange toepassen
            if (totalQuantity - lastBuy.Quantity - Symbol.QuantityMinimum > 0)
            {
                // dit wordt een trailing oco (die een stop limit sell wordt indien geactiveerd)
                totalQuantity -= lastBuy.Quantity;
                doUseProfitTrailingLastOrder = true;
            }
        }
        decimal startQuantity = totalQuantity;



        // Onderstaande kan allemaal optimaler (maar zo is het voorlopig makkelijk manipuleerbaar)


        // 1 stap onder de bijkoop prijs
        decimal stopPrice;
        if (DcaIndex < Config.AmountOfDca)
            stopPrice = CalculateStopPrice(dcaPrice, DcaIndex + 1, true);
        else
            stopPrice = CalculateStopPrice(dcaPrice, DcaIndex, true);


        if (startQuantity > 0m)
        {
            decimal sellPrice;
            CryptoPositionStep oco;

            List<CryptoPositionStep> placedOrders = new();

            if (takeProfitMethod != TakeProfitMethod.FixedProfit)
            {
                sellPrice = Data.Position.BreakEvenPrice + Config.TakeProfitMethodPercentage1 * Data.Position.BreakEvenPrice / 100m;
                if (DcaIndex == 0)
                {
                    switch (takeProfitMethod)
                    {
                        case TakeProfitMethod.BollingerBandSma:
                            sellPrice = (decimal)CandleLast.CandleData.Sma20 + Config.TakeProfitMethodCorrectionTicks * Symbol.PriceTickSize;
                            break;
                        case TakeProfitMethod.BollingerBandUpper:
                            sellPrice = (decimal)CandleLast.CandleData.BollingerBandsUpperBand + Config.TakeProfitMethodCorrectionTicks * Symbol.PriceTickSize;
                            break;
                    }
                }
                if (sellPrice < CandleLast.High)
                    sellPrice = CandleLast.High;

                sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                quantity = startQuantity;
                quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                // Ehhh, die hoort bij een vorige buy part, maar welke is het? ;-)
                oco = Data.Position.CreateOrder(TradeDirection.Short, CryptoOrderType.Oco, sellPrice, quantity, stopPrice);
                oco.Id = ++BackTest.UniqueId;
                oco.OrderId = ++BackTest.UniqueId;
                //oco.Name = "oco";
                oco.LockingMethod = LockingMethod.FixedQuantity;
                placedOrders.Add(oco);
            }

            else
            {

                // Dan kan deze over meerdere orders verdeeld worden (ietwat lastig vanwege limiten en afronding)

                List<(decimal, decimal)> quantityList = new();
                if (Config.TakeProfitMethodPercentage1 > 0m)
                    quantityList.Add((Config.TakeProfitMethodPercentage1, Config.TakeProfitMethodQuantityPercentage1));
                if (Config.TakeProfitMethodPercentage2 > 0m)
                    quantityList.Add((Config.TakeProfitMethodPercentage2, Config.TakeProfitMethodQuantityPercentage2));
                if (Config.TakeProfitMethodPercentage3 > 0m)
                    quantityList.Add((Config.TakeProfitMethodPercentage3, Config.TakeProfitMethodQuantityPercentage3));
                if (Config.TakeProfitMethodPercentage4 > 0m)
                    quantityList.Add((Config.TakeProfitMethodPercentage4, Config.TakeProfitMethodQuantityPercentage4));


                for (int i = 0; i < quantityList.Count; i++)
                {
                    (decimal, decimal) q = quantityList[i];

                    // De Xe OCO sell zetten
                    sellPrice = price + q.Item1 * price / 100m; // was Data.Position.BreakEvenPrice
                    sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    if (i == quantityList.Count - 1)
                        quantity = totalQuantity;
                    else
                        quantity = startQuantity * q.Item2 / 100m;

                    if (quantity < 0)
                    {
                        StringBuilder builder = new();
                        PositionTools.Dump(Data.Position, builder);
                        Log.AppendLine("");
                        Log.AppendLine("Something is wrong, quantity < 0 #1");
                        Log.AppendLine(builder.ToString());
                        Log.AppendLine("");
                        throw new Exception("Something is wrong, quantity < 0 (zie log)");
                    }

                    // Dat is vervelend, deze rond het af (maaf geeft dat problemen?)!
                    quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    totalQuantity -= quantity;
                    if (totalQuantity < 0)
                    {
                        StringBuilder builder = new();
                        PositionTools.Dump(Data.Position, builder);
                        Log.AppendLine("");
                        Log.AppendLine("Something is wrong, TotalQuantity < 0 #1");
                        Log.AppendLine(builder.ToString());
                        Log.AppendLine("");
                        throw new Exception("Something is wrong, TotalQuantity < 0 (zie log)");
                    }


                    // Het totaal is vanwege exchange limitene te weinig om te kunnen plaatsen op de exchange, dan bij de vorige order toevoegen!
                    if ((placedOrders.Count > 0) && ((quantity * stopPrice <= Symbol.MinNotional) || quantity <= Symbol.QuantityMinimum))
                    {
                        oco = placedOrders.Last();
                        oco.Quantity += quantity;
                    }
                    else
                    {
                        // Ehhh, die hoort bij een vorige buy part, maar welke is het? ;-)
                        oco = Data.Position.CreateOrder(TradeDirection.Short, CryptoOrderType.Oco, sellPrice, quantity, stopPrice);
                        oco.Id = ++BackTest.UniqueId;
                        oco.OrderId = ++BackTest.UniqueId;
                        //oco.Name = "oco";
                        oco.LockingMethod = LockingMethod.FixedQuantity;
                        placedOrders.Add(oco);
                    }


                }
            }

            foreach (CryptoPositionStep order in placedOrders)
            {
                // Wat administratie bijwerken
                order.QuoteQuantityInitial = order.Quantity * order.Price;

                if ((order.Mode == TradeDirection.Short) && (Config.UseSellTracing))
                {
                    order.Trailing = CryptoTrailing.TrailWaiting;
                    order.TrailActivatePrice = Data.Position.BreakEvenPrice + Config.UseSellTrailingPercentage * Data.Position.BreakEvenPrice / 100m;
                }

                Log.AppendLine(string.Format("{0} placed {1}", text, order.DisplayText(Symbol.PriceDisplayFormat)));
            }
        }


        // De laatste buy wordt (bij activatie) een aparte yoyo order die met een stop limit de order proberen te trailen 
        // vanaf de trailprice (deze blijft interessant om een positie zo weinig mogelijk verlies te laten oplopen (denk ik))
        if (doUseProfitTrailingLastOrder)
        {
            // De Xe OCO sell zetten
            decimal sellPrice = Data.Position.BreakEvenPrice + Config.TakeProfitMethodPercentage1 * Data.Position.BreakEvenPrice / 100m;
            sellPrice = sellPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

            quantity = lastBuy.Quantity;
            quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

            // Ehhh, die hoort bij een vorige buy part, maar welke is het? ;-)
            CryptoPositionStep oco = Data.Position.CreateOrder(TradeDirection.Short, CryptoOrderType.Oco, sellPrice, quantity, stopPrice);
            oco.Id = ++BackTest.UniqueId;
            oco.OrderId = ++BackTest.UniqueId;
            //oco.Name = "oco";
            oco.Trailing = CryptoTrailing.TrailWaiting;
            oco.TrailActivatePrice = lastBuy.Price + Config.UseSellTrailingPercentage * lastBuy.Price / 100m;
            oco.QuoteQuantityInitial = quantity * sellPrice; // Een benadering
            oco.LockingMethod = LockingMethod.FixedQuantity;
            oco.FromDcaIndex = DcaIndex;
            Log.AppendLine(string.Format("{0} placed {1}", text, oco.DisplayText(Symbol.PriceDisplayFormat)));
        }


        Log.AppendLine("...");
    }


    private bool CalculateTrailingQuestion(CryptoPositionStep order, decimal limit, out decimal price)
    {
        price = 0.0m;
        //CryptoCandle candleX;
        //long loopTime = CandleLast.OpenTime;

        //// Zijn er x groene candles? (leuk voor de sell, maar niet voor de buy!)
        //int greenCandles = 0;
        //for (int i = 0; i < 8; i++)
        //{
        //    if ((Candles.TryGetValue(loopTime - i * Interval.Duration, out candleX)) && (candleX.Close < candleX.Open))
        //        greenCandles++;
        //}
        //if (greenCandles < 3)
        //    return false;

        //decimal price;
        if (!CalculateTrailing(order, out price))
            return false;

        decimal diff = Math.Abs(100 * ((decimal)price - CandleLast.Close) / CandleLast.Close);
        if (diff < limit)
            return false;

        return true;

    }


    /// <summary>
    /// Tracing.
    /// Er is gekozen om 4 candles terug te kijken voor de open/close prijs
    /// En voor de PSAR gebruiken we de waarde van 1 candle terug (of 2 want de candle is done)
    /// </summary>
    private bool CalculateTrailing(CryptoPositionStep order, out decimal price)
    {
        CryptoCandle candle;
        decimal minValue = decimal.MaxValue;
        decimal maxValue = decimal.MinValue;

        // de candle waarder erbij betrekken
        for (int x = 0; x < Config.UseSellTracingCandles; x++)
        {
            if (!Candles.TryGetValue(CandleLast.OpenTime - x * Interval.Duration, out candle))
            {
                price = 0m;
                return false;
            }

            minValue = Math.Min(minValue, candle.Low);
            maxValue = Math.Max(maxValue, candle.High);
        }


        // de psar value erbij betrekken
        int steps;
        bool usePsar;
        if (order.Mode == TradeDirection.Long)
        {
            usePsar = Config.UseBuyTracingWithPSar;
            steps = Config.UseBuyTracingPSarCandles;
        }
        else
        {
            usePsar = Config.UseSellTracingWithPSar;
            steps = Config.UseSellTracingPSarCandles;
        }

        if (usePsar)
        {
            for (int x = 0; x < steps; x++)
            {
                if (!Candles.TryGetValue(CandleLast.OpenTime - x * Interval.Duration, out candle))
                {
                    price = 0m;
                    return false;
                }

                minValue = Math.Min(minValue, (decimal)candle.CandleData.PSar);
                maxValue = Math.Max(maxValue, (decimal)candle.CandleData.PSar);
            }
        }

        if (order.Mode == TradeDirection.Long)
            price = maxValue + Symbol.PriceTickSize;
        else
            price = minValue - Symbol.PriceTickSize;
        price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);


        if (order.Trailing == CryptoTrailing.TrailWaiting)
        {
            if (order.Mode == TradeDirection.Long)
            {
                if (price < order.TrailActivatePrice)
                    return true;
            }
            else
            {
                if (price > order.TrailActivatePrice)
                    return true;
            }
        }
        else
        {
            if (order.Mode == TradeDirection.Long)
            {
                if (price < order.StopPrice)
                    return true;
            }
            else
            {
                if (price > order.StopPrice)
                    return true;
            }
        }

        price = 0.0m;
        return false;
    }



    public void CalculateBuyPrice(CryptoPosition position)
    {
        // Bereken de instap op basis van de aangeboden candle
        DcaIndex = 0;
        BuyTimeOut = Config.WaitCandles;

        // **********************************************************
        // We maken de buy prijs beslissing op basis van de laatste candle
        // (en een market order moet plaatsvinden op de volgende candle..)
        // **********************************************************
        string text = CandleLast.OhlcText(Symbol, Interval, Symbol.PriceDisplayFormat, false, false, Config.LogIncludeVolume); // string.Format("{0} {1}", Symbol.Name, CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
        string stos = CryptoBackTest.DisplayText();
        Log.AppendLine(text + " " + stos + " starting emulator..");
        Log.AppendLine("...");


        if (Config.BuyPriceStrategy != BuyPriceStrategy.MarketOrder)
        {
            // De eerste aankoop om in positie te komen, en de vraag is wat we als 
            // aankoop prijs willen (hoeveel geven we "toe" om in de trade te komen?).
            string name = "?";
            decimal price = 0;
            switch (Config.BuyPriceStrategy)
            {
                case BuyPriceStrategy.CandleLowest:
                    name = "Candle.Lowest";
                    price = Math.Min(CandleLast.Close, CandleLast.Open);
                    break;
                case BuyPriceStrategy.CandleClose:
                    name = "Candle.Lowest";
                    price = CandleLast.Close;
                    break;
                case BuyPriceStrategy.CandleAverage:
                    name = "Candle.Average";
                    price = Math.Min(CandleLast.Close, CandleLast.Open) + (Math.Max(CandleLast.Close, CandleLast.Open) - Math.Min(CandleLast.Close, CandleLast.Open)) / 2;
                    break;
                case BuyPriceStrategy.CandleHighest:
                    name = "Candle.Highest";
                    price = Math.Max(CandleLast.Close, CandleLast.Open);
                    break;
                case BuyPriceStrategy.BollingerBands:
                    name = "Candle.LowerBand";
                    price = (decimal)CandleLast.CandleData.BollingerBandsLowerBand;
                    break;
                case BuyPriceStrategy.Experiment:
                    name = "Candle.Experiment";
                    price = (decimal)CandleLast.Close - ((Config.BuyPriceLower / 100) * CandleLast.Close);
                    break;
            }

            price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
            decimal quantity = Config.BuyAmount / price;

            CryptoPositionPart part = PositionTools.CreatePositionPart(Data.Position, CandleLast);
            part.Id = ++BackTest.UniqueId;
            PositionTools.AddPositionPart(Data.Position, part);

            CryptoPositionStep buy = PositionTools.CreatePositionStep(part, TradeDirection.Long, CryptoOrderType.Limit, price, quantity, null);
            buy.Id = ++BackTest.UniqueId;
            buy.OrderId = ++BackTest.UniqueId;
            buy.Name = "DCA" + DcaIndex.ToString();
            PositionTools.AddPositionPartStep(part, buy);

            if (Config.UseBuyTracingForFirstOrder)
            {
                buy.StopPrice = buy.Price;
                buy.TrailActivatePrice = buy.Price;
                buy.OrderType = CryptoOrderType.NotOnMarketYet;
                buy.Trailing = CryptoTrailing.TrailWaiting;
            }
            //buy.Name = "buy";
            buy.QuoteQuantityInitial = Config.BuyAmount;
            buy.LockingMethod = LockingMethod.FixedValue; // voor trailing
            Log.AppendLine(string.Format("{0} buymethod={1} Limit buy order price={2} quantity={3} cost={4}", text, name + buy.Mode.ToString(), buy.Price.ToString(Symbol.PriceDisplayFormat), buy.Quantity, Config.BuyAmount));

            position.BuyPrice = price;
            position.BuyAmount = quantity;
        }
    }


    public EmulatorResult HandleCandle()
    {
        string text = CandleLast.OhlcText(Symbol, Interval, Symbol.PriceDisplayFormat, false, false, Config.LogIncludeVolume);
        //Log.AppendLine(text);


        // De eerste koop (of trace)
        if (DcaIndex == 0)
        {
            // Zolang niet gekocht flink wat informatie tonen (daarna niet spannend meer)
            // Als die niet teruggeeft doe dan een fallback naar de oude tekst
            string stos = CryptoBackTest.DisplayText();
            Log.AppendLine(string.Format("{0} {1}", text, stos));


            // De initiele market buy order uitvoeren
            if (Config.BuyPriceStrategy == BuyPriceStrategy.MarketOrder)
            {
                // Er van uitgaande dat we de order zo snel kunnen zetten? (denk het niet, aannames...)
                decimal price = CandleLast.Close;
                price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                decimal quantity = Config.BuyAmount / price;
                quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                CryptoPositionPart part = PositionTools.CreatePositionPart(Data.Position, CandleLast);
                part.Id = ++BackTest.UniqueId;
                PositionTools.AddPositionPart(Data.Position, part);
                //part.DcaIndex = DcaIndex; ?

                // als nog een step aanmaken met daarin de market order (+direct afhandelen)
                CryptoPositionStep step = PositionTools.CreatePositionStep(part, TradeDirection.Long, CryptoOrderType.Market, price, quantity, null);
                step.Id = ++BackTest.UniqueId;
                step.OrderId = ++BackTest.UniqueId;
                step.Name = "DCA" + DcaIndex.ToString();
                PositionTools.AddPositionPartStep(part, step);

                // Het is een market order, dus direct afmelden (beetje jammer, had graag direct het geheel het liefst via trades getest)
                // Bij uitzondering de prijs bijwerken omdat het een market order is
                step.CloseTime = DateTime.UtcNow;
                step.Status = OrderStatus.Filled;
                step.QuantityFilled = step.Quantity;
                step.QuoteQuantityFilled = step.Price * step.Quantity;

                // verwarring, welke moet nu? (doe beide voorlopig maar, part zou genoeg moeten zijn....)
                // (en waarom doen we het voor de andere niet?)
                part.BuyPrice = price;
                part.BuyAmount = quantity;
                Data.Position.BuyPrice = price;
                Data.Position.BuyAmount = quantity;
                LastBuyPrice = step.Price;

                PositionTools.CalculateProfitAndBeakEvenPrice(Data.Position);

                DcaIndex++;
                Log.AppendLine("");
                Log.AppendLine(string.Format("{0} filled {1} breakeven={2} profit={3:N2} left={4}",
                    text, step.DisplayText(Symbol.PriceDisplayFormat), Data.Position.BreakEvenPrice.ToString(Symbol.PriceDisplayFormat),
                    Data.Position.Percentage, Data.Position.Quantity.ToString0()));
                ZetVervolgOrders(text, Data.Position.BreakEvenPrice); // was order.Price
            }
            else
            {
                // Probeer te kopen binnen zoveel candles
                // In de ideale wereld werkt dit, maar in de praktijk wordt er soms niet of gedeeltelijk
                // gevuld. Onderstaande is dus slechts een benadering van de werkelijkheid. 
                // En we passen hier bewust geen DCA toe (dat is voor nu te lastig)
                Log.AppendLine(text + " (waiting)");

                // Buy timeout?
                BuyTimeOut--;
                if (BuyTimeOut <= 0)
                {
                    Log.AppendLine(string.Format("{0} give up (timeout)", text));
                    Log.AppendLine("");
                    return EmulatorResult.Timeout;
                }

                // Het algoritme vragen
                
                if (SymbolInterval.Signal != null)
                {
                    CryptoSignal signal = SymbolInterval.Signal;
                    if (CryptoBackTest.GiveUp(signal))
                    {
                        Log.AppendLine(string.Format("{0} give up (algoritme) {1}", text, CryptoBackTest.ExtraText));
                        Log.AppendLine("");
                        return EmulatorResult.Timeout;
                    }
                }
            }
        }


        // Is er iets gekocht of verkocht?
        decimal doVervolgOrder = 0.0m;
        //CryptoPositionStep doVervolgLastDca = null;
        foreach (CryptoPositionPart part in Data.Position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status == OrderStatus.New)
                {
                    if ((step.Mode == TradeDirection.Long) && (
                        ((step.OrderType == CryptoOrderType.Limit) && (CandleLast.Low < step.Price)) ||
                        ((step.OrderType == CryptoOrderType.Oco) && (CandleLast.Low >= step.StopPrice)) ||
                        ((step.OrderType == CryptoOrderType.StopLimit) && (CandleLast.High >= step.StopPrice))
                        ))
                    {
                        if ((step.OrderType == CryptoOrderType.StopLimit) && (CandleLast.High >= step.StopPrice))
                            step.Price = (decimal)step.StopPrice; // voor de herberekening
                        if ((step.OrderType == CryptoOrderType.Oco) && (CandleLast.Low >= step.StopPrice))
                            step.Price = (decimal)step.StopPrice; // voor herberekening

                        step.CloseTime = DateTime.UtcNow; // todo - via de OpenTime + interval
                        step.Status = OrderStatus.Filled;
                        PositionTools.CalculateProfitAndBeakEvenPrice(Data.Position);
                        LastBuyPrice = step.Price;

                        Log.AppendLine("");
                        Log.AppendLine(string.Format("{0} filled {1} breakeven={2} profit={3:N2} left={4}",
                            text, step.DisplayText(Symbol.PriceDisplayFormat), Data.Position.BreakEvenPrice.ToString(Symbol.PriceDisplayFormat),
                            Data.Position.Percentage, Data.Position.Quantity.ToString0()));

                        // De orders opnieuw zetten
                        DcaIndex++;
                        doVervolgOrder = Math.Max(Data.Position.BreakEvenPrice, step.Price);
                    }
                    else if ((step.Mode == TradeDirection.Short) && (
                        ((step.OrderType == CryptoOrderType.Limit) && (CandleLast.High > step.Price)) ||
                        ((step.OrderType == CryptoOrderType.Oco) && (CandleLast.High > step.Price)) ||
                        ((step.OrderType == CryptoOrderType.Oco) && (CandleLast.Low <= step.StopPrice)) ||
                        ((step.OrderType == CryptoOrderType.StopLimit) && (CandleLast.Low <= step.StopPrice)) //&& (order.Trailing != CryptoTrailing.TrailWaiting)
                        ))
                    {

                        if ((step.OrderType == CryptoOrderType.StopLimit) && (CandleLast.Low <= step.StopPrice))
                            step.Price = (decimal)step.StopPrice; // voor de herberekening
                        if ((step.OrderType == CryptoOrderType.Oco) && (CandleLast.Low <= step.StopPrice))
                            step.Price = (decimal)step.StopPrice; // voor de herberekening

                        // Ook hier graag ets met een trade ipv hardcoded (dan testen we uiteindelijk meer van het algoritme)
                        step.CloseTime = DateTime.UtcNow; // todo - via de OpenTime + interval
                        step.Status = OrderStatus.Filled;
                        PositionTools.CalculateProfitAndBeakEvenPrice(Data.Position);
                        Log.AppendLine("");
                        Log.AppendLine(string.Format("{0} filled {1} breakeven={2} profit={3:N2} left={4}",
                            text, step.DisplayText(Symbol.PriceDisplayFormat), Data.Position.BreakEvenPrice.ToString(Symbol.PriceDisplayFormat),
                            Data.Position.Percentage, Data.Position.Quantity.ToString0()));

                        if (Data.Position.Quantity < 0)
                        {
                            StringBuilder builder = new();
                            PositionTools.Dump(Data.Position, builder);
                            Log.AppendLine("");
                            Log.AppendLine("Something is wrong, quantity < 0 #1");
                            Log.AppendLine(builder.ToString());
                            Log.AppendLine("");
                            throw new Exception("Something is wrong, quantity < 0 (zie log)");
                        }


                        if (Data.Position.Quantity == 0)
                        {
                            //Symbol.Exchange.PositionList.Clear(); //GlobalData.RemovePosition(position); //Symbol.PositionList.Clear();
                            PositionTools.RemovePosition(Data.Position);

                            // De results toevoegen op het moment dat de positie gesloten wordt
                            // (er kan op het einde nog een trade lopen die niet afgesloten kan worden)
                            Data.Position.CloseTime = CandleTools.GetUnixDate(CandleLast.OpenTime + Interval.Duration);
                            Results.Invested += Data.Position.Invested;
                            Results.Returned += Data.Position.Returned;
                            Results.Commission += Data.Position.Commission;

                            TimeSpan diffTime = (DateTime)Data.Position.CloseTime - (DateTime)Data.Position.CreateTime;
                            Results.TimeTotal += diffTime.TotalMinutes;
                            if (diffTime.TotalMinutes > Results.TimeLongest)
                                Results.TimeLongest = diffTime.TotalMinutes;
                            if (diffTime.TotalMinutes < Results.TimeShortest)
                                Results.TimeShortest = diffTime.TotalMinutes;

                            Results.ProfitLowest = Data.Position.Percentage;
                            Results.ProfitHighest = Data.Position.Percentage;

                            // De order kan verplaatst zijn (alsnog winst?)
                            if (Data.Position.Percentage >= 100m)
                            {
                                Results.WinCount++;
                                Results.ShowFooter(Log);
                                return EmulatorResult.Win;
                            }
                            else
                            {
                                Results.LossCount++;
                                Results.ShowFooter(Log);
                                return EmulatorResult.Lost;
                            }
                        }
                        else
                        {
                            // De laatste buy order is verkocht, de DCA opnieuw zetten (wat leven in de brouwerij)
                            if (step.FromDcaIndex > 0)
                            {
                                DcaIndex--; // verlagen, het is boven de vorige dca verkocht!

                                // nee, opnieuw plaatsen
                                //doVervolgOrder = Math.Max(Data.Position.BreakEvenPrice, order.Price);

                                CryptoPositionPart partX = PositionTools.CreatePositionPart(Data.Position, CandleLast);
                                partX.Id = ++BackTest.UniqueId;
                                partX.Name = "DCA";
                                PositionTools.AddPositionPart(Data.Position, partX);


                                // Ehhh, die hoort bij een vorige buy part, maar welke is het? ;-)
                                CryptoPositionStep buy = PositionTools.CreatePositionStep(partX, TradeDirection.Long, CryptoOrderType.Limit, step.Price, step.Quantity, null);
                                buy.Id = ++BackTest.UniqueId;
                                buy.OrderId = ++BackTest.UniqueId;
                                PositionTools.AddPositionPartStep(partX, buy);

                                //buy.Name = "dca";
                                buy.LockingMethod = LockingMethod.FixedValue; // Alleen relevant indien trail
                                buy.QuoteQuantityInitial = step.QuoteQuantityInitial;

                                // De buy wordt pas geactiveerd als het onder de activatie grens komt.
                                if (Config.UseBuyTracing)
                                {
                                    buy.StopPrice = buy.Price;
                                    buy.TrailActivatePrice = buy.Price;
                                    buy.Trailing = CryptoTrailing.TrailWaiting;
                                    buy.OrderType = CryptoOrderType.NotOnMarketYet;
                                }

                                Log.AppendLine(string.Format("{0} placed (again) {1} DcaIndex={2}", text, buy.DisplayText(Symbol.PriceDisplayFormat), DcaIndex));

                            }
                        }
                    }
                }
            }
        }


        // De trailing buy of sell (daar heb ik niet heel veel succes mee)
        foreach (CryptoPositionPart part in Data.Position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep order in part.Steps.Values.ToList())
            {
                if ((order.Status == OrderStatus.New) && (order.Trailing != CryptoTrailing.TrailNone))
                {
                    decimal price;
                    if (order.Mode == TradeDirection.Long)
                    {
                        if (order.Trailing == CryptoTrailing.TrailWaiting)
                        {
                            if (order.TrailActivatePrice.HasValue && (CandleLast.High < order.TrailActivatePrice) & CalculateTrailing(order, out price))
                            {
                                order.OrderType = CryptoOrderType.StopLimit; // // De order komt nu pas op de markt (was NotOnMarket)
                                order.Trailing = CryptoTrailing.TrailActive;
                                Log.AppendLine(string.Format("{0} {1} trailing {2} stoplimit verlaagd van {3:N8} naar={4:N8}", text, order.Mode, order.Short(), order.StopPrice, price));
                                // de prijs staat dan uiteraard hoger als de stopprijs (maar dat doet er niet aan toe in deze emulator)                                
                                order.Price = 2 * price;
                                order.StopPrice = price;

                                // er kan reeds van alles verkocht zijn, niet teveel kopen
                                order.Quantity = Config.DcaFactor * Data.Position.Quantity;
                                order.Quantity = order.Quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

                                // Administratie
                                order.QuoteQuantityInitial = order.Quantity * order.Price;
                            }
                        }
                        else if (order.Trailing == CryptoTrailing.TrailActive)
                        {
                            if (CalculateTrailing(order, out price))
                            {
                                Log.AppendLine(string.Format("{0} {1} trailing {2} stoplimit verlaagd van {3:N8} naar={4:N8}", text, order.Mode, order.Short(), order.StopPrice, price));
                                order.Price = 2 * price;
                                order.StopPrice = price;
                                if (order.LockingMethod == LockingMethod.FixedValue)
                                {
                                    // De prijs gaat naar beneden en dus krijgen we meer munten
                                    order.Quantity = order.QuoteQuantityInitial / order.Price;
                                    order.Quantity = order.Quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                                }
                            }
                        }
                    }
                    else if (order.Mode == TradeDirection.Short)
                    {
                        if (order.Trailing == CryptoTrailing.TrailWaiting)
                        {
                            if (order.TrailActivatePrice.HasValue && (CandleLast.Low > order.TrailActivatePrice) & CalculateTrailing(order, out price))
                            {
                                order.OrderType = CryptoOrderType.StopLimit;
                                order.Trailing = CryptoTrailing.TrailActive;
                                // de prijs staat dan uiteraard lager als de stopprijs (maar dat doet er niet aan toe in deze emulator)
                                Log.AppendLine(string.Format("{0} {1} trailing {2} stoplimit verhoogd van {3:N8} naar={4:N8}", text, order.Mode, order.Short(), order.StopPrice, price));
                                order.Price = 0.5m * price;
                                order.StopPrice = price;
                            }
                        }
                        else if (order.Trailing == CryptoTrailing.TrailActive)
                        {
                            if (CalculateTrailing(order, out price))
                            {
                                Log.AppendLine(string.Format("{0} {1} trailing {2} stoplimit verhoogd van {3:N8} naar={4:N8}", text, order.Mode, order.Short(), order.StopPrice, price));
                                order.Price = 0.5m * price;
                                order.StopPrice = price;
                                if (order.LockingMethod == LockingMethod.FixedValue)
                                {
                                    // De prijs gaat naar beneden en dus krijgen we meer munten
                                    order.Quantity = order.QuoteQuantityInitial / order.Price;
                                    order.Quantity = order.Quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                                }
                            }
                        }
                    }
                }
            }
        }



        // Een beetje gammel met die prijs wellicht?
        if (doVervolgOrder > 0.0m)
            ZetVervolgOrders(text, doVervolgOrder);


        return EmulatorResult.Waiting;
    }
}
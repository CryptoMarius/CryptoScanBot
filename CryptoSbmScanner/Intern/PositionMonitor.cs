using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;

using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Intern;

//Mogelijke foutmeldingen bij het kopen (of verkopen?):

//Te laag bedrag
//Filter failure: MIN_NOTIONAL
//price* quantity is too low to be a valid order for the symbol.

//Problemen met het aantal decimalen (zowel price als amount)
//buyResult.Error = {-1111: Precision is over the maximum defined for this asset. }

//Er is te weinig geld om de order te plaatsen
//buyResult.Error = {-1013: Filter failure: PRICE_FILTER }

// buyResult.Error = { -1013: Filter failure: LOT_SIZE }

//The relationship of the prices for the orders is not correct". The prices set in the OCO is breaking the Price rules.
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price < Last Price < Stop Price
// mooit overzicht: https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md

public class TradeParams
{
    // standaard buy of sell
    public bool IsBuy { get; set; }
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



    static public async Task<(bool result, TradeParams tradeParams)> PlaceBuyOrder(CryptoPosition position, int sequence, CryptoSymbol symbol, decimal lastBuyPrice, string dcaType, bool marketOrder = false)
    {
        decimal price = lastBuyPrice;
        decimal quantity;

        // Maak een lagere prijs als het niet de 1e buy order is
        if (sequence > 0)
        {
            // Afgesterd omdat een grote BB% bijna betekend dat de OCO stop boven de buy staat...

            // De lagere DCA-X prijs bepalen, 1.4% lager dan de price (anders 50% van de BB%) ---> zo te zien uiteindelijk op ~2% gefixeerd
            //decimal bbPercentage = 0m;
            //CalculateBollingerBandPercentage(symbol, intervalPeriod, out bbPercentage);
            //if (bbPercentage < 0.1m)
            //    bbPercentage = 1.5m;
            //else bbPercentage = 0.75m * bbPercentage;
            //if (bbPercentage < 2.0m)
            //    bbPercentage = 2.0m;

            // Dus gewoon lekker 2% onder vorige prijs
            decimal bbPercentage = Math.Abs(GlobalData.Settings.Trading.DcaPercentage);
            price -= price * (bbPercentage / 100);
        }


        // corrigeer de aangeboden prijs
        // Als de actuele prijs lager is dan de berekende prijs nemen we de actuele prijs
        // Reminder:
        // BTCUSDT, Base=BTC, Quote=USDT
        // De quantity wordt in Base (BTC)
        // De price en MinNotional is in Quote (USDT)

        if ((symbol.LastPrice.HasValue) && (symbol.LastPrice < price))
        {
            decimal oldPrice = price;
            price = (decimal)symbol.LastPrice;
            GlobalData.AddTextToLogTab("BUY correction: " + symbol.Name + " " + oldPrice.ToString("N6") + " to " + price.ToString0());
        }
        // De aankoop prijs verlagen (niet direct laten kopen?)
        if ((sequence == 0) && (GlobalData.Settings.Trading.GlobalBuyVarying != 0.0m))
        {
            decimal oldPrice = price;
            price += price * (GlobalData.Settings.Trading.GlobalBuyVarying / 100);
            GlobalData.AddTextToLogTab("BUY percentage: " + symbol.Name + " " + oldPrice.ToString("N6") + " to " + price.ToString0());
        }
        // De aankoop prijs verlagen (niet direct laten kopen?)
        if ((sequence > 0) && (GlobalData.Settings.Trading.GlobalBuyVarying < 0.0m))
        {
            decimal oldPrice = price;
            price += price * (GlobalData.Settings.Trading.GlobalBuyVarying / 100);
            GlobalData.AddTextToLogTab("BUY percentage: " + symbol.Name + " " + oldPrice.ToString("N6") + " to " + price.ToString0());
        }
        decimal oldDebugPrice = price;
        price = price.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);


        // Het aantal te kopen muntjes bepalen (quantity)
        // Eerste aankoop via het aankoop bedrag van de quote
        if (sequence == 0)
            quantity = symbol.QuoteData.BuyAmount / price;
        else
        {
            // Gebruik het aankoop bedrag wat in de 1e aankoop order is gedaan (de waarde in de settings kan veranderen!)
            quantity = position.BuyAmount / price;
            quantity = sequence * quantity * Math.Abs(GlobalData.Settings.Trading.DcaFactor);
        }
        decimal oldDebugQuantity = quantity;
        quantity = quantity.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);


        // Controleer de limiten van de berekende bedragen, Minimum bedrag
        string text;
        if (!symbol.InsideBoundaries(quantity, price, out text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, text, oldDebugPrice, oldDebugQuantity));
            return (false, null);
        }


        // Plaats een buy order op Binance
        //TradeParams tradeParams;
        //WebCallResult<BinancePlacedOrder> result = Buy(symbol, quantity, price, out tradeParams);
        (bool result, TradeParams tradeParams) result;
        if (marketOrder)
            result = await Buy(symbol, quantity, null);
        else
            result = await Buy(symbol, quantity, price);
        if (result.result)
        {
            string text2 = string.Format("{0} POSITION {1} ORDER PLACED {2} {3} {4}", symbol.Name, dcaType,
                result.tradeParams.Price.ToString0(), result.tradeParams.Quantity.ToString0(), result.tradeParams.QuoteQuantity.ToString0());
            GlobalData.AddTextToLogTab(text2, true);
            GlobalData.AddTextToTelegram(text2);

            return result;
        }
        else return (false, null);
    }


    public static void CreatePositionStep(CryptoDatabase database, CryptoPosition position, TradeParams tradeParams, string name)
    {
        CryptoPositionStep step = new();
        step.PositionId = position.Id;
        step.Name = name;
        step.IsBuy = tradeParams.IsBuy;
        step.Status = OrderStatus.New;
        step.CreateTime = tradeParams.CreateTime;
        step.Price = tradeParams.Price;
        step.StopPrice = tradeParams.StopPrice;
        step.StopLimitPrice = tradeParams.LimitPrice;
        step.Quantity = tradeParams.Quantity;
        step.QuantityFilled = 0;
        step.QuoteQuantityFilled = 0;
        step.OrderId = tradeParams.OrderId;
        step.Order2Id = tradeParams.Order2Id;
        step.OrderListId = tradeParams.OrderListId; // onzin, maar ach

        position.Steps.Add(step.OrderId, step);
        if (step.Order2Id.HasValue)
            position.Steps.Add((long)step.Order2Id, step);

        database.Connection.Insert<CryptoPositionStep>(step);
    }

    /// <summary>
    /// Administratie bijwerken en de positie bewaren
    /// </summary>
    private void HandleAdministration(CryptoDatabase databaseThread, CryptoSymbol symbol, CryptoPosition position)
    {
        //TradeTools.CalculateProfit(position); is al gedaan aan het begin van de trade

        if ((position.Status == CryptoPositionStatus.positionTakeOver) || (position.Status == CryptoPositionStatus.positionTimeout))
        {
            // Dat is niet meer relevant
            position.Profit = 0;
            position.Invested = 0;
            position.Percentage = 0;
        }

        using (var transaction = databaseThread.BeginTransaction())
        {
            try
            {
                databaseThread.Connection.Update<CryptoPosition>(position, transaction);
                transaction.Commit();
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                transaction.Rollback();
                throw;
            }
        }

        if (position.CloseTime.HasValue)
        {
            symbol.ClearSignals();
            GlobalData.RemovePosition(position);
            GlobalData.AddTextToLogTab(String.Format("Debug: positie removed {0}", position.Status.ToString()));
        }
    }


    static public async Task<(bool result, TradeParams tradeParams)> Buy(CryptoSymbol symbol, decimal? quantity, decimal? price)
    {
        //tradeParams = null;
        BinanceWeights.WaitForFairBinanceWeight(1);

        // Plaats een buy order op Binance
        using (BinanceClient client = new())
        {
            WebCallResult<BinancePlacedOrder> result = null;
            if (price.HasValue)
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Buy, SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
            else
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Buy, price.HasValue ? SpotOrderType.Limit : SpotOrderType.Market, quantity);

            if (!result.Success)
            {
                string text = string.Format("{0} ERROR buy order {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
                text += string.Format("quantity={0}\r\n", quantity.ToString0());
                text += string.Format("sellprice={0}\r\n", price.ToString0());
                //text += string.Format("sellstop={0}\r\n", stop.ToString0());
                //text += string.Format("selllimit={0}\r\n", limit.ToString0());
                text += string.Format("lastprice={0}\r\n", symbol.LastPrice.ToString0());
                text += string.Format("trades={0}\r\n", symbol.TradeList.Count);
                GlobalData.AddTextToLogTab(text);
                GlobalData.AddTextToTelegram(text);
            }

            if ((result.Success) && (result.Data != null))
            {
                TradeParams tradeParams = new TradeParams();
                tradeParams.IsBuy = true;
                tradeParams.OrderId = result.Data.Id;
                tradeParams.Price = result.Data.Price;
                tradeParams.Quantity = result.Data.Quantity;
                tradeParams.CreateTime = result.Data.CreateTime;
                tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;

                return (true, tradeParams);
            }
            else return (false, null);
        }
    }


    static public async Task<(bool result, TradeParams tradeParams)> Sell(CryptoSymbol symbol, decimal? quantity, decimal? price)
    {
        BinanceWeights.WaitForFairBinanceWeight(1);

        // Plaats een sell order op Binance      
        using (BinanceClient client = new())
        {
            WebCallResult<BinancePlacedOrder> result = null;
            if (price.HasValue)
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
            else
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, SpotOrderType.Market, quantity);

            if (!result.Success)
            {
                string text = string.Format("{0} ERROR SELL order {1} {2}\r\n", symbol.Name, result.ResponseStatusCode, result.Error);
                text += string.Format("quantity={0}\r\n", quantity.ToString0());
                text += string.Format("sellprice={0}\r\n", price.ToString0());
                //text += string.Format("sellstop={0}\r\n", stop.ToString0());
                //text += string.Format("selllimit={0}\r\n", limit.ToString0());
                text += string.Format("lastprice={0}\r\n", symbol.LastPrice.ToString0());
                text += string.Format("trades={0}", symbol.TradeList.Count);
                GlobalData.AddTextToLogTab(text);
                GlobalData.AddTextToTelegram(text);

                //The relationship of the prices for the orders is not correct."	The prices set in the OCO is breaking the Price rules.
                //The rules are:
                //SELL Orders: Limit Price > Last Price > Stop Price
                //BUY Orders: Limit Price < Last Price < Stop Price

                // https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md
            }

            if ((result.Success) && (result.Data != null))
            {
                TradeParams tradeParams = new TradeParams();
                tradeParams.IsBuy = false;
                tradeParams.OrderId = result.Data.Id;
                tradeParams.Price = result.Data.Price;
                tradeParams.Quantity = result.Data.Quantity;
                tradeParams.CreateTime = result.Data.CreateTime;
                tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                return (true, tradeParams);
            }
            else return (false, null);
        }
    }


    static public async Task<(bool result, TradeParams tradeParams)> SellOco(CryptoSymbol symbol, decimal quantity, decimal price, decimal stop, decimal limit)
    {
        BinanceWeights.WaitForFairBinanceWeight(1);

        // Vanwege "The relationship of the prices for the orders is not correct." The prices set in the OCO 
        // is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)

        //TODO: Percentage DCA baseren op de 0.5* de BB%.
        //TODO: Niet bijkopen als de barometer te laag staat
        //TODO: Buy order weghalen als de barometer te laag staat
        //TODO: Posities verkopen als de barometer lager dan -2.5 komt te staan (market order)

        //"The relationship of the prices for the orders is not correct." The prices set in the OCO is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)
        //The rules are:
        //SELL Orders: Limit Price > Last Price > Stop Price
        //BUY Orders: Limit Price<Last Price<Stop Price

        //De prijs is dan ondertussen al onder de StopPrice beland?

        if (symbol.LastPrice > price)
        {
            // deze verkooppt dan zo'n beetje onmiddelijk (denk ik), lastig!!
            return await Sell(symbol, quantity, price);
        }
        else
        {
            // Plaats een buy order op Binance
            using (BinanceClient client = new())
            {
                WebCallResult<BinanceOrderOcoList> result = await client.SpotApi.Trading.PlaceOcoOrderAsync(symbol.Name, OrderSide.Sell, quantity, price: price, stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);
                if (!result.Success)
                {
                    string text = string.Format("{0} ERROR OCO SELL order {1} {2}\r\n", symbol.Name, result.ResponseStatusCode, result.Error);
                    text += string.Format("quantity={0}\r\n", quantity.ToString0());
                    text += string.Format("sellprice={0}\r\n", price.ToString0());
                    text += string.Format("sellstop={0}\r\n", stop.ToString0());
                    text += string.Format("selllimit={0}\r\n", limit.ToString0());
                    text += string.Format("lastprice={0}\r\n", symbol.LastPrice.ToString0());
                    text += string.Format("trades={0}\r\n", symbol.TradeList.Count);
                    GlobalData.AddTextToLogTab(text);
                    GlobalData.AddTextToTelegram(text);

                    //The relationship of the prices for the orders is not correct."	The prices set in the OCO is breaking the Price rules.
                    //The rules are:
                    //SELL Orders: Limit Price > Last Price > Stop Price
                    //BUY Orders: Limit Price < Last Price < Stop Price

                    // https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md
                }

                if ((result.Success) && (result.Data != null))
                {
                    //string text2 = string.Format("{0} POSITION {1} ORDER PLACED {2} {3} {4}", data.Symbol, dcaType, Price, Quantity, Price * Quantity);
                    //GlobalData.AddTextToLogTab(text2, true);
                    //GlobalData.AddTextToTelegram(text2);

                    // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
                    // TODO: COntroleren van de order id's (want ik heb ze in de administratie omgedraaid denk ik)
                    // niet dat dat zo heel veel uitmaakt, maar de ID's vielen bij het traceren op.

                    // De eerste order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
                    // De tweede order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
                    // het voorbeeld in de API is een buy (dus het kan ook andersom zijn)
                    // Een van de twee heeft ook een price/stopprice, de andere enkel een price


                    BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
                    BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();

                    TradeParams tradeParams = new TradeParams();
                    tradeParams.IsBuy = false;
                    tradeParams.OrderId = order1.Id;
                    tradeParams.Price = order1.Price;
                    tradeParams.StopPrice = stop; // order2.StopPrice;
                    tradeParams.LimitPrice = limit; // order2.  Hey, waarom is deze er niet?
                    tradeParams.Quantity = order1.Quantity;
                    tradeParams.CreateTime = order1.CreateTime;
                    tradeParams.Order2Id = order2.Id; // Een tweede ordernummer
                    tradeParams.OrderListId = order1.OrderListId; // Linked order
                    tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                    return (true, tradeParams);
                }
                else return (false, null);
            }
        }
    }


    public async Task<WebCallResult<BinanceOrderBase>> Cancel(CryptoSymbol symbol, CryptoPosition position, long? orderId)
    {
        BinanceWeights.WaitForFairBinanceWeight(1);

        // Annuleer een order 
        if (orderId.HasValue)
        {
            using (var client = new BinanceClient())
            {
                WebCallResult<BinanceOrderBase> result = await client.SpotApi.Trading.CancelOrderAsync(symbol.Name, orderId);
                if (!result.Success)
                {
                    string text = string.Format("{0} ERROR cancel order {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
                    GlobalData.AddTextToLogTab(text);
                    GlobalData.AddTextToTelegram(text);
                }
                return result;
            }
        }
        return null;
    }





    /// <summary>
    /// Calculate the BollingerBands once
    /// //Duplicaat code in het SignalAlgorim!!
    /// </summary>
    static public bool CalculateBollingerBandPercentage(CryptoSymbol symbol, CryptoIntervalPeriod intervalPeriod, out decimal percentage)
    {
        percentage = 0;
        //TODO: De CandleData informatie gebruiken van de candle ipv berekenen

        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(intervalPeriod);
        SortedList<long, CryptoCandle> lastMaxCandles = symbolPeriod.CandleList;
        List<CryptoCandle> quoteHistory = SignalCreate.CalculateHistory(lastMaxCandles, 30);

        List<BollingerBandsResult> bollingerList = (List<BollingerBandsResult>)Indicator.GetBollingerBands(quoteHistory);
        if (bollingerList.Count == 0)
            return false;

        //Als er te weinig candles aangeboden worden zijn ze null
        BollingerBandsResult lastbb = bollingerList[bollingerList.Count - 1];
        if ((lastbb.LowerBand == null) || (lastbb.UpperBand == null))
            return false;

        decimal bollingerBandsLowerBand = (decimal)lastbb.LowerBand;
        decimal bollingerBandsUpperBand = (decimal)lastbb.UpperBand;
        //De totale hoogte in % van de laatste prijs berekenen
        decimal bollingerBandsHeight = bollingerBandsUpperBand - bollingerBandsLowerBand;

        CryptoCandle lastQuote = quoteHistory[quoteHistory.Count - 1];
        decimal bollingerBandsPercentage = (bollingerBandsHeight / lastQuote.Close) * 100.0m;

        percentage = bollingerBandsPercentage;
        return true;
    }


    // standaard
    public async Task HandleTrade(CryptoSymbol symbol, BinanceStreamOrderUpdate data, bool paperTrade)
    {
        using (CryptoDatabase databaseThread = new())
        {
            //try
            //{
            /* ExecutionType: New = 0, Canceled = 1, Replaced = 2, Rejected = 3, Trade = 4, Expired = 5 */
            /* OrderStatus:  New = 0, PartiallyFilled = 1, Filled = 2, Canceled = 3, PendingCancel = 4, Rejected = 5, Expired = 6, Insurance = 7, Adl = 8 */
            string msgInfo = string.Format("{0} side={1} type={2} status={3} order={4} price={5} quantity={6} {7}=filled={8} /{9}", data.Symbol, data.Side, data.Type, data.Status,
                data.Id, data.Price.ToString0(), data.Quantity.ToString0(), symbol.Quote, data.QuoteQuantityFilled.ToString0(), data.QuoteQuantity.ToString0());


            // We zijn slechts geinteresseerd in een paar statussen. want de andere zijn niet interessant voor de order afhandeling,
            // het wordt enkel interessant na filled, partiallyfilled of Canceled! Nieuwe orders lijken mij niet interessant.
            // Opmerking - observatie: Daar wordt de log aardig wat rustiger van, heerlijk!
            string s = string.Format("handletrade#1 {0}", msgInfo);
            if (paperTrade)
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
            await symbol.Exchange.PositionListSemaphore.WaitAsync();
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
                CryptoPositionStep step = null;

                // Er kunnen meerdere posities bij de munt open staan
                if (symbol.Exchange.PositionList.TryGetValue(symbol.Name, out var positionList))
                {
                    for (int i = 0; i < positionList.Count; i++)
                    {
                        CryptoPosition posTemp = positionList.Values[i];
                        if (posTemp.PaperTrade == paperTrade && posTemp.Steps.TryGetValue(data.Id, out step))
                        {
                            position = posTemp;

                            s = string.Format("handletrade#2 {0} positie gevonden, name={1} id={2} positie.status={3} (memory)", msgInfo, step.Name, step.Id, position.Status);
                            GlobalData.AddTextToLogTab(s);
                            break;
                        }
                    }
                }


                // De positie staat niet in het geheugen (timeout en buy hebben elkaar wellicht gekruist?)
                if ((position == null) || (step == null))
                {
                    // Controleer tevens via de database of we een positie niet alsnog moeten inladen
                    string sql = string.Format("select * from positionstep where OrderId={0} or Order2Id={1}", data.Id, data.Id);
                    step = databaseThread.Connection.QueryFirstOrDefault<CryptoPositionStep>(sql);
                    if ((step != null) && (step.Id > 0))
                    {
                        // De gevonden positie en stappen alsnog uit de database laden
                        position = databaseThread.Connection.Get<CryptoPosition>(step.PositionId);
                        GlobalData.AddPosition(position);

                        sql = string.Format("select * from positionstep where PositionId={0} order by id", position.Id);
                        foreach (CryptoPositionStep stepX in databaseThread.Connection.Query<CryptoPositionStep>(sql))
                        {
                            // Ook de orders aanmelden in de indexlijst (willen we dat straks ook?)
                            position.Steps.Add(stepX.OrderId, stepX);
                            if (stepX.Order2Id.HasValue)
                                position.Steps.Add((long)stepX.Order2Id, stepX);
                        }

                        if ((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled))
                        {
                            CryptoPositionStatus? oldStatus = position.Status;
                            position.Status = CryptoPositionStatus.positionTrading;
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
                            await Task.Run(async () => { await BinanceFetchTrades.FetchTradesForSymbol(symbol); }); // wachten tot deze klaar is
                        }

                        s = string.Format("handletrade#4 {0} geen step gevonden. Statistiek bijwerken (exit)", msgInfo);
                        GlobalData.AddTextToLogTab(s);

                        // Wellicht optioneel?
                        if ((data.Status == OrderStatus.Filled) || (data.Status == OrderStatus.PartiallyFilled))
                            GlobalData.AddTextToTelegram(msgInfo);

                        return;
                    }
                }






                s = string.Format("handletrade#5 {0} step gevonden, name={1} id={2} positie.status={3}", msgInfo, step.Name, step.Id, position.Status);
                GlobalData.AddTextToLogTab(s);


                // Er is een trade gemaakt die belangrijk voor deze posities is
                // Synchroniseer de trades voor de break-even point en winst.
                // Laad tevens de steps als deze nog niet geladen zijn.
                await TradeTools.RefreshTrades(databaseThread, position);
                {
                    if ((data.Status == OrderStatus.Filled) && (!symbol.TradeList.TryGetValue(data.TradeId, out CryptoTrade tradeLast)))
                    {
                        // Deze is nog niet bewaard in de database, maar dat geeft niet
                        tradeLast = new CryptoTrade();
                        Helper.PickupTrade(symbol, tradeLast, data);
                        symbol.TradeList.Add(tradeLast.TradeId, tradeLast);

                        s = string.Format("handletrade#6 {0} missende trade, toegevoegd!", msgInfo);
                        GlobalData.AddTextToLogTab(s);
                    }
                }
                // Herberekenen
                TradeTools.CheckPosition(databaseThread, position);


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




                // Even het aantal vermelden (dat was een goed hulpje bij het uitzoeken)
                 if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
                {
                    if (exchange.AssetList.TryGetValue(position.Symbol.Base, out CryptoAsset asset))
                    {
                        GlobalData.AddTextToLogTab(string.Format("Available asset {0} {1} Free={2}", asset.Quote, asset.Total.ToString0(), asset.Free.ToString0()));
                    }
                }


                // Een order kan geannuleerd worden door de gebruiker, en dan gaan we ervan uit dat de gebruiker de order overneemt.
                // Hierop reageren door de positie te sluiten (de statistiek wordt gereset zodat het op conto van de gebruiker komt)
                if (data.Status == OrderStatus.Canceled)
                {
                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    //if ((position.Status == CryptoPositionStatus.positionPending) || (position.Status == CryptoPositionStatus.positionWaiting))
                    //{
                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    if ((position.Status == CryptoPositionStatus.positionTakeOver) || (position.Status == CryptoPositionStatus.positionTimeout) || (step.Status == OrderStatus.Expired))
                    {
                        // Wij! Anders was de status niet op expired gezet of de positie op timeout gezet
                        // Eventueel kunnen we deze open laten staan als de step.QuantityFilled niet 0 is?
                        //if (!step.CloseTime.HasValue)
                        //  step.CloseTime = data.EventTime;
                    }
                    else
                    {

                        // De gebruiker heeft de order geannuleerd, het is nu de verantwoordelijkheid van de gebruiker om het recht te trekken
                        position.Profit = 0;
                        position.Invested = 0;
                        position.Percentage = 0;
                        position.CloseTime = data.EventTime;
                        CryptoPositionStatus? oldStatus = position.Status;
                        position.Status = CryptoPositionStatus.positionTakeOver;
                        GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                        s = string.Format("handletrade#7 {0} positie cancelled, user takeover? positie.status={1}", msgInfo, position.Status);
                        GlobalData.AddTextToLogTab(s);
                        GlobalData.AddTextToTelegram(s);
                    }


                    HandleAdministration(databaseThread, symbol, position);
                    return;
                }



                // De sell order is uitgevoerd, de positie afmelden
                if (!step.IsBuy && (data.Status == OrderStatus.Filled))
                {
                    // We zijn uit de trade, alles verkocht
                    s = string.Format("handletrade#8 {0} positie sold", msgInfo);
                    GlobalData.AddTextToLogTab(s);
                    GlobalData.AddTextToTelegram(s);

                    position.CloseTime = data.EventTime;
                    CryptoPositionStatus? oldStatus = position.Status;
                    position.Status = CryptoPositionStatus.positionReady;
                    GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                    // Annuleer openstaande dca orders
                    foreach (CryptoPositionStep stepX in position.Steps.Values)
                    {
                        // ??? Waarom niet alle orders annuleren?
                        // Annuleer alle openstaande orders (DCA1..3)
                        //if (stepX.IsBuy && (stepX.Status == OrderStatus.New))
                        if (stepX.IsBuy && (stepX.Status == OrderStatus.New))
                        {
                            stepX.CloseTime = data.EventTime;
                            stepX.Status = OrderStatus.Expired;
                            databaseThread.Connection.Update<CryptoPositionStep>(stepX);

                            await Cancel(symbol, position, stepX.OrderId);
                            //break;
                        }
                    }

                    HandleAdministration(databaseThread, symbol, position);
                    return;
                }



                //De buy order is gevuld -> Sell order instellen, DCA plaatsen, Stop loss plaatsen
                if (step.IsBuy && (data.Status == OrderStatus.Filled))
                {
                    s = string.Format("{0} POSITION {1} ORDER FILLED {2} {3} {4}", symbol.Name, step.Name,
                        step.Price.ToString0(), step.Quantity.ToString0(), step.QuoteQuantityFilled.ToString0());
                    GlobalData.AddTextToLogTab(s);
                    GlobalData.AddTextToTelegram(s);

                    // We zitten in de trade want er is iets gekocht
                    CryptoPositionStatus? oldStatus = position.Status;
                    position.Status = CryptoPositionStatus.positionTrading;
                    GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                    // nieuwe methode met de "step"
                    // Annuleer de (eventuele) openstaande sell order, deze is bij de 1e buy niet aanwezig (dat wordt door de HasValue afgevangen)
                    // (de tweede OCO sell order gaat automatisch mee als je deze annuleert.)
                    foreach (CryptoPositionStep stepX in position.Steps.Values)
                    {
                        if (!stepX.IsBuy && (stepX.Status == OrderStatus.New))
                        {
                            stepX.CloseTime = data.UpdateTime;
                            stepX.Status = OrderStatus.Expired;
                            databaseThread.Connection.Update<CryptoPositionStep>(stepX);

                            await Cancel(symbol, position, stepX.OrderId);
                            //break;
                        }
                    }


                    // Bereken de break-even prijs (op deze positie)
                    decimal breakEven = position.BreakEvenPriceViaTrades();

                    // De sell price 0.7% hoger dan de buyPrice (TODO: rekening houden met fee's)
                    decimal sellPrice = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100)); ;
                    sellPrice = sellPrice.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);

                    // Stop prijs 4% lager
                    decimal sellStop = breakEven - (breakEven * (Math.Abs(GlobalData.Settings.Trading.GlobalStopPercentage) / 100));
                    sellStop = sellStop.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);

                    // Limit prijs 5% lager
                    decimal sellLimit = breakEven - (breakEven * (Math.Abs(GlobalData.Settings.Trading.GlobalStopLimitPercentage) / 100));
                    sellLimit = sellLimit.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);

                    // Wat hebben we gekocht (tot dusver)
                    decimal SellQuantity = 0;
                    foreach (CryptoPositionStep stepX in position.Steps.Values)
                    {
                        if (stepX.IsBuy && (stepX.Status <= OrderStatus.Filled))
                            SellQuantity += stepX.QuantityFilled;
                    }
                    SellQuantity = SellQuantity.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);


                    // Afhankelijk van de invoer stop of stoplimit een OCO of standaard sell plaatsen.
                    (bool result, TradeParams tradeParams) buyResult;
                    if ((GlobalData.Settings.Trading.GlobalStopPercentage == 0) || (GlobalData.Settings.Trading.GlobalStopLimitPercentage == 0))
                        buyResult = await Sell(symbol, SellQuantity, sellPrice);
                    else
                        buyResult = await SellOco(symbol, SellQuantity, sellPrice, sellStop, sellLimit);

                    // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
                    if (buyResult.result)
                    {
                        //string text = string.Format("{0} POSITION SELL ORDER PLACED {1} {2} {3}", data.Symbol, SellPrice, SellQuantity, SellPrice * SellQuantity);
                        //GlobalData.AddTextToLogTab(text, true);
                        //GlobalData.AddTextToTelegram(text);


                        // Administratie van de nieuwe sell bewaren
                        position.SellPrice = sellPrice;

                        // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
                        CreatePositionStep(databaseThread, position, buyResult.tradeParams, "SELL");
                    }

                    HandleAdministration(databaseThread, symbol, position);
                    return;
                }
            }
            finally
            {
                symbol.Exchange.PositionListSemaphore.Release();
            }
        }
    }


    /// <summary>
    /// Controleer de openstaande posities van deze symbol
    /// </summary>
    /// <param name="symbol"></param>
    public async Task CheckOpenPositions(CryptoSymbol symbol)
    {
        using (CryptoDatabase databaseThread = new())
        {
            databaseThread.Open();

            await symbol.Exchange.PositionListSemaphore.WaitAsync();
            try
            {
                // Er kunnen meerdere posities bij deze munt open staan
                if (symbol.Exchange.PositionList.TryGetValue(symbol.Name, out var positionList))
                {

                    for (int i = positionList.Values.Count - 1; i >= 0; i--)
                    {
                        //GlobalData.AddTextToBarometerTab("Monitor position " + symbol.Name + " (debug)");

                        CryptoPosition position = positionList.Values[i];

                        // DONE: Niet bijkopen als de barometer te laag staat
                        // DONE: Niet bijkopen als de bollingerbands % te hoog is
                        // DONE: Buy order(s) weghalen als de barometer te laag staat
                        // TODO: Posities verkopen als de barometer lager dan -2.5 komt te staan (market order)

                        // Controleren van order(s)
                        // GET /api/v3/order (HMAC SHA256)        = Check an order's status.
                        // GET /api/v3/openOrders  (HMAC SHA256)  = Current open orders (USER_DATA)
                        // GET /api/v3/allOrders (HMAC SHA256)    = All orders (USER_DATA)

                        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(position.Interval.IntervalPeriod);
                        SortedList<long, CryptoCandle> intervalCandles = symbolPeriod.CandleList;
                        CryptoCandle candleLast = intervalCandles.Values.Last();


                        // Vraag: Is er reeds gekocht? 
                        // Nee, dan na zoveel minuten annuleren (mits bb%, barometer enzovoort)
                        // Ja: Dan na zoveel minuten bijkopen (mits bb%, barometer enzovoort)

                        if (position.Status == CryptoPositionStatus.positionWaiting)
                        {
                            bool removePosition = false;

                            // De order is ouder is dan 20 minuten dan deze verwijderen

                            // Even een quick fix voor de barometer
                            CryptoQuoteData quoteData;
                            decimal? Barometer1h = -99m;
                            if (GlobalData.Settings.QuoteCoins.TryGetValue(symbol.Quote, out quoteData))
                            {
                                BarometerData barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
                                Barometer1h = barometerData.PriceBarometer;
                            }

                            if (!removePosition && Barometer1h.HasValue && (Barometer1h <= GlobalData.Settings.Trading.Barometer01hBotMinimal))
                            {
                                removePosition = true;
                                GlobalData.AddTextToLogTab(string.Format("Monitor position (barometer negatief) REMOVE start", symbol.Name, Barometer1h, GlobalData.Settings.Trading.Barometer01hBotMinimal));
                            }

                            if (!removePosition && position.CreateTime.AddSeconds(GlobalData.Settings.Trading.GlobalBuyRemoveTime * symbolPeriod.Interval.Duration) < DateTime.UtcNow)
                            {
                                removePosition = true;
                                GlobalData.AddTextToLogTab("Monitor position (buy expired) REMOVE " + symbol.Name + " start");
                            }

                            //// De prijs is ondertussen hoger dan de middelste bollinger band (ma20)
                            //if (!removePosition && symbol.LastPrice.HasValue && (candleLast.CandleData != null) && (candleLast.CandleData.BollingerBands.LowerBand.HasValue) && ((decimal)symbol.LastPrice >= (decimal)candleLast.CandleData.BollingerBands.Sma))
                            //{
                            //    removePosition = true;
                            //    GlobalData.AddTextToLogTab("Monitor position (price > middle bb) REMOVE " + symbol.Name + " start");
                            //}

                            //// De prijs is ondertussen 0.75% hoger (geen winst meer?)
                            //if ((!removePosition && symbol.LastPrice.HasValue && (decimal)symbol.LastPrice - position.BuyPrice > position.BuyPrice * 0.75m / 100m))
                            //{
                            //    removePosition = true;
                            //    GlobalData.AddTextToLogTab("Monitor position (price > 0.75% startprice) REMOVE " + symbol.Name + " start");
                            //}


                            // TODO: Een extra controle inbouwen want wellicht is de order toch gedeeltelijk of (net) geheel gevuld
                            // Indien dat het geval is alsnog een sell order zetten (+quantity laten doorrekenen enzovoort)

                            if (removePosition)
                            {
                                // Statistieken resetten zodat we makkelijk kunnen tellen & presenteren
                                position.Profit = 0;
                                position.Invested = 0;
                                position.Percentage = 0;
                                position.CloseTime = DateTime.UtcNow;
                                CryptoPositionStatus? oldStatus = position.Status;
                                position.Status = CryptoPositionStatus.positionTimeout;
                                databaseThread.Connection.Update<CryptoPosition>(position);
                                GlobalData.AddTextToLogTab(String.Format("Debug: positie status van {0} naar {1}", oldStatus.ToString(), position.Status.ToString()));

                                // Annuleer de openstaande buy order
                                foreach (CryptoPositionStep step in position.Steps.Values)
                                {
                                    if (step.IsBuy && step.Status == OrderStatus.New)
                                    {
                                        string text = string.Format("{0} POSITION BUY ORDER removed {1} {2} {3}", symbol.Name,
                                            step.Price.ToString0(), step.Quantity.ToString0(), (step.Price * step.Quantity).ToString0());
                                        GlobalData.AddTextToLogTab(text);
                                        GlobalData.AddTextToTelegram(text);

                                        // beetje vreemd dat de status al gezet wordt terwijl de cancel kan mislukken
                                        //step.Status = OrderStatus.Expired;
                                        //step.CloseTime = position.CloseTime;
                                        //databaseThread.Update<CryptoPositionStep>(step);

                                        WebCallResult<BinanceOrderBase> result = await Cancel(symbol, position, step.OrderId);
                                        if (result == null)
                                        {
                                            // Geen geldige order? De cancel kan er niets mee
                                            step.Status = OrderStatus.Expired;
                                            step.CloseTime = position.CloseTime;
                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
                                        }
                                        else if (result.Success)
                                        {
                                            // Gelukt!
                                            step.Status = OrderStatus.Expired;
                                            step.CloseTime = position.CloseTime;
                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
                                        }
                                        else if (!result.Success)
                                        {
                                            // tsja, dat weet ik niet
                                        }
                                    }
                                }

                            }

                        }
                        else if (position.Status == CryptoPositionStatus.positionTrading)
                        {
                            // We zitten dus in een trade (volledig, maar de niet volledige zijn nog wel een probleem!)
                            // We willen niet direct bijkopen maar met een vertraging bijkopen (voorkomen dat alle dca's in 1x worden gekocht).
                            // Dit voorkomt dat we bij een plotselinge koersdaling alle DCA's in 1m wordt uitgevoerd en dan ook nog eens een 
                            // stopLimit raken (we raken dus een "kleiner" gedeelte van onze investering kwijt).

                            // Even een quick fix voor de barometer
                            // Geobserveerd dat ondanks dat de barometer heel erg hoog staat er een market order wordt aanbevolen!
                            // Onderstaand werkt dus niet goed, of de GlobalData.Settings.QuoteCoins heeft plotseling geen BNB en BTC meer? 
                            // Dat zou wel erg raar zijn, melding ingebouwd!

                            CryptoQuoteData quoteData;
                            decimal? Barometer1h = -99m;
                            if (GlobalData.Settings.QuoteCoins.TryGetValue(symbol.Quote, out quoteData))
                            {
                                BarometerData barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
                                Barometer1h = barometerData.PriceBarometer;
                            }
                            else GlobalData.AddTextToLogTab(string.Format("De barometer is niet aanwezig voor {0}", symbol.Quote));

                            // Annuleer openstaande DCA orders als de barometer TE negatief is

                            // TODO: Willen we dit wel? (de backtest laat zien dat het niet nodig is)
                            if ((Barometer1h.HasValue) && (Barometer1h <= -1.5m)) // GlobalData.Settings.Bot.Barometer01hBotMinimal
                            {
                                // TODO: Posities verkopen als de barometer lager dan -2.5 komt te staan (market order)

                                // Je wordt echt knettergek van deze meldingen (terwijl het al spannend genoeg is met zo'n barometer <g>)
                                //if (Barometer1h <= -2.5m)
                                //{
                                //    string text = string.Format("{0} SUGGESTIE om het met een market order te verkopen! (test)", symbol.Name);
                                //    GlobalData.AddTextToLogTab(text);
                                //    GlobalData.AddTextToTelegram(text);
                                //}

                                // TODO : uitzoeken wat we met de OCO order willen doen 
                                // want met zo'n lage barometer raak je die stoploss wel

                                // Annuleer de DCAx buy orders
                                foreach (CryptoPositionStep step in position.Steps.Values)
                                {
                                    if (step.IsBuy && step.Status == OrderStatus.New)
                                    {
                                        string text = string.Format("{0} CANCEL {1} order, de barometer is te laag", symbol.Name, step.Name);
                                        GlobalData.AddTextToLogTab(text);
                                        GlobalData.AddTextToTelegram(text);

                                        // beetje vreemd dat de status al gezet wordt terwijl de cancel kan mislukken
                                        //step.CloseTime = DateTime.UtcNow;
                                        //step.Status = OrderStatus.Expired;
                                        //databaseThread.Update<CryptoPositionStep>(step);

                                        WebCallResult<BinanceOrderBase> result = await Cancel(symbol, position, step.OrderId);
                                        if (result == null)
                                        {
                                            // Geen geldige order? De cancel kan er niets mee
                                            step.Status = OrderStatus.Expired;
                                            step.CloseTime = DateTime.UtcNow;
                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
                                        }
                                        else if (result.Success)
                                        {
                                            // Gelukt!
                                            step.Status = OrderStatus.Expired;
                                            step.CloseTime = DateTime.UtcNow;
                                            databaseThread.Connection.Update<CryptoPositionStep>(step);
                                        }
                                        else if (!result.Success)
                                        {
                                            // tsja, dat weet ik niet
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Bij een positievere barometer de DCA order alsnog plaatsen

                                // Is er een DCA order aanwezig?
                                int sequence = 0;
                                decimal lastPrice = decimal.MaxValue;
                                DateTime lastDate = DateTime.MinValue;
                                CryptoPositionStep buyStep = null;
                                foreach (CryptoPositionStep step in position.Steps.Values)
                                {
                                    if (step.IsBuy && (step.Status <= OrderStatus.Filled))
                                    {
                                        // Onthoud de laatste prijs en datum dat een buy order gevuld is
                                        if (step.Status == OrderStatus.Filled)
                                        {
                                            // na Buy=1, na Dca1=2, na Dca2=3, Dca2=4, enzovoort
                                            sequence++;

                                            if (step.Price < lastPrice)
                                                lastPrice = step.Price;

                                            if ((step.CloseTime.HasValue) && ((DateTime)step.CloseTime > lastDate))
                                                lastDate = (DateTime)step.CloseTime;
                                        }

                                        // Er is nog een openstaand buy opdracht (laat maar)
                                        if (step.Status == OrderStatus.New)
                                        {
                                            buyStep = step;
                                            break;
                                        }
                                    }

                                }


                                // Indien niet geplaatst dan de DCA order alsnog plaatsen (niet meer dan X DCA's plaatsen)
                                // (en stiekum wachten we tot het weer rustig is in de markt door de BB with te controleren)
                                if ((buyStep == null) && (sequence <= GlobalData.Settings.Trading.DcaCount))
                                {
                                    if ((candleLast.CandleData != null) && (candleLast.CandleData.BollingerBandsPercentage.HasValue) && ((decimal)candleLast.CandleData.BollingerBandsPercentage <= 5.0m))
                                    {
                                        if (lastDate == DateTime.MaxValue)
                                            lastDate = DateTime.UtcNow;

                                        DateTime date = lastDate;
                                        // Was eerst met een AddSeconds en * interval.Duration, maar dan duurt het echt veel te lang!
                                        if (date.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) < DateTime.UtcNow)
                                        {
                                            string name = string.Format("DCA{0}", sequence);
                                            // Plaats de buy 
                                            (bool result, TradeParams tradeParams) result;
                                            result = await PlaceBuyOrder(position, sequence, position.Symbol, lastPrice, name);
                                            if (result.result)
                                            {
                                                CreatePositionStep(databaseThread, position, result.tradeParams, name);
                                            }
                                        }
                                    }
                                }


                            }

                        }
                        else if (position.Status == CryptoPositionStatus.positionTimeout)
                        {
                            string text = string.Format("{0} POSITION timeout HANGING? {1} waarom?", symbol.Name, position.Status);
                            GlobalData.AddTextToLogTab(text);
                            //GlobalData.AddTextToTelegram(text); telegram houdt niet van zoveel teksten

                            // De order is reeds afgesloten (maar overlap met algoritme, dus pas na x minuten weghalen, wellicht afhankelijk van interval?)
                            // Dis is meer een soort van symptoom bestreiding, want waarom wordt deze positie niet weggehaald uit het geheugen?
                            // En wellicht nog meer een reden om alles vanuit de database te beredeneren?
                            if (position.CloseTime.HasValue && (position.CloseTime?.AddMinutes(4) < DateTime.UtcNow))
                            {
                                databaseThread.Connection.Update<CryptoPosition>(position);
                                symbol.ClearSignals();
                                GlobalData.RemovePosition(position);
                                GlobalData.AddTextToLogTab(String.Format("Debug: positie removed {0}", position.Status.ToString()));
                                GlobalData.AddTextToLogTab("Monitor position (hanging timeout?) REMOVE " + symbol.Name + " start");
                            }
                        }
                        else if (position.Status == CryptoPositionStatus.positionReady)
                        {
                            string text = string.Format("{0} POSITION ready HANGING? {1} waarom?", symbol.Name, position.Status);
                            GlobalData.AddTextToLogTab(text);
                            GlobalData.AddTextToTelegram(text);

                            // De order is reeds afgesloten (maar overlap met algoritme, dus pas na x minuten weghalen, wellicht afhankelijk van interval?)
                            // Dis is meer een soort van symptoom bestreiding, want waarom wordt deze positie niet weggehaald uit het geheugen?
                            // En wellicht nog meer een reden om alles vanuit de database te beredeneren?
                            if (position.CloseTime.HasValue && (position.CloseTime?.AddMinutes(4) < DateTime.UtcNow))
                            {
                                databaseThread.Connection.Update<CryptoPosition>(position);
                                symbol.ClearSignals();
                                GlobalData.RemovePosition(position);
                                GlobalData.AddTextToLogTab(String.Format("Debug: positie removed {0}", position.Status.ToString()));
                                GlobalData.AddTextToLogTab("Monitor position (hanging ready?) REMOVE " + symbol.Name + " start");
                            }
                        }

                    }
                }
            }
            finally
            {
                symbol.Exchange.PositionListSemaphore.Release();
            }
        }
    }
}


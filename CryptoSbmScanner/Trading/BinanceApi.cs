using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Trading;

public class BinanceApi
{
    private DateTime CurrentDate { get; set; }
    private CryptoTradeAccount TradeAccount { get; set; }
    private CryptoSymbol Symbol { get; set; }
    private Model.CryptoExchange Exchange { get; set; }
    private CryptoSymbolInterval SymbolInterval { get; set; }


    public BinanceApi(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, DateTime currentDate)
    {
        TradeAccount = tradeAccount;

        Exchange = symbol.Exchange;
        Symbol = symbol;
        SymbolInterval = symbolInterval;
        CurrentDate = currentDate;
    }


    static public async Task<(bool result, TradeParams tradeParams)> PlaceBuyOrder(CryptoTradeAccount tradeAccount, CryptoPosition position, int sequence, 
        CryptoSymbol symbol, decimal lastBuyPrice, string dcaType, DateTime createTime, bool marketOrder = false)
    {
        decimal price = lastBuyPrice;

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
        decimal quantity;
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
        if (!symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, text, oldDebugPrice, oldDebugQuantity));
            return (false, null);
        }


        // Plaats een buy order op Binance
        //TradeParams tradeParams;
        //WebCallResult<BinancePlacedOrder> result = Buy(symbol, quantity, price, out tradeParams);
        (bool result, TradeParams tradeParams) result;
        if (marketOrder)
            result = await Buy(tradeAccount, symbol, quantity, null);
        else
            result = await Buy(tradeAccount, symbol, quantity, price);
        if (result.result)
        {
            string text2 = string.Format("{0} POSITION {1} ORDER {2} PLACED price={3} quantity={4} quotequantity={5} type={6}", symbol.Name, dcaType,
                result.tradeParams.OrderId, result.tradeParams.Price.ToString0(), result.tradeParams.Quantity.ToString0(), 
                result.tradeParams.QuoteQuantity.ToString0(), result.tradeParams.OrderType.ToString());
            GlobalData.AddTextToLogTab(text2, true);
            GlobalData.AddTextToTelegram(text2);

            return result;
        }
        else return (false, null);
    }


    static public async Task<(bool result, TradeParams tradeParams)> Buy(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, decimal? quantity, decimal? price)
    {
        if (tradeAccount.AccountType != CryptoTradeAccountType.RealTrading)
        {
            TradeParams tradeParams = new();
            tradeParams.Side = CryptoTradeDirection.Long;
            tradeParams.OrderId = 0; // result.Data.Id;
            if (price == null)
            {
                tradeParams.Price = (decimal)symbol.LastPrice;
                tradeParams.OrderType = CryptoOrderType.Market;
            }
            else
            {
                tradeParams.Price = (decimal)price;
                tradeParams.OrderType = CryptoOrderType.Limit;
            }
            tradeParams.Quantity = (decimal)quantity;
            tradeParams.CreateTime = DateTime.UtcNow;
            tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
            return (true, tradeParams);
        }

        // Plaats een buy order op Binance
        BinanceWeights.WaitForFairBinanceWeight(1);
        using (BinanceClient client = new())
        {
            WebCallResult<BinancePlacedOrder> result = null;
            if (price.HasValue)
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Buy, 
                    SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
            else
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Buy, 
                    price.HasValue ? SpotOrderType.Limit : SpotOrderType.Market, quantity);
            if (!result.Success)
            {
                string text = string.Format("{0} ERROR buy order {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
                text += string.Format("quantity={0}\r\n", quantity?.ToString0());
                text += string.Format("sellprice={0}\r\n", price?.ToString0());
                //text += string.Format("sellstop={0}\r\n", stop.ToString0());
                //text += string.Format("selllimit={0}\r\n", limit.ToString0());
                text += string.Format("lastprice={0}\r\n", symbol.LastPrice?.ToString0());
                text += string.Format("trades={0}\r\n", symbol.TradeList.Count);
                GlobalData.AddTextToLogTab(text);
                GlobalData.AddTextToTelegram(text);
            }

            if ((result.Success) && (result.Data != null))
            {
                TradeParams tradeParams = new();
                tradeParams.Side = CryptoTradeDirection.Long;
                tradeParams.OrderId = result.Data.Id;
                //Zou handig zijn als we een SpotOrderType naar CryptoOrderType zouden hebben
                if (price == null)
                    tradeParams.OrderType = CryptoOrderType.Market;
                else
                    tradeParams.OrderType = CryptoOrderType.Limit;
                tradeParams.Price = result.Data.Price;
                tradeParams.Quantity = result.Data.Quantity;
                tradeParams.CreateTime = result.Data.CreateTime;
                tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;

                return (true, tradeParams);
            }
            else return (false, null);
        }
    }


    static public async Task<(bool result, TradeParams tradeParams)> Sell(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, decimal? quantity, decimal? price)
    {
        if (tradeAccount.AccountType != CryptoTradeAccountType.RealTrading)
        {
            TradeParams tradeParams = new();
            tradeParams.Side = CryptoTradeDirection.Short;
            tradeParams.OrderId = 0; // result.Data.Id;
            if (price == null)
            {
                tradeParams.OrderType = CryptoOrderType.Market;
                tradeParams.Price = (decimal)symbol.LastPrice;
            }
            else
            {
                tradeParams.OrderType = CryptoOrderType.Limit;
                tradeParams.Price = (decimal)price;
            }
            tradeParams.Quantity = (decimal)quantity;
            tradeParams.QuoteQuantity = (decimal)quantity* tradeParams.Price;
            tradeParams.CreateTime = DateTime.UtcNow;
            return (true, tradeParams);
        }

        BinanceWeights.WaitForFairBinanceWeight(1);

        // Plaats een sell order op Binance      
        using (BinanceClient client = new())
        {
            WebCallResult<BinancePlacedOrder> result = null;
            if (price.HasValue)
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, 
                    SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
            else
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, 
                    SpotOrderType.Market, quantity);

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
                TradeParams tradeParams = new();
                tradeParams.Side = CryptoTradeDirection.Short;
                tradeParams.OrderId = result.Data.Id;
                tradeParams.Price = result.Data.Price;
                tradeParams.Quantity = result.Data.Quantity;
                tradeParams.CreateTime = result.Data.CreateTime;
                tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                if (price == null)
                    tradeParams.OrderType = CryptoOrderType.Market;
                else
                    tradeParams.OrderType = CryptoOrderType.Limit;
                return (true, tradeParams);
            }
            else return (false, null);
        }
    }


    static public async Task<(bool result, TradeParams tradeParams)> SellOco(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, decimal quantity, decimal price, decimal stop, decimal limit)
    {
        if (tradeAccount.AccountType != CryptoTradeAccountType.RealTrading)
        {
            TradeParams tradeParams = new();
            tradeParams.Side = CryptoTradeDirection.Short;
            tradeParams.OrderId = 0;
            tradeParams.Price = price; // the sell part
            tradeParams.StopPrice = stop; // the price at which the limit order to sell is activated
            tradeParams.LimitPrice = limit; // the lowest price that the trader is willing to accept
            tradeParams.Quantity = quantity;
            // TODO - Dit is niet de juiste datum (krijg je ervan als je het static maakt)
            tradeParams.CreateTime = DateTime.UtcNow;
            tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
            tradeParams.OrderType = CryptoOrderType.Oco;
            return (true, tradeParams);
        }


        BinanceWeights.WaitForFairBinanceWeight(1);

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


        // Vanwege "The relationship of the prices for the orders is not correct." The prices set in the OCO 
        // is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)

        //"The relationship of the prices for the orders is not correct." The prices set in the OCO is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)
        //The rules are:
        //SELL Orders: Limit Price > Last Price > Stop Price
        //BUY Orders: Limit Price<Last Price<Stop Price

        //De prijs is dan ondertussen al onder de StopPrice beland?

        if (symbol.LastPrice > price)
        {
            // deze verkoopt dan zo'n beetje onmiddelijk (denk ik), lastig!!
            return await Sell(tradeAccount, symbol, quantity, price);
        }
        else
        {
            // Plaats een buy order op Binance
            using (BinanceClient client = new())
            {
                WebCallResult<BinanceOrderOcoList> result = await client.SpotApi.Trading.PlaceOcoOrderAsync(symbol.Name, OrderSide.Sell, 
                    quantity, price: price, stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);
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

                    //The relationship of the prices for the orders is not correct." The prices set in the OCO is breaking the Price rules.
                    //The rules are:
                    //SELL Orders: Limit Price > Last Price > Stop Price
                    //BUY Orders: Limit Price < Last Price < Stop Price

                    // https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md
                }

                if ((result.Success) && (result.Data != null))
                {
                    //string text2 = string.Format("{0} POSITION {1} ORDER PLACED {2} {3} {4}", symbol.Name, dcaType, price, quantity, price * quantity);
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

                    TradeParams tradeParams = new()
                    {
                        Side = CryptoTradeDirection.Short,
                        // enzovoort
                    };
                    tradeParams.OrderId = order1.Id;
                    tradeParams.Price = order1.Price;
                    tradeParams.StopPrice = stop; // order2.StopPrice;
                    tradeParams.LimitPrice = limit; // order2.  Hey, waarom is deze er niet?
                    tradeParams.Quantity = order1.Quantity;
                    tradeParams.CreateTime = order1.CreateTime;
                    tradeParams.Order2Id = order2.Id; // Een tweede ordernummer
                    tradeParams.OrderListId = order1.OrderListId; // Linked order
                    tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                    tradeParams.OrderType = CryptoOrderType.Oco;
                    return (true, tradeParams);
                }
                else return (false, null);
            }
        }
    }


    public static async Task<WebCallResult<BinanceOrderBase>> Cancel(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoPosition position, long? orderId)
    {
        if (tradeAccount.AccountType != CryptoTradeAccountType.RealTrading)
        {
            //TradeParams tradeParams = new();
            //tradeParams.IsBuy = false;
            //tradeParams.OrderId = 0; // result.Data.Id;
            //tradeParams.StopPrice = stop; // order2.StopPrice;
            //tradeParams.LimitPrice = limit; // order2.  Hey, waarom is deze er niet?
            //if (price == null)
            //    tradeParams.Price = (decimal)symbol.LastPrice;
            //else
            //    tradeParams.Price = (decimal)price;
            //tradeParams.Quantity = (decimal)quantity;
            //tradeParams.CreateTime = DateTime.UtcNow;
            //tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
            //return (true, tradeParams);
            return null; // what?
        }

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


    //public static async Task<(bool result, string reaction)> DoOnSignal(CryptoDatabase databaseThread, CryptoTradeAccount tradeAccount, CryptoPosition position, CryptoPositionPart part, DateTime createTime, decimal price)
    //{
    //    // TODO - opheffen en code in PositionMonitor

    //    if (part.Mode == CryptoTradeDirection.Long)
    //    {
    //        //string reaction;

    //        //{
    //        //    // Was reeds gecontroleerd bij positie nemen/uitbreiden
    //        //    decimal assetQuantity;
    //        //    if (tradeAccount.AccountType == CryptoTradeAccountType.RealTrading)
    //        //    {
    //        //        // Is er een API key aanwezig (met de juiste opties)
    //        //        if (!SymbolTools.CheckValidApikey(out reaction))
    //        //            return (false, reaction);

    //        //        // Hoeveel muntjes hebben we?
    //        //        var resultPortFolio = SymbolTools.CheckPortFolio(tradeAccount, part.Symbol);
    //        //        if (!resultPortFolio.result)
    //        //            return (false, reaction);
    //        //        assetQuantity = resultPortFolio.value;
    //        //    }
    //        //    else
    //        //        assetQuantity = 100000m; // genoeg.. (todo? assets voor papertrading?)

    //        //    // Is er "geld" om de order te kunnen plaatsen?
    //        //    // De Quantity is in Quote bedrag (bij BTCUSDT de USDT dollar)
    //        //    if (!SymbolTools.CheckValidAmount(part.Symbol, assetQuantity, out decimal _, out reaction))
    //        //        return (false, reaction);
    //        //}

    //        // Plaats buy order
    //        // Dit triggert een notificatie die technisch gezien eerder kan arriveren dan dat wij 
    //        // de positie toevoegen, daarom locken we hier de posities voor het plaatsen van de buy.
    //        (bool result, TradeParams tradeParams) result = await PlaceBuyOrder(tradeAccount, null, 0, part.Symbol,
    //            price, "BUY", createTime, GlobalData.Settings.Trading.BuyOrderMethod == CryptoBuyOrderMethod.MarketOrder);
    //        if (result.result)
    //        {
    //            var step = PositionTools.CreatePositionStep(position, part, result.tradeParams, "BUY");
    //            PositionTools.InsertPositionStep(databaseThread, position, step);
    //            PositionTools.AddPositionPartStep(part, step);
    //        }
    //        return (true, "");
    //    }

    //    return (false, "");
    //}
}

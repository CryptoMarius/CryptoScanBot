using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Binance;




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


//The relationship of the prices for the orders is not correct."	The prices set in the OCO is breaking the Price rules.
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price < Last Price < Stop Price
// https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md


public class BinanceApi
{
    private DateTime CurrentDate { get; set; }
    private CryptoTradeAccount TradeAccount { get; set; }
    private CryptoSymbol Symbol { get; set; }
    private Model.CryptoExchange Exchange { get; set; }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(SpotOrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            SpotOrderType.Market => CryptoOrderType.Market,
            SpotOrderType.Limit => CryptoOrderType.Limit,
            SpotOrderType.StopLoss => CryptoOrderType.StopLimit,
            SpotOrderType.StopLossLimit => CryptoOrderType.Oco,
            _ => throw new Exception("Niet ondersteunde ordertype"),
        };

        return localOrderType;
    }

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Niet ondersteunde orderside"),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }

    public static void SetExchangeDefaults()
    {
        BinanceClient.SetDefaultOptions(new BinanceClientOptions() { });
        BinanceSocketClientOptions options = new();
        options.SpotStreamsOptions.AutoReconnect = true;
        options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
        BinanceSocketClient.SetDefaultOptions(options);
    }

    public BinanceApi(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate)
    {
        TradeAccount = tradeAccount;

        Exchange = symbol.Exchange;
        Symbol = symbol;
        CurrentDate = currentDate;
    }


    //// TODO: Een StopLimit order ondersteunen!
    ///// <summary>
    ///// Bereken de te kopen aantal muntjes tegen de opgegeven prijs en plaats dan een buy opdracht
    ///// Optionele prijs (default = symbol.LastPrice)
    ///// Optionele Quantity (default = Symbol.QuoteData.BuyAmount / price)
    ///// </summary>
    //public async Task<(bool result, TradeParams tradeParams)> PlaceBuyOrder(CryptoOrderType orderType, decimal? quantity, decimal? price)
    //{
    //    // Zelfs met een MarketOrder moet je een prijs hebben omdat we een bepaalde quantity willen kopen (c.q. willen opgeven)


    //    // prijs perikelen
    //    if (!price.HasValue)
    //        price = (decimal)Symbol.LastPrice;

    //    if (Symbol.LastPrice.HasValue && Symbol.LastPrice < price)
    //    {
    //        decimal oldPrice = (decimal)price;
    //        price = (decimal)Symbol.LastPrice;
    //        GlobalData.AddTextToLogTab("BUY correction: " + Symbol.Name + " " + oldPrice.ToString("N6") + " to " + price.ToString0());
    //    }
    //    // De aankoop prijs verlagen (niet direct laten kopen?)
    //    if (GlobalData.Settings.Trading.GlobalBuyVarying != 0.0m)
    //    {
    //        decimal oldPrice = (decimal)price;
    //        price += price * (GlobalData.Settings.Trading.GlobalBuyVarying / 100);
    //        GlobalData.AddTextToLogTab("BUY percentage: " + Symbol.Name + " " + oldPrice.ToString("N6") + " to " + price.ToString0());
    //    }
    //    price = price?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);



    //    // quantity perikelen
    //    if (!quantity.HasValue)
    //        quantity = Symbol.QuoteData.BuyAmount / price;
    //    quantity = quantity?.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);



    //    // Controleer de limiten van de berekende bedragen, Minimum bedrag
    //    if (!Symbol.InsideBoundaries(quantity, price, out string text))
    //    {
    //        GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", Symbol.Name, text, price, quantity));
    //        return (false, null);
    //    }

    //    // Plaats de buy order
    //    (bool result, TradeParams tradeParams) result = await BuyOrSell(orderType, CryptoOrderSide.Long, (decimal)quantity, (decimal)price, null, null);
    //    if (result.result)
    //    {
    //        //string text2 = string.Format("{0} POSITION {1} ORDER {2} PLACED price={3} quantity={4} quotequantity={5} type={6}", Symbol.Name, "BUY",
    //        //    result.tradeParams.OrderId, result.tradeParams.Price.ToString0(), result.tradeParams.Quantity.ToString0(), 
    //        //    result.tradeParams.QuoteQuantity.ToString0(), result.tradeParams.OrderType.ToString());
    //        //GlobalData.AddTextToLogTab(text2, true);
    //        //GlobalData.AddTextToTelegram(text2);
    //        return result;
    //    }
    //    else return (false, null);
    //}


    public async Task<(bool result, TradeParams tradeParams)> BuyOrSell(CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit)
    {
        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", Symbol.Name, text, price, quantity));
            return (false, null);
        }

        if (TradeAccount.AccountType != CryptoTradeAccountType.RealTrading)
        {
            TradeParams tradeParams = new();
            tradeParams.CreateTime = CurrentDate;
            tradeParams.Side = orderSide;
            tradeParams.OrderType = orderType;
            tradeParams.Price = price; // the sell part (can also be a buy)
            tradeParams.StopPrice = stop; // OCO - the price at which the limit order to sell is activated
            tradeParams.LimitPrice = limit; // OCO - the lowest price that the trader is willing to accept
            tradeParams.Quantity = quantity;
            tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
            tradeParams.OrderId = 0;

            // todo, deze tekst ook verderop plaatsen!
            string text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
                Symbol.Name, orderSide, 
                tradeParams.OrderType.ToString(),
                tradeParams.OrderId, 
                tradeParams.Price.ToString0(),
                tradeParams.StopPrice?.ToString0(),
                tradeParams.Quantity.ToString0(),
                tradeParams.QuoteQuantity.ToString0());
            GlobalData.AddTextToLogTab(text2);
            GlobalData.AddTextToTelegram(text2);

            return (true, tradeParams);
        }

        // Plaats een order op Binance
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
        using (BinanceClient client = new())
        {
            // Een OCO is afwijkend ten opzichte van een standaard buy or sell
            if (orderType == CryptoOrderType.Oco)
            {
                WebCallResult<BinanceOrderOcoList> result;
                if (orderSide == CryptoOrderSide.Buy)
                {
                    result = await client.SpotApi.Trading.PlaceOcoOrderAsync(Symbol.Name, OrderSide.Buy,
                        quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);
                }
                else
                {
                    result = await client.SpotApi.Trading.PlaceOcoOrderAsync(Symbol.Name, OrderSide.Sell,
                        quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);
                }

                if (!result.Success)
                {
                    text = string.Format("{0} ERROR {1} {2} order {3} {4}\r\n", Symbol.Name, orderType, orderSide, result.ResponseStatusCode, result.Error);
                    text += string.Format("quantity={0}\r\n", quantity.ToString0());
                    text += string.Format("sellprice={0}\r\n", price.ToString0());
                    text += string.Format("sellstop={0}\r\n", stop?.ToString0());
                    text += string.Format("selllimit={0}\r\n", limit?.ToString0());
                    text += string.Format("lastprice={0}\r\n", Symbol.LastPrice?.ToString0());
                    text += string.Format("trades={0}\r\n", Symbol.TradeList.Count);
                    GlobalData.AddTextToLogTab(text);
                    GlobalData.AddTextToTelegram(text);
                }


                if (result.Success && result.Data != null)
                {
                    //string text2 = string.Format("{0} POSITION {1} ORDER PLACED {2} {3} {4}", symbol.Name, dcaType, price, quantity, price * quantity);
                    //GlobalData.AddTextToLogTab(text2, true);
                    //GlobalData.AddTextToTelegram(text2);

                    // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
                    // TODO: Controleren van de order id's (want ik heb ze in de administratie omgedraaid denk ik)
                    // niet dat dat zo heel veel uitmaakt, maar de ID's vielen bij het traceren op.

                    // De eerste order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
                    // De tweede order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
                    // het voorbeeld in de API is een buy (dus het kan ook andersom zijn)
                    // Een van de twee heeft ook een price/stopprice, de andere enkel een price
                    BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
                    BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();

                    TradeParams tradeParams = new();
                    tradeParams.CreateTime = order1.CreateTime;
                    tradeParams.Side = orderSide;
                    tradeParams.OrderType = orderType;
                    tradeParams.Price = order1.Price;
                    tradeParams.StopPrice = stop;
                    tradeParams.LimitPrice = limit;
                    tradeParams.Quantity = order1.Quantity;
                    tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                    tradeParams.OrderId = order1.Id;
                    tradeParams.Order2Id = order2.Id; // Een 2e ordernummer!
                    //tradeParams.OrderListId = order1.OrderListId; // Linked order
                    return (true, tradeParams);
                }
                else return (false, null);
            }
            else
            {
                WebCallResult<BinancePlacedOrder> result;
                if (orderSide == CryptoOrderSide.Buy)
                {
                    if (orderType == CryptoOrderType.Market)
                        result = await client.SpotApi.Trading.PlaceOrderAsync(Symbol.Name, OrderSide.Buy, SpotOrderType.Market, quantity);
                    // Ehhh? nog eens oefenen op het testnet denk ik...
                    //else
                    //if (orderType == CryptoOrderType.StopLimit)
                    //  result = await client.SpotApi.Trading.PlaceOrderAsync(Symbol.Name, OrderSide.Buy, SpotOrderType., quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    else
                        result = await client.SpotApi.Trading.PlaceOrderAsync(Symbol.Name, OrderSide.Buy, SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                }
                else
                {
                    if (orderType == CryptoOrderType.Market)
                        result = await client.SpotApi.Trading.PlaceOrderAsync(Symbol.Name, OrderSide.Sell, SpotOrderType.Market, quantity);
                    else
                        result = await client.SpotApi.Trading.PlaceOrderAsync(Symbol.Name, OrderSide.Sell, SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                }

                if (!result.Success)
                {
                    text = string.Format("{0} ERROR {1} {2} order {3} {4}\r\n", Symbol.Name, orderType, orderSide, result.ResponseStatusCode, result.Error);
                    text += string.Format("quantity={0}\r\n", quantity.ToString0());
                    text += string.Format("sellprice={0}\r\n", price.ToString0());
                    text += string.Format("sellstop={0}\r\n", stop?.ToString0());
                    text += string.Format("selllimit={0}\r\n", limit?.ToString0());
                    text += string.Format("lastprice={0}\r\n", Symbol.LastPrice?.ToString0());
                    text += string.Format("trades={0}\r\n", Symbol.TradeList.Count);
                    GlobalData.AddTextToLogTab(text);
                    GlobalData.AddTextToTelegram(text);
                }

                if (result.Success && result.Data != null)
                {
                    TradeParams tradeParams = new();
                    tradeParams.CreateTime = result.Data.CreateTime;
                    tradeParams.Side = orderSide;
                    tradeParams.OrderType = orderType;
                    tradeParams.Price = result.Data.Price;
                    tradeParams.StopPrice = stop;
                    tradeParams.LimitPrice = limit;
                    tradeParams.Quantity = result.Data.Quantity;
                    tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                    tradeParams.OrderId = result.Data.Id;
                    return (true, tradeParams);
                }
                else return (false, null);
            }
        }
    }


    public static async Task<WebCallResult<BinanceOrderBase>> Cancel(CryptoTradeAccount tradeAccount,
        CryptoSymbol symbol, CryptoPosition position, long? orderId)
    {
        if (tradeAccount.AccountType != CryptoTradeAccountType.RealTrading)
        {
            //TradeParams tradeParams = new();
            //tradeParams.CreateTime = CurrentDate;
            //tradeParams.IsBuy = false;
            //tradeParams.OrderId = 0; // result.Data.Id;
            //tradeParams.StopPrice = stop; // order2.StopPrice;
            //tradeParams.LimitPrice = limit; // order2.  Hey, waarom is deze er niet?
            //if (price == null)
            //    tradeParams.Price = (decimal)symbol.LastPrice;
            //else
            //    tradeParams.Price = (decimal)price;
            //tradeParams.Quantity = (decimal)quantity;
            //tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
            //return (true, tradeParams);

            // todo, deze tekst ook verderop plaatsen!
            //string text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} CANCEL price={4} stop={5} quantity={6} quotequantity={7}",
            //    symbol.Name, 
            //    orderId);
            //GlobalData.AddTextToLogTab(text2);
            //GlobalData.AddTextToTelegram(text2);

            return null; // what?
        }

        // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul

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

    static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BinanceBalance> balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            foreach (var assetInfo in balances)
            {
                if (assetInfo.Total > 0)
                {
                    if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                    {
                        asset = new CryptoAsset();
                        asset.Quote = assetInfo.Asset;
                        tradeAccount.AssetList.Add(asset.Quote, asset);
                    }
                    asset.Free = assetInfo.Available;
                    asset.Total = assetInfo.Total;
                    asset.Locked = assetInfo.Locked;

                    if (asset.Total == 0)
                        tradeAccount.AssetList.Remove(asset.Quote);
                }
            }
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
    }

    static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BinanceStreamBalance> balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        {
            try
            {
                foreach (var assetInfo in balances)
                {
                    if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                    {
                        asset = new CryptoAsset()
                        {
                            Quote = assetInfo.Asset,
                        };
                        tradeAccount.AssetList.Add(asset.Quote, asset);
                    }
                    asset.Free = assetInfo.Available;
                    asset.Total = assetInfo.Total;
                    asset.Locked = assetInfo.Locked;

                    if (asset.Total == 0)
                        tradeAccount.AssetList.Remove(asset.Quote);
                }
            }
            finally
            {
                tradeAccount.AssetListSemaphore.Release();
            }
        }
    }


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceTrade item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.Id;
        trade.OrderId = item.OrderId;
        //trade.OrderListId = (long)item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#1trade QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;

        trade.TradeTime = item.Timestamp;

        if (item.IsBuyer)
            trade.Side = CryptoOrderSide.Buy;
        else
            trade.Side = CryptoOrderSide.Sell;
    }


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceStreamOrderUpdate item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId;
        trade.OrderId = item.Id;
        //trade.OrderListId = item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#2stream QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;

        trade.TradeTime = item.EventTime;

        if (item.Side == OrderSide.Buy)
            trade.Side = CryptoOrderSide.Buy;
        else
            trade.Side = CryptoOrderSide.Sell;
    }


    public static async Task FetchAssets(CryptoTradeAccount tradeAccount)
    {
        // We onderteunen momenteel enkel de exchange "binance"
        //if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab("Reading asset information from Binance");

                BinanceWeights.WaitForFairBinanceWeight(1);

                using (var client = new BinanceClient())
                {
                    WebCallResult<BinanceAccountInfo> accountInfo = await client.SpotApi.Account.GetAccountInfoAsync();

                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo == null | accountInfo.Data == null)
                        throw new ExchangeException("Geen account data ontvangen");

                    try
                    {
                        BinanceApi.PickupAssets(tradeAccount, accountInfo.Data.Balances);
                        GlobalData.AssetsHaveChanged("");
                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
    }

}

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Spot;

namespace CryptoSbmScanner.Exchange.Kucoin;


public class Api: ExchangeBase
{
    public static readonly string ExchangeName = "Kucoin";

    public Api() : base()
    {
    }

    public override void ExchangeDefaults()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} defaults");
        // Default opties voor deze exchange
        KucoinRestClient.SetDefaultOptions(options =>
        {
            // type mismatch?
            //if (GlobalData.Settings.ApiKey != "")
            //    options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        KucoinSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            // type mismatch?
            //if (GlobalData.Settings.ApiKey != "")
            //    options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        ExchangeHelper.PriceTicker = new PriceTicker();
        ExchangeHelper.KLineTicker = new KLineTicker();
        //ExchangeHelper.UserData = new userData();
    }

    public async override Task FetchSymbolsAsync()
    {
        await FetchSymbols.ExecuteAsync();
    }

    public async override Task FetchCandlesAsync()
    {
        await FetchCandles.ExecuteAsync();
    }

#if TRADEBOT
    //// Converteer de orderstatus van Exchange naar "intern"
    //public static CryptoOrderType LocalOrderType(SpotOrderType orderType)
    //{
    //    CryptoOrderType localOrderType = orderType switch
    //    {
    //        SpotOrderType.Market => CryptoOrderType.Market,
    //        SpotOrderType.Limit => CryptoOrderType.Limit,
    //        SpotOrderType.StopLoss => CryptoOrderType.StopLimit,
    //        SpotOrderType.StopLossLimit => CryptoOrderType.Oco,
    //        _ => throw new Exception("Niet ondersteunde ordertype"),
    //    };

    //    return localOrderType;
    //}

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
            OrderStatus.Active => CryptoOrderStatus.New,
            OrderStatus.Done => CryptoOrderStatus.Filled,
            //OrderStatus.New => CryptoOrderStatus.New,
            //OrderStatus.Filled => CryptoOrderStatus.Filled,
            //OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            //OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
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

    public async Task<(bool result, TradeParams tradeParams)> BuyOrSell(
                CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate,
CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit)
    {
        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, text, price, quantity));
            return (false, null);
        }

        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
        {
            TradeParams tradeParams = new()
            {
                CreateTime = currentDate,
                Side = orderSide,
                OrderType = orderType,
                Price = price, // the sell part (can also be a buy)
                StopPrice = stop, // OCO - the price at which the limit order to sell is activated
                LimitPrice = limit, // OCO - the lowest price that the trader is willing to accept
                Quantity = quantity,
                QuoteQuantity = price * quantity,
                OrderId = 0,
            };

            // todo, deze tekst ook verderop plaatsen!
            string text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
                symbol.Name, orderSide,
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
        using KucoinRestClient client = new();
        {
            // Een OCO is afwijkend ten opzichte van een standaard buy or sell
            if (orderType == CryptoOrderType.Oco)
            {
                throw new Exception("Not supported");
                //WebCallResult<BybitOrderOcoList> result;
                //if (orderSide == CryptoOrderSide.Buy)
                //{
                //    result = await client.SpotApi.Trading.PlaceOcoOrderAsync(Symbol.Name, OrderSide.Buy,
                //        quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);
                //}
                //else
                //{
                //    result = await client.V5Api.Trading.PlaceOcoOrderAsync(Category.Spot, Symbol.Name, OrderSide.Sell,
                //        quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);
                //}

                //if (!result.Success)
                //{
                //    text = string.Format("{0} ERROR {1} {2} order {3} {4}\r\n", Symbol.Name, orderType, orderSide, result.ResponseStatusCode, result.Error);
                //    text += string.Format("quantity={0}\r\n", quantity.ToString0());
                //    text += string.Format("sellprice={0}\r\n", price.ToString0());
                //    text += string.Format("sellstop={0}\r\n", stop?.ToString0());
                //    text += string.Format("selllimit={0}\r\n", limit?.ToString0());
                //    text += string.Format("lastprice={0}\r\n", Symbol.LastPrice?.ToString0());
                //    text += string.Format("trades={0}\r\n", Symbol.TradeList.Count);
                //    GlobalData.AddTextToLogTab(text);
                //    GlobalData.AddTextToTelegram(text);
                //}


                //if (result.Success && result.Data != null)
                //{
                //    //string text2 = string.Format("{0} POSITION {1} ORDER PLACED {2} {3} {4}", symbol.Name, dcaType, price, quantity, price * quantity);
                //    //GlobalData.AddTextToLogTab(text2, true);
                //    //GlobalData.AddTextToTelegram(text2);

                //    // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
                //    // TODO: Controleren van de order id's (want ik heb ze in de administratie omgedraaid denk ik)
                //    // niet dat dat zo heel veel uitmaakt, maar de ID's vielen bij het traceren op.

                //    // De eerste order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
                //    // De tweede order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
                //    // het voorbeeld in de API is een buy (dus het kan ook andersom zijn)
                //    // Een van de twee heeft ook een price/stopprice, de andere enkel een price
                //    BybitOrderId order1 = result.Data.OrderReports.First();
                //    BybitOrderId order2 = result.Data.OrderReports.Last();

                //    TradeParams tradeParams = new();
                //    tradeParams.CreateTime = order1.CreateTime;
                //    tradeParams.Side = orderSide;
                //    tradeParams.OrderType = orderType;
                //    tradeParams.Price = order1.Price;
                //    tradeParams.StopPrice = stop;
                //    tradeParams.LimitPrice = limit;
                //    tradeParams.Quantity = order1.Quantity;
                //    tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
                //    tradeParams.OrderId = order1.Id;
                //    tradeParams.Order2Id = order2.Id; // Een 2e ordernummer!
                //    //tradeParams.OrderListId = order1.OrderListId; // Linked order
                //    return (true, tradeParams);
                //}
                //else return (false, null);
            }
            else
            {

                WebCallResult<KucoinNewOrder> result;
                if (orderSide == CryptoOrderSide.Buy)
                {
                    if (orderType == CryptoOrderType.Market)
                        result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Buy, NewOrderType.Market, quantity);
                    // Ehhh? nog eens oefenen op het testnet denk ik...
                    else
                    if (orderType == CryptoOrderType.StopLimit)
                        throw new Exception("Stop limit not supported? <uitzoeken>");
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Symbol.Name, OrderSide.Buy, NewOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    else
                        result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Buy, NewOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                }
                else
                {
                    if (orderType == CryptoOrderType.Market)
                        result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, NewOrderType.Market, quantity);
                    else
                        result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, NewOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                }

                if (!result.Success)
                {
                    text = string.Format("{0} ERROR {1} {2} order {3} {4}\r\n", symbol.Name, orderType, orderSide, result.ResponseStatusCode, result.Error);
                    text += string.Format("quantity={0}\r\n", quantity.ToString0());
                    text += string.Format("sellprice={0}\r\n", price.ToString0());
                    text += string.Format("sellstop={0}\r\n", stop?.ToString0());
                    text += string.Format("selllimit={0}\r\n", limit?.ToString0());
                    text += string.Format("lastprice={0}\r\n", symbol.LastPrice?.ToString0());
                    text += string.Format("trades={0}\r\n", symbol.TradeList.Count);
                    GlobalData.AddTextToLogTab(text);
                    GlobalData.AddTextToTelegram(text);
                }

                //TODO: Dit is raar, alsof dit de exacte data is voor een MarketOrder..
                // (=> dan komt er natuurlijk wel een trade die de werkelijkheid bevat)
                if (result.Success && result.Data != null)
                {
                    TradeParams tradeParams = new()
                    {
                        CreateTime = currentDate, // result.Data.CreateTime;
                        Side = orderSide,
                        OrderType = orderType,
                        Price = price, //result.Data.Price;
                        StopPrice = stop,
                        LimitPrice = limit,
                        Quantity = quantity, // result.Data.Quantity;
                        QuoteQuantity = price * quantity,
                        OrderId = long.Parse(result.Data.Id),
                    };
                    return (true, tradeParams);
                }
                else return (false, null);
            }
        }
    }


    public static async Task<WebCallResult<KucoinCanceledOrders>> Cancel(CryptoTradeAccount tradeAccount,
        CryptoSymbol symbol, long? orderId)
    {
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
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
            using var client = new KucoinRestClient();
            var result = await client.SpotApi.Trading.CancelOrderAsync(orderId.ToString());
            if (!result.Success)
            {
                string text = string.Format("{0} ERROR cancel order {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
                GlobalData.AddTextToLogTab(text);
                GlobalData.AddTextToTelegram(text);
            }
            return result;
        }
        return null;
    }

    //static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BybitUserAssetInfo> balances)
    //{
    //    tradeAccount.AssetListSemaphore.Wait();
    //    try
    //    {
    //        foreach (var assetInfo in balances)
    //        {
    //            // TODO, verder uitzoeken (lijkt de verkeerde info te zijn)
    //            if (assetInfo.QuantityRemaining > 0)
    //            {
    //                //if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
    //                //{
    //                //    asset = new CryptoAsset();
    //                //    asset.Quote = assetInfo.Asset;
    //                //    tradeAccount.AssetList.Add(asset.Quote, asset);
    //                //}
    //                //asset.Free = assetInfo.Available;
    //                //asset.Total = assetInfo.Total;
    //                //asset.Locked = assetInfo.Locked;

    //                //if (asset.Total == 0)
    //                //    tradeAccount.AssetList.Remove(asset.Quote);
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        tradeAccount.AssetListSemaphore.Release();
    //    }
    //}

    //static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitUserTrade item)
    //{
    //    trade.TradeAccount = tradeAccount;
    //    trade.TradeAccountId = tradeAccount.Id;
    //    trade.Exchange = symbol.Exchange;
    //    trade.ExchangeId = symbol.ExchangeId;
    //    trade.Symbol = symbol;
    //    trade.SymbolId = symbol.Id;

    //    trade.TradeId = long.Parse(item.TradeId);
    //    trade.OrderId = long.Parse(item.OrderId);
    //    //trade.OrderListId = (long)item.OrderListId;

    //    trade.Price = item.Price;
    //    trade.Quantity = item.Quantity;
    //    trade.QuoteQuantity = item.Price * item.Quantity;
    //    // enig debug werk, soms wordt het niet ingevuld!
    //    //if (item.QuoteQuantity == 0)
    //    //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#1trade QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

    //    trade.Commission = item.Fee;
    //    trade.CommissionAsset = symbol.Quote; // item.FeeAsset;?

    //    trade.TradeTime = item.Timestamp;

    //    if (item.Side == OrderSide.Buy)
    //        trade.Side = CryptoOrderSide.Buy;
    //    else
    //        trade.Side = CryptoOrderSide.Sell;
    //}


    //static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceStreamOrderUpdate item)
    //{
    //    trade.TradeAccount = tradeAccount;
    //    trade.TradeAccountId = tradeAccount.Id;
    //    trade.Exchange = symbol.Exchange;
    //    trade.ExchangeId = symbol.ExchangeId;
    //    trade.Symbol = symbol;
    //    trade.SymbolId = symbol.Id;

    //    trade.TradeId = item.TradeId;
    //    trade.OrderId = item.Id;
    //    //trade.OrderListId = item.OrderListId;

    //    trade.Price = item.Price;
    //    trade.Quantity = item.Quantity;
    //    trade.QuoteQuantity = item.Price * item.Quantity;
    //    // enig debug werk, soms wordt het niet ingevuld!
    //    //if (item.QuoteQuantity == 0)
    //    //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#2stream QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

    //    trade.Commission = item.Fee;
    //    trade.CommissionAsset = item.FeeAsset;

    //    trade.TradeTime = item.EventTime;

    //    if (item.Side == OrderSide.Buy)
    //        trade.Side = CryptoOrderSide.Buy;
    //    else
    //        trade.Side = CryptoOrderSide.Sell;
    //}


    public override async Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        //await BinanceFetchTrades.FetchTradesForSymbol(tradeAccount, symbol);
    }


    public async override Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        //    //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange exchange))
        //    {
        //        try
        //        {
        //            GlobalData.AddTextToLogTab("Reading asset information from Bybit");

        //            BybitWeights.WaitForFairWeight(1);

        //            using var client = new KucoinRestClient();
        //            {
        //                //https://openapi-sandbox.kucoin.com/api/v1/accounts

        //                var accountInfo = await client.SpotApi.Account.GetAccountAsync();

        //                if (!accountInfo.Success)
        //                {
        //                    GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
        //                }

        //                //Zo af en toe komt er geen data of is de Data niet gezet.
        //                //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
        //                if (accountInfo == null | accountInfo.Data == null)
        //                    throw new ExchangeException("Geen account data ontvangen");

        //                try
        //                {
        //                    PickupAssets(tradeAccount, accountInfo.Data.Assets);
        //                    GlobalData.AssetsHaveChanged("");
        //                }
        //                catch (Exception error)
        //                {
        //                    GlobalData.Logger.Error(error);
        //                    GlobalData.AddTextToLogTab(error.ToString());
        //                    throw;
        //                }
        //            }
        //        }
        //        catch (Exception error)
        //        {
        //            GlobalData.Logger.Error(error);
        //            GlobalData.AddTextToLogTab(error.ToString());
        //            GlobalData.AddTextToLogTab("");
        //        }

        //    }
    }

#endif

}

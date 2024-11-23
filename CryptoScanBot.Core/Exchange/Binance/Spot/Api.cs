using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Binance.Spot;




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

/*
 * 
https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-servertime
https://api-testnet.bybit.com/v2/public/time
{"ret_code":0,"ret_msg":"OK","result":{},"ext_code":"","ext_info":"","time_now":"1688116858.760925"}

https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-announcement
https://api-testnet.bybit.com/v2/public/announcement
{"ret_code":0,"ret_msg":"OK","result":[],"ext_code":"","ext_info":"","time_now":"1688116961.886013"}
(dat lijkt nogal op die eerste..)


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querykline
https://api-testnet.bybit.com/v2/public/kline/list
{"retCode":10001,"retMsg":"The requested symbol is invalid.","result":{},"retExtInfo":{},"time":1688117090806}
https://api-testnet.bybit.com/v2/public/kline/list?symbol=BTCUSDT&interval=1


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querysymbol
https://api-testnet.bybit.com/spot/v3/public/symbols
(denk om de versie verschillen)

 */

public class Api : ExchangeBase
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        Asset = new Asset(this);
        Candle = new Candle(this);
        Symbol = new Symbol(this);
        Order = new Order(this);
        Trade = new Trade(this);
    }

    public override IDisposable GetClient()
    {
        return new BinanceRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.ExchangeName = "Binance Spot";
        ExchangeOptions.LimitAmountOfSymbols = false;
        ExchangeOptions.SymbolLimitPerSubscription = 200;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default opties voor deze exchange
        BinanceRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BinanceSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;

            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);
    }


    //public override async Task<(bool succes, TradeParams tradeParams)> BuyOrSell(CryptoDatabase database,
    //    CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate,
    //    CryptoOrderType orderType, CryptoOrderSide orderSide,
    //    decimal quantity, decimal price, decimal? stop, decimal? limit)
    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.PlaceOrder {symbol.Name}");
        // debug
        //GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, "not at this moment", price, quantity));
        //return (false, null);


        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!position.Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", position.Symbol.Name, text, price, quantity));
            return (false, null);
        }

        TradeParams tradeParams = new()
        {
            Purpose = part.Purpose,
            CreateTime = currentDate,
            OrderSide = orderSide,
            OrderType = orderType,
            Price = price,
            StopPrice = stop, // OCO - the price at which the limit order to sell is activated
            LimitPrice = limit, // OCO - the lowest price that the trader is willing to accept
            Quantity = quantity,
            QuoteQuantity = price * quantity,
            //OrderId = 0,
        };
        if (orderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;
        if (position.Account.AccountType != CryptoAccountType.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return (true, tradeParams);
        }


        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)
        using BinanceRestClient client = new();

        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    WebCallResult<BinancePlacedOrder> result;
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        SpotOrderType.Market, quantity);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = result.Data.CreateTime;
                        tradeParams.OrderId = result.Data.Id.ToString();
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    WebCallResult<BinancePlacedOrder> result;
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = result.Data.CreateTime;
                        tradeParams.OrderId = result.Data.Id.ToString();
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    WebCallResult<BinancePlacedOrder> result;
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        SpotOrderType.StopLossLimit, quantity, price: price, stopPrice: stop, timeInForce: TimeInForce.GoodTillCanceled);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = result.Data.CreateTime;
                        tradeParams.OrderId = result.Data.Id.ToString();
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Oco:
                {
                    WebCallResult<BinanceOrderOcoList> result;
                    result = await client.SpotApi.Trading.PlaceOcoOrderAsync(position.Symbol.Name, side,
                        quantity, price: price, stop ?? 0, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);

                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
                        // De 1e order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
                        // De 2e order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
                        // De ene order heeft een price/stopprice, de andere enkel een price (combi)
                        BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
                        BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();
                        tradeParams.CreateTime = result.Data.TransactionTime; // order1.CreateTime;
                        tradeParams.OrderId = order1.Id.ToString();
                        tradeParams.Order2Id = order2.Id.ToString(); // Een 2e ordernummer (welke eigenlijk?)
                    }
                    return (result.Success, tradeParams);
                }
            default:
                throw new Exception("${orderType} not supported");
        }
    }


    public override async Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Order gegevens overnemen (voor een eventuele error dump)
        TradeParams tradeParams = new()
        {
            Purpose = part.Purpose,
            CreateTime = step.CreateTime,
            OrderSide = step.Side,
            OrderType = step.OrderType,
            Price = step.Price, // the sell part (can also be a buy)
            StopPrice = step.StopPrice, // OCO - the price at which the limit order to sell is activated
            LimitPrice = step.StopLimitPrice, // OCO - the lowest price that the trader is willing to accept
            Quantity = step.Quantity,
            QuoteQuantity = step.Price * step.Quantity,
            OrderId = step.OrderId,
            Order2Id = step.Order2Id,
        };
        // Eigenlijk niet nodig
        if (step.OrderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (position.Account.AccountType != CryptoAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != null && step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1);
            using var client = new BinanceRestClient();
            var result = await client.SpotApi.Trading.CancelOrderAsync(position.Symbol.Name, long.Parse(step.OrderId));
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            }
            return (result.Success, tradeParams);
        }

        return (false, tradeParams);
    }

}

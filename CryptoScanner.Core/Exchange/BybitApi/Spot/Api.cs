using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitApi.Spot;

public class Api : ExchangeBase
{
    private static readonly Category Category = Category.Spot;


    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        //Asset = new Asset();
        Candle = new Candle(this);
        Symbol = new Symbol();
        //Order = new Order();
        //Trade = new Trade();
    }


    public override IDisposable GetClient()
    {
        return new BybitRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.SetDefaultOptions("Bybit Spot", "USDT", 1000, false, 10);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");


        // Default opties voor deze exchange
        BybitRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new BybitCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BybitSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;

            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
            options.V5Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            //options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new BybitCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        BybitExchange.RateLimiter.RateLimitTriggered += (x) =>
        {
            GlobalData.AddTextToLogTab($"RateLimitTriggered {x.Limit} {x.ApiLimit} {x.LimitDescription} {x.Current} {x.Behaviour}");
            //if (x.Behaviour == RateLimitingBehaviour.Wait && x.DelayTime.HasValue)
            //    Thread.Sleep(x.DelayTime.Value);
            //Thread.Sleep(1000);
        };
    }


    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part,
        DateTime currentDate, CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.PlaceOrder {symbol.Name}");
        // debug
        //GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, "not at this moment", price, quantity));
        //return (false, null);


        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!position.Symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {text} (debug={price} {quantity})");
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
        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return (true, tradeParams);
        }


        // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)

        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        using BybitRestClient client = new();

        WebCallResult<BybitOrderId> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    // JA, price * quantity omdat dat blijkbaar zo moet, zie voorbeeld (onderin)
                    // https://bybit-exchange.github.io/docs/v5/order/create-order
                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side,
                        NewOrderType.Market, price * quantity, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.OrderId;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    //if (generateJsonDebug)
                    //    client.ClientOptions.OutputOriginalData = true;

                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side,
                    NewOrderType.Limit, quantity: quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.OrderId;
                    }
                    //if (generateJsonDebug)
                    //{
                    //    tradeParams.DebugJson = JsonSerializer.Serialize(result, JsonTools.JsonSerializerIndented).Trim();
                    //    client.ClientOptions.OutputOriginalData = false;
                    //}

                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    throw new Exception($"{orderType} not supported");

                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side,
                    //    NewOrderType.Limit, quantity: quantity, price: price, isLeverage: false,
                    //    //stopLossOrderType: OrderType.LimitMaker
                    //    //triggerDirection: TriggerDirection.Fall, orderFilter: OrderFilter.OcoOrder, triggerPrice: 50000m,

                    //    //stopLossTrigger: TriggerType.LastPrice,
                    //    //stopLossOrderType: OrderType.Market,
                    //    //stopLossTakeProfitMode:, 
                    //    //StopLossTakeProfitMode.Full,
                    //    //stopLoss: stop,
                    //    //stopPrice: stop,

                    //    takeProfitOrderType: OrderType.Limit,
                    //    takeProfit: price,

                    //    stopLossTriggerBy: TriggerType.IndexPrice,
                    //    stopLossOrderType: OrderType.Market,
                    //    stopLoss: stop,

                    //    stopLossLimitPrice: limit,

                    //    timeInForce: TimeInForce.GoodTillCanceled
                    //);



                    ////client.V5Api.Trading.PlaceOrderAsync(Category.Spot, "BTCUSDT", OrderSide.Sell, NewOrderType.Limit, quantity: quantity, 
                    ////    timeInForce: TimeInForce.GoodTillCanceled, 
                    ////    stopLossOrderType: OrderType.Limit, 
                    ////    stopLoss: price, stopLossLimitPrice: stopPrice, 
                    ////    stopLossTakeProfitMode: StopLossTakeProfitMode.Full, 
                    ////    stopLossTriggerBy: TriggerType.LastPrice, 
                    ////    clientOrderId: newClientOrderId
                    //// );



                    //////Task<WebCallResult<BybitOrderId>> PlaceOrderAsync(Category category, string symbol, OrderSide side, NewOrderType type, decimal quantity, decimal? price = null, 
                    //////    bool? isLeverage = null, TriggerDirection? triggerDirection = null, OrderFilter? orderFilter = null, decimal? triggerPrice = null, 
                    //////    TriggerType? triggerBy = null, decimal? orderIv = null, TimeInForce? timeInForce = null, PositionIdx? positionIdx = null, string? clientOrderId = null, 
                    //////    OrderType? takeProfitOrderType = null, decimal? takeProfit = null, decimal? takeProfitLimitPrice = null, OrderType? stopLossOrderType = null, 
                    //////    decimal? stopLoss = null, decimal? stopLossLimitPrice = null, TriggerType? takeProfitTriggerBy = null, TriggerType? stopLossTriggerBy = null, 
                    //////    bool? reduceOnly = null, bool? closeOnTrigger = null, bool? marketMakerProtection = null, StopLossTakeProfitMode? stopLossTakeProfitMode = null, 
                    //////    SelfMatchPreventionType? selfMatchPreventionType = null, MarketUnit? marketUnit = null, CancellationToken ct = default(CancellationToken));


                    //if (!result.Success)
                    //{
                    //    tradeParams.Error = result.Error;
                    //    tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    //}
                    //if (result.Success && result.Data != null)
                    //{
                    //    tradeParams.CreateTime = currentDate;
                    //    tradeParams.OrderId = result.Data.OrderId;
                    //}
                    ////if (generateJsonDebug)
                    ////{
                    ////    tradeParams.DebugJson = JsonSerializer.Serialize(result, JsonTools.JsonSerializerIndented).Trim();
                    ////    client.ClientOptions.OutputOriginalData = false;
                    ////}

                    //return (result.Success, tradeParams);

                }
            case CryptoOrderType.Oco:
                {
                    // Een OCO is afwijkend ten opzichte van een standaard buy or sell
                    //    Bij Binance was een OCO totaal afwijkend ten opzichte van een standaard buy or sell
                    //    het had ook andere parameters en results
                    //WebCallResult<BybitOrderOcoList> result;?????
                    //    throw new Exception("${orderType} not supported");
                    throw new Exception($"{orderType} not supported");
                }
            default:
                throw new Exception("${orderType} not supported");
        }

        /*
        exchange.create_order(
    symbol='BTC/USDT:USDT',
    type='limit',
    side='sell',
    amount=0.01,
    price=21100,
    params={
        'leverage': 1,
        'stopLossPrice': 20950,
        'takeProfitPrice': 21950,
    },
)
         */
    }


    public override async Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.Cancel {symbol.Name}");
        // Order gegevens overnemen (enkel voor een eventuele error dump)
        TradeParams tradeParams = new()
        {
            Purpose = part.Purpose,
            CreateTime = step.CreateTime,
            OrderSide = step.Side,
            OrderType = step.OrderType,
            Price = step.Price, // the buy or sell price
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

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != null && step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
            using var client = new BybitRestClient();
            var result = await client.V5Api.Trading.CancelOrderAsync(Category, position.Symbol.Name, step.OrderId.ToString());
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;

                // If its already gone ignore the error
                if (result.Error?.ErrorCode == "110001") // 110001: Order does not exist
                    return (true, tradeParams);
            }
            return (result.Success, tradeParams);
        }

        return (false, tradeParams);
    }

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "BYBI",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/BYBI_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "hypertrader://bybit-spot/{BASE}-{QUOTE}/{interval}",
                Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit-spot&a={BASE}&b={QUOTE}&i={interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{BASE}{QUOTE}&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.bybit.nl/trade/spot/{BASE}/{QUOTE}",
            }
        };
    }
}
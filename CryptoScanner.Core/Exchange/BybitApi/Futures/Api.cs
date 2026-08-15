using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;

using System.Text.Json;


namespace CryptoScanner.Core.Exchange.BybitApi.Futures;

public class Api : ExchangeBase
{
    private static readonly Category Category = Category.Linear;

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
        // 9.3 billion USDT over 718 pairs a day (14-08-2026), 132 symbols stay above the boundary
        ExchangeOptions.SetDefaultOptions("Bybit Futures", "USDT", 1000, false, 10, minimalVolume: 3_200_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default options for this exchange
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

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        BybitExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;
    }


    //public override async Task GetSymbolsAsync()
    //{
    //    await Symbol.GetSymbolsAsync();
    //}


    //public override async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol, long fetchEndUnix)
    //{
    //    await Candle.GetCandlesForAllIntervalsAsync(symbol, fetchEndUnix);
    //}


    public static async Task<bool> DoSwitchCrossIsolatedMarginAsync(BybitRestClient client, CryptoSymbol symbol)
    {
        //await client.V5Api.Account.SetLeverageAsync(Category, symbol.Name, 1, 1);
        //await client.V5Api.Account.SetMarginModeAsync(Category, symbol.Name, TradeMode.Isolated);
        Bybit.Net.Enums.TradeMode tradeMode = (Bybit.Net.Enums.TradeMode)GlobalData.Settings.Trading.CrossOrIsolated; // coincidentally the same order
        GlobalData.AddTextToLogTab($"{symbol.Name} Setting CrossOrIsolated={tradeMode} leverage={GlobalData.Settings.Trading.Leverage}");

        var result = await client.V5Api.Account.SwitchCrossIsolatedMarginAsync(Category, symbol.Name,
            tradeMode: tradeMode,
            buyLeverage: GlobalData.Settings.Trading.Leverage,
            sellLeverage: GlobalData.Settings.Trading.Leverage);
        if (!result.Success)
        {
            // A change without anything actually having been changed
            // 110026: Cross / isolated margin mode is not modified
            if (result.Error?.ErrorCode == "110026")
                return true; // {110026: Cross/isolated margin mode is not modified }
            if (result.Error?.ErrorCode == "110027")
                return true; // {110027	Margin is not modified }

            GlobalData.AddErrorToLogTab($"{symbol.Name} ERROR setting CrossOrIsolated={tradeMode} and leverage={GlobalData.Settings.Trading.Leverage} {result.Error}");
        }

        // Because of an unexpected liquidation this one is logged extensively as well
        string text = JsonSerializer.Serialize(result, JsonTools.JsonSerializerNotIndented);
        GlobalData.AddTextToLogTab("SwitchCrossIsolatedMarginAsync :" + text);


        // Cannot be verified????
        //if (result.Success)
        //{
        //    await client.V5Api.Account.GetSpotMarginStatusAndLeverageAsync();

        //        //BybitSpotMarginLeverageStatus
        //}


        return result.Success;
    }


    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // Check the maximum and minimum amount limits and the quantity
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


        // BinanceWeights.WaitForFairBinanceWeight(1); nonsense for that single tick (no repetition, right?)

        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Place an order on the exchange (they look alike, but it is slightly different every time)
        using BybitRestClient client = new();
        if (!await DoSwitchCrossIsolatedMarginAsync(client, position.Symbol))
        {
            // Retry
            if (!await DoSwitchCrossIsolatedMarginAsync(client, position.Symbol))
            {
                // Retry
                if (!await DoSwitchCrossIsolatedMarginAsync(client, position.Symbol))
                    return (false, null); // raise? throw?
            }
        }


        HttpResult<BybitOrderId> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    // YES, price * quantity because that is apparently required, see the example (at the bottom)
                    // https://bybit-exchange.github.io/docs/v5/order/create-order
                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side,
                        NewOrderType.Market, quantity: quantity, price: null, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
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
                    // Temporary (this can only sit here as long as you know we go long ONLY!)
                    bool reduce = false;
                    if (position.Side == CryptoTradeSide.Long && orderSide == CryptoOrderSide.Sell)
                        reduce = true;
                    if (position.Side == CryptoTradeSide.Short && orderSide == CryptoOrderSide.Buy)
                        reduce = true;

                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, position.Symbol.Name, side,
                        NewOrderType.Limit, quantity: quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false, reduceOnly: reduce);
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
            case CryptoOrderType.StopLimit:
                {
                    // is it supported or not? It could also be an extra option of the limit (a tp is there)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category, symbol.Name, side, NewOrderType.Market,
                    //    quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
                    throw new Exception("${orderType} not supported");
                }
            case CryptoOrderType.Oco:
                {
                    // An OCO deviates from a standard buy or sell
                    //    On Binance an OCO was completely different from a standard buy or sell
                    //    it also had different parameters and results
                    //HttpResult<BybitOrderOcoList> result;?????
                    //    throw new Exception("${orderType} not supported");
                    throw new Exception("${orderType} not supported");
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
        // Order details carried over (only for a possible error dump)
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
        // Not really needed
        if (step.OrderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            return (true, tradeParams);


        // Cancel the order 
        if (step.OrderId != null && step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); nonsense
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
                Code = "BYBIF",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/BYBIF_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "hypertrader://bybit/{BASE}-{QUOTE}/{interval}",
                Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit&a={BASE}&b={QUOTE}&i={interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{BASE}{QUOTE}.P&interval={interval}",
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.bybit.nl/trade/{quote}/{BASE}{QUOTE}",
            }
        };
    }
}
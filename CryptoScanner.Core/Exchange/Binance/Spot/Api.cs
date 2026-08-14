using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Spot;

// The order errors and the OCO price rules that used to be listed here have moved to Binance.md,
// the leftover Bybit v2 endpoint notes to BybitApi\Bybit.md.

public class Api : ExchangeBase
{
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
        return new BinanceRestClient();
    }

    public override void ExchangeDefaults()
    {
        // 4.1 billion USDT over 671 pairs a day (14-08-2026), 134 symbols stay above the boundary
        ExchangeOptions.SetDefaultOptions("Binance Spot", "USDT", 1000, false, 50, minimalVolume: 1_400_000);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default options for this exchange
        BinanceRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new BinanceCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BinanceSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;

            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new BinanceCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //PriceTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new SubscriptionManager(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        BinanceExchange.RateLimiter.RateLimitTriggered += OnRateLimitTriggered;

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


        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Place an order on the exchange (they look alike, but it is slightly different every time)
        //BinanceWeights.WaitForFairBinanceWeight(1); nonsense for that single tick (no repetition, right?)
        using BinanceRestClient client = new();

        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    HttpResult<BinancePlacedOrder> result;
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
                    HttpResult<BinancePlacedOrder> result;
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
                    HttpResult<BinancePlacedOrder> result;
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
                    HttpResult<BinanceOrderOcoList> result;
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
                        // The 1st order is the stop loss (recognisable by "type": "STOP_LOSS")
                        // The 2nd order is the normal sell (recognisable by "type": "LIMIT_MAKER")
                        // One order has a price/stop price, the other one only a price (combined)
                        BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
                        BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();
                        tradeParams.CreateTime = result.Data.TransactionTime; // order1.CreateTime;
                        tradeParams.OrderId = order1.Id.ToString();
                        tradeParams.Order2Id = order2.Id.ToString(); // A 2nd order number (which one exactly?)
                    }
                    return (result.Success, tradeParams);
                }
            default:
                throw new Exception("${orderType} not supported");
        }
    }


    public override async Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // Order details carried over for a possible error dump
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
        // Not really needed
        if (step.OrderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            return (true, tradeParams);


        // Cancel the order 
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

    public static CryptoExternalUrls GetExchangeLinks()
    {
        return new()
        {
            Altrady = new()
            {
                Code = "BINA",
                Execute = CryptoExternalUrlType.Internal,
                Url = "https://app.altrady.com/d/BINA_{QUOTE}_{BASE}:{interval}",
            },
            HyperTrader = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "hypertrader://binance/{BASE}-{QUOTE}/{interval}",
                Telegram = "http://www.ccscanner.nl/hypertrader/?e=binance&a={BASE}&b={QUOTE}&i={interval}",
            },
            TradingView = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.tradingview.com/chart/?symbol=BINANCE:{BASE}{QUOTE}&interval={interval}"
            },
            ExchangeUrl = new()
            {
                Execute = CryptoExternalUrlType.External,
                Url = "https://www.binance.com/en/trade/{BASE}_{QUOTE}?_from=markets&type=spot",
            }
        };
    }
}

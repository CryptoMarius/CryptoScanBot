using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;


// Just a testsetup based upon Binance, this is not working for now!


namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;


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
        ExchangeOptions.SetDefaultOptions("Binance Spot", "USDT", 1000, false, 50);
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

        //PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        //UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);

        BinanceExchange.RateLimiter.RateLimitTriggered += (x) =>
        {
            GlobalData.AddTextToLogTab($"RateLimitTriggered {x.Limit} {x.ApiLimit} {x.LimitDescription} {x.Current} {x.Behaviour}");
            //if (x.Behaviour == RateLimitingBehaviour.Wait && x.DelayTime.HasValue)
            //    Thread.Sleep(x.DelayTime.Value);
            //Thread.Sleep(1000);
        };

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

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
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

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class Api : ExchangeBase
{
    public override void ExchangeDefaults()
    {
        ExchangeOptions.ExchangeName = "Kraken Spot";
        ExchangeOptions.LimitAmountOfSymbols = false;
        ExchangeOptions.SubscriptionLimitSymbols = 10; // onbekend
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default opties voor deze exchange
        KrakenRestClient.SetDefaultOptions(options =>
        {
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        KrakenSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        ExchangeHelper.PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        ExchangeHelper.KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
#if TRADEBOT
        ExchangeHelper.UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);
#endif
    }


    public override async Task GetSymbolsAsync()
    {
        await Symbol.ExecuteAsync();
    }

    public override async Task GetCandlesForAllSymbolsAsync()
    {
        await Candle.GetCandlesForAllSymbolsAsync();
    }

    public override async Task GetCandlesForSymbolAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        await Candle.GetCandlesForSymbolAsync(symbol, fetchEndUnix);
    }



#if TRADEBOT
    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.StopLoss => CryptoOrderType.StopLimit,
            OrderType.StopLossLimit => CryptoOrderType.Oco,
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
            OrderStatus.Pending => CryptoOrderStatus.New,
            OrderStatus.Open => CryptoOrderStatus.Filled,
            //OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
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
            tradeParams.QuoteQuantity = (tradeParams.StopPrice ?? 0) * tradeParams.Quantity;
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
        using KrakenRestClient client = new();

        WebCallResult<KrakenPlacedOrder> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        OrderType.Market, quantity);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = "12345"; // long.Parse(result.Data.OrderIds); dat wordt een conversie!
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                    OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GTC);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = "12345"; // long.Parse(result.Data.OrderIds); dat wordt een conversie!
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    // wordt het nu wel of niet ondersteund? Het zou ook een extra optie van de limit kunnen (zie wel een tp)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category.Linear, symbol.Name, side, NewOrderType.Market,
                    //    quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled, isLeverage: false);
                    throw new Exception("${orderType} not supported");
                }
            case CryptoOrderType.Oco:
                {
                    // Een OCO is afwijkend ten opzichte van een standaard buy or sell
                    //    Bij Binance was een OCO totaal afwijkend ten opzichte van een standaard buy or sell
                    //    het had ook andere parameters en results
                    //WebCallResult<BybitOrderOcoList> result;?????
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
        // Order gegevens overnemen (enkel voor een eventuele error dump)
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
            tradeParams.QuoteQuantity = (tradeParams.StopPrice ?? 0) * tradeParams.Quantity;

        if (position.Account.AccountType != CryptoAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
            using var client = new KrakenRestClient();
            var result = await client.SpotApi.Trading.CancelOrderAsync(position.Symbol.Name, step.OrderId.ToString());
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            }
            return (result.Success, tradeParams);
        }

        return (false, tradeParams);
    }

    public override async Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        return await Trade.FetchTradesForSymbolAsync(database, position);
    }


    public override Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult(0);
    }


    public async override Task GetAssetsAsync(CryptoAccount tradeAccount)
    {
        await Asset.GetAssetsAsync(tradeAccount);
    }

#endif

}

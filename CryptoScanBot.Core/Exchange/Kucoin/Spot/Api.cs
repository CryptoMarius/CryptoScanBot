using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Spot;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;


public class Api : ExchangeBase
{
    public override void ExchangeDefaults()
    {
        ExchangeOptions.ExchangeName = "Kucoin Spot";
        ExchangeOptions.SymbolLimitPerSubscription = 1;
        ExchangeOptions.SubscriptionLimitPerClient = 20;
        ExchangeOptions.LimitAmountOfSymbols = true;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Ik begrijp hier niet zoveel van.....
        //var logFactory = new LoggerFactory();
        //logFactory.AddProvider(new ConsoleLoggerProvider());
        //var binanceClient = new KucoinRestClient(new HttpClient(), logFactory, options => { });

        //var KucoinRestClient = new KucoinRestClient(null, factory, opts =>
        //{
        //    // set options
        //});



        // Default opties voor deze exchange
        KucoinRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            //options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new KucoinApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        KucoinSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;

            options.RequestTimeout = TimeSpan.FromSeconds(60); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds
            //options.V5Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            //options.SpotV3Options.SocketNoDataTimeout = options.SocketNoDataTimeout;
            options.SocketSubscriptionsCombineTarget = 20;

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new KucoinApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret, GlobalData.TradingApi.PassPhrase);
        });

        ExchangeHelper.PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price)
        {
            Enabled = false // many many errors
        };
        ExchangeHelper.KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        ExchangeHelper.UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);
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

    //public override string ExchangeSymbolName(CryptoSymbol symbol)
    //{
    //    return symbol.Base + '-' + symbol.Quote;
    //}

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


    public override async Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
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
        if (position.Account.AccountType != CryptoAccountType.RealTrading)
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
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)
        using KucoinRestClient client = new();

        WebCallResult<KucoinOrderId> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                        NewOrderType.Market, quantity);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.Id;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(position.Symbol.Name, side,
                    NewOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.Id;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    // wordt het nu wel of niet ondersteund? Het zou ook een extra optie van de limit kunnen (zie wel een tp)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category.Linear, symbol.Name, side, NewOrderType.Market,
                    //    quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
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
            tradeParams.QuoteQuantity = tradeParams.StopPrice ?? 0 * tradeParams.Quantity;

        if (position.Account.AccountType != CryptoAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
            using var client = new KucoinRestClient();
            var result = await client.SpotApi.Trading.CancelOrderAsync(step.OrderId!);
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            }
            return (result.Success, tradeParams);
        }

        return (false, tradeParams);
    }


    //static public void PickupAssets(CryptoTradeAccount tradeAccount, Dictionary<string, Kucoin> balances)
    //{
    //}


    public override Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult(0); // await GetTrades.FetchTradesForSymbolAsync(database, position);
    }

    public override Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult(0);
    }

    public override Task GetAssetsAsync(CryptoAccount tradeAccount)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeOptions.ExchangeName} TODO!!!!");

                //BybitWeights.WaitForFairWeight(1);

                using var client = new KucoinRestClient();
                {
                    //https://openapi-sandbox.kucoin.com/api/v1/accounts

                    //var accountInfo = await client.SpotApi.Account.get();

                    //if (!accountInfo.Success)
                    //{
                    //    GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    //}

                    ////Zo af en toe komt er geen data of is de Data niet gezet.
                    ////De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    //if (accountInfo == null | accountInfo.Data == null)
                    //    throw new ExchangeException("Geen account data ontvangen");

                    //try
                    //{
                    //    //PickupAssets(tradeAccount, accountInfo.Data.Assets);
                    //    GlobalData.AssetsHaveChanged("");
                    //}
                    //catch (Exception error)
                    //{
                    //    ScannerLog.Logger.Error(error, "");
                    //    GlobalData.AddTextToLogTab(error.ToString());
                    //    throw;
                    //}
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
        return Task.CompletedTask;
    }

}

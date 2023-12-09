using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.Socket;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Exchange.BybitFutures;

public class Api : ExchangeBase
{
    public static readonly string ExchangeName = "Bybit Futures";
#if TRADEBOT
    private static readonly Category Category = Category.Linear;
    static private UserDataStream TaskBybitStreamUserData { get; set; }
#endif
    public static List<KLineTickerItem> TickerList { get; set; } = new();


    public Api() : base()
    {
    }

    public override void ExchangeDefaults()
    {
        GlobalData.AddTextToLogTab($"{ExchangeName} defaults");

        // Default opties voor deze exchange
        BybitRestClient.SetDefaultOptions(options =>
        {
            if (GlobalData.Settings.Trading.ApiKey != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.Settings.Trading.ApiKey, GlobalData.Settings.Trading.ApiSecret);
        });

        BybitSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.Settings.Trading.ApiKey != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.Settings.Trading.ApiKey, GlobalData.Settings.Trading.ApiSecret);
        });

        ExchangeHelper.PriceTicker = new PriceTicker();
        ExchangeHelper.KLineTicker = new KLineTicker();
#if TRADEBOT
        ExchangeHelper.UserData = new UserData();
#endif
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
    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.LimitMaker => CryptoOrderType.StopLimit, /// ????????????????????????????????????????????????
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
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public async Task<bool> DoSwitchCrossIsolatedMarginAsync(BybitRestClient client, CryptoSymbol symbol)
    {
        //await client.V5Api.Account.SetLeverageAsync(Category, symbol.Name, 1, 1);
        //await client.V5Api.Account.SetMarginModeAsync(Category, symbol.Name, TradeMode.Isolated);
        Bybit.Net.Enums.TradeMode tradeMode = (Bybit.Net.Enums.TradeMode)GlobalData.Settings.Trading.CrossOrIsolated; // toevallig dezelfde volgorde
        GlobalData.AddTextToLogTab($"{symbol.Name} Setting CrossOrIsolated={tradeMode} leverage={GlobalData.Settings.Trading.Leverage}");

        var result = await client.V5Api.Account.SwitchCrossIsolatedMarginAsync(Category, symbol.Name,
            tradeMode, GlobalData.Settings.Trading.Leverage, GlobalData.Settings.Trading.Leverage);
        if (!result.Success)
        {
            // Aanpassing zonder dat er daadwerkelijk iets aangepast is
            // 110026: Cross / isolated margin mode is not modified
            if (result.Error.Code == 110026)
                return true; // {110026: Cross/isolated margin mode is not modified }
            if (result.Error.Code == 110027)
                return true; // {110027	Margin is not modified }

            GlobalData.AddTextToLogTab($"{symbol.Name} ERROR setting CrossOrIsolated={tradeMode} en leverage={GlobalData.Settings.Trading.Leverage} {result.Error}");
        }
        return result.Success;
    }


    public override async Task<(bool result, TradeParams tradeParams)> PlaceOrder(CryptoDatabase database,
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

        TradeParams tradeParams = new()
        {
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
            tradeParams.QuoteQuantity = (decimal)tradeParams.StopPrice * tradeParams.Quantity;
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
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
        using BybitRestClient client = new();
        if (!await DoSwitchCrossIsolatedMarginAsync(client, symbol))
            return (false, null);


        WebCallResult<BybitOrderId> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    // JA, price * quantity omdat dat blijkbaar zo moet, zie voorbeeld (onderin)
                    // https://bybit-exchange.github.io/docs/v5/order/create-order
                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, symbol.Name, side,
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
                    // Even tijdelijk (kan ook alleen maar op deze plek als je weet dat we ALLEEN long gaan!)
                    bool reduce = false;
                    if (orderSide == CryptoOrderSide.Sell)
                        reduce = true;

                    result = await client.V5Api.Trading.PlaceOrderAsync(Category, symbol.Name, side,
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
                    // wordt het nu wel of niet ondersteund? Het zou ook een extra optie van de limit kunnen (zie wel een tp)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category, symbol.Name, side, NewOrderType.Market,
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


    public override async Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoPositionStep step)
    {
        // Order gegevens overnemen (enkel voor een eventuele error dump)
        TradeParams tradeParams = new()
        {
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
            tradeParams.QuoteQuantity = (decimal)tradeParams.StopPrice * tradeParams.Quantity;

        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return (true, tradeParams);


        // Annuleer de order 
        if (step.OrderId != "")
        {
            // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul
            using var client = new BybitRestClient();
            var result = await client.V5Api.Trading.CancelOrderAsync(Category, symbol.Name, step.OrderId.ToString());
            if (!result.Success)
            {
                tradeParams.Error = result.Error;
                tradeParams.ResponseStatusCode = result.ResponseStatusCode;
            }
            return (true, tradeParams);
        }

        return (false, tradeParams);
    }

    static public void PickupAssets(CryptoTradeAccount tradeAccount, BybitAllAssetBalances balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                foreach (var assetInfo in balances.Balances)
                {
                    if (assetInfo.WalletBalance > 0)
                    {
                        if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                        {
                            asset = new CryptoAsset();
                            asset.Name = assetInfo.Asset;
                            asset.TradeAccountId = tradeAccount.Id;
                            tradeAccount.AssetList.Add(asset.Name, asset);
                        }
                        asset.Total = (decimal)assetInfo.WalletBalance;
                        asset.Locked = (decimal)assetInfo.WalletBalance - (decimal)assetInfo.TransferBalance;
                        asset.Free = asset.Total - asset.Locked;

                        if (asset.Id == 0)
                            databaseThread.Connection.Insert(asset, transaction);
                        else
                            databaseThread.Connection.Update(asset, transaction);
                    }
                }

                // remove assets with total=0
                foreach (var asset in tradeAccount.AssetList.Values.ToList())
                {
                    if (asset.Total == 0)
                    {
                        databaseThread.Connection.Delete(asset, transaction);
                        tradeAccount.AssetList.Remove(asset.Name);
                    }
                }

                transaction.Commit();
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
                // Als er ooit een rolback plaatsvindt is de database en objects in het geheugen niet meer in sync..
                transaction.Rollback();
                throw;
            }
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
    }

    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitUserTrade item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId;
        trade.OrderId = item.OrderId;
        //trade.OrderListId = (long)item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#1trade QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.TradeTime = item.Timestamp;
        trade.Side = LocalOrderSide(item.Side);

        trade.Commission = item.Fee.Value;
        trade.CommissionAsset = symbol.Quote; // item.FeeAsset;?
    }


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitUsdPerpetualOrderUpdate item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        //TODO: Uitzoeken!!!!
        // Dit zijn updates op orders en we willen eigenlijk de trades!
        //trade.TradeId = item.Id; //Ehhhh????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        trade.OrderId = item.Id;
        //trade.OrderListId = item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#2stream QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        // Verwarrend want deze moet toch altijd gevuld zijn?
        if (item.UpdateTime.HasValue)
            trade.TradeTime = item.UpdateTime.Value; // Timestamp;
        else
            trade.TradeTime = item.Timestamp;

        trade.Side = LocalOrderSide(item.Side);

        trade.Commission = item.Fee;
        trade.CommissionAsset = symbol.Quote;
    }


    public override async Task FetchTradesForSymbolAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        await FetchTrades.FetchTradesForSymbolAsync(tradeAccount, symbol);
    }

    public override async Task<int> FetchTradesForOrderAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, string orderId)
    {
        return await FetchTradeForOrder.FetchTradesForOrderAsync(tradeAccount, symbol, orderId);
    }

public async override Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {Api.ExchangeName}");

                LimitRates.WaitForFairWeight(1);

                using var client = new BybitRestClient();
                {
                    var accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Contract);
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
                        PickupAssets(tradeAccount, accountInfo.Data);
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


    public static void StartUserDataStream()
    {
        TaskBybitStreamUserData = new UserDataStream();
        var _ = Task.Run(async () => { await TaskBybitStreamUserData.ExecuteAsync(); });
    }

    public static async Task StopUserDataStream()
    {
        if (TaskBybitStreamUserData != null)
            await TaskBybitStreamUserData?.StopAsync();
        TaskBybitStreamUserData = null;
    }

    public static void ResetUserDataStream()
    {
        // niets, hmm
    }
#endif

}
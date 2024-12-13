using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using Bybit.Net.Objects.Options;

using CryptoExchange.Net.Authentication;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeTest.Bybit.SpotUta;


internal class FileName
{
    internal static LoggerFactory LogFactory;

    internal static BybitRestClient CreateRestClient()
    {
        // Ik snap er helemaal niets van.. Heb een paar classes verkeerd begrepen log en logger

        //NLog.Extensions.Logging.NLogLoggerFactory loggerFactory = new();
        //MyClass myClass = new(loggerFactory);
        //loggerFactory.
        //LoadNLogConfigurationOnFactory(loggerFactory);
        //using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddNLog());
        //Microsoft.Extensions.Logging.ILogger logger = factory.CreateLogger("Program");
        //logger.LogInformation("Hello World! Logging is {Description}.", "fun");

        //var loggerFactory = new NLog.Extensions.Logging.NLogLoggerFactory();

        //Logger moduleLogger = NLog.LogManager.GetLogger("Modules.MyModuleName");
        //NLog.LogFactory Factory1 = new LogFactory();
        //LoadNLogConfigurationOnFactory(Factory1);

        var options = new BybitRestOptions()
        {
            //options.Environment = _environment,
            OutputOriginalData = true,
            ReceiveWindow = TimeSpan.FromSeconds(15),
        };
        if (GlobalData.TradingApi.Key != "")
            options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);

        BybitRestClient client = new(null, LogFactory, Options.Create(options));
        return client;
    }

    private static void BybitPickupAssets(BybitAllAssetBalances balances)
    {
        if (balances is null)
        {
            GlobalData.AddTextToLogTab($"No account data received");
            return;
        }

        CryptoAccount tradeAccount = new()
        {
            Exchange = null,
        };

        foreach (var assetInfo in balances.Balances)
        {
            if (assetInfo.WalletBalance > 0)
            {
                if (!tradeAccount.Data.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset? asset))
                {
                    asset = new()
                    {
                        Name = assetInfo.Asset,
                        TradeAccountId = tradeAccount.Id
                    };
                    tradeAccount.Data.AssetList.Add(asset.Name, asset);
                }
                asset.Total = (decimal)assetInfo.WalletBalance;
                asset.Locked = (decimal)assetInfo.WalletBalance - (decimal)assetInfo.TransferBalance;
                asset.Free = asset.Total - asset.Locked;
            }
        }

        // remove assets with total=0
        foreach (var asset in tradeAccount.Data.AssetList.Values.ToList())
        {
            if (asset.Total == 0)
            {
                //databaseThread.Connection.Delete(asset, transaction);
                tradeAccount.Data.AssetList.Remove(asset.Name);
            }
        }

        if (tradeAccount.Data.AssetList.Count > 0)
        {
            foreach (var asset in tradeAccount.Data.AssetList.Values.ToList())
            {
                GlobalData.AddTextToLogTab($"{asset.Name} {asset.Total} {asset.Free}");
            }
        }
        else
            GlobalData.AddTextToLogTab($"no assets");
    }


    private static async void ByBitGetAssets()
    {
        try
        {
            //GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeOptions.ExchangeName}");
            //LimitRates.WaitForFairWeight(1);


            //GlobalData.AddTextToLogTab($"{asset.Name} {asset.Total} {asset.Free}");
            foreach (AccountType accountType in (AccountType[])Enum.GetValues(typeof(AccountType)))
            {
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab($"Bybit GetAssets for {accountType}");

                using var client = new BybitRestClient();
                {
                    try
                    {
                        var accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(accountType);
                        //accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Spot);
                        //accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Option); // Option?
                        //accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Contract); // futures
                        //accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Unified);
                        //var accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Spot);

                        if (!accountInfo.Success)
                            GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                        else GlobalData.AddTextToLogTab("success getting accountinfo " + accountInfo.Error);

                        BybitPickupAssets(accountInfo?.Data);
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
            GlobalData.AddTextToLogTab("");
        }

    }

    private static async void ByBitGetOrders(string symbolName)
    {
        if (GlobalData.ExchangeListName.TryGetValue("Bybit Spot", out CryptoScanBot.Core.Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab($"Bybit GetOrders for {symbolName}");

                BybitRestClient client = CreateRestClient();
                try
                {
                    DateTime startDate = DateTime.UtcNow.AddDays(-1);
                    string text = "lient.V5Api.Trading.GetOrderHistoryAsync";

                    var resultData = await client.V5Api.Trading.GetOrderHistoryAsync(
                        Category.Spot,
                        symbol: symbol.Name,
                        //startTime: startDate
                        orderId: "1636499753797799680"
                    );

                    if (resultData.Success && resultData.Data != null)
                    {
                        foreach (var order in resultData.Data.List)
                        {
                            text = JsonSerializer.Serialize(order, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                            System.Diagnostics.Debug.WriteLine(text);
                            GlobalData.AddTextToLogTab(text);
                        }
                    }

                    if (!resultData.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting mytrades " + resultData.Error);
                    }
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab("error get prices " + error.ToString()); // symbol.Text + " " + 
                }


            }
        }
    }

    private static async void ByBitGetTrades(string symbolName)
    {
        if (GlobalData.ExchangeListName.TryGetValue("Bybit Spot", out CryptoScanBot.Core.Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab($"Bybit GetTrades for {symbolName}");

                BybitRestClient client = CreateRestClient();
                try
                {
                    // , fromId: 1636499753797799679, toId: 1636499753797799681
                    long fromId = 1637244246473975808;
                    long toId = fromId + 1000; //, toId : toId
                    string text = "client.SpotApiV3.Trading.GetUserTradesAsync";
                    System.Diagnostics.Debug.WriteLine(text);
                    var resultData = await client.V5Api.Trading.GetUserTradesAsync(Category.Spot, symbol.Name); //fromId: fromId
                    text = JsonSerializer.Serialize(resultData, JsonTools.JsonSerializerIndented);
                    System.Diagnostics.Debug.WriteLine(text);
                    GlobalData.AddTextToLogTab(text);

                    //text = "Output client.V5Api.Trading.GetUserTradesAsync";
                    //System.Diagnostics.Debug.WriteLine(text);
                    //GlobalData.AddTextToLogTab(text);
                    //var resultV5 = await client.V5Api.Trading.GetUserTradesAsync(Category.Spot, symbol.Name, orderId: "1636499753797799680");
                    //text = JsonSerializer.Serialize(resultV5, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    //System.Diagnostics.Debug.WriteLine(text);
                    //GlobalData.AddTextToLogTab(text);


                    if (!resultData.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting mytrades " + resultData.Error);
                    }
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab("error get prices " + error.ToString()); // symbol.Text + " " + 
                }


            }
        }
    }
}

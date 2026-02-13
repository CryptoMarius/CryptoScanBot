using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Text.Json;

namespace CryptoScanner.CoreTests;

public class TestBase
{
    static bool IsSetupOnce = false;

    public static void ConfigurePlatformServices(IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IPlatformService, MacOSPlatformService>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IPlatformService, LinuxPlatformService>();
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");
    }

    public static void InitializeApplicationVariables()
    {
        // We need a version from the main assembly
        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];
        GlobalData.AppVersion = appVersion;
        //System.Diagnostics.Debug.WriteLine($"GlobalData.AppVersion =  {GlobalData.AppVersion}");

        // We need a folder for accessing the Sounds
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        //System.Diagnostics.Debug.WriteLine($"GlobalData.AppPath =  {GlobalData.AppPath}");

        // We need a data folder to store our data (temporary dependency injection to hide details)
        var services = new ServiceCollection();
        ConfigurePlatformServices(services);
        var platformService = services.BuildServiceProvider().GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");
        GlobalData.AppDataFolder = platformService.GetDataDirectory();
        //System.Diagnostics.Debug.WriteLine($"GlobalData.AppDataFolder =  {GlobalData.AppDataFolder}");

        // DEBUG OUTPUT
        Console.WriteLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        //Console.WriteLine($"ApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}");
        //Console.WriteLine($"LocalApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
        //Console.WriteLine($"UserProfile: {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
        //Console.WriteLine($"Personal: {Environment.GetFolderPath(Environment.SpecialFolder.Personal)}");
        Console.WriteLine($"Scanner Version: {GlobalData.AppVersion}");
        Console.WriteLine($"Scanner AppPath: {GlobalData.AppPath}");
        Console.WriteLine($"Scanner AppDataFolder: {GlobalData.AppDataFolder}");

        // Initialize the logging system (as soon as possible)
        ScannerLog.InitializeLogging();
    }

    static void SetupOnce()
    {
        if (!IsSetupOnce)
        {
            IsSetupOnce = true;

            ApplicationParams.Options = new ApplicationParams()
            {
                ExchangeName = "Binance Futures",
                AppDataFolder = Path.Combine("E:\\CryptoScanBot", "Test"),
            };
            ;
            InitializeApplicationVariables();
            
            // Description: toevoegen en mergen van candles (de happy flow)
            GlobalData.LogToLogTabEvent -= AddTextToLogTab;
            GlobalData.LogToLogTabEvent -= AddTextToLogTab;
            GlobalData.LogToLogTabEvent += AddTextToLogTab;

            GlobalData.LoadConfiguration();
            CryptoDatabase.SetDatabaseDefaults();
            GlobalData.LoadExchanges();
            GlobalData.LoadIntervals();
            GlobalData.ActiveExchange = GlobalData.ExchangeListId.Values[0];
            GlobalData.LoadSymbols();
            GlobalData.BackTest = true;
        }
    }

    internal static void InitTestSession()
    {
        SetupOnce();
    }

    internal static void AddTextToLogTab(string text)
    {
        text = text.Trim();
        Console.WriteLine(text);
    }

    internal static CryptoSymbol CreateTestSymbol(CryptoDatabase database)
    {
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out CryptoScanner.Core.Model.CryptoExchange? exchange))
        {
            if (!exchange.SymbolListName.TryGetValue("TESTUSDT", out CryptoSymbol? symbol))
            {
                var quoteData = GlobalData.AddQuoteData("USDT");
                symbol = new()
                {
                    Status = 1,
                    Base = "TEST",
                    Quote = "USDT",
                    Name = "TESTUSDT",
                    Exchange = exchange,
                    ExchangeId = exchange.Id,
                    QuoteData = quoteData,
                    ExchangeName = "TEST exchange",

                    QuantityTickSize = 0.01m,
                    QuantityMinimum = 0.2m,
                    QuantityMaximum = 87823.299521m,

                    PriceTickSize = 0.0001m,
                    PriceMinimum = 0.0m,
                    PriceMaximum = 0.0m,

                    QuoteValueMinimum = 1,
                    QuoteValueMaximum = 200000,
                };

                GlobalData.AddSymbol(symbol);
                database.Connection.Insert(symbol);
            }

            symbol.ClearCandles();
            GlobalData.AddTextToLogTab($"Cleared candles for {symbol.Name}");

            return symbol;
        }


        throw new Exception("Exchange bestaat niet");
    }

    public static TradeParams CreateTradeParams(CryptoDatabase database, DateTime createTime, CryptoOrderSide orderSide, CryptoOrderType orderType, decimal price, decimal quantity)
    {
        TradeParams tradeParams = new()
        {
            CreateTime = createTime,
            OrderSide = orderSide,
            OrderType = orderType,
            OrderId = "X" + database.CreateNewUniqueId(),
            Price = price,
            Quantity = quantity,
            QuoteQuantity = price * quantity,
        };
        return tradeParams;
    }


    public static CryptoCandle GenerateCandles(CryptoSymbol symbol, ref DateTime startTime, int count, decimal price)
    {
        CryptoCandle? candle = null;

        CandleTime startTimeUnix = CandleTime.AlignFromDateTime(startTime, 1);
        while (count > 0)
        {
            startTime = startTimeUnix.ToDateTime();
            candle = CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], startTime, price, price, price, price, 1);
            symbol.LastPrice = price;
            //CandleTools.UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);
            //string text = $"ticker(1m):" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true);
            //Console.WriteLine(text);

            //// Calculate higher timeframes
            //long candle1mCloseTime = candle.OpenTime + 1;
            //foreach (CryptoInterval interval in GlobalData.IntervalList)
            //{
            //    if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
            //    {
            //        // Deze doet een call naar de TaskSaveCandles en de UpdateCandleFetched (overlappend?)
            //        CryptoCandle candleX = CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle1mCloseTime);
            //        CandleTools.UpdateCandleFetched(symbol, interval);
            //        string text2 = $"ticker({interval.Name}):" + candleX.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true);
            //        Console.WriteLine(text2);
            //    }
            //}

            startTimeUnix += 60;
            count--;
        }

        if (candle == null)
            throw new Exception("Geen count opgegeven");
        return candle;
    }


    internal static void DeleteAllPositionRelatedStuff(CryptoDatabase database)
    {
        // Voorgaande orders en trades verwijderen
        database.Connection.Execute($"delete from [Asset]");
        database.Connection.Execute($"delete from [PositionStep]");
        database.Connection.Execute($"delete from [PositionPart]");
        database.Connection.Execute($"delete from [Position]");
        database.Connection.Execute($"delete from [Order]");
        database.Connection.Execute($"delete from [Trade]");

        GlobalData.ActiveExchange!.Data.Clear();
    }

    /// <summary>
    /// load candles from a file for testing
    /// </summary>
    public static void LoadCandleDataFromDisk(CryptoCandleList candleList, string fileName)
    {
        if (!File.Exists(fileName))
            throw new Exception($"File {fileName} not found");

        string text = File.ReadAllText(fileName);
        var list = JsonSerializer.Deserialize<CryptoCandleList>(text, JsonTools.DeSerializerOptions)
            ?? throw new Exception($"Unable to load candles from {fileName}");

        // Clear list so we not have unexpected stuff..
        candleList.Clear();

        // Add the candles
        foreach (var c in list.Values)
            candleList.TryAdd(c.OpenTime, c);

        // We expect at least 1..
        if (candleList.Count == 0)
            throw new Exception("Error loading candles");
    }

}

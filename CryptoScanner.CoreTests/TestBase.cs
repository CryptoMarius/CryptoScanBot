using CryptoScanner.Core.Context;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Text.Json;

namespace CryptoScanner.CoreTests;

public class TestBase
{
    static bool IsSetupOnce = false;



    static void SetupOnce()
    {
        if (!IsSetupOnce)
        {
            IsSetupOnce = true;

            // AppPath and AppDataFolder are guaranteed to be set by TestAssemblySetup.[AssemblyInitialize].
            GlobalData.LoadConfiguration();
            CryptoDatabase.SetDatabaseDefaults();
            GlobalData.LoadExchanges();
            GlobalData.LoadIntervals();
            GlobalData.ActiveExchange = GlobalData.ExchangeListId.Values[0];
            GlobalData.LoadSymbols();
            GlobalData.IsEmulatorMode = true;
        }
    }

    internal static void InitTestSession()
    {
        SetupOnce();
    }


    /// <summary>
    /// Register a plugin and enable all its strategies for both sides.
    /// <para>
    /// Both halves are needed and they do different things. Registration makes the plugin's declared
    /// indicators (IStrategyPlugin.RequiredIndicators) part of the hub; enabling is what makes the hub
    /// build the plugin's IIndicatorExtension, because the heavy kernels are only run for a strategy
    /// somebody actually switched on. Do only the first half and the plugin's own values stay null,
    /// the strategy never signals, and the test fails with no indication why — which is exactly what
    /// happened to the VBS stop-loss tests. PluginManager is process-static, so a test class that
    /// forgets this can still pass by accident when another class registered the plugin first.
    /// </para>
    /// </summary>
    internal static void RegisterAndEnablePlugin(IStrategyPlugin plugin)
    {
        PluginManager.Register(plugin);
        foreach (var strategy in plugin.Strategies)
            EnableStrategy(strategy.Name);
    }

    /// <summary>Register a plugin without enabling it — for tests that only need its declared indicators.</summary>
    internal static void RegisterPlugin(IStrategyPlugin plugin)
    {
        PluginManager.Register(plugin);
    }

    /// <summary>Add a strategy name to the enabled long and short lists (idempotent).</summary>
    internal static void EnableStrategy(string name)
    {
        name = name.ToLower();
        if (!GlobalData.Settings.Signal.Long.Strategy.Contains(name))
            GlobalData.Settings.Signal.Long.Strategy.Add(name);
        if (!GlobalData.Settings.Signal.Short.Strategy.Contains(name))
            GlobalData.Settings.Signal.Short.Strategy.Add(name);
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


    public static void ResetIndicatorState(CryptoSymbol symbol)
    {
        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            symbolInterval.IndicatorHub = null;
            symbolInterval.IndicatorHubLastAdded = null;
            symbolInterval.IndicatorHubAddCount = 0;
            symbolInterval.BandRange = null;
            symbolInterval.Data.Clear();
            symbolInterval.ResetTrendData();
        }
    }

    public static CryptoCandle GenerateCandles(CryptoSymbol symbol, ref DateTime startTime, int count, decimal price)
    {
        CryptoCandle candle = default;

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
            //long candle1mCloseTime = candle.OpenTime + 60;
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

            startTimeUnix += 1;
            count--;
        }

        if (candle.OpenTime == 0)
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

        // CryptoCandle is a struct that stores prices as integer ticks. The tick
        // precision depends on TickDecimals, which defaults to 0. Normal
        // deserialization would convert decimal prices using TickSize=1.0,
        // truncating everything < 1.0 to 0. Parse the raw JSON to preserve
        // the original decimal values and auto-detect TickDecimals.
        string text = File.ReadAllText(fileName);
        using JsonDocument doc = JsonDocument.Parse(text);

        candleList.Clear();

        byte tickDecimals = 0;
        foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
        {
            JsonElement e = entry.Value;
            decimal open = e.GetProperty("Open").GetDecimal();
            decimal high = e.GetProperty("High").GetDecimal();
            decimal low = e.GetProperty("Low").GetDecimal();
            decimal close = e.GetProperty("Close").GetDecimal();
            decimal volume = e.GetProperty("Volume").GetDecimal();
            uint openTime = e.GetProperty("OpenTime").GetUInt32();

            if (tickDecimals == 0 && close != 0)
                tickDecimals = DetectTickDecimals(close);

            var candle = new CryptoCandle
            {
                TickDecimals = tickDecimals,
                OpenTime = new CandleTime(openTime),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
            };
            candleList.TryAdd(candle.OpenTime, candle);
        }

        if (candleList.Count == 0)
            throw new Exception("Error loading candles");
    }

    private static byte DetectTickDecimals(decimal value)
    {
        // Count how many decimal places the value has to determine TickDecimals.
        for (int d = 0; d <= 8; d++)
        {
            decimal scaled = value * (decimal)Math.Pow(10, d);
            if (scaled == Math.Floor(scaled))
                return (byte)d;
        }
        return 8;
    }

}

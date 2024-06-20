using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBotTests;
public class TestBase
{
    static bool IsSetupOnce = false;
    static void SetupOnce()
    {
        if (!IsSetupOnce)
        {
            IsSetupOnce = true;

            // Description: toevoegen en mergen van candles (de happy flow)
            GlobalData.AppName = "CryptoScanBot.Test";
            GlobalData.LogToLogTabEvent -= AddTextToLogTab;
            GlobalData.LogToLogTabEvent -= AddTextToLogTab;
            GlobalData.LogToLogTabEvent += AddTextToLogTab;

            GlobalData.LoadSettings();
            CryptoDatabase.SetDatabaseDefaults();
            GlobalData.LoadExchanges();
            GlobalData.LoadIntervals();
            GlobalData.LoadAccounts();
            GlobalData.LoadSymbols();
            GlobalData.BackTest = true;
        }
    }

    internal static void InitTestSession()
    {
        SetupOnce();
    }

    internal static void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        text = text.TrimEnd();
        if (extraLineFeed)
            text += "\r\n";

        Console.WriteLine(text);
    }

    internal static CryptoSymbol CreateTestSymbol(CryptoDatabase database)
    {
        if (GlobalData.ExchangeListId.TryGetValue(GlobalData.Settings.General.ExchangeId, out CryptoScanBot.Core.Model.CryptoExchange? exchange))
        {
            if (!exchange.SymbolListName.TryGetValue("MASKUSDT", out CryptoSymbol? symbol))
            {
                symbol = new()
                {
                    Status = 1,
                    Base = "MASK",
                    Quote = "USDT",
                    Name = "MASKUSDT",
                    Exchange = exchange,
                    ExchangeId = exchange.Id,

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
                database.Connection.Insert<CryptoSymbol>(symbol);
            }
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

        long startTimeUnix = CandleTools.GetUnixTime(startTime, 60);
        while (count > 0)
        {
            startTime = CandleTools.GetUnixDate(startTimeUnix);
            candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], startTime, price, price, price, price, 1, false);
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

        GlobalData.ActiveAccount?.Data.Clear();
    }

}

using System.Text;

using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper;

namespace CryptoScanBot.Core.Emulator;

public class Emulator
{
    public static void DeletePreviousData()
    {
        StringBuilder b = new();
        using var database = new CryptoDatabase();
        database.Open();


        using var transaction = database.BeginTransaction();

        // clean signals
        b.Clear();
        b.AppendLine("delete from signal where BackTest=1");
        database.Connection.Execute(b.ToString(), transaction);


        // clean positionsteps
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 1/3");
        b.AppendLine("delete from positionStep where positionstep.PositionId in");
        b.AppendLine("(select position.id from position");
        b.AppendLine("inner join TradeAccount on position.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0)");
        database.Connection.Execute(b.ToString(), transaction);


        // clean positionparts
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 2/3");
        b.AppendLine("delete from positionPart where positionPart.PositionId in");
        b.AppendLine("(select position.id from position");
        b.AppendLine("inner join TradeAccount on position.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0)");
        database.Connection.Execute(b.ToString(), transaction);


        // clean positions
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 3/3");
        b.AppendLine("delete from position where position.Id in");
        b.AppendLine("(select position.id from position");
        b.AppendLine("inner join TradeAccount on position.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0)");
        database.Connection.Execute(b.ToString(), transaction);


        // clean orders
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting orders");
        b.AppendLine("delete from [order] where [order].Id in");
        b.AppendLine("(select [order].id from [order]");
        b.AppendLine("inner join TradeAccount on [order].TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0)");
        database.Connection.Execute(b.ToString(), transaction);


        // clean trades
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting trades");
        b.AppendLine("delete from [trade] where [trade].Id in");
        b.AppendLine("(select [trade].id from [trade]");
        b.AppendLine("inner join TradeAccount on [trade].TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0)");
        database.Connection.Execute(b.ToString(), transaction);


        // clean assets
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 3/3");
        b.AppendLine("delete from asset where asset.Id in");
        b.AppendLine("(select asset.id from asset");
        b.AppendLine("inner join TradeAccount on asset.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0)");
        database.Connection.Execute(b.ToString(), transaction);

        transaction.Commit();
    }

    public static async Task Execute(CryptoSymbol btcSymbol, CryptoSymbol symbol)
    {
        try
        {
            if (GlobalData.ActiveAccount!.AccountType != CryptoAccountType.BackTest)
                return;


            GlobalData.AddTextToLogTab($"Emulator {symbol.Name} started");

            // Emulator date boundaries
            DateTime start = GlobalData.Settings.BackTest.BackTestStartTime;
            DateTime end = GlobalData.Settings.BackTest.BackTestEndTime;


            // Todo: Restore afterwards!
            symbol.LastTradeDate = null;
            GlobalData.BackTestCandle = null;
            GlobalData.BackTestDateTime = start;
            GlobalData.ActiveAccount!.Data.Clear();


            // Load the historic candles
            await LoadHistoricalData.Execute(btcSymbol, start, end); // for the trading rules ?
            await LoadHistoricalData.Execute(symbol, start, end);


            // Calculate the indicators for all intervals
            foreach (var interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                CandleIndicatorData.CalculateIndicators([.. symbolInterval.CandleList.Values], symbolInterval.CandleList.Count);
            }

            bool exec = true;

            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            CryptoSymbolInterval symbolPeriod1Hour = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h);
            foreach (CryptoCandle candle in symbolPeriod.CandleList.Values.ToList())
            {
                if (candle.Date < start)
                    continue;
                if (candle.Date > end)
                    break;

                if (candle.Date.Minute == 0 && candle.Date.Hour % 4 == 0)
                {
                    // show some progress (quite minimal, but voila)
                    GlobalData.AddTextToLogTab($"Emulator execute {candle.Date}");
                }

                //string t = $"Emulator execute {candle.Date}";
                //GlobalData.AddTextToLogTab($"Emulator execute {candle.Date}");
                //if (t.Equals("Emulator execute 2024-05-01 00:36:00"))
                //    t = t;


                // This is the last know price
                symbol.LastPrice = candle.Close;
                symbol.AskPrice = candle.Close;
                symbol.BidPrice = candle.Close;

                GlobalData.BackTestCandle = candle;
                GlobalData.BackTestDateTime = CandleTools.GetUnixDate(candle.OpenTime + 60);

                // Calculate barometer
                if (exec)
                {
                    BarometerTools barometerTools = new();
                    barometerTools.CalculatePriceBarometerForQuote(symbol.QuoteData);
                    exec = false;
                }

                // Pickup the right volume of the symbol (its kind of an estimate from the last 24 hours)
                // This can be made a litter bit quicker, but this wont hurt the cpu that much I assume...
                int count = 24;
                decimal volume = 0;
                long unix = IntervalTools.StartOfIntervalCandle2(candle.OpenTime, 60, symbolPeriod1Hour.Interval.Duration);
                while (count-- > 0)
                {
                    if (symbolPeriod1Hour.CandleList.TryGetValue(unix, out CryptoCandle? candleHour))
                        volume += candleHour.Volume;
                    unix -= symbolPeriod1Hour.Interval.Duration;
                }
                symbol.Volume = volume;

                PositionMonitor positionMonitor = new(GlobalData.ActiveAccount!, symbol, candle);
                await positionMonitor.NewCandleArrivedAsync();

            }
            GlobalData.BackTestCandle = null;

            // report something?
            GlobalData.AddTextToLogTab($"Emulator {symbol.Name} completed");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("Emulator ERROR " + error.ToString());
        }
    }
}

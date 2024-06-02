using System.Text;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper;

namespace ExchangeTest.Emulator;

public class Emulator
{
    private static void DeletePreviousData()
    {
        StringBuilder b = new();
        using var database = new CryptoDatabase();


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
        b.AppendLine("where TradeAccount.TradeAccountType = 0");
        database.Connection.Execute(b.ToString(), transaction);


        // clean positionparts
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 2/3");
        b.AppendLine("delete from positionPart where positionPart.PositionId in");
        b.AppendLine("(select position.id from position");
        b.AppendLine("inner join TradeAccount on position.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0");
        b.AppendLine(")");
        database.Connection.Execute(b.ToString(), transaction);


        // clean positions
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 3/3");
        b.AppendLine("delete from position where position.Id in");
        b.AppendLine("(select position.id from position");
        b.AppendLine("inner join TradeAccount on position.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0");
        b.AppendLine(")");
        database.Connection.Execute(b.ToString(), transaction);


        // clean orders
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting orders");
        b.AppendLine("delete from [order] where [order].Id in");
        b.AppendLine("(select [order].id from [order]");
        b.AppendLine("inner join TradeAccount on [order].TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0");
        b.AppendLine(")");
        database.Connection.Execute(b.ToString(), transaction);


        // clean trades
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting trades");
        b.AppendLine("delete from [trade] where [trade].Id in");
        b.AppendLine("(select [trade].id from [trade]");
        b.AppendLine("inner join TradeAccount on [trade].TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0");
        b.AppendLine(")");
        database.Connection.Execute(b.ToString(), transaction);


        // clean assets
        b.Clear();
        GlobalData.AddTextToLogTab("Deleting positions 3/3");
        b.AppendLine("delete from asset where asset.Id in");
        b.AppendLine("(select asset.id from asset");
        b.AppendLine("inner join TradeAccount on asset.TradeAccountId = TradeAccount.Id");
        b.AppendLine("where TradeAccount.TradeAccountType = 0");
        b.AppendLine(")");
        database.Connection.Execute(b.ToString(), transaction);

        transaction.Commit();
    }

    public static async Task Execute(CryptoSymbol btcSymbol, CryptoSymbol symbol)
    {
        GlobalData.AddTextToLogTab($"Emulator {symbol.Name} started");

        // (done) Adjust GetLuxIndicator (only uses the last 30 candles, needs start and enddate)
        // (todo) rename TradingConfig to Config (not the Trader prefix because it is confusing)
        // (done) The trend calculation is really really slow! Takes hours more if activated!!!

        DeletePreviousData();

        // Emulator boundaries
        DateTime start = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = DateTime.UtcNow; // new(2025, 1, 1);


        // TODO: some kind of barometer calculation? (for now just a reset)
        foreach (var interval in GlobalData.IntervalList)
        {
            BarometerData barometerData = symbol.QuoteData.BarometerList[interval.IntervalPeriod];
            barometerData.PriceBarometer = 0.0m;
            barometerData.VolumeBarometer = 0.0m;
        }

        await LoadHistoricalData.Execute(btcSymbol, start, end); // for the trading rules ?
        await LoadHistoricalData.Execute(symbol, start, end);

        
        // calculate the indicators for all intervals
        foreach (var interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            CandleIndicatorData.CalculateIndicators(symbolPeriod.CandleList.Values.ToList(), symbolPeriod.CandleList.Count);
        }



        // slowest (interesting though)
        {
            //int showProgress = 0;
            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            foreach (CryptoCandle candle in symbolPeriod.CandleList.Values)
            {
                if (candle.Date >= start)
                {
                    //if (showProgress <= 0)
                    //{
                    //    showProgress = 1220;
                    //    GlobalData.AddTextToLogTab($"Emulator execute {candle.Date}");
                    //}

                    // This is the last know price
                    symbol.LastPrice = candle.Close;
                    symbol.AskPrice = candle.Close;
                    symbol.BidPrice = candle.Close;

                    PositionMonitor positionMonitor = new(symbol, candle);
                    await positionMonitor.NewCandleArrivedAsync();
                    //showProgress--;
                }
            }
        }

        // report something?
        GlobalData.AddTextToLogTab($"Emulator {symbol.Name} completed");
    }
}

using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace ExchangeTest.Emulator;

public class Emulator
{
    private static void DeletePreviousData()
    {
        /*
        // clean previous signals from the emulator (db) "delete from signal where BackTest = 1"
        delete from signal (ja, voorlopig allemaal, wellicht nog een veld toevoegen?)

        // clean previous assets from the emulator (db)
        delete from asset where asset.Id in
        (select asset.id from asset
        inner join TradeAccount on asset.TradeAccountId = TradeAccount.Id
        where TradeAccount.TradeAccountType = 0
        )

        // clean previous positions from the emulator (db) "delete from positionStep where BackTest = 1"?
        delete from positionStep where positionstep.PositionId in
        (select position.id from position
        inner join TradeAccount on position.TradeAccountId = TradeAccount.Id
        where TradeAccount.TradeAccountType = 0
        )

        delete from positionPart where positionPart.PositionId in
        (select position.id from position
        inner join TradeAccount on position.TradeAccountId = TradeAccount.Id
        where TradeAccount.TradeAccountType = 0
        )

        delete from position where position.Id in
        (select position.id from position
        inner join TradeAccount on position.TradeAccountId = TradeAccount.Id
        where TradeAccount.TradeAccountType = 0
        )
        */
    }

    public static async Task Execute(CryptoSymbol btcSymbol, CryptoSymbol symbol)
    {
        GlobalData.AddTextToLogTab($"Emulator {symbol.Name} started");

        // todo:
        // Adjust GetLuxIndicator (only uses the last 30 candles, needs start and enddate)
        // rename TradingConfig to Config (not the Trader prefix because it is confusing)
        // todo: The trend calculation is really really slow! Takes hours more if activated!!!
        // (gebruikt ook het volledige aantal candles, 

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

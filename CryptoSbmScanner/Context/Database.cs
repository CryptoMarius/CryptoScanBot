using System.Data;
using System.Globalization;
using System.Text;

using CryptoSbmScanner.Model;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

namespace CryptoSbmScanner.Context;


// SqlConnection is sealed, dus dan maar via een compositie
// De SqliteConnection is niet sealed (die gebruiken we ook)

public class CryptoDatabase : IDisposable
{
    public static string BasePath { get; set; }
    public SqliteConnection Connection { get; set; }

    public CryptoDatabase()
    {
        Connection = new("Filename=" + BasePath + "CryptoSbmScanner.db;Mode=ReadWriteCreate");
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

    public void Open()
    {
        Connection.Open();
    }

    public void Close()
    {
        Connection.Close();
    }

    public SqliteTransaction BeginTransaction()
    {
        return Connection.BeginTransaction();
    }


    public void BulkInsertSymbol(List<CryptoSymbol> cache, SqliteTransaction transaction)
	{
	}

    public void BulkInsertTrades(CryptoSymbol symbol, List<CryptoTrade> cache, SqliteTransaction transaction)
    {
	}

    public void BulkInsertCandles(List<CryptoCandle> cache, SqliteTransaction transaction)
    {
	}

    private void CreateTableInterval(CryptoDatabase connection)
    {
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Interval';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [Interval] (" +
                "Id integer primary key autoincrement not null, " +
                "IntervalPeriod tinyint NOT NULL," +
                "Name nvarchar(100) NOT NULL," +
                "Duration int NOT NULL," +
                "ConstructFromId int NULL," +
                "FOREIGN KEY(ConstructFromId) REFERENCES Interval(Id)" +
            ")");
            Connection.Execute("CREATE INDEX IntervalId ON Interval(Id)");
            Connection.Execute("CREATE INDEX IntervalName ON Interval(Name)");
            Connection.Execute("CREATE INDEX IntervalConstructFromId ON Interval(ConstructFromId)");


            using var transaction = connection.BeginTransaction();

            // De intervallen moeten aanwezig zijn na initialisatie
            List<CryptoInterval> IntervalList = new()
            {
                CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1m, "1m", 1 * 60, null) //0
            };
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2m, "2m", 2 * 60, IntervalList[0])); //1
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3m, "3m", 3 * 60, IntervalList[0])); //2
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval5m, "5m", 5 * 60, IntervalList[0])); //3
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval10m, "10m", 10 * 60, IntervalList[3])); //4
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval15m, "15m", 15 * 60, IntervalList[3]));  //5
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval30m, "30m", 30 * 60, IntervalList[5])); //6
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1h, "1h", 01 * 60 * 60, IntervalList[6])); //7
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2h, "2h", 02 * 60 * 60, IntervalList[7])); //8
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3h, "3h", 03 * 60 * 60, IntervalList[7])); //9
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval4h, "4h", 04 * 60 * 60, IntervalList[8])); //10
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval6h, "6h", 06 * 60 * 60, IntervalList[9])); //11
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval8h, "8h", 08 * 60 * 60, IntervalList[10])); //12
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval12h, "12h", 12 * 60 * 60, IntervalList[10])); //13
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1d, "1d", 1 * 24 * 60 * 60, IntervalList[11])); //14

            // iets teveel, niet relevant voor deze tool
            //IntervalList.Add(new CryptoInterval(CryptoIntervalPeriod.interval3d, "3d", 3 * 24 * 60 * 60, IntervalList[12])); //13
            //IntervalList.Add(new Interval(IntervalPeriod.interval1w, "1w", 7 * 24 * 60 * 60, IntervalList[12], (decimal)4)); //14
            //IntervalList.Add(new Interval(IntervalPeriod.interval2w, "2w", 14 * 24 * 60 * 60, IntervalList[14], (decimal)4)); //15

            foreach (CryptoInterval interval in IntervalList)
            {
                Connection.Insert(interval, transaction);

                // bijwerken
                foreach (CryptoInterval interval2 in IntervalList)
                {
                    if (interval2.ConstructFrom != null)
                        interval2.ConstructFromId = interval2.ConstructFrom.Id;
                }
            }
            transaction.Commit();

        }
    }

    private void CreateTableExchange(CryptoDatabase connection)
    {
        // ****************************************************
        // Exchange (voorlopig alleen Binance, daar is nu alels op afgestemd)
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Exchange';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [Exchange] (" +
                 "Id integer primary key autoincrement not null, " +
                 "Name nvarchar(100) not NULL" +
            ")");
            Connection.Execute("CREATE INDEX ExchangeId ON Exchange(Id)");
            Connection.Execute("CREATE INDEX ExchangeName ON Exchange(Name)");


            // De exchanges moeten aanwezig zijn na initialisatie
            using var transaction = connection.BeginTransaction();
            Model.CryptoExchange exchange = new()
            {
                Name = "Binance"
            };
            Connection.Insert(exchange, transaction);
            transaction.Commit();

            //GlobalData.AddExchange(exchange);
        }
    }

    private void CreateTableSymbol(CryptoDatabase connection)
    {
        // ****************************************************
        // Symbol (wat muntjes van Binance met aardig wat attributen)
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Symbol';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [Symbol] (" +
                "Id INTEGER primary key autoincrement not null, " +
                "ExchangeId INTEGER NOT NULL, " +
                "Name TEXT NOT NULL," +
                "Base TEXT NOT NULL," +
                "Quote TEXT NOT NULL," +
                "Status integer NOT NULL," +
                "Volume TEXT NULL," +
                "BaseAssetPrecision integer NULL," +
                "QuoteAssetPrecision integer NULL," +
                "MinNotional TEXT NULL," +
                "PriceMinimum TEXT NULL," +
                "PriceMaximum TEXT NULL," +
                "PriceTickSize TEXT NULL," +
                "QuantityMinimum TEXT NULL," +
                "QuantityMaximum TEXT NULL," +
                "QuantityTickSize TEXT NULL," +
                "LastOrderfetched TEXT NULL," +
                "LastTradefetched TEXT NULL," +
                "IsSpotTradingAllowed INTEGER NULL," +
                "IsMarginTradingAllowed INTEGER NULL," +
                "LastPrice TEXT NULL," +
                "TrendInfoDate TEXT NULL," +
                "TrendPercentage TEXT NULL," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
            ")");
            Connection.Execute("CREATE INDEX SymbolId ON Symbol(Id)");
            Connection.Execute("CREATE INDEX SymbolExchangeId ON Symbol(ExchangeId)");
            Connection.Execute("CREATE INDEX SymbolName ON Symbol(Name)");
            Connection.Execute("CREATE INDEX SymbolBase ON Symbol(Base)");
            Connection.Execute("CREATE INDEX SymbolQuote ON Symbol(Quote)");
            //Connection.Execute("CREATE INDEX SymbolVolume ON Symbol(Volume)");

            // We hebben niets ingelezen hebben bij het opstarten
            //using (var transaction = connection.BeginTransaction())
            //{
            //    foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
            //    {
            //        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            //        {
            //            connection.Insert(symbol, transaction);
            //        }
            //    }
            //    transaction.Commit();
            //}
        }
    }

    private void CreateTableSymbolInterval(CryptoDatabase connection)
    {
        // ****************************************************
        // SymbolInterval (administratie, maar overlapt met de bestanden, via bestand is beter denk ik, rest is overkill)
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'SymbolInterval';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [SymbolInterval] (" +
                "Id integer primary key autoincrement not null, " +
                "ExchangeId Integer NOT NULL, " +
                "SymbolId Integer NOT NULL, " +
                "IntervalId Integer NOT NULL, " +
                "TrendInfoDate TEXT NULL," +
                "TrendIndicator Integer NULL," +
                "LastCandleSynchronized TEXT NULL," + // overlapt
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")");
            Connection.Execute("CREATE INDEX SymbolIntervalId ON SymbolInterval(Id)");
            Connection.Execute("CREATE INDEX SymbolIntervalExchangeId ON SymbolInterval(ExchangeId)");
            Connection.Execute("CREATE INDEX SymbolIntervalSymbolId ON SymbolInterval(SymbolId)");
            Connection.Execute("CREATE INDEX SymbolIntervalIntervalId ON SymbolInterval(IntervalId)");
        }
    }


    private void CreateTableSignal(CryptoDatabase connection)
    {
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Signal';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [Signal] (" +
                "Id integer primary key autoincrement not null, " +
                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NULL," +

                "BackTest INTEGER NULL," +
                "IsInvalid INTEGER NULL," +

                "EventTime bigint NOT NULL," +
                "Mode INTEGER NOT NULL," +
                "Price TEXT NOT NULL," +
                "EventText nvarchar(100) NULL," +

                "Last24HoursChange TEXT NULL," +
                "Last48Hours TEXT NULL," +
                "Last24HoursEffective TEXT NULL," +
                "Volume TEXT NULL," +
                "OpenDate TEXT NULL," +
                "CloseDate TEXT NULL," +
                "ExpirationDate TEXT NULL," +

                "TrendIndicator INTEGER NULL," +
                "TrendPercentage TEXT NULL," +

                "infodate TEXT NULL," +
                "BarcodePercentage TEXT NULL," +
                "Strategy INTEGER NULL," +
                "CandlesWithZeroVolume INTEGER NULL," +
                "CandlesWithFlatPrice INTEGER NULL," +
                "AboveBollingerBandsSma INTEGER NULL," +
                "AboveBollingerBandsUpper INTEGER NULL," +

                "StochSignal TEXT NULL," +
                "StochOscillator TEXT NULL," +
                //"BollingerBandsLowerBand TEXT NULL," +
                //"BollingerBandsUpperBand TEXT NULL," +
                "BollingerBandsDeviation TEXT NULL," +
                "BollingerBandsPercentage TEXT NULL," +

                "KeltnerLowerBand TEXT NULL," +
                "KeltnerUpperBand TEXT NULL," +

                "Rsi TEXT NULL," +
                "SlopeRsi TEXT NULL," +
                "Psar TEXT NULL," +
                "Psar2 TEXT NULL," +

                "sma20 TEXT NULL," +
                "ema50 TEXT NULL," +
                "ema100 TEXT NULL," +
                "ema200 TEXT NULL," +
                "SlopeEma20 TEXT NULL," +
                "SlopeEma50 TEXT NULL," +

                "ema20 TEXT NULL," +
                "sma200 TEXT NULL," +
                "sma100 TEXT NULL," +
                "sma50 TEXT NULL," +
                "SlopeSma50 TEXT NULL," +
                "SlopeSma20 TEXT NULL," +

                "FluxIndicator5m TEXT NULL," +
                

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")");
            Connection.Execute("CREATE INDEX SignalId ON Signal(Id)");
            Connection.Execute("CREATE INDEX SignalExchangeId ON Signal(ExchangeId)");
            Connection.Execute("CREATE INDEX SignalSymbolId ON Signal(SymbolId)");
            Connection.Execute("CREATE INDEX SignalIntervalId ON Signal(IntervalId)");
        }
    }


    private void CreateTablePosition(CryptoDatabase connection)
    {
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Position';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [Position] (" +
                "Id integer primary key autoincrement not null, " +
                "CreateTime TEXT NOT NULL, " +
                "CloseTime TEXT NULL, " +

                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL, " +
                "IntervalId Integer NULL," +
                "SignalId Integer NULL," +
                "Status INTEGER NULL, " +
                "PaperTrade INTEGER NOT NULL, " +

                "BuyPrice TEXT NOT NULL, " +
                "Quantity TEXT NULL, " +
                "BuyAmount TEXT NULL, " +
                "SellPrice TEXT NULL, " +
                "Profit TEXT NULL, " +
                "BreakEvenPrice TEXT NULL, " +

                "Invested TEXT NULL, " +
                "Commission TEXTNULL, " +
                "Returned TEXT NULL, " +
                "Percentage TEXT NULL, " +
                "data TEXT NULL," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)," +
                "FOREIGN KEY(SignalId) REFERENCES Signal(Id)" +
            ")");
            Connection.Execute("CREATE INDEX PositionId ON Position(Id)");
            Connection.Execute("CREATE INDEX PositionExchangeId ON Position(ExchangeId)");
            Connection.Execute("CREATE INDEX PositionSymbolId ON Position(SymbolId)");
            Connection.Execute("CREATE INDEX PositionSignalId ON Position(SignalId)");
            Connection.Execute("CREATE INDEX PositionCreateTime ON Position(CreateTime)");
            Connection.Execute("CREATE INDEX PositionCloseTime ON Position(CloseTime)");
        }

    }

    private void CreateTablePositionStep(CryptoDatabase connection)
    {
        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'PositionStep';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [PositionStep] (" +
                "Id integer primary key autoincrement not null, " +
                "PositionId integer NOT NULL," +
                "CreateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +
                "Name TEXT NOT NULL," +
                "Status INTEGER NOT NULL," +
                "IsBuy INTEGER NOT NULL," +
                "Price TEXT NOT NULL," +
                "StopPrice TEXT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuantityFilled TEXT NOT NULL," +
                "QuoteQuantityFilled TEXT NOT NULL," +
                "OrderId TEXT NOT NULL," +
                "Order2Id TEXT NULL," +
                "OrderListId TEXT NULL," +
                "StopLimitPrice TEXT NULL," +
                    "FOREIGN KEY(PositionId) REFERENCES Position(Id)" +
            ")");
            Connection.Execute("CREATE INDEX PositionStepId ON Position(Id)");
            Connection.Execute("CREATE INDEX PositionStepPositionId ON PositionStep(PositionId)");
            Connection.Execute("CREATE INDEX PositionStepCreateTime ON PositionStep(CreateTime)");
            Connection.Execute("CREATE INDEX PositionStepCloseTime ON PositionStep(CloseTime)");
        }

    }

    private void CreateTableOrder(CryptoDatabase connection)
    {
        // ****************************************************
        // Order (de order zoals Binance geplaatst heeft <kan geannuleerd zijn>)
        // Besloten dat momenteel alleen trades van belang zijn

        //string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Order';").FirstOrDefault();
        //if (string.IsNullOrEmpty(tableName))
        //{
        //    Connection.Execute("CREATE TABLE [Order] (" +
        //        "Id integer primary key autoincrement not null, " +
        //        "ExchangeId integer NOT NULL," +
        //        "SymbolId integer NOT NULL," +
        //        "CreateTime TEXT NOT NULL," +
        //        "IcebergQuantity TEXT NOT NULL," +
        //        "StopPrice TEXT NOT NULL," +
        //        "Side integer NULL," +
        //        "Type integer NULL," +
        //        "TimeInForce integer NULL," +
        //        "Status integer NULL," +
        //        "QuoteQuantity TEXT NOT NULL," +
        //        "QuoteQuantityFilled TEXT NOT NULL," +
        //        "QuantityFilled TEXT NOT NULL," +
        //        "Quantity TEXT NOT NULL," +
        //        "Price TEXT NOT NULL," +
        //        "OriginalClientOrderId TEXT NULL," +
        //        "OrderListId TEXT NOT NULL," +
        //        "ClientOrderId TEXT NULL," +
        //        "OrderId TEXT NOT NULL," +
        //        "UpdateTime TEXT NOT NULL," +
        //        "IsWorking INTEGER NOT NULL," +
        //        "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
        //        "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
        //    ")");
        //    Connection.Execute("CREATE INDEX OrderId ON [Order](Id)");
        //    Connection.Execute("CREATE INDEX OrderExchangeId ON [Order](ExchangeId)");
        //    Connection.Execute("CREATE INDEX OrderSymbolId ON [Order](SymbolId)");
        //    Connection.Execute("CREATE INDEX OrderCreateTime ON [Order](CreateTime)");
        //    Connection.Execute("CREATE INDEX OrderUpdateTime ON [Order](UpdateTime)");
        //}
    }

    private void CreateTableTrade(CryptoDatabase connection)
    {
        // ****************************************************
        // Trade (de trade zoals Binance die uitgevoerd heeft)

        string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Trade';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            Connection.Execute("CREATE TABLE [Trade] (" +
                "Id integer primary key autoincrement not null, " +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +
                "TradeId TEXT NOT NULL," +
                "OrderId TEXT NOT NULL," +
                "OrderListId TEXT NULL," +
                "Price TEXT NOT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuoteQuantity TEXT NOT NULL," +
                "Commission TEXT NOT NULL," +
                "CommissionAsset TEXT NOT NULL," +
                "TradeTime TEXT NOT NULL," +
                "IsBuyer Integer NOT NULL," +
                "IsMaker Integer NOT NULL," +
                "IsBestMatch Integer NOT NULL," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            Connection.Execute("CREATE INDEX TradeId ON [Trade](Id)");
            Connection.Execute("CREATE INDEX TradeExchangeId ON [Trade](ExchangeId)");
            Connection.Execute("CREATE INDEX TradeSymbolId ON [Trade](SymbolId)");
            Connection.Execute("CREATE INDEX TradeTradeTime ON [Trade](TradeTime)");
        }

    }

    private void CreateTableBalancing(CryptoDatabase connection)
    {
        //// ****************************************************
        //// Balance (echt? weet niet waarom we dit op deze manier opslaan, balanceren doe je binnen groep, die mis ik, een oude versie wellicht?)
        //string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Balance';").FirstOrDefault();
        //if (string.IsNullOrEmpty(tableName))
        //{
        //    Connection.Execute("CREATE TABLE [Balance] (" +
        //        "Id integer primary key autoincrement not null, " +
        //        "ExchangeId Integer NOT NULL," +
        //        "SymbolId Integer NOT NULL," +
        //        "EventTime TEXT NOT NULL," +
        //        "Name TEXT NOT NULL," +
        //        "Price TEXT NOT NULL," +
        //        "Quantity TEXT NOT NULL," +
        //        "QuoteQuantity TEXT NOT NULL," +
        //        "InvestedQuantity TEXT NULL," +
        //        "InvestedValue TEXT NULL," +
        //        "UsdtValue TEXT NULL," +
        //        "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
        //        "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
        //    ")");
        //    Connection.Execute("CREATE INDEX BalanceId ON [Balance](Id)");
        //    Connection.Execute("CREATE INDEX BalanceExchangeId ON [Balance](ExchangeId)");
        //    Connection.Execute("CREATE INDEX BalanceSymbolId ON [Balance](SymbolId)");
        //    Connection.Execute("CREATE INDEX BalanceEventTime ON [Balance](EventTime)");
        //}
    }

    public void CreateDatabase()
    {
        // Sqlite gaat afwijkend met datatypes om, zie https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types

        using var connection = new CryptoDatabase();
        connection.Open();

        CreateTableInterval(connection); // (met een hardcoded lijst, voorlopig prima)
        CreateTableExchange(connection); // (met een hardcoded lijst, voorlopig prima)

        CreateTableSymbol(connection);
        CreateTableSymbolInterval(connection);
        CreateTableSignal(connection);
        
        CreateTablePosition(connection);
        CreateTablePositionStep(connection);
        
        CreateTableOrder(connection);
        CreateTableTrade(connection);

        CreateTableBalancing(connection);
    }

}
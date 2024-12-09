using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

namespace CryptoScanBot.Core.Context;

// SqlConnection is sealed, dus dan maar via een compositie
// De SqliteConnection is niet sealed (die gebruiken we ook)

public class CryptoDatabase : IDisposable
{
    public static void SetDatabaseDefaults()
    {
        SqlMapper.Settings.CommandTimeout = 180;

        basePath = GlobalData.GetBaseDir();
        CreateDatabase();
    }


    private static string? basePath;
    public SqliteConnection Connection { get; set; }

    public CryptoDatabase()
    {
        Connection = new("Filename=" + basePath + GlobalData.AppName + ".db;Mode=ReadWriteCreate;"); //Pooling=True;default)
    }

    public SqliteTransaction BeginTransaction()
    {
        return Connection.BeginTransaction();
    }

    public void Dispose()
    {
        //Dispose(true);
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Open()
    {
        Connection.Open();
    }

    public void Close()
    {
        Connection.Close();
    }

    private static bool MissingTable(CryptoDatabase connection, string tableName)
    {
        string sql = $"SELECT name FROM sqlite_master WHERE type='table' AND name = '{tableName}';";
        return string.IsNullOrEmpty(connection.Connection.Query<string>(sql).FirstOrDefault());
    }


    private static void CreateTableVersion(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Version"))
        {
            connection.Connection.Execute("CREATE TABLE [Version] (" +
                "Id integer primary key autoincrement not null," +
                "Version INTEGER NOT NULL" +
            ")");

            // De exchanges moeten aanwezig zijn na initialisatie
            using var transaction = connection.Connection.BeginTransaction();
            CryptoVersion databaseVersion = new()
            {
                Version = Migration.CurrentDatabaseVersion,
            };
            connection.Connection.Insert(databaseVersion, transaction);
            transaction.Commit();
        }
    }

    private static void CreateTableSequence(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Sequence"))
        {
            connection.Connection.Execute("CREATE TABLE [Sequence] (" +
                "Id integer primary key autoincrement not null," +
                "Name TEXT NOT NULL" +
            ")");

        }
    }


    private static void CreateTableInterval(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Interval"))
        {
            connection.Connection.Execute("CREATE TABLE [Interval] (" +
                "Id integer primary key autoincrement not null," +
                "IntervalPeriod INTEGER NOT NULL," +
                "Name TEXT NOT NULL," +
                "Duration INTEGER NOT NULL," +
                "ConstructFromId INTEGER NULL," +
                "FOREIGN KEY(ConstructFromId) REFERENCES Interval(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxIntervalId ON Interval(Id)");
            connection.Connection.Execute("CREATE INDEX IdxIntervalName ON Interval(Name)");
            connection.Connection.Execute("CREATE INDEX IdxIntervalConstructFromId ON Interval(ConstructFromId)");


            using var transaction = connection.BeginTransaction();

            // De intervallen moeten aanwezig zijn na initialisatie
            List<CryptoInterval> IntervalList = [];
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1m, "1m", 1 * 60, null)); //0
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
                connection.Connection.Insert(interval, transaction);

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

    private static List<Model.CryptoExchange> CreateExchangeList()
    {
        return
        [
            new() { Name = "Binance Spot", FeeRate = 0.1m, IsSupported = true, ExchangeType = CryptoExchangeType.Binance, TradingType=CryptoTradingType.Spot },
            new() { Name = "Binance Futures", FeeRate = 0.0m, IsSupported = true, ExchangeType = CryptoExchangeType.Binance, TradingType=CryptoTradingType.Futures},

            new() { Name = "Bybit Spot", FeeRate = 0.15m, IsSupported = true, ExchangeType = CryptoExchangeType.Bybit, TradingType=CryptoTradingType.Spot },
            new() { Name = "Bybit Futures", FeeRate = 0.0m, IsSupported = true, ExchangeType = CryptoExchangeType.Bybit, TradingType=CryptoTradingType.Futures },

            new() { Name = "Kucoin Spot", FeeRate = 0.1m, IsSupported = true, ExchangeType = CryptoExchangeType.Kucoin, TradingType=CryptoTradingType.Spot },
            // not supported (meeds additional work)
            new() { Name = "Kucoin Futures", FeeRate = 0.0m, IsSupported = false, ExchangeType = CryptoExchangeType.Kucoin, TradingType=CryptoTradingType.Futures },

            // not supported (kline streams not working properly)
            new() { Name = "Kraken Spot", FeeRate = 0.1m, IsSupported = false, ExchangeType = CryptoExchangeType.Kraken, TradingType=CryptoTradingType.Spot },

            new() { Name = "Mexc Spot", FeeRate = 0.1m, IsSupported = true, ExchangeType = CryptoExchangeType.Mexc, TradingType=CryptoTradingType.Spot },
            // Er is nog geen nuget library die deze ondersteund
            //new() { Name = "Mexc Futures", FeeRate = 0.1m, IsSupported = false, ExchangeType = CryptoExchangeType.Mexc, TradingType=CryptoTradingType.Futures },
        ];
    }

    private static void CreateTableExchange(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Exchange"))
        {
            connection.Connection.Execute("CREATE TABLE [Exchange] (" +
                 "Id integer primary key autoincrement not null," +
                 "LastTimeFetched TEXT NULL," +
                 "IsSupported Integer not NULL," +
                 "Name TEXT not NULL," +
                 "FeeRate TEXT not NULL," +
                 "ExchangeType Integer not NULL," +
                 "TradingType Integer not NULL" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxExchangeId ON Exchange(Id)");
            connection.Connection.Execute("CREATE INDEX IdxExchangeName ON Exchange(Name)");


            // De ondersteunde exchanges toevoegen
            // NB: In de code wordt aannames van de ID gedaan dus gaarne niet knutselen met volgorde
            using var transaction = connection.Connection.BeginTransaction();

            foreach (var exchange in CreateExchangeList())
                connection.Connection.Insert(exchange, transaction);

            transaction.Commit();
        }
    }

    private static void CreateTableTradeAccount(CryptoDatabase connection)
    {
        if (MissingTable(connection, "TradeAccount"))
        {
            connection.Connection.Execute("CREATE TABLE [TradeAccount] (" +
                "Id integer primary key autoincrement not null," +
                "ExchangeId INTEGER NOT NULL," +
                "AccountType Integer not NULL," +
                "CanTrade INTEGER CanTrade not NULL," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxTradeAccountId ON TradeAccount(Id)");
            connection.Connection.Execute("CREATE INDEX IdxTradeAccountExchangeId ON TradeAccount(ExchangeId)");


            // De exchanges moeten aanwezig zijn na initialisatie
            using var transaction = connection.Connection.BeginTransaction();

            foreach (var exchange in CreateExchangeList())
            {
                foreach (CryptoAccountType accountType in Enum.GetValues(typeof(CryptoAccountType)))
                {
                    CryptoAccount tradeAccount = new()
                    {
                        ExchangeId = exchange.Id,
                        Exchange = exchange,
                        CanTrade = exchange.IsSupported,
                        AccountType = accountType,
                    };

                    // Only Bybit has been tested sofar
                    if (exchange.ExchangeType != CryptoExchangeType.Bybit && tradeAccount.AccountType == CryptoAccountType.RealTrading)
                        tradeAccount.CanTrade = false;

                    connection.Connection.Insert(tradeAccount, transaction);
                }
            }
            transaction.Commit();
        }
    }

    private static void CreateTableSymbol(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Symbol"))
        {
            connection.Connection.Execute("CREATE TABLE [Symbol] (" +
                "Id INTEGER primary key autoincrement not null," +
                "ExchangeId INTEGER NOT NULL," +
                "Name TEXT NOT NULL," +
                "Base TEXT NOT NULL," +
                "Quote TEXT NOT NULL," +
                "Status INTEGER NOT NULL," +
                "Volume TEXT NULL," +

                "PriceMinimum TEXT NULL," +
                "PriceMaximum TEXT NULL," +
                "PriceTickSize TEXT NULL," +

                "QuantityMinimum TEXT NULL," +
                "QuantityMaximum TEXT NULL," +
                "QuantityTickSize TEXT NULL," +

                "QuoteValueMinimum TEXT NULL," +
                "QuoteValueMaximum TEXT NULL," +

                "LastTradeFetched TEXT NULL," +
                "LastTradeIdFetched TEXT NULL," +
                "LastOrderFetched TEXT NULL," +

                "IsSpotTradingAllowed INTEGER NULL," +
                "IsMarginTradingAllowed INTEGER NULL," +
                //"LastPrice TEXT NULL," +
                "TrendInfoDate TEXT NULL," +
                "TrendPercentage TEXT NULL," +
                "LastTradeDate TEXT NULL," +

                // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
                "FundingRate TEXT NULL," +
                "FundingInterval TEXT NULL," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxSymbolId ON Symbol(Id)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolExchangeId ON Symbol(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolName ON Symbol(Name)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolBase ON Symbol(Base)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolQuote ON Symbol(Quote)");
        }
    }

    //private static void CreateTableSymbolInterval(CryptoDatabase connection)
    //{
    //    // SymbolInterval (administratie, maar overlapt met de bestanden, via bestand is beter denk ik, rest is overkill)
    //    if (MissingTable(connection, "SymbolInterval"))
    //    {
    //        connection.Connection.Execute("CREATE TABLE [SymbolInterval] (" +
    //            "Id INTEGER primary key autoincrement not null," +
    //            "ExchangeId INTEGER NOT NULL," +
    //            "SymbolId INTEGER NOT NULL," +
    //            "IntervalId INTEGER NOT NULL," +
    //            "TrendInfoDate TEXT NULL," +
    //            "TrendIndicator Integer NULL," +
    //            "LastCandleSynchronized TEXT NULL," + // overlapt
    //            "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
    //            "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
    //            "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
    //        ")");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalId ON SymbolInterval(Id)");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalExchangeId ON SymbolInterval(ExchangeId)");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalSymbolId ON SymbolInterval(SymbolId)");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalIntervalId ON SymbolInterval(IntervalId)");
    //    }
    //}


    private static void CreateTableSignal(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Signal"))
        {
            connection.Connection.Execute("CREATE TABLE [Signal] (" +
                "Id integer primary key autoincrement not null," +
                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NULL," +
                "Strategy INTEGER NULL," +
                "Side INTEGER NOT NULL," +

                "BackTest INTEGER NOT NULL," +
                "IsInvalid INTEGER NOT NULL," +

                "EventTime bigint NOT NULL," +
                "OpenDate TEXT NULL," +
                "CloseDate TEXT NULL," +
                "ExpirationDate TEXT NULL," +

                "SignalPrice TEXT NOT NULL," +
                "SignalVolume TEXT NULL," +
                "EventText TEXT NULL," +

                "Last24HoursChange TEXT NULL," +
                "Last48Hours TEXT NULL," +
                "Last24HoursEffective TEXT NULL," +
                "Last10DaysEffective TEXT NULL," +

                "TrendIndicator INTEGER NULL," +
                "TrendPercentage TEXT NULL," +

                "BarcodePercentage TEXT NULL," +
                "CandlesWithZeroVolume INTEGER NULL," +
                "CandlesWithFlatPrice INTEGER NULL," +
                "AboveBollingerBandsSma INTEGER NULL," +
                "AboveBollingerBandsUpper INTEGER NULL," +

                "StochSignal TEXT NULL," +
                "StochOscillator TEXT NULL," +

                "AvgBb TEXT NULL," +
                "BollingerBandsDeviation TEXT NULL," +
                "BollingerBandsPercentage TEXT NULL," +

                "KeltnerLowerBand TEXT NULL," +
                "KeltnerUpperBand TEXT NULL," +

                "MacdValue TEXT NULL," +
                "MacdSignal TEXT NULL," +
                "MacdHistogram TEXT NULL," +

                "Rsi TEXT NULL," +
                "SlopeRsi TEXT NULL," +

                "Psar TEXT NULL," +

                "Ema20 TEXT NULL," +
                "SlopeEma20 TEXT NULL," +
                "Ema50 TEXT NULL," +
                "SlopeEma50 TEXT NULL," +
                "Ema100 TEXT NULL," +
                "SlopeEma100 TEXT NULL," +
                "Ema200 TEXT NULL," +
                "SlopeEma200 TEXT NULL," +

                "Sma20 TEXT NULL," +
                "SlopeSma20 TEXT NULL," +
                "Sma50 TEXT NULL," +
                "SlopeSma50 TEXT NULL," +
                "Sma100 TEXT NULL," +
                "SlopeSma100 TEXT NULL," +
                "Sma200 TEXT NULL," +
                "SlopeSma200 TEXT NULL," +

                "LuxIndicator5m TEXT NULL," +

                "Trend15m INTEGER NULL," +
                "Trend30m INTEGER NULL," +
                "Trend1h INTEGER NULL," +
                "Trend4h INTEGER NULL," +
                "Trend1d INTEGER NULL," +

                "Barometer15m TEXT NULL," +
                "Barometer30m TEXT NULL," +
                "Barometer1h TEXT NULL," +
                "Barometer4h TEXT NULL," +
                "Barometer1d TEXT NULL," +

                // statistics
                "PriceMin TEXT NULL," +
                "PriceMax TEXT NULL," +
                "PriceMinPerc TEXT NULL," +
                "PriceMaxPerc TEXT NULL," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxSignalId ON Signal(Id)");
            connection.Connection.Execute("CREATE INDEX IdxSignalExchangeId ON Signal(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxSignalSymbolId ON Signal(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxSignalIntervalId ON Signal(IntervalId)");
        }
    }


    private static void CreateTablePosition(CryptoDatabase connection, SqliteTransaction? transaction = null)
    {
        if (MissingTable(connection, "Position"))
        {
            connection.Connection.Execute("CREATE TABLE [Position] (" +
                "Id integer primary key autoincrement not null," +
                "TradeAccountId integer," + //"AccountId integer," +

                "CreateTime TEXT NOT NULL," +
                "UpdateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +

                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +
                "IntervalId Integer NOT NULL," +
                "Strategy Integer NOT NULL," +
                "Side Integer NOT NULL," +

                "Status Integer NOT NULL," +
                "data TEXT NULL," +

                "EntryPrice TEXT NULL," +
                "EntryAmount TEXT NULL," +
                "Quantity TEXT NULL," +
                "RemainingDust TEXT NULL," +
                "ProfitPrice TEXT NULL," +
                "PartCount Integer NOT NULL," +
                "ActiveDca Integer NOT NULL," +
                "Profit TEXT NULL," +
                "BreakEvenPrice TEXT NULL," +

                "Invested TEXT NULL," +
                "Commission TEXT NULL," +
                "CommissionBase TEXT NULL," +
                "CommissionQuote TEXT NULL," +
                "Returned TEXT NULL," +
                "Reserved TEXT NULL," +
                "Percentage TEXT NULL," +
                "Reposition Integer," +

                "AltradyPositionId TEXT NULL," +

                /// --------------------------------------------------------------
                /// added from the signal...
                /// --------------------------------------------------------------
                "SignalEventTime TEXT NOT NULL," +
                "SignalPrice TEXT NOT NULL," +
                "SignalVolume TEXT NULL," +

                "Last24HoursChange TEXT NULL," +
                "Last24HoursEffective TEXT NULL," +
                "Last10DaysEffective TEXT NULL," +

                "TrendIndicator INTEGER NULL," +
                "TrendPercentage TEXT NULL," +

                "BarcodePercentage TEXT NULL," +
                "CandlesWithZeroVolume INTEGER NULL," +
                "CandlesWithFlatPrice INTEGER NULL," +
                "AboveBollingerBandsSma INTEGER NULL," +
                "AboveBollingerBandsUpper INTEGER NULL," +

                "StochSignal TEXT NULL," +
                "StochOscillator TEXT NULL," +

                "AvgBb TEXT NULL," +
                "BollingerBandsDeviation TEXT NULL," +
                "BollingerBandsPercentage TEXT NULL," +

                "KeltnerLowerBand TEXT NULL," +
                "KeltnerUpperBand TEXT NULL," +

                "MacdValue TEXT NULL," +
                "MacdSignal TEXT NULL," +
                "MacdHistogram TEXT NULL," +

                "Rsi TEXT NULL," +
                "SlopeRsi TEXT NULL," +

                "Psar TEXT NULL," +

                "Ema20 TEXT NULL," +
                "SlopeEma20 TEXT NULL," +
                "Ema50 TEXT NULL," +
                "SlopeEma50 TEXT NULL," +
                "Ema100 TEXT NULL," +
                "SlopeEma100 TEXT NULL," +
                "Ema200 TEXT NULL," +
                "SlopeEma200 TEXT NULL," +

                "Sma20 TEXT NULL," +
                "SlopeSma20 TEXT NULL," +
                "Sma50 TEXT NULL," +
                "SlopeSma50 TEXT NULL," +
                "Sma100 TEXT NULL," +
                "SlopeSma100 TEXT NULL," +
                "Sma200 TEXT NULL," +
                "SlopeSma200 TEXT NULL," +

                "LuxIndicator5m TEXT NULL," +

                "Trend15m INTEGER NULL," +
                "Trend30m INTEGER NULL," +
                "Trend1h INTEGER NULL," +
                "Trend4h INTEGER NULL," +
                "Trend1d INTEGER NULL," +

                "Barometer15m TEXT NULL," +
                "Barometer30m TEXT NULL," +
                "Barometer1h TEXT NULL," +
                "Barometer4h TEXT NULL," +
                "Barometer1d TEXT NULL," +

                // statistics
                "PriceMin TEXT NULL," +
                "PriceMax TEXT NULL," +
                "PriceMinPerc TEXT NULL," +
                "PriceMaxPerc TEXT NULL," +

                "FOREIGN KEY(TradeAccountId) REFERENCES TradeAccount(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionId ON Position(Id)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionExchangeId ON Position(ExchangeId)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionSymbolId ON Position(SymbolId)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionCreateTime ON Position(CreateTime)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionCloseTime ON Position(CloseTime)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionTradeAccountId ON Position(TradeAccountId)", transaction);
            //connection.Connection.Execute("CREATE INDEX IdxPositionAccountId ON Position(AccountId)", transaction);
        }
    }

    private static void CreateTablePositionPart(CryptoDatabase connection)
    {
        if (MissingTable(connection, "PositionPart"))
        {
            connection.Connection.Execute("CREATE TABLE [PositionPart] (" +
                "Id integer primary key autoincrement not null," +
                "PositionId Integer NOT NULL," +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +
                "IntervalId Integer NOT NULL," +
                "Strategy TEXT NOT NULL," +

                "Purpose Integer NOT NULL," +
                "PartNumber Integer NOT NULL," +
                "CreateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +

                "Invested TEXT NULL," +
                "Commission TEXT NULL," +
                "CommissionBase TEXT NULL," +
                "CommissionQuote TEXT NULL," +
                "Returned TEXT NULL," +
                "Reserved TEXT NULL," +
                "Profit TEXT NULL," +
                "Percentage TEXT NULL," +

                "Quantity TEXT NULL," +
                "BreakEvenPrice TEXT NULL," +
                "SignalPrice TEXT NOT NULL," +
                "RemainingDust TEXT NULL," +

                "EntryMethod INTEGER NULL," +
                "ProfitMethod INTEGER NULL," +

                "ManualOrder INTEGER NULL," +

                "FOREIGN KEY(PositionId) REFERENCES Position(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartId ON PositionPart(Id)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartExchangeId ON PositionPart(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartSymbolId ON PositionPart(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartIntervalId ON PositionPart(IntervalId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartCreateTime ON PositionPart(CreateTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartCloseTime ON PositionPart(CloseTime)");
        }
    }

    private static void CreateTablePositionStep(CryptoDatabase connection)
    {
        if (MissingTable(connection, "PositionStep"))
        {
            connection.Connection.Execute("CREATE TABLE [PositionStep] (" +
                "Id integer primary key autoincrement not null," +
                "PositionId integer NOT NULL," +
                "PositionPartId integer NOT NULL," +
                "CreateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +
                "Status INTEGER NOT NULL," +
                "Side INTEGER NOT NULL," +
                "CancelInProgress INTEGER NOT NULL," +
                "OrderType INTEGER NOT NULL," +
                "OrderId TEXT NOT NULL," +
                "Order2Id TEXT NULL," +
                "Price TEXT NOT NULL," +
                "StopPrice TEXT NULL," +
                "StopLimitPrice TEXT NULL," +
                "Quantity TEXT NOT NULL," +
                "AveragePrice TEXT NULL," +
                "QuantityFilled TEXT NOT NULL," +
                "QuoteQuantityFilled TEXT NOT NULL," +
                "Commission NOT NULL," +
                "CommissionBase TEXT NULL," +
                "CommissionQuote TEXT NULL," +
                "CommissionAsset NULL," +
                "RemainingDust Text null," +
                "Trailing INTEGER NULL," +
                "IsCalculated Integer null," +
                "FOREIGN KEY(PositionId) REFERENCES Position(Id)," +
                "FOREIGN KEY(PositionPartId) REFERENCES PositionPart(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepId ON Position(Id)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepPositionId ON PositionStep(PositionId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepCreateTime ON PositionStep(CreateTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepCloseTime ON PositionStep(CloseTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepPositionPartId ON PositionStep(PositionPartId)");
        }
    }

    private static void CreateTableOrder(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Order"))
        {
            connection.Connection.Execute("CREATE TABLE [Order] (" +
                "Id integer primary key autoincrement not null," +

                "CreateTime TEXT NOT NULL," +
                "UpdateTime TEXT NOT NULL," +

                "TradeAccountId Integer NOT NULL," +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +

                "OrderId TEXT NOT NULL," +
                "Side integer NOT NULL," +
                "Type integer NOT NULL," +
                "Status integer NOT NULL," +

                "Price TEXT NOT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuoteQuantity TEXT NOT NULL," +

                "AveragePrice TEXT NULL," +
                "QuantityFilled TEXT NULL," +
                "QuoteQuantityFilled TEXT NULL," +

                "Commission TEXT NULL," +
                "CommissionAsset TEXT NULL," +

                "FOREIGN KEY(TradeAccountId) REFERENCES TradeAccount(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxOrderId ON [Order](Id)");
            connection.Connection.Execute("CREATE INDEX IdxOrderExchangeId ON [Order](ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxOrderSymbolId ON [Order](SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxOrderCreateTime ON [Order](CreateTime)");
            connection.Connection.Execute("CREATE INDEX IdxOrderUpdateTime ON [Order](UpdateTime)");
        }
    }

    private static void CreateTableTrade(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Trade"))
        {
            connection.Connection.Execute("CREATE TABLE [Trade] (" +
                "Id integer primary key autoincrement not null," +
                "TradeTime TEXT NOT NULL," +

                "TradeAccountId Integer NOT NULL," +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +

                "TradeId TEXT NOT NULL," +
                "OrderId TEXT NOT NULL," +

                "Price TEXT NOT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuoteQuantity TEXT NOT NULL," +
                "Commission TEXT NOT NULL," +
                "CommissionAsset TEXT NULL," +

                "FOREIGN KEY(TradeAccountId) REFERENCES TradeAccount(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxTradeId ON [Trade](Id)");
            connection.Connection.Execute("CREATE INDEX IdxTradeExchangeId ON [Trade](ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeSymbolId ON [Trade](SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeTradeTime ON [Trade](TradeTime)");
            connection.Connection.Execute("CREATE INDEX IdxTradeTradeAccountId ON [Trade](TradeAccountId)");
        }

    }

    private static void CreateTableAsset(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Asset"))
        {
            connection.Connection.Execute("CREATE TABLE [Asset] (" +
                "Id integer primary key autoincrement not null," +
                "TradeAccountId integer," +

                "Name TEXT NOT NULL," +
                "Total TEXT NOT NULL," +
                "Free TEXT NOT NULL," +
                "Locked TEXT NOT NULL," +

                "FOREIGN KEY(TradeAccountId) REFERENCES TradeAccount(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxAssetId ON Asset(Id)");
            connection.Connection.Execute("CREATE INDEX IdxAssetTradeAccountId ON Asset(TradeAccountId)");
        }
    }


    private static void CreateTableZone(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Zone"))
        {
            connection.Connection.Execute("CREATE TABLE [Zone] (" +
                "Id integer primary key autoincrement not null," +
                "CreateTime TEXT not NULL," +
                "AccountId Integer NOT NULL," +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +
                "Strategy Integer NOT NULL," +
                "Side integer not null," +
                "OpenTime TEXT NULL," +
                "Top TEXT not null," +
                "Bottom TEXT not null," +
                "AlarmDate TEXT," +
                "CloseTime TEXT NULL," +
                "LastSignalDate TEXT NULL," +
                "Description TEXT NULL," +
                "FOREIGN KEY(AccountId) REFERENCES TradeAccount(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxZoneId ON Zone(Id)");
            connection.Connection.Execute("CREATE INDEX IdxZoneExchangeId ON Zone(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxZoneSymbolId ON Zone(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxZoneAccountId ON Zone(AccountId)");
        }
    }

//private static void CreateTableBalancing(CryptoDatabase connection)
//{
//    //// Balance (echt? weet niet waarom we dit op deze manier opslaan, balanceren doe je binnen groep, die mis ik, een oude versie wellicht?)
//    if (MissingTable(connection, "Balance"))
//    //{
//    //    connection.Connection.Execute("CREATE TABLE [Balance] (" +
//    //        "Id integer primary key autoincrement not null," +
//    //        "ExchangeId Integer NOT NULL," +
//    //        "SymbolId Integer NOT NULL," +
//    //        "EventTime TEXT NOT NULL," +
//    //        "Name TEXT NOT NULL," +
//    //        "Price TEXT NOT NULL," +
//    //        "Quantity TEXT NOT NULL," +
//    //        "QuoteQuantity TEXT NOT NULL," +
//    //        "InvestedQuantity TEXT NULL," +
//    //        "InvestedValue TEXT NULL," +
//    //        "UsdtValue TEXT NULL," +
//    //        "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
//    //        "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
//    //    ")");
//    //    connection.Connection.Execute("CREATE INDEX IdxBalanceId ON [Balance](Id)");
//    //    connection.Connection.Execute("CREATE INDEX IdxBalanceExchangeId ON [Balance](ExchangeId)");
//    //    connection.Connection.Execute("CREATE INDEX IdxBalanceSymbolId ON [Balance](SymbolId)");
//    //    connection.Connection.Execute("CREATE INDEX IdxBalanceEventTime ON [Balance](EventTime)");
//    //}
//}

public string CreateNewUniqueId()
    {
        // SQL server
        // Create Sequence UniqueSequenceId as int start with 1 increment by 1
        // SELECT NEXT VALUE FOR UniqueSequenceId AS Id

        using var transaction = Connection.BeginTransaction();
        {
            CryptoSequence sequence = new()
            {
                Name = "Whatever"
            };
            Connection.Insert(sequence, transaction);
            Connection.Delete(sequence, transaction);
            transaction.Commit();
            return sequence.Id.ToString();
        }
    }


    public static void CleanUpDatabase()
    {
        // Database cleanup (there is no need for old signals)
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();
            using var transaction = databaseThread.BeginTransaction();
            {
                databaseThread.Connection.Execute("delete from signal where ExpirationDate < @opendate", new { opendate = DateTime.UtcNow.AddDays(-7) });
                transaction.Commit();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR " + error.ToString());
        }
    }


    public static void CreateDatabase()
    {
        // Sqlite gaat afwijkend met datatypes om, zie https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
        //SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        //SqlMapper.AddTypeHandler(new DateTimeHandler());

        SqlMapper.RemoveTypeMap(typeof(DateTimeHandler));
        SqlMapper.AddTypeHandler(new DateTimeHandler());

        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        using var connection = new CryptoDatabase();
        connection.Open();

        //connection.Connection.ServerVersion

        //connection.Connection.ForceDateTimesToUtc = true;

        CreateTableInterval(connection); // (met een hardcoded lijst, voorlopig prima)
        CreateTableExchange(connection); // (met een hardcoded lijst, voorlopig prima)

        CreateTableSymbol(connection);
        //CreateTableSymbolInterval(connection); -- opgeslagen in files, prima zo
        CreateTableSignal(connection);

        CreateTableTradeAccount(connection);
        CreateTablePosition(connection);
        CreateTablePositionPart(connection);
        CreateTablePositionStep(connection);

        CreateTableOrder(connection);
        CreateTableTrade(connection);
        CreateTableAsset(connection);

        CreateTableZone(connection);

        //CreateTableBalancing(connection); -- todo ooit
        CreateTableSequence(connection); // Fake-ID's for orders en trades
        CreateTableVersion(connection); // Administration database & migration


        // Indien noodzakelijk database upgraden 
        Migration.Execute(connection, Migration.CurrentDatabaseVersion);


        CleanUpDatabase();

        // Only works during startup (because of exclusive acces)
        using var command = connection.Connection.CreateCommand();
        command.CommandText = "vacuum;";
        command.ExecuteNonQuery();
    }

}
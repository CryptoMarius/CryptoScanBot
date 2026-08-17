using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Context;

// SqlConnection is sealed, dus dan maar via een compositie
// De SqliteConnection is niet sealed (die gebruiken we ook)

public class CryptoDatabase : IDisposable
{

    public static void SetDatabaseDefaults()
    {
        SqlMapper.Settings.CommandTimeout = 180;
        CreateDatabase();
    }

    public SqliteConnection Connection { get; set; }

    private readonly string connectionString;

    public CryptoDatabase()
    {
        string dbFile = Path.Combine(GlobalData.AppDataFolder, Constants.AppName + ".db");
        connectionString = $"Filename={dbFile};Mode=ReadWriteCreate;";
        Connection = new(connectionString);
    }

    public SqliteTransaction BeginTransaction()
    {
        // Retry transient SQLITE_ERROR (rc 1) that can occur under heavy concurrent WAL writes
        // (multiple emulator threads opening transactions on the same database file).
        const int maxAttempts = 4;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return Connection.BeginTransaction();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && attempt < maxAttempts)
            {
                Thread.Sleep(attempt * 25);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Connection is deliberately left in place after Dispose (see the commented-out null below),
    // so the object still looks usable afterwards. Remember that it is gone, so Open() can say so
    // instead of failing deep inside SQLitePCL with a bare "Cannot access a disposed object".
    private bool disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
                //Connection = null;
            }
            disposed = true;
        }
    }

    public void Open()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        // Microsoft.Data.Sqlite keeps a pool of native sqlite3 handles per connection string and prunes
        // idle ones on a background timer. Under the scanner's constant open/close churn (a fresh
        // connection per symbol per 1m candle, on several threads at once) a leased handle occasionally
        // turns out to be already disposed, which surfaces as "ObjectDisposedException: Cannot access a
        // disposed object. Object name: 'SQLitePCL.sqlite3'" on the first statement after Open. Nothing
        // was written yet at that point, so throw the poisoned connection away and lease a new one
        // instead of losing the whole candle.
        const int maxAttempts = 3;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                Connection.Open();
                // Make concurrent writers WAIT for the lock instead of failing with SQLITE_BUSY. SQLite allows
                // only one writer at a time; the emulator's parallel symbol processing can open several
                // connections that each write (positions, zones). With WAL (set per run) + this timeout those
                // writes simply serialize at the storage level instead of throwing. Per-connection setting, so
                // it must be applied on every Open. Harmless for the live scanner (only matters under
                // contention, which the single-threaded live path never hits).
                Connection.Execute("PRAGMA busy_timeout=5000;");
                return;
            }
            catch (ObjectDisposedException) when (attempt < maxAttempts)
            {
                ReplaceConnection();
            }
        }
    }

    /// <summary>
    /// Drop the current (poisoned) connection and create a fresh one, so the next Open leases another
    /// handle from the pool. Disposing the old one may itself fail on the dead handle, hence the catch.
    /// </summary>
    private void ReplaceConnection()
    {
        try
        {
            Connection.Dispose();
        }
        catch (Exception)
        {
            // ignore, the handle is gone anyway
        }
        Connection = new(connectionString);
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
                "Id INTEGER primary key autoincrement not null," +
                "Version INTEGER NOT NULL" +
            ")");

            // De exchanges moeten aanwezig zijn na initialisatie
            using var transaction = connection.Connection.BeginTransaction();
            CryptoVersion databaseVersion = new()
            {
                Version = DatabaseMigration.CurrentDatabaseVersion,
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
                "Id INTEGER primary key autoincrement not null," +
                "Name TEXT NOT NULL" +
            ")");

        }
    }


    private static void CreateTableInterval(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Interval"))
        {
            connection.Connection.Execute("CREATE TABLE [Interval] (" +
                "Id INTEGER primary key autoincrement not null," +
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

            List<CryptoInterval> IntervalList = CryptoInterval.CreateStandardIntervalList();

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

    public static List<Model.CryptoExchange> CreateExchangeList()
    {
        return
        [
            // You must have an account and register the api key, otherwise "error unauthorized"
            new() { Name = "Alpaca", IsSupported = true, ExchangeType = CryptoExchangeType.Alpaca, TradingType=CryptoTradingType.Spot },
            new() { Name = "Alpaca Futures", IsSupported = false, ExchangeType = CryptoExchangeType.Alpaca, TradingType=CryptoTradingType.Futures},

            new() { Name = "Binance Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Binance, TradingType=CryptoTradingType.Spot },
            new() { Name = "Binance Futures", IsSupported = true, ExchangeType = CryptoExchangeType.Binance, TradingType=CryptoTradingType.Futures},

            // The spot side is small: the exchange offers 65 pairs here, of which 32 are quoted in USDT
            new() { Name = "BitMart Spot", IsSupported = false, ExchangeType = CryptoExchangeType.BitMart, TradingType=CryptoTradingType.Spot },
            new() { Name = "BitMart Futures", IsSupported = true, ExchangeType = CryptoExchangeType.BitMart, TradingType=CryptoTradingType.Futures },

            new() { Name = "Bitvavo Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Bitvavo, TradingType=CryptoTradingType.Spot },
            new() { Name = "Bitvavo Futures", IsSupported = false, ExchangeType = CryptoExchangeType.Bitvavo, TradingType=CryptoTradingType.Futures},

            // The exchange does offer spot (241 instruments), but the BloFin.Net package (JKorf) only
            // implements the futures and account api - there is no spot client to talk to. The source
            // in Exchange\BloFin\Spot was written against the SpotApi of an older package version and
            // was removed on 17-08-2026; only this registration is left, to keep the market visible as
            // an unsupported one.
            new() { Name = "BloFin Spot", IsSupported = false, ExchangeType = CryptoExchangeType.BloFin, TradingType=CryptoTradingType.Spot },
            new() { Name = "BloFin Futures", IsSupported = true, ExchangeType = CryptoExchangeType.BloFin, TradingType=CryptoTradingType.Futures },

            // Bybit global
            new() { Name = "Bybit Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Bybit, TradingType=CryptoTradingType.Spot },
            new() { Name = "Bybit Futures", IsSupported = true, ExchangeType = CryptoExchangeType.Bybit, TradingType=CryptoTradingType.Futures },

            // Bybit EU (the MiCA regulated european entity, api.bybit.eu) is a spot only exchange:
            // instruments-info returns 132 spot instruments (111 USDC, 17 EUR, 4 PLN) and an empty
            // list for linear, inverse and option (checked 14-08-2026). So there is nothing to
            // subscribe to on the futures side, this is not a boundary or a filter problem. The
            // futures source was removed on 17-08-2026; only this registration is left, to keep the
            // market visible as an unsupported one.
            new() { Name = "Bybit EU Spot", IsSupported = true, ExchangeType = CryptoExchangeType.BybitEu, TradingType=CryptoTradingType.Spot },
            new() { Name = "Bybit EU Futures", IsSupported = false, ExchangeType = CryptoExchangeType.BybitEu, TradingType=CryptoTradingType.Futures },

            // The kline stream of this exchange is fixed at 5 minutes, which is of no use for the 1m
            // candles, so those are built from the trade feed instead (as with Kraken Futures).
            new() { Name = "Coinbase Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Coinbase, TradingType=CryptoTradingType.Spot },
            // There is no Coinbase futures implementation at all, ExchangeHelper.GetApi throws for it
            new() { Name = "Coinbase Futures", IsSupported = false, ExchangeType = CryptoExchangeType.Coinbase, TradingType=CryptoTradingType.Futures },

            // HyperLiquid
            new() { Name = "HyperLiquid Spot", IsSupported = true, ExchangeType = CryptoExchangeType.HyperLiquid, TradingType=CryptoTradingType.Spot },
            new() { Name = "HyperLiquid Futures", IsSupported = true, ExchangeType = CryptoExchangeType.HyperLiquid, TradingType=CryptoTradingType.Futures },

            // Kraken Futures has no kline websocket feed, the 1m candles are built from the trades
            new() { Name = "Kraken Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Kraken, TradingType=CryptoTradingType.Spot },
            new() { Name = "Kraken Futures", IsSupported = true, ExchangeType = CryptoExchangeType.Kraken, TradingType=CryptoTradingType.Futures },

            new() { Name = "Kucoin Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Kucoin, TradingType=CryptoTradingType.Spot },
            new() { Name = "Kucoin Futures", IsSupported = true, ExchangeType = CryptoExchangeType.Kucoin, TradingType=CryptoTradingType.Futures },

            // Mexc Futures: market data only, the order endpoints of that exchange have been marked
            // "(Under maintenance)" since 25-07-2022 - so it can be scanned but never traded on
            new() { Name = "Mexc Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Mexc, TradingType=CryptoTradingType.Spot },
            new() { Name = "Mexc Futures", IsSupported = true, ExchangeType = CryptoExchangeType.Mexc, TradingType=CryptoTradingType.Futures },

            new() { Name = "Okx Spot", IsSupported = true, ExchangeType = CryptoExchangeType.Okx, TradingType=CryptoTradingType.Spot },
            new() { Name = "Okx Futures", IsSupported = true, ExchangeType = CryptoExchangeType.Okx, TradingType=CryptoTradingType.Futures },
        ];
    }

    private static void CreateTableExchange(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Exchange"))
        {
            connection.Connection.Execute("CREATE TABLE [Exchange] (" +
                 "Id INTEGER primary key autoincrement not null," +
                 "LastTimeFetched TEXT NULL," +
                 "IsSupported INTEGER NOT NULL DEFAULT 0," +
                 "Name TEXT not NULL," +
                 "FeeRate TEXT not NULL," +
                 "ExchangeType INTEGER not NULL," +
                 "TradingType INTEGER not NULL," +
                 "LastZoneCheckTime TEXT NULL" +
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
                "ExchangeName TEXT NOT NULL," +
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
                "LastTradeDate TEXT NULL," +
                "LastLossDate TEXT NULL," +

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
    //            "TrendInterval INTEGER NULL," +
    //            "LastCandleSynchronized TEXT NULL," + // overlapt
    //            "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
    //            "FOREIGN KEY(SymbolId) REFERENCES ScannerSymbol(Id)," +
    //            "FOREIGN KEY(IntervalId) REFERENCES IntervalList(Id)" +
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
                "Id INTEGER primary key autoincrement not null," +
                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NULL," +
                "Strategy TEXT NULL," +
                "Side INTEGER NOT NULL," +

                "IsInvalid INTEGER NOT NULL," +
                "EmulatorRunId INTEGER NULL," +

                "OpenDate TEXT NULL," +
                "CloseDate TEXT NULL," +
                "ExpirationDate TEXT NULL," +

                "EventText TEXT NULL," +
                "SignalPrice TEXT NOT NULL," +
                "SignalVolume TEXT NULL," +

                "Last24HoursChange TEXT NULL," +
                "LastXDaysEffective TEXT NULL," +

                "TrendPercentagePrimary TEXT NULL," +
                "TrendPercentageSecondary TEXT NULL," +

                "BarcodePercentage TEXT NULL," +
                "CandlesWithZeroVolume INTEGER NULL," +
                "CandlesWithFlatPrice INTEGER NULL," +
                "AboveBollingerBandsSma INTEGER NULL," +
                "AboveBollingerBandsUpper INTEGER NULL," +

                "StochSignal TEXT NULL," +
                "StochOscillator TEXT NULL," +
                "StochSurface TEXT NULL," +
                "StochSurface2 TEXT NULL," +

                "AvgBb TEXT NULL," +
                "BollingerBandsDeviation TEXT NULL," +
                "BollingerBandsPercentage TEXT NULL," +
                "BandRangeIndex TEXT NULL," +
                "BandRangeCount INTEGER NULL," +

                "KeltnerLowerBand TEXT NULL," +
                "KeltnerUpperBand TEXT NULL," +

                "MacdValue TEXT NULL," +
                "MacdSignal TEXT NULL," +
                "MacdHistogram TEXT NULL," +

                "Rsi TEXT NULL," +
                "SlopeRsi TEXT NULL," +
                "RsiSurface TEXT NULL," +
                "RsiSurface2 TEXT NULL," +

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
                "TrendInterval INTEGER NULL," +

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
                "SignalStatus TEXT NULL," +

                // Optional per-signal SL/TP distances (% from entry) computed by the strategy.
                // SlPrice/TpPrice are legacy columns kept for schema compatibility; the levels
                // are now stored as SlPercentage / TpPercentage.
                "SlPercentage TEXT NULL," +
                "TpPercentage TEXT NULL," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)," +
                "FOREIGN KEY(EmulatorRunId) REFERENCES EmulatorRun(Id)" +
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
                "Id INTEGER primary key autoincrement not null," +

                "CreateTime TEXT NOT NULL," +
                "UpdateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +
                "EmulatorRunId INTEGER NULL," +

                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NOT NULL," +
                "Strategy TEXT NOT NULL," +
                "Side INTEGER NOT NULL," +
                "Status INTEGER NOT NULL," +

                "EventText TEXT NULL," +
                "EntryPrice TEXT NULL," +
                "EntryAmount TEXT NULL," +
                "Quantity TEXT NULL," +
                "RemainingDust TEXT NULL," +
                "ProfitPrice TEXT NULL," +
                "PartCount INTEGER NOT NULL," +
                "ActiveDca INTEGER NOT NULL," +
                "Profit TEXT NULL," +
                "BreakEvenPrice TEXT NULL," +
                "TpGridAnchorPrice TEXT NULL," +

                "Invested TEXT NULL," +
                "Commission TEXT NULL," +
                "CommissionBase TEXT NULL," +
                "CommissionQuote TEXT NULL," +
                "Returned TEXT NULL," +
                "Reserved TEXT NULL," +
                "Percentage TEXT NULL," +
                "Reposition INTEGER," +

                "AltradyPositionId TEXT NULL," +

                /// --------------------------------------------------------------
                /// added from the signal...
                /// --------------------------------------------------------------
                "SignalId INTEGER NULL," + // NO FK!
                "SignalEventTime TEXT NOT NULL," +
                "SignalPrice TEXT NOT NULL," +
                "SignalVolume TEXT NULL," +

                "Last24HoursChange TEXT NULL," +
                "LastXDaysEffective TEXT NULL," +

                "TrendPercentagePrimary TEXT NULL," +
                "TrendPercentageSecondary TEXT NULL," +

                "BarcodePercentage TEXT NULL," +
                "CandlesWithZeroVolume INTEGER NULL," +
                "CandlesWithFlatPrice INTEGER NULL," +
                "AboveBollingerBandsSma INTEGER NULL," +
                "AboveBollingerBandsUpper INTEGER NULL," +

                "StochSignal TEXT NULL," +
                "StochOscillator TEXT NULL," +
                "StochSurface TEXT NULL," +
                "StochSurface2 TEXT NULL," +

                "AvgBb TEXT NULL," +
                "BollingerBandsDeviation TEXT NULL," +
                "BollingerBandsPercentage TEXT NULL," +
                "BandRangeIndex TEXT NULL," +
                "BandRangeCount INTEGER NULL," +

                "KeltnerLowerBand TEXT NULL," +
                "KeltnerUpperBand TEXT NULL," +

                "MacdValue TEXT NULL," +
                "MacdSignal TEXT NULL," +
                "MacdHistogram TEXT NULL," +

                "Rsi TEXT NULL," +
                "SlopeRsi TEXT NULL," +
                "RsiSurface TEXT NULL," +
                "RsiSurface2 TEXT NULL," +

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
                "TrendInterval INTEGER NULL," +

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
                "SignalStatus TEXT NULL," +

                // Optional per-position SL/TP distances (% from entry) carried over from
                // the signal SlPrice/TpPrice are legacy columns kept for schema compatibility;
                // the levels are now stored as SlPercentage / TpPercentage.
                "SlPercentage TEXT NULL," +
                "TpPercentage TEXT NULL," +
                "SlMovedToBreakEven INTEGER NOT NULL DEFAULT 0," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)," +
                "FOREIGN KEY(EmulatorRunId) REFERENCES EmulatorRun(Id)" +
            ")", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionId ON Position(Id)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionExchangeId ON Position(ExchangeId)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionSymbolId ON Position(SymbolId)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionCreateTime ON Position(CreateTime)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionCloseTime ON Position(CloseTime)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionEmulatorRunId ON Position(EmulatorRunId)", transaction);
            connection.Connection.Execute("CREATE INDEX IdxPositionSignalId ON Position(SignalId)", transaction);
        }
    }

    private static void CreateTablePositionPart(CryptoDatabase connection)
    {
        if (MissingTable(connection, "PositionPart"))
        {
            connection.Connection.Execute("CREATE TABLE [PositionPart] (" +
                "Id INTEGER primary key autoincrement not null," +
                "PositionId INTEGER NOT NULL," +
                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NOT NULL," +
                "Strategy TEXT NOT NULL," +

                "Purpose INTEGER NOT NULL," +
                "PartNumber INTEGER NOT NULL," +
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
                "Id INTEGER primary key autoincrement not null," +
                "PositionId INTEGER NOT NULL," +
                "PositionPartId INTEGER NOT NULL," +
                "CreateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +
                "Status INTEGER NOT NULL," +
                "Side INTEGER NOT NULL," +
                "CancelInProgress INTEGER NOT NULL DEFAULT 0," +
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
                "RemainingDust TEXT null," +
                "Trailing INTEGER NULL," +
                "IsCalculated INTEGER NOT NULL DEFAULT 0," +
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
                "Id INTEGER primary key autoincrement not null," +

                "CreateTime TEXT NOT NULL," +
                "UpdateTime TEXT NOT NULL," +

                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +

                "OrderId TEXT NOT NULL," +
                "Side INTEGER NOT NULL," +
                "Type INTEGER NOT NULL," +
                "Status INTEGER NOT NULL," +

                "Price TEXT NOT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuoteQuantity TEXT NOT NULL," +

                "AveragePrice TEXT NULL," +
                "QuantityFilled TEXT NULL," +
                "QuoteQuantityFilled TEXT NULL," +

                "Commission TEXT NULL," +
                "CommissionAsset TEXT NULL," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxOrderId ON [Order](Id)");
            connection.Connection.Execute("CREATE INDEX IdxOrderOrderId ON [Order](OrderId)");
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
                "Id INTEGER primary key autoincrement not null," +
                "TradeTime TEXT NOT NULL," +

                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +

                "TradeId TEXT NOT NULL," +
                "OrderId TEXT NOT NULL," +

                "Price TEXT NOT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuoteQuantity TEXT NOT NULL," +
                "Commission TEXT NOT NULL," +
                "CommissionAsset TEXT NULL," +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxTradeId ON [Trade](Id)");
            connection.Connection.Execute("CREATE INDEX IdxTradeOrderId ON [Trade](OrderId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeExchangeId ON [Trade](ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeSymbolId ON [Trade](SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeTradeTime ON [Trade](TradeTime)");
        }

    }

    private static void CreateTableAsset(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Asset"))
        {
            connection.Connection.Execute("CREATE TABLE [Asset] (" +
                "Id INTEGER primary key autoincrement not null," +

                "Name TEXT NOT NULL," +
                "Total TEXT NOT NULL," +
                "Free TEXT NOT NULL," +
                "Locked TEXT NOT NULL" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxAssetId ON Asset(Id)");
        }
    }


    private static void CreateTableZone(CryptoDatabase connection)
    {
        if (MissingTable(connection, "Zone"))
        {
            connection.Connection.Execute("CREATE TABLE [Zone] (" +
                "Id INTEGER primary key autoincrement not null," +
                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NOT NULL," +
                "Kind INTEGER not null," +
                "Side INTEGER not null," +
                "Strength INTEGER not null, " +
                "OpenTime TEXT NULL," +
                "Top TEXT not null," +
                "Bottom TEXT not null," +
                "AlarmDate TEXT," +
                "CloseTime TEXT NULL," +
                "Description TEXT NULL," +
                "IsValid INTEGER not null," +
                "TouchCount INTEGER NOT NULL DEFAULT 0," +
                "IsMitigated INTEGER NOT NULL DEFAULT 0," +
                "EmulatorRunId INTEGER NULL," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)," +
                "FOREIGN KEY(EmulatorRunId) REFERENCES EmulatorRun(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxZoneId ON Zone(Id)");
            connection.Connection.Execute("CREATE INDEX IdxZoneExchangeId ON Zone(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxZoneSymbolId ON Zone(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxZoneIntervalId ON Zone(IntervalId)");
            connection.Connection.Execute("CREATE INDEX IdxZoneEmulatorRunId ON Zone(EmulatorRunId)");
        }
    }

    //private static void CreateTableBalancing(CryptoDatabase connection)
    //{
    //    //// Balance (echt? weet niet waarom we dit op deze manier opslaan, balanceren doe je binnen groep, die mis ik, een oude versie wellicht?)
    //    if (MissingTable(connection, "Balance"))
    //    //{
    //    //    connection.Connection.Execute("CREATE TABLE [Balance] (" +
    //    //        "Id INTEGER primary key autoincrement not null," +
    //    //        "ExchangeId INTEGER NOT NULL," +
    //    //        "SymbolId INTEGER NOT NULL," +
    //    //        "EventTime TEXT NOT NULL," +
    //    //        "ExchangeSymbol TEXT NOT NULL," +
    //    //        "Price TEXT NOT NULL," +
    //    //        "Quantity TEXT NOT NULL," +
    //    //        "QuoteQuantity TEXT NOT NULL," +
    //    //        "InvestedQuantity TEXT NULL," +
    //    //        "InvestedValue TEXT NULL," +
    //    //        "UsdtValue TEXT NULL," +
    //    //        "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
    //    //        "FOREIGN KEY(SymbolId) REFERENCES ScannerSymbol(Id)" +
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
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();
            using var transaction = databaseThread.BeginTransaction();
            {
                // Database cleanup (there is no need for old signals <fixed 7 day's>)
                var opendate = GlobalData.Clock.UtcNow.AddDays(-7);

                databaseThread.Connection.Execute("delete from signal where ExpirationDate < @opendate",
                    new { opendate });

                // Database cleanup (there is no need for old zones older than the configured value)
                foreach (var interval in GlobalData.IntervalList)
                {
                    CandleTime openTime = CandleTime.FromDateTime(GlobalData.Clock.UtcNow.AddMinutes(-GlobalData.Settings.Signal.ZonesDlz.CandleCount * interval.Duration));

                    // we use the same candlecount for both the fvg and dlz zones
                    databaseThread.Connection.Execute("delete from zone where OpenTime < @OpenTime",
                        new { OpenTime = openTime });
                }
                transaction.Commit();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab("ERROR " + error.ToString());
        }
    }

    private static void CreateTableEmulatorRun(CryptoDatabase connection)
    {
        if (MissingTable(connection, "EmulatorRun"))
        {
            connection.Connection.Execute("CREATE TABLE [EmulatorRun] (" +
                "Id INTEGER primary key autoincrement not null," +
                "StartedAt TEXT NOT NULL," +
                "FinishedAt TEXT NULL," +
                "Label TEXT NULL," +
                "FromDate TEXT NULL," +
                "ToDate TEXT NULL," +
                "ConfigJson TEXT NOT NULL," +
                "SettingsJson TEXT NULL," +
                "GitSha TEXT NULL," +
                "Result TEXT NULL," +
                "SignalCount INTEGER NOT NULL DEFAULT 0," +
                "PositionCount INTEGER NOT NULL DEFAULT 0," +
                "PositionsOpen INTEGER NOT NULL DEFAULT 0," +
                "PositionsWon INTEGER NOT NULL DEFAULT 0," +
                "PositionsLost INTEGER NOT NULL DEFAULT 0," +
                "PositionsTimeout INTEGER NOT NULL DEFAULT 0," +
                "PositionsCancelled INTEGER NOT NULL DEFAULT 0," +
                "Profit TEXT NULL," +
                "Invested TEXT NULL" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxEmulatorRunId ON EmulatorRun(Id)");
        }
    }

    public static void CreateTables(CryptoDatabase connection)
    {
        CreateTableInterval(connection); // (+hardcoded list)
        CreateTableExchange(connection); // (+hardcoded list)

        CreateTableSymbol(connection);
        CreateTableEmulatorRun(connection); // before Signal/Position (FK targets, though SQLite doesn't enforce)
        CreateTableSignal(connection);

        CreateTablePosition(connection);
        CreateTablePositionPart(connection);
        CreateTablePositionStep(connection);

        CreateTableOrder(connection);
        CreateTableTrade(connection);
        CreateTableAsset(connection);

        CreateTableZone(connection);

        CreateTableSequence(connection); // Fake-ID's for orders en trades
        CreateTableVersion(connection); // Administration database & migration
    }

    public static void CreateDatabase()
    {
        // https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
        //SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        //SqlMapper.AddTypeHandler(new DateTimeHandler());

        SqlMapper.RemoveTypeMap(typeof(DateTimeHandler));
        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
        SqlMapper.AddTypeHandler(new CandleTimeTypeHandler());
        // BUGFIX: Dapper has a built-in fast path for primitive types (incl. double / double?)
        // that bypasses TypeHandler<double>. Without RemoveTypeMap, NaNDoubleHandler.SetValue
        // is never called, so IEEE-NaN values reach Microsoft.Data.Sqlite directly and trigger
        // "Cannot store 'NaN' values". Removing the built-in mapping forces Dapper to consult
        // the registered handler, which converts NaN to DBNull.
        SqlMapper.RemoveTypeMap(typeof(double));
        SqlMapper.RemoveTypeMap(typeof(double?));
        SqlMapper.AddTypeHandler(new NaNDoubleHandler());


        using var connection = new CryptoDatabase();
        connection.Open();

        CreateTables(connection);

        // Indien noodzakelijk database upgraden
        DatabaseMigration.Execute(connection, DatabaseMigration.CurrentDatabaseVersion);

        // Tables are sometimes dropped
        CreateTables(connection);

        CleanUpDatabase();

        // Only works during startup (because of exclusive acces)
        // Skip in emulator mode: the emulator DB can be >1 GB and VACUUM rewrites the
        // entire file, adding 30-40s to every startup for no practical benefit.
        if (!GlobalData.IsEmulatorMode)
        {
            using var command = connection.Connection.CreateCommand();
            command.CommandText = "vacuum;";
            command.ExecuteNonQuery();
        }
    }

}
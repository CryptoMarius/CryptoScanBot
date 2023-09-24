using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Context;

public class Migration
{
    // De huidige database versie (zoals in de code is gedefinieerd)
    public readonly static int CurrentDatabaseVersion = 6;


    public static void Execute(CryptoDatabase database, int CurrentVersion)
    {
        CryptoVersion version = database.Connection.GetAll<CryptoVersion>().FirstOrDefault();

        if (CurrentVersion > version.Version)
        {

            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 1)
            {
                using var transaction = database.BeginTransaction();

                // De exchangeId's in de TradeAccount staan initieel allemaal op Binance (verkeerde initialisatie)
                database.Connection.Execute("update TradeAccount set ExchangeId=1 where name like 'Binance%'", transaction);
                database.Connection.Execute("update TradeAccount set ExchangeId=2 where name like 'Bybit Spot%'", transaction);
                database.Connection.Execute("update TradeAccount set ExchangeId=3 where name like 'Bybit Futures%'", transaction);
                database.Connection.Execute("update TradeAccount set ExchangeId=4 where name like 'Kucoin%'", transaction);

                // De fee moet erbij zodat we achteraf kunnen rapporteren (anders moet dat via de trades)
                database.Connection.Execute("alter table PositionStep add Commission TEXT NOT NULL default 0", transaction);

                // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
                // Wat het inhoud weet ik nog niet (toegevoegde waarde, voor trading is er waarschijnlijk wel)
                // Het type is waarschijnlijk ook niet goed ingesteld, maar met text kom je een heel eind
                // https://bybit-exchange.github.io/docs/v5/market/history-fund-rate
                database.Connection.Execute("alter table symbol add FundingRate TEXT", transaction);
                database.Connection.Execute("alter table symbol add FundingInterval TEXT", transaction);

                // Forceer dat de symbol informatie (en funding rates) opgehaald wordt
                database.Connection.Execute("update Exchange set LastTimeFetched=null", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }



            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 2)
            {
                using var transaction = database.BeginTransaction();


                // De accounttype voor Futures is niet goed ingevuld, deze staan allemaal op spot (verkeerde initialisatie)
                database.Connection.Execute("update TradeAccount set AccountType=1 where name like '%Futures%'", transaction);

                // Ongebruikte kolommen
                database.Connection.Execute("alter table PositionPart drop column BuyAmount", transaction);
                database.Connection.Execute("alter table PositionPart drop column Sellprice", transaction);

                // Duidelijke naam geven
                database.Connection.Execute("alter table PositionPart rename column BuyPrice TO SignalPrice", transaction);

                // De reden van aankoop (c.q. methode van aankoop)
                database.Connection.Execute("alter table PositionPart add StepInMethod Integer", transaction);
                // De reden van verkoop (c.q. methode van verkoop)
                database.Connection.Execute("alter table PositionPart add StepOutMethod Integer", transaction);
                

                // De gemiddelde prijs dat het gekocht of verkocht is (meerdere trades ivm market of stoplimit)
                database.Connection.Execute("alter table PositionStep add AvgPrice TEXT", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }



            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 3)
            {
                using var transaction = database.BeginTransaction();

                // De laatste mutatie datum van een positie ("leeft" de positie?)
                database.Connection.Execute("alter table Position add UpdateTime TEXT", transaction);
                database.Connection.Execute("alter table Position add Reposition Integer", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 4)
            {
                using var transaction = database.BeginTransaction();

                database.Connection.Execute("insert into exchange(Name) values('Kraken')", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId) values('Trading', 'Kraken trading', 0, 2, 5);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId) values('Paper', 'Kraken paper', 0, 1, 5);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId) values('Backtest', 'Kraken backtest', 0, 0, 5);", transaction);
                
                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 5)
            {
                using var transaction = database.BeginTransaction();

                // Op wat voor signaal doen we de aan- of bijkoop
                database.Connection.Execute("alter table PositionPart add Strategy Integer;", transaction);

                // Welk interval had de BUY of DCA? (buy trailen in het juiste interval)
                database.Connection.Execute("alter table PositionPart add IntervalId Integer;", transaction);
                // Je kunt achteraf niet een contraint toevoegen, dan moet de hele tabel opnieuw gemaakt worden, pfft..
                //database.Connection.Execute("alter table PositionPart add constraint fkPositionPartInterval foreign key(IntervalId) references Interval(id);", transaction);

                database.Connection.Execute("CREATE INDEX IdxPositionPartIntervalId ON PositionPart(IntervalId)");

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            // TODO: Wellicht de fee van exchange administreren, 0.01% voor limt orders
            // en 0.015 voor market orders of iets dergelijks (iets met maker en taker)
        }
    }


}

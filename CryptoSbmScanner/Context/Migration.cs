using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Context;

public class Migration
{
    // De huidige database versie
    public readonly static int CurrentDatabaseVersion = 11;


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


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 6)
            {
                using var transaction = database.BeginTransaction();

                // +Introductie van een Exchange.FeeRate en deze vullen voor alle exchanges (de default fee voor een exchange)
                //  0.01% voor limt orders en 0.015 voor market orders of iets dergelijks (iets met maker en taker)
                database.Connection.Execute("alter table Exchange add FeeRate TEXT", transaction);
                // Alle exchanges staan voorlopig op dezelfde feerate
                database.Connection.Execute("update Exchange set FeeRate=0.001", transaction);

                // -Verwijderen van de Part.Status + de laatste code (verplicht veld)
                database.Connection.Execute("alter table PositionPart drop column Status", transaction);

                // -Verwijderen van de Step.Name, dit is een alias voor de Side
                database.Connection.Execute("alter table PositionStep drop column Name", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 7)
            {
                using var transaction = database.BeginTransaction();

                // Introductie van een nummer per part (initiele buy/sell=0, >0 zijn de dca's)
                database.Connection.Execute("alter table PositionPart add PartNumber Integer", transaction);
                database.Connection.Execute("update PositionPart set PartNumber=0 where name='BUY'", transaction);
                database.Connection.Execute("update PositionPart set PartNumber=1 where name='DCA'", transaction);

                // Op verzoek enige trend indicatoren per interval (slechts een paar)
                database.Connection.Execute("alter table Signal add Trend15m Integer", transaction);
                database.Connection.Execute("alter table Signal add Trend30m Integer", transaction);
                database.Connection.Execute("alter table Signal add Trend1h Integer", transaction);
                database.Connection.Execute("alter table Signal add Trend4h Integer", transaction);
                database.Connection.Execute("alter table Signal add Trend12h Integer", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }



            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 8)
            {
                using var transaction = database.BeginTransaction();

                // -Een ongebruikte kolom
                database.Connection.Execute("alter table Position drop column BuyAmount", transaction);

                // ? of verwijder ik de buy en sell price helemaal?

                // Vervangt de buyprice (naamgeving ivm long/short)
                database.Connection.Execute("alter table Position add EntryPrice TEXT null", transaction);
                database.Connection.Execute("update Position set EntryPrice=buyPrice", transaction);
                database.Connection.Execute("alter table Position drop column BuyPrice", transaction);

                // Vervangt de sellprice (naamgeving ivm long/short)
                database.Connection.Execute("alter table Position add ProfitPrice TEXT null", transaction);
                database.Connection.Execute("update Position set ProfitPrice=sellPrice", transaction);
                database.Connection.Execute("alter table Position drop column SellPrice", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 9)
            {
                using var transaction = database.BeginTransaction();

                // Vanwege DCA bijkoop (daarvoor was die ongebruikte kolom BuyAmount dus bedoeld!)
                database.Connection.Execute("alter table Position add EntryAmount TEXT null", transaction);


                // Introductie van een purpose voor vasstellen van het doen van een part (entry of dca)
                database.Connection.Execute("alter table PositionPart add Purpose Integer", transaction);
                database.Connection.Execute("update PositionPart set Purpose=0 where name='BUY'", transaction);
                database.Connection.Execute("update PositionPart set Purpose=1 where name='DCA'", transaction);

                // Daardoor vervalt het bestaansrecht van de velden Name en Side
                database.Connection.Execute("alter table PositionPart drop column Name", transaction);
                database.Connection.Execute("alter table PositionPart drop column Side", transaction);


                // Vervangt de StepInMethod door EntryMethod (naamgeving ivm long/short)
                database.Connection.Execute("alter table PositionPart add EntryMethod TEXT null", transaction);
                database.Connection.Execute("update PositionPart set EntryMethod=StepInMethod", transaction);
                database.Connection.Execute("alter table PositionPart drop column StepInMethod", transaction);

                // Vervangt de StepOutMethod door ProfitMethod (naamgeving ivm long/short)
                database.Connection.Execute("alter table PositionPart add ProfitMethod TEXT null", transaction);
                database.Connection.Execute("update PositionPart set ProfitMethod=StepOutMethod", transaction);
                database.Connection.Execute("alter table PositionPart drop column StepOutMethod", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 10)
            {
                using var transaction = database.BeginTransaction();

                database.Connection.Execute("alter table PositionPart add EntryAmount TEXT null", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 11)
            {
                using var transaction = database.BeginTransaction();

                // Deze bestaat reeds op position niveau en kan daarom weer weg
                database.Connection.Execute("alter table PositionPart drop column EntryAmount", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }
        }
    }

}

using CryptoScanBot.Core.Enums;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Context;

public class Migration
{
    // De huidige database versie
    public readonly static int CurrentDatabaseVersion = 31;


    public static void Execute(CryptoDatabase database, int CurrentVersion)
    {
        CryptoVersion version = database.Connection.GetAll<CryptoVersion>().First();

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
                database.Connection.Execute("alter table Signal add Trend1d Integer", transaction);

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

                // Indicatie dat er een openstaande DCA aanwezig is + migratie
                // Voor een nieuw statistiek idee moet de partcount alleen de actieve dca's bevatten
                database.Connection.Execute("alter table Position add ActiveDca Integer null", transaction);
                database.Connection.Execute("update Position set ActiveDca=0", transaction);
                database.Connection.Execute("update Position set ActiveDca=(select count(*) from positionpart where positionpart.positionid=Position.id and invested <= 0.0)", transaction);
                database.Connection.Execute("update Position set ActiveDca=1 where ActiveDca>1", transaction);

                // Administratie voor het geval we handmatig een order openen (en we deze niet willen laten aanpassen)
                database.Connection.Execute("alter table PositionPart add ManualOrder Integer", transaction);
                database.Connection.Execute("update PositionPart set ManualOrder=0", transaction);

                // Deze bestaat reeds op position niveau en kan daarom weg
                database.Connection.Execute("alter table PositionPart drop column EntryAmount", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 12)
            {
                using var transaction = database.BeginTransaction();

                // Indicatie van wat er aan DCA's gereserveerd is (extra geld welke nodig is voor de dca's)
                database.Connection.Execute("alter table Position add Reserved TEXT NULL", transaction);
                database.Connection.Execute("alter table PositionPart add Reserved TEXT NULL", transaction);

                // Er zitten blijkbaar typo's in de exchange tabel
                database.Connection.Execute("update TradeAccount set Short='Paper' where Short='Pater'", transaction);
                database.Connection.Execute("update TradeAccount set ExchangeId=2 where Name='Bybit Spot trading'", transaction);

                // We ondersteunen niet op alle exchanges trading (of het is ongetest)
                database.Connection.Execute("alter table TradeAccount add CanTrade Integer null", transaction);
                database.Connection.Execute("update TradeAccount set CanTrade=1", transaction);
                database.Connection.Execute("update TradeAccount set CanTrade=0 where Name like '%Binance%' and TradeAccountType=2", transaction);
                database.Connection.Execute("update TradeAccount set CanTrade=0 where Name like '%Kraken%' and TradeAccountType=2", transaction);
                database.Connection.Execute("update TradeAccount set CanTrade=0 where Name like '%Kucoin%' and TradeAccountType=2", transaction);

                // Voor Bybit spot moeten we bijhouden hoeveel dust we overhouden (administratie)
                database.Connection.Execute("alter table positionstep add RemainingDust Text null", transaction);
                database.Connection.Execute("alter table PositionPart add RemainingDust Text null", transaction);
                database.Connection.Execute("alter table Position add RemainingDust Text null", transaction);

                // Voor Bybit spot moeten we bijhouden in welke asset de commisie wordt afgetrokken
                // TODO: Leuk voro de debug, maar kan ook al weer weg
                database.Connection.Execute("alter table PositionStep add CommissionAsset Text null", transaction);

                // In de trade tabel is het een not nullable veld, mag null zijn (blijkbaar)
                database.Connection.Execute("alter table Trade drop column CommissionAsset", transaction);
                database.Connection.Execute("alter table Trade add CommissionAsset Text null", transaction);


                // Voor Bybit spot moeten we bijhouden hoeveel dust we overhouden (administratie)
                database.Connection.Execute("alter table positionstep add CommissionBase Text null", transaction);
                database.Connection.Execute("alter table PositionPart add CommissionBase Text null", transaction);
                database.Connection.Execute("alter table Position add CommissionBase Text null", transaction);

                // Voor Bybit spot moeten we bijhouden hoeveel dust we overhouden (administratie)
                database.Connection.Execute("alter table positionstep add CommissionQuote Text null", transaction);
                database.Connection.Execute("alter table PositionPart add CommissionQuote Text null", transaction);
                database.Connection.Execute("alter table Position add CommissionQuote Text null", transaction);

                // Bybit spot doet de administratie op ID ipv date
                database.Connection.Execute("alter table symbol add LastTradeIdFetched integer null", transaction);
                database.Connection.Execute("alter table symbol add LastOrderFetched Text null", transaction);

                // Bybit spot levert deze niet aan en om problemen te vermijden uit de orders halen
                database.Connection.Execute("alter table trade drop column side", transaction);

                // Boundaries for Quote Value
                database.Connection.Execute("alter table symbol add QuoteValueMinimum Text null", transaction);
                database.Connection.Execute("alter table symbol add QuoteValueMaximum Text null", transaction);

                database.Connection.Execute("alter table positionstep add AveragePrice Text null", transaction);
                database.Connection.Execute("alter table positionstep drop column AvgPrice", transaction);
                database.Connection.Execute("alter table positionstep drop column CommissionAsset", transaction);

                database.Connection.Execute("alter table PositionPart add QuantityEntry Text null", transaction);
                database.Connection.Execute("alter table PositionPart add QuantityTakeProfit Text null", transaction);
                database.Connection.Execute("alter table Position add QuantityEntry Text null", transaction);
                database.Connection.Execute("alter table Position add QuantityTakeProfit Text null", transaction);

                // De trade tabel is nu vervangen door de order tabel
                //database.Connection.Execute("drop table trade", transaction);

                // vervallen? step.AvgPrice

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 13)
            {
                using var transaction = database.BeginTransaction();

                database.Connection.Execute("alter table positionstep add CancelInProgress Integer null", transaction);
                database.Connection.Execute("update positionstep set CancelInProgress=0", transaction);
                database.Connection.Execute("update positionstep set CancelInProgress=1 where status>4", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 14)
            {
                using var transaction = database.BeginTransaction();

                // Nieuwe exchange Binance Futures
                database.Connection.Execute("insert into exchange(Name, FeeRate) values('Binance Futures', 0.1)", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Trading', 'Binance Futures trading', 0, 2, 6, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Paper', 'Binance Futures paper', 0, 1, 6, 0);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Backtest', 'Binance Futures backtest', 0, 0, 6, 0);", transaction);

                // Onderverdeling maken in Spot en Futures
                database.Connection.Execute("update exchange set name='Binance Spot' where name='Binance'", transaction);
                database.Connection.Execute("update TradeAccount set name='Binance Spot trading' where name= 'Binance trading';", transaction);
                database.Connection.Execute("update TradeAccount set name='Binance Spot paper' where name= 'Binance paper';", transaction);
                database.Connection.Execute("update TradeAccount set name='Binance Spot backtest' where name= 'Binance backtest';", transaction);

                // Onderverdeling maken in Spot en Futures
                database.Connection.Execute("update exchange set name='Kraken Spot', FeeRate=0.25 where name='Kraken'", transaction);
                database.Connection.Execute("update TradeAccount set name='Kraken Spot trading' where name= 'Kraken trading';", transaction);
                database.Connection.Execute("update TradeAccount set name='Kraken Spot paper' where name= 'Kraken paper';", transaction);
                database.Connection.Execute("update TradeAccount set name='Kraken Spot backtest' where name= 'Kraken backtest';", transaction);

                // Onderverdeling maken in Spot en Futures
                database.Connection.Execute("update exchange set name='Kucoin Spot' where name='Kucoin'", transaction);
                database.Connection.Execute("update TradeAccount set name='Kucoin Spot trading' where name= 'Kucoin trading';", transaction);
                database.Connection.Execute("update TradeAccount set name='Kucoin Spot paper' where name= 'Kucoin paper';", transaction);
                database.Connection.Execute("update TradeAccount set name='Kucoin Spot backtest' where name= 'Kucoin backtest';", transaction);

                // Kraken inactief zetten (de klines zijn ontzettend traag en de fee is ook gewoon te hoog 0.25%)
                database.Connection.Execute("update TradeAccount set CanTrade=0 where Name like '%Kraken%' and TradeAccountType=2", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }


            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 15)
            {
                using var transaction = database.BeginTransaction();

                // psar values debug
                database.Connection.Execute("alter table signal add PSarDave Text null", transaction);
                database.Connection.Execute("alter table signal add PSarJason Text null", transaction);
                database.Connection.Execute("alter table signal add PSarTulip Text null", transaction);

                // statistics
                database.Connection.Execute("alter table signal add PriceMin Text null", transaction);
                database.Connection.Execute("alter table signal add PriceMax Text null", transaction);
                database.Connection.Execute("alter table signal add PriceMinPerc Text null", transaction);
                database.Connection.Execute("alter table signal add PriceMaxPerc Text null", transaction);

                // For now Kraken is not fully supported (so we make it inactive until it is fixed)
                database.Connection.Execute("alter table exchange add IsActive Integer", transaction);
                database.Connection.Execute("update exchange set IsActive=1", transaction);
                database.Connection.Execute("update exchange set IsActive=0 where Name like '%Kraken%'", transaction);

                // New exchange Kucoin Futures (experiment, failed on klines unitil now)
                database.Connection.Execute("insert into exchange(Name, FeeRate, IsActive) values('Kucoin Futures', 0.1, 0)", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Trading', 'Kucoin Futures trading', 0, 2, 7, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Paper', 'Kucoin Futures paper', 0, 1, 7, 0);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Backtest', 'Kucoin Futures backtest', 0, 0, 7, 0);", transaction);

                // The exchangeId's in the TradeAccount of Binance Futures are not right (wrong initialisation)
                database.Connection.Execute("update TradeAccount set ExchangeId=6 where name like 'Binance Futures%'", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }




            //***********************************************************
            // 20240602 1.9.3 in progress
            if (CurrentVersion > version.Version && version.Version == 16)
            {
                using var transaction = database.BeginTransaction();

                // rename Signal.FluxIndicator5m LuxIndicator5m (typo)
                database.Connection.Execute("alter table signal add LuxIndicator5m Text null", transaction);
                database.Connection.Execute("update signal set LuxIndicator5m=FluxIndicator5m", transaction);
                database.Connection.Execute("alter table signal drop column FluxIndicator5m", transaction);

                // Introduce Signal.Backtest (because of emulator)
                database.Connection.Execute("alter table signal add Backtest Integer null", transaction);

                // After some tweaking no longer needed (we correct the quantity field instead)
                database.Connection.Execute("alter table PositionPart drop column QuantityEntry", transaction);
                database.Connection.Execute("alter table PositionPart drop column QuantityTakeProfit", transaction);
                database.Connection.Execute("alter table Position drop column QuantityEntry", transaction);
                database.Connection.Execute("alter table Position drop column QuantityTakeProfit", transaction);

                // Feerate was recently increased, also for market orders.
                database.Connection.Execute("update exchange set FeeRate=0.15 where Name like '%Bybit Spot%'", transaction);


                // Introduce separate fee for market orders and fee for limit orders?

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }



            //***********************************************************
            // 20240615 1.9.3 in progress
            if (CurrentVersion > version.Version && version.Version == 17)
            {
                using var transaction = database.BeginTransaction();

                // remove unused Symbol.LastOrderFetched 
                database.Connection.Execute("alter table Symbol drop column LastOrderFetched", transaction);

                // remove Symbol.TrendPercentage
                database.Connection.Execute("alter table Symbol drop column TrendPercentage", transaction);

                // remove Symbol.TrendInfoDate
                database.Connection.Execute("alter table Symbol drop column TrendInfoDate", transaction);

                // New exchange Mexc Spot
                database.Connection.Execute("insert into exchange(Name, FeeRate, IsActive) values('Mexc Spot', 0.1, 0)", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Trading', 'Mexc Spot trading', 0, 2, 8, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Paper', 'Mexc Spot paper', 0, 1, 8, 0);", transaction);
                database.Connection.Execute("insert into TradeAccount(Short, Name, AccountType, TradeAccountType, ExchangeId, CanTrade) values('Backtest', 'Mexc Spot backtest', 0, 0, 8, 0);", transaction);


                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }

            //***********************************************************
            // 20240618 1.9.3 in progress
            if (CurrentVersion > version.Version && version.Version == 18)
            {
                using var transaction = database.BeginTransaction();

                // Exchange, AccountType=spot or futures
                database.Connection.Execute("alter table Exchange add TradingType Integer null", transaction);
                database.Connection.Execute("update exchange set TradingType=0", transaction);
                database.Connection.Execute("update exchange set TradingType=1 where Name like '%Futures%'", transaction);

                // Purpose was if the exchange can be truely supported
                database.Connection.Execute("alter table exchange rename column IsActive to IsSupported", transaction);

                // Introduce an attribute for spot/futures
                database.Connection.Execute("alter table Exchange add ExchangeType Integer null", transaction);
                database.Connection.Execute($"update exchange set ExchangeType={(int)CryptoExchangeType.Binance} where Name like '%Binance%'", transaction);
                database.Connection.Execute($"update exchange set ExchangeType={(int)CryptoExchangeType.Bybit} where Name like '%Bybit%'", transaction);
                database.Connection.Execute($"update exchange set ExchangeType={(int)CryptoExchangeType.Kraken} where Name like '%Kraken%'", transaction);
                database.Connection.Execute($"update exchange set ExchangeType={(int)CryptoExchangeType.Kucoin} where Name like '%Kucoin%'", transaction);
                database.Connection.Execute($"update exchange set ExchangeType={(int)CryptoExchangeType.Mexc} where Name like '%Mexc%'", transaction);

                // Remove redundant fields
                database.Connection.Execute("drop index idxTradeAccountName", transaction);
                database.Connection.Execute("alter table TradeAccount drop column Name", transaction);
                database.Connection.Execute("alter table TradeAccount drop column Short", transaction);
                database.Connection.Execute("alter table TradeAccount drop column AccountType", transaction);
                database.Connection.Execute("alter table TradeAccount rename column TradeAccountType to AccountType", transaction);

                // Add Altrady webhook accounts
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 1, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 2, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 3, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 4, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 5, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 6, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 7, 1);", transaction);
                database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 8, 1);", transaction);

                //// in the model the table name is account -> TradeAccount
                //// Would love to rename the table in the db as well, but there is a contraint position -> tradeaccount
                //// And sqllite does not support dropping columns with contraints <really, what a weird database enigine>
                //// Need to copy that table, recreate it without constraint and copy the data back <sigh>
                //// There is however a rename table

                ////database.Connection.Execute("alter table position add AccountId Integer null", transaction);
                ////database.Connection.Execute("update position set AccountId=TradeAccountId", transaction);
                ////database.Connection.Execute("alter table position drop TradeAccountId", transaction); // not possible because of positions

                //database.Connection.Execute("drop INDEX IdxPositionId", transaction);
                //database.Connection.Execute("drop INDEX IdxPositionExchangeId", transaction);
                //database.Connection.Execute("drop INDEX IdxPositionSymbolId", transaction);
                //database.Connection.Execute("drop INDEX IdxPositionCreateTime", transaction);
                //database.Connection.Execute("drop INDEX IdxPositionCloseTime", transaction);
                //database.Connection.Execute("drop INDEX IdxPositionTradeAccountId", transaction);

                //database.Connection.Execute("alter table Position rename to PositionCopy", transaction);
                //CryptoDatabase.CreateTablePosition(database, transaction);
                ////database.Connection.Execute("insert into position select positionCopy.TradeAccountId as AccountId, positionCopy.* from positionCopy", transaction);
                //database.Connection.Execute("insert into position select * from positionCopy", transaction);
                //database.Connection.Execute("drop table if exists PositionCopy", transaction);
                ////transaction.Commit();


                //database.Connection.Execute("insert into Account select * from TradeAccount", transaction);
                //database.Connection.Execute("drop table if exists TradeAccount", transaction);




                // The table was created by the the database check
                //database.Connection.Execute("drop table if exists Account", transaction);
                //database.Connection.Execute("alter table TradeAccount rename to Account", transaction);
                // fixed it by table attribute

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }

        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 19)
        {
            using var transaction = database.BeginTransaction();

            // Mexc Spot fix
            database.Connection.Execute("update Exchange set IsSupported=1 where name='Mexc Spot'", transaction);

            // symbol, drop LastPrice
            database.Connection.Execute("alter table symbol drop column LastPrice", transaction);

            // signal, drop psar comparison columns
            database.Connection.Execute("alter table signal drop column PSarDave", transaction);
            database.Connection.Execute("alter table signal drop column PSarJason", transaction);
            database.Connection.Execute("alter table signal drop column PSarTulip", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }
        // Mexc Futures (experimental), but Mexc Futures does not have a proper api yet
        //database.Connection.Execute("insert into exchange(Name, FeeRate, IsSupported, ExchangeType, TradingType) values('Mexc Futures', 0.1, 0, 5, 1)", transaction);
        //database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(0, 9, 1);", transaction);
        //database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(1, 9, 1);", transaction);
        //database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(2, 9, 1);", transaction);
        //database.Connection.Execute("insert into TradeAccount(AccountType, ExchangeId, CanTrade) values(3, 9, 1);", transaction);


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 20)
        {
            using var transaction = database.BeginTransaction();

            // unused field afaics
            database.Connection.Execute("alter table Signal drop column InfoDate", transaction);
            database.Connection.Execute("alter table Signal drop column Last48Hours", transaction);

            // Added a long time ago, but not in db?
            database.Connection.Execute("alter table Signal add Last10DaysEffective text", transaction);


            // add data from the original signal (for statistics etc)
            database.Connection.Execute("alter table Position add SignalEventTime Text null", transaction);
            database.Connection.Execute("alter table Position add SignalPrice Text null", transaction);
            database.Connection.Execute("alter table Position add SignalVolume Text null", transaction);

            database.Connection.Execute("alter table Position add Last24HoursChange Text null", transaction);
            database.Connection.Execute("alter table Position add Last24HoursEffective Text null", transaction);
            database.Connection.Execute("alter table Position add Last10DaysEffective Text null", transaction);

            database.Connection.Execute("alter table Position add TrendPercentage Text null", transaction);
            database.Connection.Execute("alter table Position add TrendIndicator Text null", transaction);

            database.Connection.Execute("alter table Position add StochOscillator Text null", transaction);
            database.Connection.Execute("alter table Position add StochSignal Text null", transaction);

            database.Connection.Execute("alter table Position add BollingerBandsUpperBand Text null", transaction);
            database.Connection.Execute("alter table Position add BollingerBandsLowerBand Text null", transaction);
            database.Connection.Execute("alter table Position add BollingerBandsPercentage Text null", transaction);

            database.Connection.Execute("alter table Position add PSar Text null", transaction);
            database.Connection.Execute("alter table Position add Rsi Text null", transaction);
            database.Connection.Execute("alter table Position add LuxIndicator5m Text null", transaction);

            database.Connection.Execute("alter table Position add Sma20 Text null", transaction);
            database.Connection.Execute("alter table Position add Sma50 Text null", transaction);
            database.Connection.Execute("alter table Position add Sma200 Text null", transaction);

            database.Connection.Execute("alter table Position add CandlesWithZeroVolume Text null", transaction);
            database.Connection.Execute("alter table Position add CandlesWithFlatPrice Text null", transaction);
            database.Connection.Execute("alter table Position add AboveBollingerBandsSma Text null", transaction);
            database.Connection.Execute("alter table Position add AboveBollingerBandsUpper Text null", transaction);

            database.Connection.Execute("alter table Position add Trend15m Integer", transaction);
            database.Connection.Execute("alter table Position add Trend30m Integer", transaction);
            database.Connection.Execute("alter table Position add Trend1h Integer", transaction);
            database.Connection.Execute("alter table Position add Trend4h Integer", transaction);
            database.Connection.Execute("alter table Position add Trend1d Integer", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 21)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal add Barometer15m Text null", transaction);
            database.Connection.Execute("alter table Signal add Barometer30m Text null", transaction);
            database.Connection.Execute("alter table Signal add Barometer1h Text null", transaction);
            database.Connection.Execute("alter table Signal add Barometer4h Text null", transaction);
            database.Connection.Execute("alter table Signal add Barometer1d Text null", transaction);

            database.Connection.Execute("alter table Position add Barometer15m Text null", transaction);
            database.Connection.Execute("alter table Position add Barometer30m Text null", transaction);
            database.Connection.Execute("alter table Position add Barometer1h Text null", transaction);
            database.Connection.Execute("alter table Position add Barometer4h Text null", transaction);
            database.Connection.Execute("alter table Position add Barometer1d Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 22)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table signal drop column Trend12h", transaction);
            database.Connection.Execute("alter table signal add Trend1d Integer", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 23)
        {
            using var transaction = database.BeginTransaction();

            // This update is empty because I made a mess..

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 24)
        {
            using var transaction = database.BeginTransaction();

            // This update is empty because I made a mess..

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 25)
        {
            using var transaction = database.BeginTransaction();

            // Note: Add a AT signal string to the position table from the Altrady response 
            database.Connection.Execute("alter table Position add AltradyPositionId Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 04-10-2024, added slopes + make signal and position table more the same
        if (CurrentVersion > version.Version && version.Version == 26)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal add SlopeSma100 Text null", transaction);
            database.Connection.Execute("alter table Position add SlopeSma100 Text null", transaction);

            database.Connection.Execute("alter table Signal add SlopeSma200 Text null", transaction);
            database.Connection.Execute("alter table Position add SlopeSma200 Text null", transaction);

            database.Connection.Execute("alter table Signal add MacdValue Text null", transaction);
            database.Connection.Execute("alter table Signal add MacdSignal Text null", transaction);
            database.Connection.Execute("alter table Signal add MacdHistogram Text null", transaction);

            database.Connection.Execute("alter table Position add MacdValue Text null", transaction);
            database.Connection.Execute("alter table Position add MacdSignal Text null", transaction);
            database.Connection.Execute("alter table Position add MacdHistogram Text null", transaction);

            database.Connection.Execute("alter table Position add BollingerBandsDeviation Text null", transaction);
            database.Connection.Execute("alter table Position drop column BollingerBandsLowerBand", transaction);
            database.Connection.Execute("alter table Position drop column BollingerBandsUpperBand", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }



        //***********************************************************
        // 04-10-2024, make signal and position table more the same
        if (CurrentVersion > version.Version && version.Version == 27)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal rename column Price to SignalPrice", transaction);
            database.Connection.Execute("alter table Signal rename column Volume to SignalVolume", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 08-10-2024, make signal and position table more the same (added statistics)
        if (CurrentVersion > version.Version && version.Version == 28)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Position add PriceMin Text null", transaction);
            database.Connection.Execute("alter table Position add PriceMax Text null", transaction);
            database.Connection.Execute("alter table Position add PriceMinPerc Text null", transaction);
            database.Connection.Execute("alter table Position add PriceMaxPerc Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        if (CurrentVersion > version.Version && version.Version == 29)
        {
            using var transaction = database.BeginTransaction();

            // This update is empty because I made a mess..

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 08-10-2024, problems with the slope fields (added way back perhaps, or not?)
        if (CurrentVersion > version.Version && version.Version == 30)
        {
            using var transaction = database.BeginTransaction();

            try { database.Connection.Execute("alter table Signal add Sma20 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add Sma50 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add Sma100 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add Sma200 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add SlopeRsi Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add SlopeSma20 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add SlopeSma50 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add SlopeSma100 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal add SlopeSma200 Text null", transaction); } catch { } // ignore

            try { database.Connection.Execute("alter table Position add Sma20 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add Sma50 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add Sma100 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add Sma200 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add SlopeRsi Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add SlopeSma20 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add SlopeSma50 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add SlopeSma100 Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position add SlopeSma200 Text null", transaction); } catch { } // ignore

            try { database.Connection.Execute("delete from asset", transaction); } catch { } // ignore, start from scratch

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }
    }
}


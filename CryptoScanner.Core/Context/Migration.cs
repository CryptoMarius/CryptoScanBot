using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Context;

public class Migration
{
    // Latest and greatest database version
    public readonly static int CurrentDatabaseVersion = 71;


    private static void UpdateExchanges(CryptoDatabase database)
    {
        using var transaction = database.BeginTransaction();

        foreach (var e in CryptoDatabase.CreateExchangeList())
        {
            string sql = $"update Exchange set " +
                $"ExchangeType={(int)e.ExchangeType}, " +
                $"TradingType={(int)e.TradingType}, " +
                $"IsSupported={e.IsSupported}, " +
                $"lastTimeFetched=null " + // Make sure symbols are loaded again from the exchange so it wil fill the Symbol.ExchangeName
                $"where name=\'{e.Name}\'";
            int count = database.Connection.Execute(sql, transaction);
            if (count == 0)
            {
                sql = $"insert into exchange(ExchangeType, TradingType, Name, FeeRate, IsSupported)" +
                "Values(" +
                $"{(int)e.ExchangeType}, " +
                $"{(int)e.TradingType}, " +
                $"\'{e.Name}\', " +
                "0.1," +
                $"{e.IsSupported}" +
                $")";
                database.Connection.Execute(sql, transaction);
            }
        }

        // Forceer dat de symbol informatie (en funding rates) opgehaald wordt
        //database.Connection.Execute("update Exchange set LastTimeFetched=null", transaction);

        transaction.Commit();
    }


    public static void Execute(CryptoDatabase database, int CurrentVersion)
    {
        bool updateExchanges = false;
        CryptoVersion version = database.Connection.GetAll<CryptoVersion>().First();
        if (CurrentVersion != version.Version || version.Version < 2)
            updateExchanges = true;


        if (CurrentVersion > version.Version)
        {
            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 1)
            {
                using var transaction = database.BeginTransaction();

                // De fee moet erbij zodat we achteraf kunnen rapporteren (anders moet dat via de trades)
                database.Connection.Execute("alter table PositionStep add Commission TEXT NOT NULL default 0", transaction);

                // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
                // Wat het inhoud weet ik nog niet (toegevoegde waarde, voor trading is er waarschijnlijk wel)
                // Het type is waarschijnlijk ook niet goed ingesteld, maar met text kom je een heel eind
                // https://bybit-exchange.github.io/docs/v5/market/History-fund-rate
                database.Connection.Execute("alter table symbol add FundingRate TEXT", transaction);
                database.Connection.Execute("alter table symbol add FundingInterval TEXT", transaction);

                // update version
                version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }



            //***********************************************************
            if (CurrentVersion > version.Version && version.Version == 2)
            {
                using var transaction = database.BeginTransaction();

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

                // -Verwijderen van de Step.ExchangeSymbol, dit is een alias voor de Side
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

                // Daardoor vervalt het bestaansrecht van de velden ExchangeSymbol en Side
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

                database.Connection.Execute("alter table positionstep add CancelInProgress INTEGER NULL DEFAULT 0", transaction);
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

                // remove unused ScannerSymbol.LastOrderFetched
                database.Connection.Execute("alter table Symbol drop column LastOrderFetched", transaction);

                // remove ScannerSymbol.TrendPercentage
                database.Connection.Execute("alter table Symbol drop column TrendPercentage", transaction);

                // remove ScannerSymbol.TrendInfoDate
                database.Connection.Execute("alter table Symbol drop column TrendInfoDate", transaction);

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

                // Purpose was if the exchange can be truely supported
                database.Connection.Execute("alter table exchange rename column IsActive to IsSupported", transaction);

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

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 29-10-2024, problems with paper asset management
        if (CurrentVersion > version.Version && version.Version == 31)
        {
            using var transaction = database.BeginTransaction();

            try { database.Connection.Execute("delete from asset", transaction); } catch { } // ignore, start from scratch
            try { database.Connection.Execute("alter table PositionStep add IsCalculated INTEGER NOT NULL DEFAULT 0", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 30-10-2024, AvgBb introduced properly
        if (CurrentVersion > version.Version && version.Version == 32)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal add AvgBb Text null", transaction);
            database.Connection.Execute("alter table Position add AvgBb Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }



        //***********************************************************
        // 24-11-2024, zones, startdate
        if (CurrentVersion > version.Version && version.Version == 33)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 25-11-2024, CloseTime -> CloseTime
        if (CurrentVersion > version.Version && version.Version == 34)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 09-12-2024, deleted two columns
        if (CurrentVersion > version.Version && version.Version == 35)
        {
            using var transaction = database.BeginTransaction();

            // set the feerates of futures to 0
            try { database.Connection.Execute("update table exchange set feerate=0 where TradingType=1", transaction); } catch { }

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 11-12-2024, to show invalid zones
        if (CurrentVersion > version.Version && version.Version == 36)
        {
            using var transaction = database.BeginTransaction();

            // Barometer problem, clean symbols without a name and dump the price and volume barometer
            try { database.Connection.Execute("delete from symbol where name = ''", transaction); } catch { } // ignore
            try { database.Connection.Execute("delete from symbol where name like '$BMP%'", transaction); } catch { } // ignore
            try { database.Connection.Execute("delete from symbol where name like '$BMV%'", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 20-12-2024, to show other kind of zones?
        if (CurrentVersion > version.Version && version.Version == 37)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 23-01-2025, added interval to zone table (foreign key, just drop the table)
        if (CurrentVersion > version.Version && version.Version == 38)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 25-01-2025, added OKX experimental
        if (CurrentVersion > version.Version && version.Version == 39)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();

            // todo: Delete CryptoScanner-weblinks.json?
        }


        //***********************************************************
        // 31-01-2025 Indication of weak or strong boxes
        if (CurrentVersion > version.Version && version.Version == 40)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 31-01-2025, added Coinbase experimental
        if (CurrentVersion > version.Version && version.Version == 41)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 12-02-2025, mixed up some statistics fields
        if (CurrentVersion > version.Version && version.Version == 42)
        {
            using var transaction = database.BeginTransaction();

            // Signals: mixed up statistics fields
            try { database.Connection.Execute("alter table Signal add LastXDaysEffective Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("update Signal set LastXDaysEffective=Last10DaysEffective", transaction); } catch { } // ignore

            try { database.Connection.Execute("alter table Signal drop column Last48Hours", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal drop column Last24HoursEffective", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Signal drop column Last10DaysEffective", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }



        //***********************************************************
        // 19-02-2025, secondary trend, two extra variables for total surface area oversold stoch/rsi?
        if (CurrentVersion > version.Version && version.Version == 43)
        {
            using var transaction = database.BeginTransaction();

            // signal, add StochSurface and RsiSurface
            // Position and Signal table: TrendPercentage, split into primary or secundary trend
            database.Connection.Execute("alter table Signal rename column TrendPercentage to TrendPercentagePrimary", transaction);
            database.Connection.Execute("alter table Signal add column TrendPercentageSecondary TEXT null", transaction);
            database.Connection.Execute("alter table Signal add column RsiSurface TEXT null", transaction);
            database.Connection.Execute("alter table Signal add column StochSurface TEXT null", transaction);
            database.Connection.Execute("alter table Signal add column RsiSurface2 TEXT null", transaction);
            database.Connection.Execute("alter table Signal add column StochSurface2 TEXT null", transaction);

            database.Connection.Execute("alter table Position rename column TrendPercentage to TrendPercentagePrimary", transaction);
            database.Connection.Execute("alter table Position add column TrendPercentageSecondary TEXT null", transaction);
            database.Connection.Execute("alter table Position add column RsiSurface TEXT null", transaction);
            database.Connection.Execute("alter table Position add column StochSurface TEXT null", transaction);
            database.Connection.Execute("alter table Position add column RsiSurface2 TEXT null", transaction);
            database.Connection.Execute("alter table Position add column StochSurface2 TEXT null", transaction);

            // Unused fields
            try { database.Connection.Execute("alter table Symbol drop column TrendInfoDate", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Symbol drop column TrendPercentage", transaction); } catch { } // ignore

            // A much better fieldname
            database.Connection.Execute("alter table Signal rename column TrendIndicator to TrendInterval", transaction);
            database.Connection.Execute("alter table Position rename column TrendIndicator to TrendInterval", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 23-08-2025
        if (CurrentVersion > version.Version && version.Version == 44)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 28-08-2025 add week interval
        if (CurrentVersion > version.Version && version.Version == 45)
        {
            using var transaction = database.BeginTransaction();

            try { database.Connection.Execute("insert into interval(intervalperiod, name, duration, constructfromid) values(15, '1w', 10080, 15)", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 02-09-2025, added HyperLiquid spot experimental
        if (CurrentVersion > version.Version && version.Version == 46)
        {
            using var transaction = database.BeginTransaction();

            // Problem, sqlite does not support dropping foreign key.
            // So we make the db corrupt because of "drop table tradeAccount!"
            try { database.Connection.Execute("drop table [TradeAccount]", transaction); } catch { } // ignore
            try { database.Connection.Execute("drop table [Zone]", transaction); } catch { } // has an accountid field

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 02-09-2025, added HyperLiquid Futures experimental
        if (CurrentVersion > version.Version && version.Version == 47)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 03-09-2025, added Kraken futures
        if (CurrentVersion > version.Version && version.Version == 48)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 04-09-2025, added BitMart
        if (CurrentVersion > version.Version && version.Version == 49)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 04-09-2025, correction, forgot to rename Position.Last10DaysEffective
        if (CurrentVersion > version.Version && version.Version == 50)
        {
            using var transaction = database.BeginTransaction();

            try { database.Connection.Execute("alter table Position add LastXDaysEffective Text null", transaction); } catch { } // ignore
            try { database.Connection.Execute("update Position set LastXDaysEffective=Last10DaysEffective", transaction); } catch { } // ignore

            try { database.Connection.Execute("alter table Position drop column Last48Hours", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position drop column Last24HoursEffective", transaction); } catch { } // ignore
            try { database.Connection.Execute("alter table Position drop column Last10DaysEffective", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 04-09-2025, added BitMart
        if (CurrentVersion > version.Version && version.Version == 51)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 11-09-2025, added Bybit EU
        if (CurrentVersion > version.Version && version.Version == 52)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 18-10-2025
        // Kraken spot is unstable, high cpu and no signals
        // Bybit EU Futures does yet not have any symbols
        // HyperLiquid Spot does not have any symbols and Altrady doesn't support it
        // Kraken is not stable enough (memory overflow problems etc)
        // Store the symbol name of the exchange (the mapping is getting complicated)
        if (CurrentVersion > version.Version && version.Version == 53)
        {
            using var transaction = database.BeginTransaction();
            // Store the symbol name of the exchange (the mapping is getting quite complicated), give it a default
            try { database.Connection.Execute("alter table symbol add ExchangeName TEXT NULL", transaction); } catch { } // ignore
            try { database.Connection.Execute("update symbol set ExchangeName=Name", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }

        //***********************************************************
        // 22-10-2025
        // Fix migration problems with the exchanges.
        if (CurrentVersion > version.Version && version.Version == 54)
        {
            using var transaction = database.BeginTransaction();

            // Has an accountid field whichs was not properly removed before (insert errors)
            try { database.Connection.Execute("drop table [Zone]", transaction); } catch { }

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 31-10-2025 Added SignalStatus
        if (CurrentVersion > version.Version && version.Version == 55)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal add column SignalStatus TEXT null", transaction);
            database.Connection.Execute("alter table Position add column SignalStatus TEXT null", transaction);
            database.Connection.Execute("update Signal set SignalStatus=0", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 18-02-2026 Changed Zone.OpenTime and CloseTime to CandleTime
        if (CurrentVersion > version.Version && version.Version == 56)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table signal drop column [EventTime]", transaction);
            database.Connection.Execute("drop table [zone]", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 03-04-2026 Added Bitvavo (experiment) and Alpaca
        // There are no field changes, only version number for UpdateExchanges
        if (CurrentVersion > version.Version && version.Version == 57)
        {
            using var transaction = database.BeginTransaction();

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }



        //***********************************************************
        // 30-05-2026 Changed position
        if (CurrentVersion > version.Version && version.Version == 58)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Position drop column Data", transaction);
            database.Connection.Execute("alter table Position add column EventText TEXT null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 05-06-2026 (Emulator preperation)
        // - Signal.Backtest (+ adjust SQL for loading signals)
        if (CurrentVersion > version.Version && version.Version == 59)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal drop column Backtest", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 05-06-2026 Emulator run-metadata
        // - Signal.EmulatorRunId  (NULL for live signals, FK to EmulatorRun)
        // - Position.EmulatorRunId (idem for positions)
        // The EmulatorRun table itself is created by Database.CreateTables() before
        // Migration.Execute runs, so it always exists when the FK targets resolve.
        // Same migration applies to the live DB; columns just stay NULL there and
        // the EmulatorRun table remains empty, invisible to the live workflow.
        if (CurrentVersion > version.Version && version.Version == 60)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal add EmulatorRunId Integer null REFERENCES EmulatorRun(Id)", transaction);
            database.Connection.Execute("alter table Position add EmulatorRunId Integer null REFERENCES EmulatorRun(Id)", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 07-06-2026 Emulator run-metadata: snapshot of the scanner settings.json
        // - EmulatorRun.SettingsJson (full GlobalData.Settings at run start, so the exact
        //   configuration that produced a run can be inspected/restored later)
        // The EmulatorRun table is created by Database.CreateTables() before Migration.Execute,
        // so it always exists here. On the live DB the table stays empty; the column just rides
        // along unused.
        if (CurrentVersion > version.Version && version.Version == 61)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table EmulatorRun add SettingsJson Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 07-06-2026 Emulator run outcome columns: replay period + position breakdown + profit
        // - EmulatorRun.FromDate / ToDate     (the replay window, so the grid can show the period)
        // - EmulatorRun.PositionsOpen/Won/Lost (outcome split, filled at run end)
        // - EmulatorRun.Profit                (summed realised result of the run)
        // Same migration on the live DB; the columns just ride along unused there.
        if (CurrentVersion > version.Version && version.Version == 62)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table EmulatorRun add FromDate Text null", transaction);
            database.Connection.Execute("alter table EmulatorRun add ToDate Text null", transaction);
            database.Connection.Execute("alter table EmulatorRun add PositionsOpen Integer not null default 0", transaction);
            database.Connection.Execute("alter table EmulatorRun add PositionsWon Integer not null default 0", transaction);
            database.Connection.Execute("alter table EmulatorRun add PositionsLost Integer not null default 0", transaction);
            database.Connection.Execute("alter table EmulatorRun add Profit Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 11-06-2026 Emulator run invested column: total invested capital of the closed positions,
        // so the Results grid can show the total return as a percentage of the investment.
        // Same migration on the live DB; the column just rides along unused there.
        if (CurrentVersion > version.Version && version.Version == 63)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table EmulatorRun add Invested Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 12-06-2026 Persist the per-signal / per-position SL and TP levels (previously [Computed],
        // i.e. in-memory only). Columns added to both Signal and Position so the strategy-anchored
        // SL/TP survive an app restart instead of falling back to the default percentage TP.
        // 12-06-2026 Store the per-signal / per-position stop-loss as a distance percentage instead of
        // an absolute price (SlPrice, added in v65, is now a legacy/unused column). A percentage is
        // reference-independent: it maps straight onto Altrady and works for market orders.
        // 12-06-2026 Store the per-signal / per-position take-profit as a distance percentage too
        // (TpPrice, added in v65, is now a legacy/unused column). Same rationale as SlPercentage.
        if (CurrentVersion > version.Version && version.Version == 64)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table Signal add TpPercentage Text null", transaction);
            database.Connection.Execute("alter table Signal add SlPercentage Text null", transaction);

            database.Connection.Execute("alter table Position add TpPercentage Text null", transaction);
            database.Connection.Execute("alter table Position add SlPercentage Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 13-06-2026 Emulator run Label column: copied from the run config at run start so the Results
        // grid can show it directly, instead of deserializing ConfigJson per row (that per-row JSON
        // parse was what made the Results tab take ~10s to open). Same column on the live DB, unused.
        if (CurrentVersion > version.Version && version.Version == 65)
        {
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("alter table EmulatorRun add Label Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 13-06-2026 Per-run zones
        // - Zone.EmulatorRunId (NULL for live zones, FK to EmulatorRun)
        // Until now zones were shared across runs and wiped per run start to avoid look-ahead
        // contamination. Tagging each zone with its run lets ZoneDlz load only one run's zones, so
        // runs stay isolated/reproducible AND a finished run's zones survive for the chart to show.
        // The EmulatorRun table is created by Database.CreateTables() before Migration.Execute runs,
        // so the FK target always exists; on the live DB the column just stays NULL.
        if (CurrentVersion > version.Version && version.Version == 66)
        {
            using var transaction = database.BeginTransaction();
            database.Connection.Execute("alter table Zone add EmulatorRunId Integer null REFERENCES EmulatorRun(Id)", transaction);
            database.Connection.Execute("CREATE INDEX IdxPositionEmulatorRunId ON Position(EmulatorRunId)", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 13-06-2026 Add signal id to releate position with signal
        if (CurrentVersion > version.Version && version.Version == 67)
        {
            using var transaction = database.BeginTransaction();
            // not needed any more
            //database.Connection.Execute("alter table Position add SignalId Integer null REFERENCES Signal(Id)", transaction);
            //database.Connection.Execute("CREATE INDEX IdxPositionSignalId ON Position(SignalId)", transaction);
            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 20-06-2026 Remove signal id again
        if (CurrentVersion > version.Version && version.Version == 68)
        {
            using var transaction = database.BeginTransaction();
            // not needed any more
            //try { database.Connection.Execute("drop INDEX IdxPositionSignalId", transaction); } catch { } // ignore
            //try { database.Connection.Execute("alter table Position drop column SignalId", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 21-06-2026 Emulator run timeout column: positions whose entry order never filled (status
        // Timeout) are now counted separately instead of falling into PositionsLost, since they never
        // became a real trade. Same migration on the live DB; the column rides along unused there.
        if (CurrentVersion > version.Version && version.Version == 69)
        {
            using var transaction = database.BeginTransaction();
            database.Connection.Execute("alter table EmulatorRun add PositionsTimeout Integer not null default 0", transaction);

            // Its back, but without a fk
            try { database.Connection.Execute("alter table Position add SignalId Integer null", transaction); } catch { } // ignore
            try { database.Connection.Execute("CREATE INDEX IdxPositionSignalId ON Position(SignalId)", transaction); } catch { } // ignore

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        // 23-06-2026 Fixed TP/DCA grid anchor price (TpGridAnchorPrice), separate from
        // BreakEvenPrice: the fixed-percentage TP/DCA grid needs an anchor that shifts when a DCA
        // fills (averaging the cost basis) but stays put when a sibling TP fills (BreakEvenPrice
        // banks the realized profit into Returned/Quantity, which moved every still-open TP level).
        if (CurrentVersion > version.Version && version.Version == 70)
        {
            using var transaction = database.BeginTransaction();
            database.Connection.Execute("alter table Position add TpGridAnchorPrice Text null", transaction);

            // update version
            version.Version += 1;
            database.Connection.Update(version, transaction);
            transaction.Commit();
        }


        //***********************************************************
        //
        //
        //
        //***********************************************************
        // 30-05-2026 Changed position
        // There are no field changes, only version number for UpdateExchanges


        // Apply the exchange defaults with each update
        if (updateExchanges)
            UpdateExchanges(database);
    }
}


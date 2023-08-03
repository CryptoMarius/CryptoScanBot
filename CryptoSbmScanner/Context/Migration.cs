using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Context;

public class Migration
{
    public static void Execute(CryptoDatabase database, int CurrentVersion)
    {
        CryptoVersion version = database.Connection.GetAll<CryptoVersion>().FirstOrDefault();

        if (CurrentVersion > version.Version)
        {

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

            if (CurrentVersion > version.Version && version.Version == 2)
            {
                using var transaction = database.BeginTransaction();
                // do the updates..

                //alter table PositionPart drop column BuyAmount
                //alter table PositionPart rename column BuyPrice TO SignalPrice;
                //alter table PositionPart drop column Sellprice

                database.Connection.Execute("alter table PositionPart drop column BuyAmount", transaction);
                database.Connection.Execute("alter table PositionPart drop column Sellprice", transaction);
                database.Connection.Execute("alter table PositionPart rename column BuyPrice TO SignalPrice", transaction);

                // De accounttype voor Futures is niet goed ingevuld, deze staan allemaal op spot (verkeerde initialisatie)
                database.Connection.Execute("update TradeAccount set AccountType=1 where name like '%Futures%'", transaction);

                // update version
                //version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }

        }
    }


}

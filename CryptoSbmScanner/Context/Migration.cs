using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Context;

public class Migration
{
    public static void Execute(CryptoDatabase database, int CurrentVersion)
    {
        CryptoVersion version = database.Connection.GetAll<CryptoVersion>().FirstOrDefault();

        if (CurrentVersion > version.Version)
        {

            if (version.Version == 1)
            {
                using var transaction = database.BeginTransaction();

                // TODO: Voor de zekerheid!
                // Want de exchangeId's in de TradeAccount zijn initieel verkeerd ingesteld
                //update TradeAccount set ExchangeId = 1 where name like 'Binance%';
                //update TradeAccount set ExchangeId = 2 where name like 'Bybit Spot%';
                //update TradeAccount set ExchangeId = 3 where name like 'Bybit Futures%';
                //update TradeAccount set ExchangeId = 3 where name like 'Kucoin%';


                // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
                // https://bybit-exchange.github.io/docs/v5/market/history-fund-rate
                // Wat het inhoud weet ik niet (toegevoegde waarde, voor trading waarschijnlijk wel?)
                // alter table symbol add FundingRate ? null.
                // alter table symbol add FundingInterval ? null.

                // Forceer dat de exchange info (en funding rates) worden opgehaald
                // update Exchange set LastTimeFetched = null;
                // het moet per munt opgevraagd worden helaas, jammer

                // update version
                //version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }

            if (version.Version == 2)
            {
                using var transaction = database.BeginTransaction();
                // do the updates..

                // update version
                //version.Version += 1;
                database.Connection.Update(version, transaction);
                transaction.Commit();
            }

        }
    }


}

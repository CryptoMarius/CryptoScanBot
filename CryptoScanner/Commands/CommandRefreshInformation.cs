using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;

namespace CryptoScanner.Commands;

public class CommandRefreshInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        Task.Run(async () =>
        {
            var api = GlobalData.ActiveExchange!.GetApiInstance();
            await api.Symbol.GetSymbolsAsync(); // niet wachten tot deze klaar is
            // Rebuild the per-quote index from the refreshed symbol table, the same thing the hourly
            // refresh in ScannerSession does. The subscription synchronisation below and the barometer
            // read that index while the candle fetch reads the symbol table itself, so without this a
            // coin listed after startup is fetched but never subscribed - Okx Perpetual, night of
            // 02/03-09-2026, two new listings and 49 missing minutes each.
            ThreadLoadData.IndexQuoteDataSymbols(GlobalData.ActiveExchange!, notifyUserInterface: false);
            CandleBase.UpdateSymbolPrecision(); // a new tick size has to reach the candle decimals
            CandleBase.UpdateVolumeDecisions(); // een antwoord voor deze hele ronde
            // The symbols and their volumes were just replaced in place. The grid caches the formatted
            // volume per row, so without this the column keeps the numbers it was built with - the same
            // reason the hourly refresh in ScannerSession sends it.
            GlobalData.SendMvvmMessage(new SymbolsHaveChangedMessage());
            if (ExchangeBase.KLineTicker != null)
                await ExchangeBase.KLineTicker!.CheckSubscriptions(); // herstarten van ticker indien errors
            //if (ExchangeBase.PriceTicker != null)
            //    await ExchangeBase.PriceTicker!.CheckSubscriptions(); // herstarten van ticker indien errors
            //if (ExchangeBase.UserTicker != null)
            //    await ExchangeBase.UserTicker!.CheckSubscriptions(); // herstarten van ticker indien errors
            if (ExchangeBase.KLineTicker != null)
                await ExchangeBase.KLineTicker.SynchronizeSymbolsAsync(); // symbols die erbij kwamen of afvielen
            await api.Candle.GetCandlesForAllSymbolsAndIntervalsAsync(); // niet wachten tot deze klaar is
        });
    }
}

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;

namespace CryptoScanner.Commands;

public class CommandRefreshInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        Task.Run(async () =>
        {
            var api = GlobalData.ActiveExchange!.GetApiInstance();
            await api.Symbol.GetSymbolsAsync(); // niet wachten tot deze klaar is
            CandleBase.UpdateVolumeDecisions(); // een antwoord voor deze hele ronde
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

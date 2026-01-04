using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchExchange : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"CommandLaunchExchange {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Open {dto.symbol.Name} in exchange");
            CommandHelper.ActivateTradingApp(CryptoTradingApp.ExchangeUrl, dto.symbol, dto.interval, CryptoExternalUrlType.External);
        }
    }
}

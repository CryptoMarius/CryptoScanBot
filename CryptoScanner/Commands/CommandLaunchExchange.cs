using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchExchange : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null)
        {
            // A grid row without an interval (double click action "Open exchange") gets the default
            dto.interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];
            System.Diagnostics.Debug.WriteLine($"CommandLaunchExchange {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Open {dto.symbol.Name} in exchange");
            CommandHelper.ActivateTradingApp(CryptoTradingApp.ExchangeUrl, dto.symbol, dto.interval, CryptoExternalUrlType.External);
        }
    }
}

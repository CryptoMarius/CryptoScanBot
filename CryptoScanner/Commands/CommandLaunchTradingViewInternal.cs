using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingViewInternal : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null)
        {
            dto.interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];
            System.Diagnostics.Debug.WriteLine($"CommandLaunchTradingViewInternal {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal tradingview browser");
            CommandHelper.ActivateTradingApp(CryptoTradingApp.TradingView, dto.symbol, dto.interval, CryptoExternalUrlType.Internal);
        }
    }
}
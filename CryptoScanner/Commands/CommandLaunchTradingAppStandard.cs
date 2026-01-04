using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingAppStandard : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"CommandLaunchTradingAppStandard {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in trading program via standard browser");

            // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
            CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
            GlobalData.LoadLinkSettings(); // refresh links
            CommandHelper.ActivateTradingApp(GlobalData.Settings.General.TradingApp, dto.symbol, dto.interval, tradingAppInternExtern);
        }
    }
}

using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchExchange : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"Open {dto.symbol.Name} in exchange");

            // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
            CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
            GlobalData.LoadLinkSettings(); // refresh links

            this.ActivateTradingApp(GlobalData.Settings.General.TradingApp, dto.symbol, dto.interval, tradingAppInternExtern);
        }
    }
}

using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingViewInternal : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            if (dto.symbol != null && dto.interval != null)
            {
                System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal tradingview browser");

                // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
                CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.Internal;
                if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                    tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
                GlobalData.LoadLinkSettings(); // refresh links

                this.ActivateTradingApp(CryptoTradingApp.TradingView, dto.symbol, dto.interval, tradingAppInternExtern);
            }
        }
    }
}

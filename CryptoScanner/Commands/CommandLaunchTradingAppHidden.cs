using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingAppHidden : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"CommandLaunchTradingAppHidden {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in trading program via hidden browser");

            //// Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
            //CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
            //if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            //    tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
            //GlobalData.LoadLinkSettings(); // refresh links

            //this.ActivateTradingApp(GlobalData.Settings.General.TradingApp, dto.symbol, dto.interval, CryptoExternalUrlType.External);

            GlobalData.LoadLinkSettings(); // refresh links
            (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, false, dto.symbol, dto.interval);
            if (Url != "")
            {
                GlobalData.AddTextToLogTab($"Linktools activate {Url}");
                App.HiddenBrowser.Navigate(Url);
            }
        }
    }
}

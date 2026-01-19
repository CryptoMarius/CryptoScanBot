using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingAppHidden : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null)
        {
            dto.interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];
            System.Diagnostics.Debug.WriteLine($"CommandLaunchTradingAppHidden {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in trading program via hidden browser");

            //this.ActivateTradingApp(GlobalData.Settings.General.TradingApp, dto.symbol, dto.interval, CryptoExternalUrlType.External);

            GlobalData.LoadWebLinkSettings(); // refresh links
            (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, false, dto.symbol, dto.interval);
            if (Url != "")
            {
                GlobalData.AddTextToLogTab($"Linktools activate {Url}");
                App.EventOpenHiddenBrowser.Navigate(Url);
            }
        }
    }
}

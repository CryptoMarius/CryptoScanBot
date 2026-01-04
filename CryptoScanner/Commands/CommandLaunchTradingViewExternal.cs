using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingViewExternal : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"CommandLaunchTradingViewExternal {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in external tradingview browser");
            CommandHelper.ActivateTradingApp(CryptoTradingApp.TradingView, dto.symbol, dto.interval, CryptoExternalUrlType.External);

            //GlobalData.LoadLinkSettings(); // refresh links
            //(string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(CryptoTradingApp.TradingView, false, dto.symbol, dto.interval);
            //if (Url != "")
            //{
            //    GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            //    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true });
            //}
        }

    }
}

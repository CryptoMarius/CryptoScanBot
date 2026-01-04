using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using CryptoScanner.Helpers;

namespace CryptoScanner.Commands;

public class CommandLaunchTradingViewInternal : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"CommandLaunchTradingViewInternal {dto.symbol.Name}");
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal tradingview browser");
            CommandHelper.ActivateTradingApp(CryptoTradingApp.TradingView, dto.symbol, dto.interval, CryptoExternalUrlType.Internal);

            //GlobalData.LoadLinkSettings(); // refresh links
            //(string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(CryptoTradingApp.TradingView, false, dto.symbol, dto.interval);
            //if (Url != "")
            //{
            //    GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            //    App.OpenInInternalBrowser(dto.datagrid!, Url);

            //    // Change tab?
            //}

        }
    }
}
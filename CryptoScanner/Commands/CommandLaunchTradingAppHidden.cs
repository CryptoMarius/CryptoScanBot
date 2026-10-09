using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Helpers;

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

            GlobalData.LoadWebLinkConfiguration(); // refresh links
            (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, false, dto.symbol, dto.interval);
            if (Url == "")
            {
                // Was a silent nothing, the same blind spot ExternalLinkHelper.ActivateTradingApp had
                GlobalData.AddErrorToLogTab($"Linktools: no URL configured for tradingApp={GlobalData.Settings.General.TradingApp} " +
                    $"exchange={GlobalData.Settings.General.ActivateExchangeName} symbol={dto.symbol.Name}");
            }
            if (Url != "")
            {
                GlobalData.AddTextToLogTab($"Linktools activate {GlobalData.Settings.General.TradingApp} {dto.symbol.Name} " +
                    $"{dto.interval.Name} via the hidden browser: {Url}");
                // Through the wrapper instead of straight at the service: it says so in the log when
                // the hidden browser does not exist yet, where this threw a NullReferenceException.
                // Since 06-10-2026 by way of the shared helper: the Altrady deep link goes over the
                // local WebSocket first and only falls back to the hidden browser (App.OpenInHiddenBrowser,
                // wired through CommandHelper.OpenHiddenBrowser at startup).
                ExternalLinkHelper.OpenHidden(Url);
            }
        }
    }
}

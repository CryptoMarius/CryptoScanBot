using Avalonia.Controls;

using CryptoScanner.Config.Views;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;

namespace CryptoScanner.Commands;

public class CommandConfiguration : CommandBase
{

    private static void GetReloadRelatedSettings(out string activeQuoteData)
    {
        activeQuoteData = "";
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                activeQuoteData += "," + quoteData.Name;
        }
    }

    public override async void Execute(object? parameter)
    {
        if (parameter is not Window parentWindow)
            return;

        System.Diagnostics.Debug.WriteLine($"Settings");
        var scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession service not found");

        // Save some old stuff for reloading stuff
        var previousExchange = GlobalData.ActiveExchange;
        GetReloadRelatedSettings(out string previousActiveQuotes);
        string previousExchangeName = GlobalData.Settings.General.ExchangeName;

        try
        {
            var dialog = new ConfigurationWindow
            {
                CanResize = true,
                Title = "Configuration form",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // Hacky, needs work...
            var result = await dialog.ShowDialog<bool?>(parentWindow!);
            if (result != true)
                return;

            GlobalData.SaveConfiguration();
            //GlobalData.SaveUserSettings(); // custom colors (not sure?)

            // Don't save exchange immediately, lots of data still in memory etc
            if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? newActiveExchange))
                return;
            if (newActiveExchange == null)
                return;


            // Did we choose another exchange (reload)
            bool exchangeChanged = previousExchangeName != GlobalData.Settings.General.ExchangeName;
            if (exchangeChanged)
                GlobalData.AddTextToLogTab("Exchange was changed (reload)!");

            // Did we changes quotes (reload)
            GetReloadRelatedSettings(out string currentActiveQuotes);
            bool quoteChanged = previousActiveQuotes != currentActiveQuotes;
            if (quoteChanged)
                GlobalData.AddTextToLogTab("Quotes have changed (reload)!");

            if (exchangeChanged || quoteChanged)
            {
                GlobalData.AddTextToLogTab("");

                //AsyncContext.Run(scannerSession.StopAsync);
                await scannerSession.StopAsync();

                // Stop the current exchange
                if (previousExchange != null)
                {
                    if (exchangeChanged)
                    {
                        previousExchange?.Clear();
                        previousExchange?.Data.Clear();
                        // TODO: Delete symbols, assets, orders, trades, positions, parts, steps from database!
                    }

                    // Clear candle data
                    if (quoteChanged)
                    {
                        foreach (var symbol in previousExchange!.SymbolListId.Values)
                        {
                            if (!symbol.QuoteData.FetchCandles || symbol.Status == 0)
                            {
                                symbol.ClearCandles();
                                //GlobalData.AddTextToLogTab($"Cleared candles for {symbol.Name}");
                            }
                        }
                    }
                }

                // Standaard timers e.d.
                await scannerSession.ApplyConfigurationAsync();

                // Schedule a reload of data
                scannerSession.ScheduleRefresh();
            }
            else await scannerSession.ApplyConfigurationAsync();

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }

    }
}
